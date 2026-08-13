import React, { useEffect, useState } from "react";
import {
  Alert,
  Linking,
  Platform,
  Pressable,
  RefreshControl,
  SafeAreaView,
  ScrollView,
  StyleSheet,
  Text,
  TextInput,
  View,
} from "react-native";
import {
  AudioModule,
  RecordingPresets,
  setAudioModeAsync,
  useAudioRecorder,
  useAudioRecorderState,
} from "expo-audio";
import { CameraView, useCameraPermissions } from "expo-camera";
import * as Notifications from "expo-notifications";
import Constants from "expo-constants";
import type { Session } from "@supabase/supabase-js";
import { claimPairing, commandApi, transcribeAudio } from "./src/lib/api";
import { supabase } from "./src/lib/supabase";

type Device = {
  id: string;
  name: string;
  last_seen_at: string | null;
  revoked_at: string | null;
};

type Command = {
  id: string;
  device_id: string;
  command_text: string;
  status: string;
  control_state: string;
  approval_state: string;
  approval_reason: string | null;
  progress: number;
  result_text: string | null;
  created_at: string;
};

type LogLine = {
  id: number;
  command_id: string;
  line: string;
  created_at: string;
};

type Artifact = {
  id: string;
  command_id: string;
  kind: string;
  file_name: string;
  storage_path: string;
  mime_type: string | null;
};

const online = (device: Device) =>
  !!device.last_seen_at &&
  !device.revoked_at &&
  Date.now() - new Date(device.last_seen_at).getTime() < 35_000;

const parsePairUri = (value: string) => {
  const url = new URL(value);
  if (url.protocol !== "operatorai:") throw new Error("Not an Operator AI pairing QR code.");

  const session = url.searchParams.get("session");
  const code = url.searchParams.get("code");
  if (!session || !code) throw new Error("Pairing QR code is incomplete.");
  return { session, code };
};

