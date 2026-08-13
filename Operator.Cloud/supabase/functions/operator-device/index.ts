import { admin, authenticateDevice, cors, json, notifyOwner } from "../_shared/operator.ts";

Deno.serve(async (req) => {
  const preflight = cors(req);
  if (preflight) return preflight;

  try {
    const device = await authenticateDevice(req);
    if (!device) return json({ error: "Invalid device credential." }, 401);

    const body = await req.json();
    const action = String(body.action ?? "");
    const supabase = admin();

    if (action === "poll") {
      const { data: command, error } = await supabase
        .from("operator_remote_commands")
        .select("*")
        .eq("device_id", device.id)
        .eq("status", "queued")
        .eq("approval_state", "approved")
        .order("created_at", { ascending: true })
        .limit(1)
        .maybeSingle();

      if (error) return json({ error: error.message }, 400);
      if (!command) return json({ device_status: "online", command: null });

      const { data: claimed, error: claimError } = await supabase
        .from("operator_remote_commands")
        .update({ status: "running", started_at: new Date().toISOString(), progress: 1 })
        .eq("id", command.id)
        .eq("device_id", device.id)
        .eq("status", "queued")
        .select("*")
        .maybeSingle();

      if (claimError) return json({ error: claimError.message }, 400);
      if (!claimed) return json({ device_status: "online", command: null });

      return json({
        device_status: "online",
        command: {
          id: claimed.id,
          command_text: claimed.command_text,
          status: claimed.status,
          control_state: claimed.control_state,
          approval_state: claimed.approval_state,
          capture_screenshot: claimed.capture_screenshot,
          collect_result_files: claimed.collect_result_files,
        },
      });
    }

    if (action === "control") {
      const { data: command, error } = await supabase
        .from("operator_remote_commands")
        .select("status,control_state,approval_state")
        .eq("id", String(body.command_id ?? ""))
        .eq("device_id", device.id)
        .maybeSingle();

      if (error || !command) return json({ error: "Command not found." }, 404);
      return json(command);
    }

    if (action === "report") {
      const commandId = String(body.command_id ?? "");
      const status = String(body.status ?? "running");
      const progress = Math.max(0, Math.min(100, Number(body.progress ?? 0)));
      const result = body.result == null ? null : String(body.result);
      const logLine = body.log_line == null ? null : String(body.log_line);

      const patch: Record<string, unknown> = { status, progress };
      if (status === "running") patch.started_at = new Date().toISOString();
      if (["completed", "failed", "cancelled", "rejected"].includes(status)) patch.finished_at = new Date().toISOString();
      if (result != null) patch.result_text = result.slice(0, 100000);

      const { data: command, error } = await supabase
        .from("operator_remote_commands")
        .update(patch)
        .eq("id", commandId)
        .eq("device_id", device.id)
        .select("id,owner_id,command_text,status")
        .single();

      if (error || !command) return json({ error: error?.message ?? "Command update failed." }, 400);

      if (logLine) {
        await supabase.from("operator_remote_task_logs").insert({
          command_id: command.id,
          owner_id: command.owner_id,
          line: logLine.slice(0, 12000),
        });
      }

      if (["completed", "failed", "cancelled"].includes(status)) {
        await notifyOwner(command.owner_id, `Operator AI: ${status}`, command.command_text.slice(0, 120), {
          command_id: command.id,
          status,
        });
      }

      return json({ ok: true });
    }

    if (action === "artifact") {
      const commandId = String(body.command_id ?? "");
      const fileName = String(body.file_name ?? "artifact.bin").replace(/[^\w.\- ]+/g, "_").slice(0, 160);
      const kind = String(body.kind ?? "file").slice(0, 40);
      const mimeType = String(body.mime_type ?? "application/octet-stream");
      const contentBase64 = String(body.content_base64 ?? "");

      const { data: command } = await supabase
        .from("operator_remote_commands")
        .select("id,owner_id")
        .eq("id", commandId)
        .eq("device_id", device.id)
        .maybeSingle();

      if (!command) return json({ error: "Command not found." }, 404);

      const binary = Uint8Array.from(atob(contentBase64), (char) => char.charCodeAt(0));
      if (binary.byteLength > 15 * 1024 * 1024) return json({ error: "Artifact exceeds 15 MB." }, 413);

      const path = `${command.owner_id}/${command.id}/${crypto.randomUUID()}-${fileName}`;
      const { error: uploadError } = await supabase.storage.from("operator-remote-artifacts").upload(path, binary, {
        contentType: mimeType,
        upsert: false,
      });
      if (uploadError) return json({ error: uploadError.message }, 400);

      const { data: artifact, error: artifactError } = await supabase.from("operator_remote_artifacts").insert({
        command_id: command.id,
        owner_id: command.owner_id,
        kind,
        file_name: fileName,
        storage_path: path,
        mime_type: mimeType,
        size_bytes: binary.byteLength,
      }).select("id").single();

      if (artifactError) return json({ error: artifactError.message }, 400);
      return json({ ok: true, artifact_id: artifact.id });
    }

    return json({ error: "Unknown device action." }, 400);
  } catch (error) {
    return json({ error: String(error?.message ?? error) }, 500);
  }
});
