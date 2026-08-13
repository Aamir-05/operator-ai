# Operator AI 1.0 - Desktop + Mobile Remote Control

This is the single consolidated 1.0 overlay for the working Operator AI 0.8 repository.

Base repository commit used for this build:

`857db47c2e72c7ce08b1a13bd55d399d62a9958b`

## What 1.0 adds

### Windows desktop
- The full 0.8 unified Windows + browser agent remains the execution engine.
- OpenAI API key can be stored in Windows Credential Manager instead of requiring a permanent environment variable.
- Operator AI can start with Windows and continue running in the system tray.
- Secure outbound-only remote command polling.
- Device pairing from a QR code shown on the PC.
- Live remote task logs.
- Real pause/resume checkpoints between planning and tool calls.
- Remote cancellation.
- Completion screenshots.
- Result-file upload for Desktop files declared by the agent with `RESULT_FILE:`.
- Existing 0.6/0.7 diagnostics remain available.

### Mobile app
- Expo/React Native iOS + Android app.
- Supabase email/password authentication.
- Pair a PC by scanning its QR code.
- Online/offline PC state.
- Select one or multiple computers.
- Text commands.
- Voice commands.
- Live task status and logs.
- Pause/resume/cancel.
- Approval/rejection for sensitive remote commands.
- Completion screenshots.
- Result-file links.
- Push notifications.
- Offline command queue.

### Operator Cloud
- Supabase Auth.
- Postgres command/device/task tables with Row Level Security.
- Private Storage bucket for screenshots and result files.
- Realtime subscriptions.
- Edge Functions for pairing, device relay, command control, and voice transcription.
- Per-PC random device secrets. Only a SHA-256 hash is stored in the device table.
- PCs make outbound HTTPS calls only; no public inbound port is required.

## Replace these 0.8 files

- `Operator.AI/OperatorAgent.cs`
- `Operator.AI/OperatorRuntime08.cs`
- `Operator.Desktop/MainWindow.xaml`
- `Operator.Desktop/Operator.Desktop.csproj`

## Add these files/directories

- `Operator.AI/OperatorExecutionHooks.cs`
- `Operator.AI/OperatorSecrets.cs`
- `Operator.Desktop/RemoteSettings.cs`
- `Operator.Desktop/RemoteApiClient.cs`
- `Operator.Desktop/RemoteAgentService.cs`
- `Operator.Desktop/WindowsScreenCapture.cs`
- `Operator.Desktop/StartupRegistration.cs`
- `Operator.Desktop/SetupWindow.xaml`
- `Operator.Desktop/SetupWindow.xaml.cs`
- `Operator.Desktop/PairingWindow.xaml`
- `Operator.Desktop/PairingWindow.xaml.cs`
- `Operator.Desktop/MainWindow.Remote.cs`
- `Operator.Mobile/*`
- `Operator.Cloud/*`
- `.github/workflows/release-1.0.yml`
- `installer/OperatorAI.iss`
- `publish-1.0.ps1`
- `setup-mobile.ps1`

## 1. Build the Windows app

Extract this overlay over your current `C:\OperatorAI` repository, then:

```powershell
cd C:\OperatorAI
dotnet clean
dotnet build
```

Then run:

```powershell
dotnet run --project Operator.Desktop
```

On first launch, Operator AI Setup asks for the OpenAI API key. The key is stored in Windows Credential Manager under:

`OperatorAI/OpenAIApiKey`

## 2. Create/deploy Operator Cloud

Use a dedicated Supabase project rather than reusing an unrelated production backend.

Once a Supabase project exists, install the Supabase CLI and run:

```powershell
cd C:\OperatorAI\Operator.Cloud

powershell -ExecutionPolicy Bypass -File .\deploy-cloud.ps1 `
  -ProjectRef YOUR_SUPABASE_PROJECT_REF
