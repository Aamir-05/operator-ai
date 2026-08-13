import { Platform } from "react-native";
import { functionUrl, publishableKey, supabase } from "./supabase";

const authHeaders = async () => {
  const { data } = await supabase.auth.getSession();
  if (!data.session) throw new Error("Sign in first.");

  return {
    Authorization: `Bearer ${data.session.access_token}`,
    apikey: publishableKey,
    "Content-Type": "application/json",
  };
};

export const commandApi = async (body: Record<string, unknown>) => {
  const response = await fetch(functionUrl("operator-command"), {
    method: "POST",
    headers: await authHeaders(),
    body: JSON.stringify(body),
  });
  const data = await response.json();
  if (!response.ok) throw new Error(data.error ?? "Operator Cloud request failed.");
  return data;
};

export const claimPairing = async (sessionId: string, code: string) => {
  const response = await fetch(functionUrl("operator-pair"), {
    method: "POST",
    headers: await authHeaders(),
    body: JSON.stringify({ action: "claim", session_id: sessionId, code }),
  });
  const data = await response.json();
  if (!response.ok) throw new Error(data.error ?? "Pairing failed.");
  return data;
};

export const transcribeAudio = async (uri: string) => {
  const { data } = await supabase.auth.getSession();
  if (!data.session) throw new Error("Sign in first.");

  const fileResponse = await fetch(uri);
  const blob = await fileResponse.blob();
  const form = new FormData();
  form.append("file", blob as any, Platform.OS === "web" ? "command.webm" : "command.m4a");

  const response = await fetch(functionUrl("transcribe-command"), {
    method: "POST",
    headers: {
      Authorization: `Bearer ${data.session.access_token}`,
      apikey: publishableKey,
    },
    body: form,
  });

  const result = await response.json();
  if (!response.ok) throw new Error(result.error ?? "Voice transcription failed.");
  return String(result.text ?? "");
};