export default function App() {
  const [session, setSession] = useState<Session | null>(null);
  const [email, setEmail] = useState("");
  const [password, setPassword] = useState("");
  const [devices, setDevices] = useState<Device[]>([]);
  const [commands, setCommands] = useState<Command[]>([]);
  const [selected, setSelected] = useState<string[]>([]);
  const [commandText, setCommandText] = useState("");
  const [captureScreenshot, setCaptureScreenshot] = useState(true);
  const [collectFiles, setCollectFiles] = useState(true);
  const [activeCommand, setActiveCommand] = useState<Command | null>(null);
  const [logs, setLogs] = useState<LogLine[]>([]);
  const [artifacts, setArtifacts] = useState<Artifact[]>([]);
  const [busy, setBusy] = useState(false);
  const [scannerOpen, setScannerOpen] = useState(false);
  const [refreshing, setRefreshing] = useState(false);
  const [cameraPermission, requestCameraPermission] = useCameraPermissions();

  const recorder = useAudioRecorder(RecordingPresets.HIGH_QUALITY);
  const recorderState = useAudioRecorderState(recorder);

  useEffect(() => {
    supabase.auth.getSession().then(({ data }) => setSession(data.session));
    const { data } = supabase.auth.onAuthStateChange((_event, next) => setSession(next));
    return () => data.subscription.unsubscribe();
  }, []);

  useEffect(() => {
    (async () => {
      const permission = await AudioModule.requestRecordingPermissionsAsync();
      if (permission.granted) {
        await setAudioModeAsync({ playsInSilentMode: true, allowsRecording: true });
      }
    })();
  }, []);

  useEffect(() => {
    if (!session) return;

    refreshAll();
    registerPushToken();

    const channel = supabase
      .channel("operator-mobile-live")
      .on("postgres_changes", { event: "*", schema: "public", table: "operator_remote_devices" }, refreshAll)
      .on("postgres_changes", { event: "*", schema: "public", table: "operator_remote_commands" }, refreshAll)
      .on("postgres_changes", { event: "INSERT", schema: "public", table: "operator_remote_task_logs" }, () => {
        if (activeCommand) refreshCommand(activeCommand.id);
      })
      .on("postgres_changes", { event: "INSERT", schema: "public", table: "operator_remote_artifacts" }, () => {
        if (activeCommand) refreshCommand(activeCommand.id);
      })
      .subscribe();

    return () => {
      supabase.removeChannel(channel);
    };
  }, [session, activeCommand?.id]);

  const refreshAll = async () => {
    if (!session) return;

    const [d, c] = await Promise.all([
      supabase.from("operator_remote_devices")
        .select("id,name,last_seen_at,revoked_at")
        .is("revoked_at", null)
        .order("created_at", { ascending: true }),
      supabase.from("operator_remote_commands")
        .select("*")
        .order("created_at", { ascending: false })
        .limit(40),
    ]);

    if (d.data) setDevices(d.data);
    if (c.data) setCommands(c.data);
  };

  const refreshCommand = async (commandId: string) => {
    const [c, l, a] = await Promise.all([
      supabase.from("operator_remote_commands").select("*").eq("id", commandId).single(),
      supabase.from("operator_remote_task_logs").select("*").eq("command_id", commandId).order("id", { ascending: true }),
      supabase.from("operator_remote_artifacts").select("*").eq("command_id", commandId).order("created_at", { ascending: true }),
    ]);

    if (c.data) setActiveCommand(c.data);
    if (l.data) setLogs(l.data);
    if (a.data) setArtifacts(a.data);
  };

  const registerPushToken = async () => {
    try {
      const permission = await Notifications.requestPermissionsAsync();
      if (!permission.granted) return;

      const projectId = process.env.EXPO_PUBLIC_EAS_PROJECT_ID || Constants.expoConfig?.extra?.eas?.projectId;
      if (!projectId) return;

      const token = (await Notifications.getExpoPushTokenAsync({ projectId })).data;
      await commandApi({ action: "register_push", token, platform: Platform.OS });
    } catch {
      // Optional during local development.
    }
  };

  const signIn = async () => {
    setBusy(true);
    try {
      const { error } = await supabase.auth.signInWithPassword({ email: email.trim(), password });
      if (error) throw error;
    } catch (error: any) {
      Alert.alert("Sign in failed", error.message);
    } finally {
      setBusy(false);
    }
  };

  const signUp = async () => {
    setBusy(true);
    try {
      const { error } = await supabase.auth.signUp({ email: email.trim(), password });
      if (error) throw error;
      Alert.alert("Account created", "If email confirmation is enabled, confirm your email and then sign in.");
    } catch (error: any) {
      Alert.alert("Sign up failed", error.message);
    } finally {
      setBusy(false);
    }
  };

  const sendCommand = async () => {
    if (!commandText.trim()) return Alert.alert("Enter a command");
    if (!selected.length) return Alert.alert("Select at least one computer");

    setBusy(true);
    try {
      const result = await commandApi({
        action: "create",
        device_ids: selected,
        command_text: commandText.trim(),
        capture_screenshot: captureScreenshot,
        collect_result_files: collectFiles,
      });

      setCommandText("");
      await refreshAll();

      const first = result.commands?.[0] as Command | undefined;
      if (first) await refreshCommand(first.id);

      if (result.commands?.some((item: Command) => item.approval_state === "pending")) {
        Alert.alert("Approval required", "Review and approve this sensitive remote command in Task Details.");
      }
    } catch (error: any) {
      Alert.alert("Command failed", error.message);
    } finally {
      setBusy(false);
    }
  };

  const startVoice = async () => {
    try {
      await recorder.prepareToRecordAsync();
      recorder.record();
    } catch (error: any) {
      Alert.alert("Microphone", error.message);
    }
  };

  const stopVoice = async () => {
    try {
      await recorder.stop();
      if (!recorder.uri) return;
      setBusy(true);
      setCommandText(await transcribeAudio(recorder.uri));
    } catch (error: any) {
      Alert.alert("Voice command", error.message);
    } finally {
      setBusy(false);
    }
  };

  const openScanner = async () => {
    if (!cameraPermission?.granted) {
      const next = await requestCameraPermission();
      if (!next.granted) return Alert.alert("Camera permission is required to scan the PC QR code.");
    }
    setScannerOpen(true);
  };

  const handleBarcode = async ({ data }: { data: string }) => {
    if (!scannerOpen) return;
    setScannerOpen(false);

    try {
      const pair = parsePairUri(data);
      await claimPairing(pair.session, pair.code);
      await refreshAll();
      Alert.alert("Paired", "The computer is now linked to this account.");
    } catch (error: any) {
      Alert.alert("Pairing failed", error.message);
    }
  };

  const controlCommand = async (commandId: string, controlState: "run" | "pause" | "cancel_requested") => {
    try {
      await commandApi({ action: "control", command_id: commandId, control_state: controlState });
      await refreshCommand(commandId);
    } catch (error: any) {
      Alert.alert("Task control", error.message);
    }
  };

  const approveCommand = async (commandId: string, approved: boolean) => {
    try {
      await commandApi({ action: "approve", command_id: commandId, approved });
      await refreshCommand(commandId);
      await refreshAll();
    } catch (error: any) {
      Alert.alert("Approval", error.message);
    }
  };

  const revokeDevice = async (device: Device) => {
    Alert.alert(
      "Revoke computer",
      `Disconnect ${device.name}? It will need to be paired again before it can receive commands.`,
      [
        { text: "Cancel", style: "cancel" },
        {
          text: "Revoke",
          style: "destructive",
          onPress: async () => {
            try {
              await commandApi({ action: "revoke_device", device_id: device.id });
              setSelected((current) => current.filter((id) => id !== device.id));
              await refreshAll();
            } catch (error: any) {
              Alert.alert("Revoke device", error.message);
            }
          },
        },
      ],
    );
  };

  const openArtifact = async (artifact: Artifact) => {
    try {
      const { data, error } = await supabase.storage
        .from("operator-remote-artifacts")
        .createSignedUrl(artifact.storage_path, 300);
      if (error) throw error;
      await Linking.openURL(data.signedUrl);
    } catch (error: any) {
      Alert.alert("Artifact", error.message);
    }
  };

  if (!session) {
    return (
      <SafeAreaView style={styles.safe}>
        <View style={styles.authCard}>
          <Text style={styles.brand}>Operator AI</Text>
          <Text style={styles.subtitle}>Mobile Remote 1.0</Text>
          <TextInput style={styles.input} value={email} onChangeText={setEmail} placeholder="Email" autoCapitalize="none" keyboardType="email-address" />
          <TextInput style={styles.input} value={password} onChangeText={setPassword} placeholder="Password" secureTextEntry />
          <Pressable style={styles.primaryButton} onPress={signIn} disabled={busy}><Text style={styles.primaryText}>Sign in</Text></Pressable>
          <Pressable style={styles.secondaryButton} onPress={signUp} disabled={busy}><Text>Create account</Text></Pressable>
        </View>
      </SafeAreaView>
    );
  }

  if (scannerOpen) {
    return (
      <SafeAreaView style={styles.safe}>
        <View style={styles.scannerContainer}>
          <CameraView
            style={StyleSheet.absoluteFill}
            barcodeScannerSettings={{ barcodeTypes: ["qr"] }}
            onBarcodeScanned={handleBarcode}
          />
          <View style={styles.scannerOverlay}>
            <Text style={styles.scannerText}>Scan the QR code shown on your Operator AI PC</Text>
            <Pressable style={styles.secondaryButton} onPress={() => setScannerOpen(false)}><Text>Cancel</Text></Pressable>
          </View>
        </View>
      </SafeAreaView>
    );
  }

  if (activeCommand) {
    return (
      <SafeAreaView style={styles.safe}>
        <ScrollView contentContainerStyle={styles.container}>
          <Pressable onPress={() => setActiveCommand(null)}><Text style={styles.link}>‹ Back</Text></Pressable>
          <Text style={styles.heading}>Task Details</Text>
          <Text style={styles.commandText}>{activeCommand.command_text}</Text>

          <View style={styles.statusCard}>
            <Text style={styles.statusTitle}>{activeCommand.status.toUpperCase()}</Text>
            <Text>Progress: {activeCommand.progress}%</Text>
            {activeCommand.approval_reason ? <Text style={styles.warning}>{activeCommand.approval_reason}</Text> : null}
          </View>

          {activeCommand.approval_state === "pending" ? (
            <View style={styles.row}>
              <Pressable style={styles.primarySmall} onPress={() => approveCommand(activeCommand.id, true)}><Text style={styles.primaryText}>Approve</Text></Pressable>
              <Pressable style={styles.dangerButton} onPress={() => approveCommand(activeCommand.id, false)}><Text style={styles.primaryText}>Reject</Text></Pressable>
            </View>
          ) : null}

          {["running", "paused"].includes(activeCommand.status) ? (
            <View style={styles.row}>
              <Pressable style={styles.secondarySmall} onPress={() => controlCommand(activeCommand.id, activeCommand.control_state === "pause" ? "run" : "pause")}>
                <Text>{activeCommand.control_state === "pause" ? "Resume" : "Pause"}</Text>
              </Pressable>
              <Pressable style={styles.dangerButton} onPress={() => controlCommand(activeCommand.id, "cancel_requested")}><Text style={styles.primaryText}>Cancel</Text></Pressable>
            </View>
          ) : null}

          {activeCommand.result_text ? (
            <>
              <Text style={styles.sectionTitle}>Result</Text>
              <Text style={styles.resultBox}>{activeCommand.result_text}</Text>
            </>
          ) : null}

          <Text style={styles.sectionTitle}>Live Activity</Text>
          <View style={styles.logBox}>
            {logs.length ? logs.map((line) => <Text key={line.id} style={styles.logLine}>{line.line}</Text>) : <Text style={styles.muted}>No activity yet.</Text>}
          </View>

          <Text style={styles.sectionTitle}>Screenshots & Files</Text>
          {artifacts.length ? artifacts.map((artifact) => (
            <Pressable key={artifact.id} style={styles.artifact} onPress={() => openArtifact(artifact)}>
              <Text style={styles.link}>{artifact.kind === "screenshot" ? "🖥 " : "📎 "}{artifact.file_name}</Text>
            </Pressable>
          )) : <Text style={styles.muted}>No artifacts yet.</Text>}
        </ScrollView>
      </SafeAreaView>
    );
  }

  return (
    <SafeAreaView style={styles.safe}>
      <ScrollView
        contentContainerStyle={styles.container}
        refreshControl={<RefreshControl refreshing={refreshing} onRefresh={async () => { setRefreshing(true); await refreshAll(); setRefreshing(false); }} />}
      >
        <View style={styles.headerRow}>
          <View><Text style={styles.brand}>Operator AI</Text><Text style={styles.subtitle}>Remote Control 1.0</Text></View>
          <Pressable style={styles.secondarySmall} onPress={() => supabase.auth.signOut()}><Text>Sign out</Text></Pressable>
        </View>

        <View style={styles.rowBetween}>
          <Text style={styles.sectionTitle}>My Computers</Text>
          <Pressable onPress={openScanner}><Text style={styles.link}>+ Pair PC</Text></Pressable>
        </View>

        {devices.length === 0 ? <Text style={styles.muted}>No computers paired. Open Operator AI on the PC and tap Pair Mobile.</Text> : devices.map((device) => {
          const chosen = selected.includes(device.id);
          return (
            <Pressable
              key={device.id}
              style={[styles.deviceCard, chosen && styles.deviceSelected]}
              onPress={() => setSelected((current) => chosen ? current.filter((id) => id !== device.id) : [...current, device.id])}
            >
              <View style={styles.rowBetween}>
                <Text style={styles.deviceName}>{device.name}</Text>
                <Pressable onPress={() => revokeDevice(device)}>
                  <Text style={styles.revoke}>Revoke</Text>
                </Pressable>
              </View>
              <Text style={online(device) ? styles.online : styles.offline}>{online(device) ? "● Online" : "● Offline / queued"}</Text>
            </Pressable>
          );
        })}

        <Text style={styles.sectionTitle}>Command</Text>
        <TextInput style={styles.commandInput} multiline value={commandText} onChangeText={setCommandText} placeholder="What should the selected computer do?" />

        <View style={styles.row}>
          <Pressable style={styles.secondarySmall} onPress={recorderState.isRecording ? stopVoice : startVoice} disabled={busy}>
            <Text>{recorderState.isRecording ? "■ Stop voice" : "🎤 Voice"}</Text>
          </Pressable>
          <Pressable style={captureScreenshot ? styles.optionOn : styles.optionOff} onPress={() => setCaptureScreenshot((v) => !v)}><Text>Screenshot {captureScreenshot ? "✓" : ""}</Text></Pressable>
          <Pressable style={collectFiles ? styles.optionOn : styles.optionOff} onPress={() => setCollectFiles((v) => !v)}><Text>Result files {collectFiles ? "✓" : ""}</Text></Pressable>
        </View>

        <Pressable style={styles.primaryButton} onPress={sendCommand} disabled={busy}>
          <Text style={styles.primaryText}>{selected.length > 1 ? `Send to ${selected.length} computers` : "Send Command"}</Text>
        </Pressable>

        <Text style={styles.sectionTitle}>Recent Tasks</Text>
        {commands.map((item) => (
          <Pressable key={item.id} style={styles.taskCard} onPress={() => refreshCommand(item.id)}>
            <View style={styles.rowBetween}><Text style={styles.taskStatus}>{item.status}</Text><Text style={styles.muted}>{item.progress}%</Text></View>
            <Text numberOfLines={2}>{item.command_text}</Text>
          </Pressable>
        ))}
      </ScrollView>
    </SafeAreaView>
  );
}

