import { createClient } from "npm:@supabase/supabase-js@2";

export const json = (data: unknown, status = 200) =>
  new Response(JSON.stringify(data), {
    status,
    headers: {
      "Content-Type": "application/json",
      "Access-Control-Allow-Origin": "*",
      "Access-Control-Allow-Headers": "authorization, apikey, content-type, x-operator-device-id, x-operator-device-secret",
    },
  });

export const cors = (req: Request) => req.method === "OPTIONS"
  ? new Response("ok", { headers: {
      "Access-Control-Allow-Origin": "*",
      "Access-Control-Allow-Headers": "authorization, apikey, content-type, x-operator-device-id, x-operator-device-secret",
    }})
  : null;

export const admin = () => createClient(
  Deno.env.get("SUPABASE_URL")!,
  Deno.env.get("SUPABASE_SERVICE_ROLE_KEY")!,
  { auth: { persistSession: false, autoRefreshToken: false } },
);

export const sha256 = async (value: string) => {
  const digest = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(value));
  return Array.from(new Uint8Array(digest)).map((x) => x.toString(16).padStart(2, "0")).join("");
};

export const randomToken = (bytes = 32) => {
  const value = new Uint8Array(bytes);
  crypto.getRandomValues(value);
  return Array.from(value).map((x) => x.toString(16).padStart(2, "0")).join("");
};

export const getAuthenticatedUser = async (req: Request) => {
  const token = (req.headers.get("authorization") ?? "").replace(/^Bearer\s+/i, "").trim();
  if (!token) return null;
  const { data, error } = await admin().auth.getUser(token);
  return error ? null : data.user ?? null;
};

export const authenticateDevice = async (req: Request) => {
  const deviceId = req.headers.get("x-operator-device-id") ?? "";
  const deviceSecret = req.headers.get("x-operator-device-secret") ?? "";
  if (!deviceId || !deviceSecret) return null;

  const supabase = admin();
  const { data: device, error } = await supabase
    .from("operator_remote_devices")
    .select("*")
    .eq("id", deviceId)
    .is("revoked_at", null)
    .maybeSingle();

  if (error || !device || device.secret_hash !== await sha256(deviceSecret)) return null;

  await supabase.from("operator_remote_devices").update({
    last_seen_at: new Date().toISOString(),
    app_version: "1.0.0",
  }).eq("id", device.id);

  return device;
};

export const notifyOwner = async (ownerId: string, title: string, body: string, data: Record<string, unknown> = {}) => {
  const { data: tokens } = await admin().from("operator_remote_push_tokens").select("token").eq("owner_id", ownerId);
  if (!tokens?.length) return;

  try {
    await fetch("https://exp.host/--/api/v2/push/send", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(tokens.map((row) => ({ to: row.token, sound: "default", title, body, data }))),
    });
  } catch { }
};