```

For mobile voice transcription, run the deployment from a shell where `OPENAI_API_KEY` is set. The script saves it as a Supabase Edge Function secret.

After deployment, configure this in Operator AI Desktop -> Setup:

```text
https://YOUR_PROJECT_REF.supabase.co
```

## 3. Configure Operator AI Mobile

```powershell
cd C:\OperatorAI\Operator.Mobile
copy .env.example .env
```

Set:

```text
EXPO_PUBLIC_SUPABASE_URL=https://YOUR_PROJECT_REF.supabase.co
EXPO_PUBLIC_SUPABASE_PUBLISHABLE_KEY=YOUR_PUBLISHABLE_KEY
EXPO_PUBLIC_EAS_PROJECT_ID=YOUR_EAS_PROJECT_ID
```

Install/fix packages for the installed Expo SDK:

```powershell
npm install
npx expo install --fix
```

Start development:

```powershell
npx expo start
```

Production mobile builds:

```powershell
npm install -g eas-cli
eas login
eas build --platform android --profile production
eas build --platform ios --profile production
```

## 4. Pair the PC

On Windows:

1. Open Operator AI.
2. Setup -> configure the Operator Cloud URL.
3. Click **Pair Mobile**.
4. The PC shows a six-digit code and QR code.

On mobile:

1. Sign in/create an Operator AI account.
2. Tap **Pair PC**.
3. Scan the PC QR code.

The raw device secret is returned to the PC once and stored in Windows Credential Manager.

## 5. Test phone -> PC execution

From the mobile app select the paired PC and send:

```text
Create a Desktop folder named MobileOperatorTest.
Create MobileOperatorTest\proof.txt containing exactly:
Operator AI mobile remote test

Verify the file exists and read it back.
Open it in Notepad and verify the document.
Return the result file to me when done.
```

Expected mobile status progression:

```text
QUEUED
RUNNING
COMPLETED
```

The task detail screen shows live logs and the completion screenshot. If the agent includes:

```text
RESULT_FILE: MobileOperatorTest\proof.txt
```

the desktop uploads the private result file to the task.

## 6. Pause / resume / cancel

A running remote command exposes:

- Pause
- Resume
- Cancel

Pause is enforced at Operator Agent planning/tool checkpoints. Resume continues the same model/tool conversation rather than restarting from the beginning.

## 7. Sensitive-command approval

The cloud performs a conservative pre-execution check for remote commands containing action markers such as overwrite, delete, upload, submit, posting, account/security changes, and money movement.

Those commands enter:

```text
AWAITING_APPROVAL
```

They do not reach the PC until the signed-in mobile owner taps **Approve**.

Mobile approval never bypasses Operator AI's existing local safe-mode rules. A high-consequence action blocked locally remains blocked.

## 8. Offline computers

Remote commands remain queued in Postgres while a PC is offline. When Operator AI reconnects, the outbound polling loop receives the oldest approved queued command.

## 9. Background operation

Setup contains:

```text
Start Operator AI with Windows
```

When enabled, Operator AI starts with `--background` for the current Windows user.

Closing the main window hides it to the system tray. Use tray -> **Exit** to stop the remote agent completely.

## 10. Windows installer / GitHub release

Publish locally:

```powershell
cd C:\OperatorAI
powershell -ExecutionPolicy Bypass -File .\publish-1.0.ps1 -SelfContained
```

Then compile `installer\OperatorAI.iss` with Inno Setup 6.

The included GitHub Actions workflow builds the Windows installer when a `v1.0*` tag is pushed.

## Security model

- No inbound listening port on the Windows PC.
- No router port forwarding.
- Mobile authentication uses Supabase Auth.
- Pairing sessions expire after ten minutes.
- Device credentials are random per PC.
- Cloud stores only the hash of the long-lived device secret.
- Raw device secret is returned to the PC once and stored in Windows Credential Manager.
- User-facing database reads are protected by RLS.
- Task artifacts are stored in a private Supabase Storage bucket.
- Device revocation is supported.
- Existing Operator AI safe mode remains active.
- Sensitive remote commands wait for mobile approval before dispatch.

## Current deployment dependency

The source package is complete, but the mobile/remote service cannot go live until a Supabase project is selected or created.

Do not place the Operator AI schema inside another production Supabase project unless that is intentional.

## Commit after end-to-end verification

```powershell
cd C:\OperatorAI

git add .
git commit -m "Operator AI 1.0 desktop mobile remote control complete"
git push

git tag v1.0.0
git push origin v1.0.0
```
