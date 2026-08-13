import { admin, cors, getAuthenticatedUser, json } from "../_shared/operator.ts";

const sensitiveReason = (command: string) => {
  const lower = command.toLowerCase();
  const hints = [
    "overwrite", "replace the file", "delete", "remove", "send email", "send message",
    "upload", "submit", "post", "purchase", "pay", "transfer money", "change password",
    "reset password", "account setting",
  ];
  return hints.find((hint) => lower.includes(hint)) ?? null;
};

Deno.serve(async (req) => {
  const preflight = cors(req);
  if (preflight) return preflight;

  try {
    const user = await getAuthenticatedUser(req);
    if (!user) return json({ error: "Authentication required." }, 401);

    const body = await req.json();
    const action = String(body.action ?? "");
    const supabase = admin();

    if (action === "create") {
      const deviceIds = Array.isArray(body.device_ids) ? body.device_ids.map(String) : [];
      const commandText = String(body.command_text ?? "").trim();
      if (!commandText || !deviceIds.length) return json({ error: "Select at least one device and enter a command." }, 400);

      const { data: devices, error: deviceError } = await supabase
        .from("operator_remote_devices")
        .select("id")
        .eq("owner_id", user.id)
        .in("id", deviceIds)
        .is("revoked_at", null);

      if (deviceError || !devices || devices.length !== deviceIds.length)
        return json({ error: "One or more devices are invalid." }, 403);

      const reason = sensitiveReason(commandText);
      const rows = deviceIds.map((deviceId) => ({
        owner_id: user.id,
        device_id: deviceId,
        command_text: commandText,
        status: reason ? "awaiting_approval" : "queued",
        control_state: "run",
        approval_state: reason ? "pending" : "approved",
        approval_reason: reason ? `Remote command contains a sensitive action marker: ${reason}` : null,
        capture_screenshot: body.capture_screenshot !== false,
        collect_result_files: body.collect_result_files !== false,
      }));

      const { data: commands, error } = await supabase.from("operator_remote_commands").insert(rows).select("*");
      if (error) return json({ error: error.message }, 400);
      return json({ commands });
    }

    if (action === "control") {
      const commandId = String(body.command_id ?? "");
      const controlState = String(body.control_state ?? "");
      if (!["run", "pause", "cancel_requested"].includes(controlState)) return json({ error: "Invalid control state." }, 400);

      const patch: Record<string, unknown> = { control_state: controlState };
      if (controlState === "pause") patch.status = "paused";
      else if (controlState === "run") patch.status = "running";

      const { error } = await supabase.from("operator_remote_commands").update(patch).eq("id", commandId).eq("owner_id", user.id);
      return error ? json({ error: error.message }, 400) : json({ ok: true });
    }

    if (action === "approve") {
      const commandId = String(body.command_id ?? "");
      const approved = body.approved === true;
      const { error } = await supabase.from("operator_remote_commands").update({
        approval_state: approved ? "approved" : "rejected",
        status: approved ? "queued" : "rejected",
      }).eq("id", commandId).eq("owner_id", user.id).eq("approval_state", "pending");
      return error ? json({ error: error.message }, 400) : json({ ok: true });
    }

    if (action === "register_push") {
      const token = String(body.token ?? "").trim();
      if (!token) return json({ error: "Push token cannot be empty." }, 400);

      const { error } = await supabase.from("operator_remote_push_tokens").upsert({
        owner_id: user.id,
        token,
        platform: String(body.platform ?? ""),
        updated_at: new Date().toISOString(),
      }, { onConflict: "owner_id,token" });

      return error ? json({ error: error.message }, 400) : json({ ok: true });
    }

    if (action === "revoke_device") {
      const { error } = await supabase.from("operator_remote_devices").update({ revoked_at: new Date().toISOString() })
        .eq("id", String(body.device_id ?? "")).eq("owner_id", user.id);
      return error ? json({ error: error.message }, 400) : json({ ok: true });
    }

    return json({ error: "Unknown command action." }, 400);
  } catch (error) {
    return json({ error: String(error?.message ?? error) }, 500);
  }
});