const styles = StyleSheet.create({
  safe: { flex: 1, backgroundColor: "#F3F5F7" },
  container: { padding: 18, paddingBottom: 50 },
  authCard: { margin: 24, marginTop: 100, padding: 22, backgroundColor: "white", borderRadius: 18, gap: 12 },
  brand: { fontSize: 30, fontWeight: "800", color: "#111827" },
  subtitle: { color: "#64748B", marginTop: 2 },
  heading: { fontSize: 26, fontWeight: "800", marginVertical: 16 },
  sectionTitle: { fontSize: 17, fontWeight: "700", marginTop: 22, marginBottom: 10 },
  input: { height: 48, borderWidth: 1, borderColor: "#CBD5E1", borderRadius: 10, paddingHorizontal: 12, backgroundColor: "white" },
  commandInput: { minHeight: 120, borderWidth: 1, borderColor: "#CBD5E1", borderRadius: 12, padding: 12, backgroundColor: "white", textAlignVertical: "top" },
  primaryButton: { backgroundColor: "#111827", minHeight: 48, borderRadius: 12, alignItems: "center", justifyContent: "center", marginTop: 12, paddingHorizontal: 16 },
  primarySmall: { backgroundColor: "#111827", minHeight: 42, borderRadius: 10, alignItems: "center", justifyContent: "center", paddingHorizontal: 18 },
  primaryText: { color: "white", fontWeight: "700" },
  secondaryButton: { minHeight: 48, borderWidth: 1, borderColor: "#CBD5E1", borderRadius: 12, alignItems: "center", justifyContent: "center", paddingHorizontal: 16 },
  secondarySmall: { minHeight: 38, borderWidth: 1, borderColor: "#CBD5E1", borderRadius: 10, alignItems: "center", justifyContent: "center", paddingHorizontal: 13 },
  dangerButton: { backgroundColor: "#B91C1C", minHeight: 42, borderRadius: 10, alignItems: "center", justifyContent: "center", paddingHorizontal: 18 },
  row: { flexDirection: "row", gap: 8, alignItems: "center", marginTop: 10 },
  rowBetween: { flexDirection: "row", justifyContent: "space-between", alignItems: "center" },
  headerRow: { flexDirection: "row", justifyContent: "space-between", alignItems: "center" },
  deviceCard: { backgroundColor: "white", borderWidth: 1, borderColor: "#E5E7EB", borderRadius: 12, padding: 14, marginBottom: 9 },
  deviceSelected: { borderColor: "#2563EB", borderWidth: 2, backgroundColor: "#EFF6FF" },
  deviceName: { fontSize: 16, fontWeight: "700" },
  revoke: { color: "#B91C1C", fontWeight: "700", fontSize: 12 },
  online: { color: "#059669", marginTop: 4 },
  offline: { color: "#64748B", marginTop: 4 },
  optionOn: { minHeight: 38, borderRadius: 10, borderWidth: 1, borderColor: "#86EFAC", backgroundColor: "#ECFDF5", justifyContent: "center", paddingHorizontal: 11 },
  optionOff: { minHeight: 38, borderRadius: 10, borderWidth: 1, borderColor: "#CBD5E1", backgroundColor: "white", justifyContent: "center", paddingHorizontal: 11 },
  taskCard: { backgroundColor: "white", borderWidth: 1, borderColor: "#E5E7EB", borderRadius: 12, padding: 13, marginBottom: 9 },
  taskStatus: { fontWeight: "700", textTransform: "uppercase", color: "#1D4ED8" },
  muted: { color: "#64748B" },
  link: { color: "#2563EB", fontWeight: "700" },
  commandText: { fontSize: 16, lineHeight: 22 },
  statusCard: { backgroundColor: "white", borderRadius: 12, padding: 14, marginTop: 14, borderWidth: 1, borderColor: "#E5E7EB" },
  statusTitle: { fontSize: 17, fontWeight: "800", marginBottom: 5 },
  warning: { color: "#B45309", marginTop: 8 },
  resultBox: { backgroundColor: "white", borderRadius: 12, padding: 14, lineHeight: 21 },
  logBox: { backgroundColor: "#111827", borderRadius: 12, padding: 12 },
  logLine: { color: "#E5E7EB", fontFamily: Platform.select({ ios: "Menlo", android: "monospace", default: "monospace" }), fontSize: 12, marginBottom: 5 },
  artifact: { backgroundColor: "white", borderWidth: 1, borderColor: "#E5E7EB", borderRadius: 10, padding: 12, marginBottom: 8 },
  scannerContainer: { flex: 1, backgroundColor: "black" },
  scannerOverlay: { position: "absolute", bottom: 40, left: 20, right: 20, backgroundColor: "rgba(255,255,255,0.95)", borderRadius: 14, padding: 16, gap: 12 },
  scannerText: { fontWeight: "700", textAlign: "center" },
});
