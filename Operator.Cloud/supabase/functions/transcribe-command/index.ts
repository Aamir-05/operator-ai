import { cors, getAuthenticatedUser, json } from "../_shared/operator.ts";

Deno.serve(async (req) => {
  const preflight = cors(req);
  if (preflight) return preflight;

  try {
    const user = await getAuthenticatedUser(req);
    if (!user) return json({ error: "Authentication required." }, 401);

    const openAiKey = Deno.env.get("OPENAI_API_KEY");
    if (!openAiKey) return json({ error: "Voice transcription is not configured. Set OPENAI_API_KEY in Supabase Edge Function secrets." }, 503);

    const incoming = await req.formData();
    const file = incoming.get("file");
    if (!(file instanceof File)) return json({ error: "Audio file is required." }, 400);
    if (file.size > 20 * 1024 * 1024) return json({ error: "Audio file exceeds 20 MB." }, 413);

    const form = new FormData();
    form.append("file", file, file.name || "command.m4a");
    form.append("model", "gpt-4o-mini-transcribe");

    const response = await fetch("https://api.openai.com/v1/audio/transcriptions", {
      method: "POST",
      headers: { Authorization: `Bearer ${openAiKey}` },
      body: form,
    });

    const text = await response.text();
    if (!response.ok) return json({ error: `OpenAI transcription failed: ${text}` }, 502);

    const parsed = JSON.parse(text);
    return json({ text: String(parsed.text ?? "") });
  } catch (error) {
    return json({ error: String(error?.message ?? error) }, 500);
  }
});
