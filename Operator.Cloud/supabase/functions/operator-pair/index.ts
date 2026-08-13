import { admin, cors, getAuthenticatedUser, json, randomToken, sha256 } from "../_shared/operator.ts";

Deno.serve(async (req) => {
  const preflight = cors(req);
  if (preflight) return preflight;

  try {
    const body = await req.json();
    const action = String(body.action ?? "");
    const supabase = admin();

    if (action === "start") {
      const code = String(crypto.getRandomValues(new Uint32Array(1))[0] % 1000000).padStart(6, "0");
      const pollToken = randomToken(32);
      const sessionId = crypto.randomUUID();
      const expiresAt = new Date(Date.now() + 10 * 60 * 1000);

      const { error } = await supabase.from("operator_pairing_sessions").insert({
        id: sessionId,
        code,
        poll_token_hash: await sha256(pollToken),
        device_name: String(body.device_name ?? "Windows PC").slice(0, 120),
        expires_at: expiresAt.toISOString(),
      });

      if (error) return json({ error: error.message }, 400);

      return json({
        session_id: sessionId,
        code,
        poll_token: pollToken,
        pair_uri: `operatorai://pair?session=${encodeURIComponent(sessionId)}&code=${encodeURIComponent(code)}`,
        expires_at: expiresAt.toISOString(),
      });
    }

    if (action === "claim") {
      const user = await getAuthenticatedUser(req);
      if (!user) return json({ error: "Authentication required." }, 401);

      const sessionId = String(body.session_id ?? "");
      const code = String(body.code ?? "");
      const { data: session, error } = await supabase
        .from("operator_pairing_sessions")
        .select("*")
        .eq("id", sessionId)
        .maybeSingle();

      if (error || !session) return json({ error: "Pairing session not found." }, 404);
      if (session.consumed_at || session.claimed_by || new Date(session.expires_at).getTime() < Date.now() || session.code !== code)
        return json({ error: "Pairing session is invalid or expired." }, 400);

      const deviceSecret = randomToken(48);
      const { data: device, error: deviceError } = await supabase
        .from("operator_remote_devices")
        .insert({
          owner_id: user.id,
          name: session.device_name,
          secret_hash: await sha256(deviceSecret),
          last_seen_at: new Date().toISOString(),
        })
        .select("id,name")
        .single();

      if (deviceError || !device) return json({ error: deviceError?.message ?? "Device creation failed." }, 400);

      const { error: updateError } = await supabase
        .from("operator_pairing_sessions")
        .update({
          claimed_by: user.id,
          device_id: device.id,
          device_secret_once: deviceSecret,
        })
        .eq("id", session.id)
        .is("claimed_by", null);

      if (updateError) return json({ error: updateError.message }, 400);

      return json({ status: "paired", device_id: device.id, device_name: device.name });
    }

    if (action === "poll") {
      const sessionId = String(body.session_id ?? "");
      const pollToken = String(body.poll_token ?? "");
      const { data: session } = await supabase
        .from("operator_pairing_sessions")
        .select("*")
        .eq("id", sessionId)
        .maybeSingle();

      if (!session || new Date(session.expires_at).getTime() < Date.now()) return json({ status: "expired" });
      if (session.poll_token_hash !== await sha256(pollToken)) return json({ error: "Invalid pairing poll token." }, 403);

      if (session.claimed_by && session.device_id && session.device_secret_once) {
        const secret = session.device_secret_once;
        await supabase.from("operator_pairing_sessions").update({
          device_secret_once: null,
          consumed_at: new Date().toISOString(),
        }).eq("id", session.id);

        return json({
          status: "paired",
          device_id: session.device_id,
          device_secret: secret,
          owner_display: "Authenticated Operator AI account",
        });
      }

      if (session.consumed_at && session.device_id)
        return json({ status: "paired", device_id: session.device_id, device_secret: "" });

      return json({ status: "waiting" });
    }

    return json({ error: "Unknown pairing action." }, 400);
  } catch (error) {
    return json({ error: String(error?.message ?? error) }, 500);
  }
});
