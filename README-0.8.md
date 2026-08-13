# Operator AI 0.8 — consolidated production release

This bundle is designed against the repository state at commit:

`bf41110c8acf158df2ec146d05a56164f1c5e468`

It consolidates the proven 0.6/0.7 browser, native Windows, multi-window, real-app, and real Notepad write/save capabilities into the normal `OperatorAgent` used by **Run AI Task**.

## Files

Replace these existing files:

- `Operator.AI/AgentRunGuard.cs`
- `Operator.AI/OperatorAgent.cs`
- `Operator.Desktop/MainWindow.xaml`
- `Operator.Desktop/Operator.Desktop.csproj`

Add these new files:

- `Operator.AI/OperatorRuntime08.cs`
- `publish-0.8.ps1` at repository root

The old 0.6/0.7 test and specialist-agent files remain in the repository. They are intentionally retained as regression coverage and are available under **Regression & Diagnostics** in the 0.8 UI.

## What 0.8 integrates

- Normal **Run AI Task** uses the unified agent.
- Persistent Playwright Chromium browser and tabs.
- Semantic browser targeting by role, label, placeholder, exact text, test id, title/alt, and CSS.
- Browser waits, forms, uploads, downloads, screenshots, viewport inspection, and guarded visual-coordinate fallback.
- Robust Win32 top-level window discovery, switching, foreground verification, and multi-window workflows.
- Native Windows UI Automation inspection and interaction through ValuePattern, InvokePattern, TogglePattern, SelectionItemPattern, and ExpandCollapsePattern.
- Desktop-confined file and folder operations.
- Safe opening of Desktop folders in File Explorer and Desktop files in Notepad.
- Verified real Notepad document replace/read/save workflow from 0.7D.
- Repetition limits, total tool-call limits, consecutive-failure limits, timeout handling, and user cancellation.
- Runtime safe mode for high-consequence actions and credential entry.
- JSONL task journals under `%LOCALAPPDATA%\OperatorAI\history`.
- Runtime configuration under `%LOCALAPPDATA%\OperatorAI\operator.settings.json`.

## Build

Close Operator AI and any controlled test windows, then run:

```powershell
cd C:\OperatorAI
dotnet clean
dotnet build
```

Expected:

```text
Operator.Tools       succeeded
Operator.AI          succeeded
Operator.Desktop     succeeded
Build succeeded
```

Then run:

```powershell
dotnet run --project Operator.Desktop
```

## Recommended first 0.8 checks

Run these through the normal **Run AI Task** box, not the old test buttons.

### Browser task

```text
Open Wikipedia, search for OpenAI, read the page, and summarize what it is about. Verify the page title and URL before finishing.
```

### Windows + file task

```text
Create a Desktop folder named Operator08Test, create Operator08Test\proof.txt containing exactly Operator AI 0.8 production test, verify the file exists, read it back, open it in Notepad, verify the Notepad window is foreground, and verify the document text.
```

### Real edit/save task

After the previous task creates the file:

```text
Open Desktop file Operator08Test\proof.txt in Notepad. Replace the entire document with exactly Operator AI 0.8 verified write and save, verify the editor text, save the existing document, verify the real Desktop file contents, open Operator08Test in File Explorer, locate proof.txt, then return to Notepad and verify the final document again. Overwrite the existing file as part of this requested edit.
```

## Runtime settings

On first agent run, Operator AI creates:

`%LOCALAPPDATA%\OperatorAI\operator.settings.json`

Default settings:

```json
{
  "Model": "gpt-5.6",
  "TaskTimeoutMinutes": 10,
  "MaximumPlanningSteps": 80,
  "MaximumRepeatedToolCalls": 3,
  "MaximumConsecutiveErrors": 5,
  "MaximumTotalToolCalls": 140,
  "MaximumToolResultCharacters": 40000,
  "SafeMode": true,
  "AllowBrowserCoordinateFallback": true,
  "AllowKeyboardFallback": true,
  "WriteTaskJournal": true
}
```

Restart Operator AI after changing settings.

## Task history

Each run writes a JSONL journal under:

`%LOCALAPPDATA%\OperatorAI\history\YYYY-MM-DD\`

A lightweight run index is stored at:

`%LOCALAPPDATA%\OperatorAI\history\index.jsonl`

Tool arguments containing common credential/sensitive markers are redacted from the journal, and tool results are stored only as short status summaries.

## Publish

From `C:\OperatorAI`:

```powershell
powershell -ExecutionPolicy Bypass -File .\publish-0.8.ps1
```

For a self-contained Windows x64 publish:

```powershell
powershell -ExecutionPolicy Bypass -File .\publish-0.8.ps1 -SelfContained
```

Published output is kept under `Operator.Desktop\bin\publish`, so normal Git ignore rules for `bin` continue to keep publish artifacts out of source control.

## Commit after successful verification

```powershell
cd C:\OperatorAI

git add .
git commit -m "Operator AI 0.8 unified production agent"
git push

git tag v0.8
git push origin v0.8
```
