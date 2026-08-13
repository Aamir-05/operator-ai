using OpenAI.Responses;
using Operator.Tools;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Operator.AI;

#pragma warning disable OPENAI001

public sealed class OperatorAgent
{
    private readonly ResponsesClient _client;
    private readonly OperatorSettings _settings;

    private static readonly ResponseTool[] Toolset =
        BuildToolset();

    private const string Instructions =
        """
        You are Operator AI 0.8, a unified Windows and browser automation agent.

        Complete the user's requested task using the available tools.

        CORE RULES
        - Use tools for real actions. Never pretend an action happened.
        - Never claim success without verification of the requested outcome.
        - Prefer deterministic structured automation over guessing.
        - Inspect before targeting unfamiliar controls.
        - Recover by changing strategy instead of repeating the same failed call.
        - Keep actions within the user's requested scope.

        WINDOWS WORKFLOW
        - Use windows_list_windows for robust top-level Win32 discovery.
        - Before manipulating a real application, focus it with windows_focus_window
          and verify it with windows_verify_foreground.
        - Prefer native UI Automation controls: list, find, inspect, then use the
          supported Value/Invoke/Toggle/Selection/ExpandCollapse patterns.
        - Use windows_find_control_any when the control type is unknown.
        - Use keyboard tools only when native UI Automation is insufficient.
        - For real Notepad editing, prefer windows_replace_document_text, then
          windows_read_document_text, then windows_save_document.
        - For cross-application workflows, re-verify the foreground window after
          every application switch.
        - Desktop file and folder tools are intentionally confined to Desktop.

        BROWSER WORKFLOW
        Target elements in this order whenever possible:
        1. ARIA role + accessible name
        2. label
        3. placeholder
        4. exact visible text
        5. test id
        6. title / alt
        7. stable CSS
        8. visual coordinate interaction only as a fallback

        For coordinate interaction:
        - prove structured targeting is insufficient,
        - capture/inspect a fresh viewport image,
        - do not scroll or navigate after the image and before the click,
        - click only a clearly identified target,
        - verify the resulting state.

        FILE SAFETY
        - Do not delete, rename, or move user files unless a dedicated tool for
          that action exists and the user explicitly requested it. No such
          destructive file tools are exposed in Operator AI 0.8.
        - Do not overwrite an existing Desktop file through a direct file-write
          tool unless the user's task clearly asks to overwrite or replace it.

        HIGH-CONSEQUENCE SAFETY
        In safe mode, final purchase/payment/transfer actions, permanent account
        deletion, password/security changes, destructive data actions, legal
        signing/submission, and credential entry are blocked by the runtime.
        Do not try to bypass a BLOCKED result.

        COMPLETION
        Before finishing, verify the final requested state using an independent
        read, state query, file check, page check, window check, or equivalent.
        Distinguish completed actions from observations and assumptions.
        """;

    public OperatorAgent()
    {
        _settings = OperatorSettings.Load();

        string? apiKey =
            OperatorSecrets.GetOpenAiApiKey();

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OpenAI API key was not configured. Open Operator AI Setup and save the key first."
            );
        }

        _client = new ResponsesClient(apiKey);
    }

    public Task<string> RunAsync(
        string task,
        Action<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        return RunAsync(
            task,
            log,
            cancellationToken,
            null
        );
    }

    public async Task<string> RunAsync(
        string task,
        Action<string>? log,
        CancellationToken cancellationToken,
        OperatorExecutionHooks? hooks)
    {
        if (string.IsNullOrWhiteSpace(task))
        {
            return "ERROR: Task cannot be empty.";
        }

        List<ResponseItem> inputItems =
        [
            ResponseItem.CreateUserMessageItem(task)
        ];

        AgentRunGuard guard = new()
        {
            MaximumRepeatedToolCalls = _settings.MaximumRepeatedToolCalls,
            MaximumConsecutiveErrors = _settings.MaximumConsecutiveErrors,
            MaximumTotalToolCalls = _settings.MaximumTotalToolCalls
        };

        using OperatorTaskJournal journal =
            OperatorTaskJournal.Start(task, _settings);

        using CancellationTokenSource timeoutSource =
            CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        timeoutSource.CancelAfter(
            TimeSpan.FromMinutes(_settings.TaskTimeoutMinutes)
        );

        CancellationToken token = timeoutSource.Token;

        log?.Invoke(
            $"[TASK] Operator AI {OperatorSettings.ProductVersion} run {journal.RunId}"
        );

        log?.Invoke(
            $"[STATE] Ready | Model={_settings.Model} | SafeMode={_settings.SafeMode}"
        );

        if (_settings.WriteTaskJournal)
        {
            log?.Invoke(
                $"[STATE] Task journal: {journal.FilePath}"
            );
        }

        try
        {
            for (int step = 1;
                 step <= _settings.MaximumPlanningSteps;
                 step++)
            {
                token.ThrowIfCancellationRequested();

                if (hooks?.BeforePlanningStepAsync != null)
                {
                    await hooks.BeforePlanningStepAsync(step, token);
                }

                token.ThrowIfCancellationRequested();

                log?.Invoke(
                    $"[PLAN] Planning step {step}/{_settings.MaximumPlanningSteps}..."
                );

                journal.Record(
                    "planning",
                    $"Planning step {step}."
                );

                CreateResponseOptions options =
                    new(_settings.Model, inputItems)
                    {
                        Instructions = Instructions
                    };

                foreach (ResponseTool tool in Toolset)
                {
                    options.Tools.Add(tool);
                }

                ResponseResult response =
                    await _client.CreateResponseAsync(
                        options,
                        token
                    );

                token.ThrowIfCancellationRequested();

                inputItems.AddRange(response.OutputItems);

                bool toolCalled = false;

                foreach (
                    FunctionCallResponseItem functionCall
                    in response.OutputItems.OfType<FunctionCallResponseItem>())
                {
                    token.ThrowIfCancellationRequested();
                    toolCalled = true;

                    string argumentsText =
                        functionCall.FunctionArguments.ToString();

                    string result;

                    OperatorToolGateDecision hookDecision =
                        OperatorToolGateDecision.Continue();

                    if (hooks?.BeforeToolAsync != null)
                    {
                        hookDecision = await hooks.BeforeToolAsync(
                            functionCall.FunctionName,
                            argumentsText,
                            token
                        );
                    }

                    token.ThrowIfCancellationRequested();

                    if (!hookDecision.Allowed)
                    {
                        result = $"BLOCKED: {hookDecision.Reason}";
                        log?.Invoke($"[REMOTE] {hookDecision.Reason}");
                    }
                    else if (!guard.CanExecuteTool(
                            functionCall.FunctionName,
                            argumentsText,
                            out string guardReason))
                    {
                        result = $"BLOCKED: {guardReason}";

                        log?.Invoke(
                            $"[RETRY] {guardReason}"
                        );
                    }
                    else if (!OperatorSafetyPolicy.CanExecute(
                                 task,
                                 functionCall.FunctionName,
                                 argumentsText,
                                 _settings,
                                 out string safetyReason))
                    {
                        result = $"BLOCKED: {safetyReason}";

                        log?.Invoke(
                            $"[SAFETY] {safetyReason}"
                        );
                    }
                    else
                    {
                        log?.Invoke(
                            $"[ACTION] {functionCall.FunctionName}"
                        );

                        journal.Record(
                            "tool_start",
                            "Tool execution started.",
                            functionCall.FunctionName,
                            argumentsText
                        );

                        result =
                            await ExecuteToolAsync(
                                functionCall,
                                token,
                                task
                            );
                    }

                    guard.RegisterResult(result);

                    if (AgentRunGuard.IsFailure(result))
                    {
                        log?.Invoke(
                            $"[ERROR] {result}"
                        );

                        journal.Record(
                            "tool_error",
                            "Tool execution failed or was blocked.",
                            functionCall.FunctionName,
                            argumentsText,
                            result
                        );
                    }
                    else
                    {
                        log?.Invoke(
                            $"[SUCCESS] {result}"
                        );

                        journal.Record(
                            "tool_success",
                            "Tool execution succeeded.",
                            functionCall.FunctionName,
                            argumentsText,
                            result
                        );
                    }

                    string modelResult =
                        LimitToolResult(result);

                    inputItems.Add(
                        new FunctionCallOutputResponseItem(
                            functionCall.CallId,
                            modelResult
                        )
                    );

                    if (guard.TooManyErrors(
                            out string failureReason))
                    {
                        string finalFailure =
                            $"ERROR: {failureReason}";

                        log?.Invoke(
                            $"[ERROR] {failureReason}"
                        );

                        journal.Finish("failed", finalFailure);
                        return finalFailure;
                    }
                }

                if (!toolCalled)
                {
                    string finalAnswer = response.GetOutputText();

                    if (string.IsNullOrWhiteSpace(finalAnswer))
                    {
                        finalAnswer = "Task completed.";
                    }

                    log?.Invoke(
                        $"[COMPLETE] {finalAnswer}"
                    );

                    journal.Finish("completed", finalAnswer);
                    return finalAnswer;
                }
            }

            string limitFailure =
                "ERROR: Maximum planning-step limit reached before completion.";

            journal.Finish("failed", limitFailure);
            return limitFailure;
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                const string cancelled =
                    "CANCELLED: Task stopped by the user.";

                journal.Finish("cancelled", cancelled);
                return cancelled;
            }

            string timedOut =
                $"TIMEOUT: Task exceeded the {_settings.TaskTimeoutMinutes}-minute limit and was stopped.";

            journal.Finish("timed_out", timedOut);
            return timedOut;
        }
        catch (Exception ex)
        {
            string error =
                $"ERROR: Agent failure: {ex.Message}";

            log?.Invoke(error);
            journal.Finish("failed", error);
            return error;
        }
    }

    private static ResponseTool[] BuildToolset()
    {
        return
        [
            NoArgs(
                "operator_runtime_info",
                "Report Operator AI 0.8 runtime configuration and safety boundaries."
            ),

            Tool(
                "open_application",
                "Open an approved Windows application: notepad, calculator, or edge.",
                ("application", "string")
            ),

            Tool(
                "create_desktop_folder",
                "Create one folder on the Windows Desktop.",
                ("folder_name", "string")
            ),

            Tool(
                "create_desktop_file",
                "Create a UTF-8 text file under Desktop. Existing files require explicit overwrite intent in the user task.",
                ("relative_path", "string"),
                ("content", "string")
            ),

            Tool(
                "read_desktop_file",
                "Read a text file located under the Windows Desktop.",
                ("relative_path", "string")
            ),

            Tool(
                "desktop_file_exists",
                "Check whether a file exists under the Windows Desktop.",
                ("relative_path", "string")
            ),

            NoArgs(
                "list_desktop",
                "List files and folders on the Windows Desktop."
            ),

            Tool(
                "windows_open_desktop_folder",
                "Open an existing Desktop folder in real File Explorer.",
                ("relative_path", "string")
            ),

            Tool(
                "windows_open_desktop_file_in_notepad",
                "Open an existing Desktop text file in real Notepad.",
                ("relative_path", "string")
            ),

            NoArgs(
                "windows_list_windows",
                "List visible titled top-level Windows using the robust Win32 window-discovery layer."
            ),

            Tool(
                "windows_wait_for_top_window",
                "Wait for a top-level Windows window whose title contains the supplied title.",
                ("window_title", "string"),
                ("timeout_seconds", "integer")
            ),

            Tool(
                "windows_focus_window",
                "Bring a visible top-level Windows window to the foreground using its title.",
                ("window_title", "string")
            ),

            Tool(
                "windows_verify_foreground",
                "Verify that the current foreground Windows window matches the expected title.",
                ("expected_title", "string")
            ),

            NoArgs(
                "windows_foreground_info",
                "Report the current foreground Windows window title, PID, and handle."
            ),

            Tool(
                "windows_list_controls",
                "List native UI Automation controls inside a Windows window. Use __FOREGROUND__ for the active window.",
                ("window_title", "string"),
                ("maximum_controls", "integer")
            ),

            Tool(
                "windows_find_control",
                "Find a native Windows control by control type and accessible name or AutomationId.",
                ("window_title", "string"),
                ("control_type", "string"),
                ("control_name", "string"),
                ("exact_name", "boolean")
            ),

            Tool(
                "windows_find_control_any",
                "Find a native Windows control by accessible Name or AutomationId without requiring a control type.",
                ("window_title", "string"),
                ("control_query", "string")
            ),

            Tool(
                "windows_get_control_info",
                "Inspect a native Windows control and its supported UI Automation patterns.",
                ("window_title", "string"),
                ("control_type", "string"),
                ("control_name", "string"),
                ("exact_name", "boolean")
            ),

            Tool(
                "windows_set_control_value",
                "Set the value of a native editable Windows control through ValuePattern.",
                ("window_title", "string"),
                ("control_type", "string"),
                ("control_name", "string"),
                ("exact_name", "boolean"),
                ("value", "string")
            ),

            Tool(
                "windows_get_control_value",
                "Read the value or text of a native Windows control.",
                ("window_title", "string"),
                ("control_type", "string"),
                ("control_name", "string"),
                ("exact_name", "boolean")
            ),

            Tool(
                "windows_click_control",
                "Activate a native Windows control through its supported UI Automation pattern.",
                ("window_title", "string"),
                ("control_type", "string"),
                ("control_name", "string"),
                ("exact_name", "boolean")
            ),

            Tool(
                "windows_set_toggle",
                "Set a native checkbox/toggle to the requested state through TogglePattern.",
                ("window_title", "string"),
                ("control_type", "string"),
                ("control_name", "string"),
                ("exact_name", "boolean"),
                ("checked", "boolean")
            ),

            Tool(
                "windows_get_toggle",
                "Read the TogglePattern state of a native Windows control.",
                ("window_title", "string"),
                ("control_type", "string"),
                ("control_name", "string"),
                ("exact_name", "boolean")
            ),

            Tool(
                "windows_select_control",
                "Select a native tab item, list item, or other SelectionItemPattern control.",
                ("window_title", "string"),
                ("control_type", "string"),
                ("control_name", "string"),
                ("exact_name", "boolean")
            ),

            Tool(
                "windows_set_expanded",
                "Expand or collapse a native ExpandCollapsePattern control.",
                ("window_title", "string"),
                ("control_type", "string"),
                ("control_name", "string"),
                ("exact_name", "boolean"),
                ("expanded", "boolean")
            ),

            Tool(
                "windows_focus_control",
                "Move keyboard focus to a native Windows control.",
                ("window_title", "string"),
                ("control_type", "string"),
                ("control_name", "string"),
                ("exact_name", "boolean")
            ),

            Tool(
                "windows_wait_for_control",
                "Wait for a native Windows control to become available.",
                ("window_title", "string"),
                ("control_type", "string"),
                ("control_name", "string"),
                ("exact_name", "boolean"),
                ("timeout_seconds", "integer")
            ),

            Tool(
                "type_text",
                "Fallback text input for a visible Windows application. Prefer native ValuePattern when available.",
                ("window_title", "string"),
                ("text", "string")
            ),

            Tool(
                "press_key",
                "Fallback Windows keyboard input such as CTRL+A, CTRL+S, ENTER, TAB, ESC, or arrows.",
                ("keys", "string")
            ),

            Tool(
                "windows_replace_document_text",
                "Replace the entire currently foreground real Notepad document and verify the edit.",
                ("window_title", "string"),
                ("text", "string")
            ),

            Tool(
                "windows_read_document_text",
                "Read and optionally verify text from a foreground real Notepad document. Pass an empty expected_text to read without a specific assertion.",
                ("window_title", "string"),
                ("expected_text", "string")
            ),

            Tool(
                "windows_save_document",
                "Save the verified foreground existing Notepad document with Ctrl+S.",
                ("window_title", "string")
            ),

            Tool(
                "save_active_document_as_desktop_file",
                "Run the verified Save As workflow for the active document to a Desktop-relative path. Existing target requires explicit overwrite intent.",
                ("relative_path", "string")
            ),

            NoArgs("start_browser", "Start the persistent Operator AI Chromium browser."),
            NoArgs("browser_session_info", "Get browser URL, title, persistent profile, and tabs."),
            Tool("browser_navigate", "Navigate the current browser tab to a URL.", ("url", "string")),
            NoArgs("browser_get_page_info", "Get the current browser page title and URL."),
            NoArgs("browser_read_page", "Read visible textual content from the current browser page."),
            NoArgs("browser_list_links", "List links visible on the current browser page."),
            NoArgs("browser_list_elements", "Inspect interactive elements on the current browser page."),

            Tool(
                "browser_find",
                "Find webpage elements. Locator types: css, text, exact_text, label, placeholder, title, testid, alt.",
                ("locator_type", "string"),
                ("query", "string")
            ),

            Tool("browser_role_find", "Find an element by ARIA role and accessible name.", ("role", "string"), ("name", "string"), ("exact", "boolean")),
            Tool("browser_role_click", "Click an element by ARIA role and accessible name.", ("role", "string"), ("name", "string"), ("exact", "boolean")),
            Tool("browser_role_fill", "Fill an editable browser element by ARIA role and accessible name.", ("role", "string"), ("name", "string"), ("exact", "boolean"), ("text", "string")),
            Tool("browser_role_wait", "Wait for an element by ARIA role and accessible name.", ("role", "string"), ("name", "string"), ("exact", "boolean"), ("state", "string"), ("timeout_seconds", "integer")),
            Tool("browser_role_get_text", "Read text from an element by ARIA role and accessible name.", ("role", "string"), ("name", "string"), ("exact", "boolean")),
            Tool("browser_exact_text", "Find an element whose visible text exactly matches supplied text.", ("text", "string")),

            Tool("browser_visual_inspect", "Observe the current browser page visually. This tool does not click.", ("question", "string"), ("full_page", "boolean")),
            Tool("browser_screenshot", "Capture a browser screenshot under Desktop\\OperatorScreenshots.", ("relative_path", "string"), ("full_page", "boolean")),
            NoArgs("browser_list_screenshots", "List screenshots under Desktop\\OperatorScreenshots."),
            NoArgs("browser_get_viewport", "Read viewport dimensions, scroll position, URL, and coordinate-click freshness state."),
            Tool("browser_element_box", "Read viewport bounds and center of a structured browser element.", ("locator_type", "string"), ("query", "string")),
            Tool("browser_mouse_move", "Move the browser mouse to a viewport coordinate without clicking.", ("x", "integer"), ("y", "integer")),
            Tool("browser_mouse_click", "Perform a guarded coordinate click in the current browser viewport.", ("x", "integer"), ("y", "integer")),
            Tool("browser_mouse_double_click", "Perform a guarded coordinate double-click in the current browser viewport.", ("x", "integer"), ("y", "integer")),

            Tool("browser_get_text", "Read text from the first browser element matching a locator.", ("locator_type", "string"), ("query", "string")),
            Tool("browser_get_attribute", "Read an HTML attribute from the first matching browser element.", ("locator_type", "string"), ("query", "string"), ("attribute_name", "string")),
            Tool("browser_get_value", "Read the current value of a browser input or textarea.", ("locator_type", "string"), ("query", "string")),
            Tool("browser_is_visible", "Check whether the first matching browser element is visible.", ("locator_type", "string"), ("query", "string")),

            Tool("browser_wait", "Wait for browser element state: visible, hidden, attached, or detached.", ("locator_type", "string"), ("query", "string"), ("state", "string"), ("timeout_seconds", "integer")),
            Tool("browser_wait_for_url", "Wait for the current browser URL to match a URL or Playwright glob pattern.", ("url_pattern", "string"), ("timeout_seconds", "integer")),
            Tool("browser_wait_for_text", "Wait for visible text to appear on the current browser page.", ("text", "string"), ("exact", "boolean"), ("timeout_seconds", "integer")),

            Tool("browser_click", "Click the first browser element matching a generic locator.", ("locator_type", "string"), ("query", "string")),
            Tool("browser_fill", "Replace the contents of a browser field with text.", ("locator_type", "string"), ("query", "string"), ("text", "string")),
            Tool("browser_type", "Type text character-by-character into a browser field.", ("locator_type", "string"), ("query", "string"), ("text", "string")),
            Tool("browser_press", "Press a keyboard key on a specific browser element.", ("locator_type", "string"), ("query", "string"), ("key", "string")),
            Tool("browser_page_key", "Press a keyboard key globally on the current browser page.", ("key", "string")),

            Tool("browser_scroll", "Scroll the current browser page vertically.", ("delta_y", "integer")),
            Tool("browser_scroll_to", "Scroll a matching browser element into view.", ("locator_type", "string"), ("query", "string")),
            Tool("browser_set_checked", "Set a checkbox or radio control to checked or unchecked.", ("locator_type", "string"), ("query", "string"), ("checked", "boolean")),
            Tool("browser_get_checked", "Read whether a checkbox or radio control is checked.", ("locator_type", "string"), ("query", "string")),
            Tool("browser_select_option", "Select an option from a standard HTML select control.", ("locator_type", "string"), ("query", "string"), ("selection_type", "string"), ("selection", "string")),

            Tool("browser_upload_desktop_file", "Upload a Desktop file into a webpage file input.", ("locator_type", "string"), ("query", "string"), ("relative_path", "string")),
            Tool("browser_download", "Click an element expected to trigger a download and save under Desktop\\OperatorDownloads.", ("locator_type", "string"), ("query", "string"), ("preferred_relative_path", "string")),
            NoArgs("browser_list_downloads", "List files under Desktop\\OperatorDownloads."),

            NoArgs("browser_back", "Navigate backward in browser history."),
            NoArgs("browser_forward", "Navigate forward in browser history."),
            NoArgs("browser_reload", "Reload the current browser page."),
            Tool("browser_new_tab", "Open a new browser tab. Pass an empty URL for a blank tab.", ("url", "string")),
            NoArgs("browser_list_tabs", "List currently open Operator AI browser tabs."),
            Tool("browser_switch_tab", "Switch to a browser tab by 1-based tab number.", ("tab_number", "integer")),
            Tool("browser_close_tab", "Close a browser tab by 1-based tab number.", ("tab_number", "integer")),
            NoArgs("stop_browser", "Close the Operator AI browser while retaining persistent session data.")
        ];
    }

    private static FunctionTool NoArgs(
        string name,
        string description)
    {
        return ResponseTool.CreateFunctionTool(
            functionName: name,
            functionDescription: description,
            functionParameters: null,
            strictModeEnabled: false
        );
    }

    private static FunctionTool Tool(
        string name,
        string description,
        params (string Name, string Type)[] parameters)
    {
        Dictionary<string, Dictionary<string, string>> properties =
            new(StringComparer.Ordinal);

        foreach ((string Name, string Type) parameter in parameters)
        {
            properties[parameter.Name] =
                new Dictionary<string, string>
                {
                    ["type"] = parameter.Type
                };
        }

        string schema = JsonSerializer.Serialize(
            new
            {
                type = "object",
                properties,
                required = parameters.Select(item => item.Name).ToArray(),
                additionalProperties = false
            }
        );

        return ResponseTool.CreateFunctionTool(
            functionName: name,
            functionDescription: description,
            functionParameters: BinaryData.FromString(schema),
            strictModeEnabled: true
        );
    }

    private async Task<string> ExecuteToolAsync(
        FunctionCallResponseItem call,
        CancellationToken cancellationToken,
        string userTask)
    {
        JsonElement arguments = ParseArguments(call);

        switch (call.FunctionName)
        {
            case "operator_runtime_info":
                return GetRuntimeInfo();

            case "open_application":
                return WindowsTools.OpenApplication(S(arguments, "application"));

            case "create_desktop_folder":
                return WindowsTools.CreateDesktopFolder(S(arguments, "folder_name"));

            case "create_desktop_file":
                return CreateDesktopFileSafely(
                    S(arguments, "relative_path"),
                    S(arguments, "content"),
                    userTask
                );

            case "read_desktop_file":
                return WindowsTools.ReadDesktopFile(S(arguments, "relative_path"));

            case "desktop_file_exists":
                return WindowsTools.DesktopFileExists(S(arguments, "relative_path"));

            case "list_desktop":
                return WindowsTools.ListDesktop();

            case "windows_open_desktop_folder":
                return OpenDesktopFolder(S(arguments, "relative_path"));

            case "windows_open_desktop_file_in_notepad":
                return OpenDesktopFileInNotepad(S(arguments, "relative_path"));

            case "windows_list_windows":
                return await Task.Run(
                    WindowsWindowTools.ListWindows,
                    cancellationToken
                );

            case "windows_wait_for_top_window":
                return await Task.Run(
                    () => WindowsWindowTools.WaitForWindow(
                        S(arguments, "window_title"),
                        I(arguments, "timeout_seconds")
                    ),
                    cancellationToken
                );

            case "windows_focus_window":
                return await Task.Run(
                    () => WindowsWindowTools.FocusWindow(
                        S(arguments, "window_title")
                    ),
                    cancellationToken
                );

            case "windows_verify_foreground":
                return await Task.Run(
                    () => WindowsWindowTools.VerifyForegroundWindow(
                        S(arguments, "expected_title")
                    ),
                    cancellationToken
                );

            case "windows_foreground_info":
                return await Task.Run(
                    WindowsWindowTools.GetForegroundWindowInfo,
                    cancellationToken
                );

            case "windows_list_controls":
                return await Task.Run(
                    () => WindowsControlTools.ListControls(
                        S(arguments, "window_title"),
                        I(arguments, "maximum_controls")
                    ),
                    cancellationToken
                );

            case "windows_find_control":
                return await Task.Run(
                    () => WindowsControlTools.FindControl(
                        S(arguments, "window_title"),
                        S(arguments, "control_type"),
                        S(arguments, "control_name"),
                        B(arguments, "exact_name")
                    ),
                    cancellationToken
                );

            case "windows_find_control_any":
                return await Task.Run(
                    () => WindowsControlTools.FindControlInfo(
                        S(arguments, "window_title"),
                        S(arguments, "control_query")
                    ),
                    cancellationToken
                );

            case "windows_get_control_info":
                return await Task.Run(
                    () => WindowsControlTools.GetControlInfo(
                        S(arguments, "window_title"),
                        S(arguments, "control_type"),
                        S(arguments, "control_name"),
                        B(arguments, "exact_name")
                    ),
                    cancellationToken
                );

            case "windows_set_control_value":
                return await Task.Run(
                    () => WindowsControlTools.SetControlValue(
                        S(arguments, "window_title"),
                        S(arguments, "control_type"),
                        S(arguments, "control_name"),
                        B(arguments, "exact_name"),
                        S(arguments, "value")
                    ),
                    cancellationToken
                );

            case "windows_get_control_value":
                return await Task.Run(
                    () => WindowsControlTools.GetControlValue(
                        S(arguments, "window_title"),
                        S(arguments, "control_type"),
                        S(arguments, "control_name"),
                        B(arguments, "exact_name")
                    ),
                    cancellationToken
                );

            case "windows_click_control":
                return await Task.Run(
                    () => WindowsControlTools.ClickControl(
                        S(arguments, "window_title"),
                        S(arguments, "control_type"),
                        S(arguments, "control_name"),
                        B(arguments, "exact_name")
                    ),
                    cancellationToken
                );

            case "windows_set_toggle":
                return await Task.Run(
                    () => WindowsControlTools.SetToggleState(
                        S(arguments, "window_title"),
                        S(arguments, "control_type"),
                        S(arguments, "control_name"),
                        B(arguments, "exact_name"),
                        B(arguments, "checked")
                    ),
                    cancellationToken
                );

            case "windows_get_toggle":
                return await Task.Run(
                    () => WindowsControlTools.GetToggleState(
                        S(arguments, "window_title"),
                        S(arguments, "control_type"),
                        S(arguments, "control_name"),
                        B(arguments, "exact_name")
                    ),
                    cancellationToken
                );

            case "windows_select_control":
                return await Task.Run(
                    () => WindowsControlTools.SelectControl(
                        S(arguments, "window_title"),
                        S(arguments, "control_type"),
                        S(arguments, "control_name"),
                        B(arguments, "exact_name")
                    ),
                    cancellationToken
                );

            case "windows_set_expanded":
                return await Task.Run(
                    () => WindowsControlTools.SetExpandedState(
                        S(arguments, "window_title"),
                        S(arguments, "control_type"),
                        S(arguments, "control_name"),
                        B(arguments, "exact_name"),
                        B(arguments, "expanded")
                    ),
                    cancellationToken
                );

            case "windows_focus_control":
                return await Task.Run(
                    () => WindowsControlTools.FocusControl(
                        S(arguments, "window_title"),
                        S(arguments, "control_type"),
                        S(arguments, "control_name"),
                        B(arguments, "exact_name")
                    ),
                    cancellationToken
                );

            case "windows_wait_for_control":
                return await Task.Run(
                    () => WindowsControlTools.WaitForControl(
                        S(arguments, "window_title"),
                        S(arguments, "control_type"),
                        S(arguments, "control_name"),
                        B(arguments, "exact_name"),
                        I(arguments, "timeout_seconds")
                    ),
                    cancellationToken
                );

            case "type_text":
                return await Task.Run(
                    () => WindowsUiTools.TypeText(
                        S(arguments, "window_title"),
                        S(arguments, "text")
                    ),
                    cancellationToken
                );

            case "press_key":
                return await Task.Run(
                    () => WindowsInputTools.PressKey(
                        S(arguments, "keys")
                    ),
                    cancellationToken
                );

            case "windows_replace_document_text":
                return await ReplaceDocumentTextAsync(
                    S(arguments, "window_title"),
                    S(arguments, "text"),
                    cancellationToken
                );

            case "windows_read_document_text":
                return await ReadDocumentTextAsync(
                    S(arguments, "window_title"),
                    S(arguments, "expected_text"),
                    cancellationToken
                );

            case "windows_save_document":
                return await SaveDocumentAsync(
                    S(arguments, "window_title"),
                    cancellationToken
                );

            case "save_active_document_as_desktop_file":
                return SaveActiveDocumentSafely(
                    S(arguments, "relative_path"),
                    userTask
                );

            case "start_browser":
                return await BrowserTools.StartBrowserAsync();

            case "browser_session_info":
                return await BrowserTools.GetSessionInfoAsync();

            case "browser_navigate":
                return await BrowserTools.NavigateAsync(S(arguments, "url"));

            case "browser_get_page_info":
                return await BrowserTools.GetPageInfoAsync();

            case "browser_read_page":
                return await BrowserTools.ReadPageTextAsync();

            case "browser_list_links":
                return await BrowserTools.ListLinksAsync();

            case "browser_list_elements":
                return await BrowserTools.ListInteractiveElementsAsync();

            case "browser_find":
                return await BrowserTools.FindElementsAsync(
                    S(arguments, "locator_type"),
                    S(arguments, "query")
                );

            case "browser_role_find":
                return await BrowserTools.FindByRoleAsync(
                    S(arguments, "role"),
                    S(arguments, "name"),
                    B(arguments, "exact")
                );

            case "browser_role_click":
                return await BrowserTools.ClickRoleAsync(
                    S(arguments, "role"),
                    S(arguments, "name"),
                    B(arguments, "exact")
                );

            case "browser_role_fill":
                return await BrowserTools.FillRoleAsync(
                    S(arguments, "role"),
                    S(arguments, "name"),
                    B(arguments, "exact"),
                    S(arguments, "text")
                );

            case "browser_role_wait":
                return await BrowserTools.WaitForRoleAsync(
                    S(arguments, "role"),
                    S(arguments, "name"),
                    B(arguments, "exact"),
                    S(arguments, "state"),
                    I(arguments, "timeout_seconds")
                );

            case "browser_role_get_text":
                return await BrowserTools.GetRoleTextAsync(
                    S(arguments, "role"),
                    S(arguments, "name"),
                    B(arguments, "exact")
                );

            case "browser_exact_text":
                return await BrowserTools.FindElementsAsync(
                    "exact_text",
                    S(arguments, "text")
                );

            case "browser_visual_inspect":
                return await BrowserVisionTools.InspectCurrentPageAsync(
                    S(arguments, "question"),
                    B(arguments, "full_page"),
                    cancellationToken
                );

            case "browser_screenshot":
                return await BrowserTools.ScreenshotAsync(
                    S(arguments, "relative_path"),
                    B(arguments, "full_page")
                );

            case "browser_list_screenshots":
                return BrowserTools.ListScreenshots();

            case "browser_get_viewport":
                return await BrowserTools.GetViewportInfoAsync();

            case "browser_element_box":
                return await BrowserTools.GetElementBoxAsync(
                    S(arguments, "locator_type"),
                    S(arguments, "query")
                );

            case "browser_mouse_move":
                return await BrowserTools.MouseMoveAsync(
                    I(arguments, "x"),
                    I(arguments, "y")
                );

            case "browser_mouse_click":
                return await BrowserTools.MouseClickAsync(
                    I(arguments, "x"),
                    I(arguments, "y")
                );

            case "browser_mouse_double_click":
                return await BrowserTools.MouseDoubleClickAsync(
                    I(arguments, "x"),
                    I(arguments, "y")
                );

            case "browser_get_text":
                return await BrowserTools.GetElementTextAsync(
                    S(arguments, "locator_type"),
                    S(arguments, "query")
                );

            case "browser_get_attribute":
                return await BrowserTools.GetAttributeAsync(
                    S(arguments, "locator_type"),
                    S(arguments, "query"),
                    S(arguments, "attribute_name")
                );

            case "browser_get_value":
                return await BrowserTools.GetValueAsync(
                    S(arguments, "locator_type"),
                    S(arguments, "query")
                );

            case "browser_is_visible":
                return await BrowserTools.IsVisibleAsync(
                    S(arguments, "locator_type"),
                    S(arguments, "query")
                );

            case "browser_wait":
                return await BrowserTools.WaitForElementAsync(
                    S(arguments, "locator_type"),
                    S(arguments, "query"),
                    S(arguments, "state"),
                    I(arguments, "timeout_seconds")
                );

            case "browser_wait_for_url":
                return await BrowserTools.WaitForUrlAsync(
                    S(arguments, "url_pattern"),
                    I(arguments, "timeout_seconds")
                );

            case "browser_wait_for_text":
                return await BrowserTools.WaitForTextAsync(
                    S(arguments, "text"),
                    B(arguments, "exact"),
                    I(arguments, "timeout_seconds")
                );

            case "browser_click":
                return await BrowserTools.ClickAsync(
                    S(arguments, "locator_type"),
                    S(arguments, "query")
                );

            case "browser_fill":
                return await BrowserTools.FillAsync(
                    S(arguments, "locator_type"),
                    S(arguments, "query"),
                    S(arguments, "text")
                );

            case "browser_type":
                return await BrowserTools.TypeAsync(
                    S(arguments, "locator_type"),
                    S(arguments, "query"),
                    S(arguments, "text")
                );

            case "browser_press":
                return await BrowserTools.PressAsync(
                    S(arguments, "locator_type"),
                    S(arguments, "query"),
                    S(arguments, "key")
                );

            case "browser_page_key":
                return await BrowserTools.PressPageKeyAsync(
                    S(arguments, "key")
                );

            case "browser_scroll":
                return await BrowserTools.ScrollPageAsync(
                    I(arguments, "delta_y")
                );

            case "browser_scroll_to":
                return await BrowserTools.ScrollToElementAsync(
                    S(arguments, "locator_type"),
                    S(arguments, "query")
                );

            case "browser_set_checked":
                return await BrowserTools.SetCheckedAsync(
                    S(arguments, "locator_type"),
                    S(arguments, "query"),
                    B(arguments, "checked")
                );

            case "browser_get_checked":
                return await BrowserTools.GetCheckedStateAsync(
                    S(arguments, "locator_type"),
                    S(arguments, "query")
                );

            case "browser_select_option":
                return await BrowserTools.SelectOptionAsync(
                    S(arguments, "locator_type"),
                    S(arguments, "query"),
                    S(arguments, "selection_type"),
                    S(arguments, "selection")
                );

            case "browser_upload_desktop_file":
                return await BrowserTools.UploadDesktopFileAsync(
                    S(arguments, "locator_type"),
                    S(arguments, "query"),
                    S(arguments, "relative_path")
                );

            case "browser_download":
                return await BrowserTools.DownloadByClickAsync(
                    S(arguments, "locator_type"),
                    S(arguments, "query"),
                    S(arguments, "preferred_relative_path")
                );

            case "browser_list_downloads":
                return BrowserTools.ListDownloads();

            case "browser_back":
                return await BrowserTools.BackAsync();

            case "browser_forward":
                return await BrowserTools.ForwardAsync();

            case "browser_reload":
                return await BrowserTools.ReloadAsync();

            case "browser_new_tab":
            {
                string url = S(arguments, "url");

                return string.IsNullOrWhiteSpace(url)
                    ? await BrowserTools.NewTabAsync()
                    : await BrowserTools.NewTabAsync(url);
            }

            case "browser_list_tabs":
                return await BrowserTools.ListTabsAsync();

            case "browser_switch_tab":
                return await BrowserTools.SwitchTabAsync(
                    I(arguments, "tab_number")
                );

            case "browser_close_tab":
                return await BrowserTools.CloseTabAsync(
                    I(arguments, "tab_number")
                );

            case "stop_browser":
                return await BrowserTools.StopBrowserAsync();

            default:
                return $"ERROR: Unknown tool '{call.FunctionName}'.";
        }
    }

    private string GetRuntimeInfo()
    {
        return
            $"Operator AI {OperatorSettings.ProductVersion}\n" +
            $"Model: {_settings.Model}\n" +
            $"SafeMode: {_settings.SafeMode}\n" +
            $"Keyboard fallback: {_settings.AllowKeyboardFallback}\n" +
            $"Browser coordinate fallback: {_settings.AllowBrowserCoordinateFallback}\n" +
            $"Task timeout: {_settings.TaskTimeoutMinutes} minutes\n" +
            $"Planning steps: {_settings.MaximumPlanningSteps}\n" +
            $"Settings: {_settings.SettingsPath}\n" +
            $"History: {_settings.HistoryDirectory}\n" +
            "Capabilities: browser automation, visual fallback, native Windows UI Automation, robust multi-window control, real Notepad editing/saving, Desktop file workflows, File Explorer targeting.";
    }

    private string CreateDesktopFileSafely(
        string relativePath,
        string content,
        string userTask)
    {
        try
        {
            string fullPath = ResolveDesktopPath(relativePath);

            if (
                File.Exists(fullPath)
                &&
                _settings.SafeMode
                &&
                !TaskAllowsOverwrite(userTask)
            )
            {
                return
                    $"BLOCKED: Desktop file already exists at {fullPath}. " +
                    "Ask explicitly to overwrite or replace it.";
            }

            return WindowsTools.CreateDesktopFile(relativePath, content);
        }
        catch (Exception ex)
        {
            return $"ERROR: {ex.Message}";
        }
    }

    private string SaveActiveDocumentSafely(
        string relativePath,
        string userTask)
    {
        try
        {
            string fullPath = ResolveDesktopPath(relativePath);

            if (
                File.Exists(fullPath)
                &&
                _settings.SafeMode
                &&
                !TaskAllowsOverwrite(userTask)
            )
            {
                return
                    $"BLOCKED: Save target already exists at {fullPath}. " +
                    "Ask explicitly to overwrite or replace it before using Save As.";
            }

            return WindowsWorkflowTools.SaveActiveDocumentAsDesktopFile(relativePath);
        }
        catch (Exception ex)
        {
            return $"ERROR: Save As workflow failed: {ex.Message}";
        }
    }

    private static string OpenDesktopFolder(string relativePath)
    {
        try
        {
            string fullPath = ResolveDesktopPath(relativePath);

            if (!Directory.Exists(fullPath))
            {
                return $"NOT_FOUND: Desktop folder does not exist: {fullPath}";
            }

            Process.Start(
                new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{fullPath}\"",
                    UseShellExecute = true
                }
            );

            return $"SUCCESS: Opened File Explorer at {fullPath}";
        }
        catch (Exception ex)
        {
            return $"ERROR: Could not open File Explorer: {ex.Message}";
        }
    }

    private static string OpenDesktopFileInNotepad(string relativePath)
    {
        try
        {
            string fullPath = ResolveDesktopPath(relativePath);

            if (!File.Exists(fullPath))
            {
                return $"NOT_FOUND: Desktop file does not exist: {fullPath}";
            }

            Process.Start(
                new ProcessStartInfo
                {
                    FileName = "notepad.exe",
                    Arguments = $"\"{fullPath}\"",
                    UseShellExecute = true
                }
            );

            return $"SUCCESS: Opened {fullPath} in Notepad.";
        }
        catch (Exception ex)
        {
            return $"ERROR: Could not open Desktop file in Notepad: {ex.Message}";
        }
    }

    private async Task<string> ReplaceDocumentTextAsync(
        string windowTitle,
        string text,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(windowTitle))
        {
            return "ERROR: Window title cannot be empty.";
        }

        string foreground =
            await Task.Run(
                () => WindowsWindowTools.VerifyForegroundWindow(windowTitle),
                cancellationToken
            );

        if (AgentRunGuard.IsFailure(foreground))
        {
            return
                "ERROR: Refusing to edit because the requested Notepad window is not foreground.\n" +
                foreground;
        }

        (string Type, string Name, bool Exact)[] candidates =
        [
            ("edit", "Text editor", false),
            ("document", "Text editor", false),
            ("edit", "Text Editor", false),
            ("document", "Text Editor", false),
            ("edit", "", false),
            ("document", "", false)
        ];

        string lastNativeResult = "";

        foreach ((string Type, string Name, bool Exact) candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string setResult =
                await Task.Run(
                    () => WindowsControlTools.SetControlValue(
                        "__FOREGROUND__",
                        candidate.Type,
                        candidate.Name,
                        candidate.Exact,
                        text
                    ),
                    cancellationToken
                );

            lastNativeResult = setResult;

            if (!AgentRunGuard.IsFailure(setResult))
            {
                string verify =
                    await ReadDocumentTextAsync(
                        windowTitle,
                        text,
                        cancellationToken
                    );

                if (!AgentRunGuard.IsFailure(verify))
                {
                    return
                        "SUCCESS: Real Notepad document replaced using native ValuePattern.\n" +
                        setResult;
                }
            }
        }

        if (!_settings.AllowKeyboardFallback)
        {
            return
                "BLOCKED: Native ValuePattern did not work and keyboard fallback is disabled.\n" +
                lastNativeResult;
        }

        string selectAll =
            await Task.Run(
                () => WindowsInputTools.PressKey("CTRL+A"),
                cancellationToken
            );

        if (AgentRunGuard.IsFailure(selectAll))
        {
            return
                "ERROR: Native ValuePattern failed and CTRL+A fallback failed.\n" +
                lastNativeResult;
        }

        await Task.Delay(150, cancellationToken);

        string typeResult =
            await Task.Run(
                () => WindowsUiTools.TypeText(windowTitle, text),
                cancellationToken
            );

        if (AgentRunGuard.IsFailure(typeResult))
        {
            return
                "ERROR: Native ValuePattern and keyboard text fallback both failed.\n" +
                typeResult;
        }

        await Task.Delay(250, cancellationToken);

        string fallbackVerification =
            await ReadDocumentTextAsync(
                windowTitle,
                text,
                cancellationToken
            );

        if (AgentRunGuard.IsFailure(fallbackVerification))
        {
            return
                "ERROR: Keyboard fallback ran but final UI verification failed.\n" +
                fallbackVerification;
        }

        return
            "SUCCESS: Real Notepad document replaced using verified keyboard fallback.\n" +
            typeResult;
    }

    private static async Task<string> ReadDocumentTextAsync(
        string windowTitle,
        string expectedText,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(windowTitle))
        {
            return "ERROR: Window title cannot be empty.";
        }

        string foreground =
            await Task.Run(
                () => WindowsWindowTools.VerifyForegroundWindow(windowTitle),
                cancellationToken
            );

        if (AgentRunGuard.IsFailure(foreground))
        {
            return
                "ERROR: Refusing to read document because the requested window is not foreground.\n" +
                foreground;
        }

        (string Type, string Name, bool Exact)[] candidates =
        [
            ("edit", "Text editor", false),
            ("document", "Text editor", false),
            ("edit", "Text Editor", false),
            ("document", "Text Editor", false),
            ("document", "", false),
            ("edit", "", false)
        ];

        string lastResult = "";

        foreach ((string Type, string Name, bool Exact) candidate in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string result =
                await Task.Run(
                    () => WindowsControlTools.GetControlValue(
                        "__FOREGROUND__",
                        candidate.Type,
                        candidate.Name,
                        candidate.Exact
                    ),
                    cancellationToken
                );

            lastResult = result;

            if (AgentRunGuard.IsFailure(result))
            {
                continue;
            }

            if (
                string.IsNullOrEmpty(expectedText)
                || result.Contains(expectedText, StringComparison.Ordinal)
            )
            {
                return
                    "SUCCESS: Real Notepad document text read and verified.\n" +
                    result;
            }
        }

        return
            "NOT_FOUND: Expected real Notepad document text was not found through UI Automation.\n" +
            $"Expected: {expectedText}\n" +
            $"Last result:\n{lastResult}";
    }

    private static async Task<string> SaveDocumentAsync(
        string windowTitle,
        CancellationToken cancellationToken)
    {
        string foreground =
            await Task.Run(
                () => WindowsWindowTools.VerifyForegroundWindow(windowTitle),
                cancellationToken
            );

        if (AgentRunGuard.IsFailure(foreground))
        {
            return
                "ERROR: Refusing to save because the requested window is not foreground.\n" +
                foreground;
        }

        return await Task.Run(
            () => WindowsInputTools.PressKey("CTRL+S"),
            cancellationToken
        );
    }

    private string LimitToolResult(string result)
    {
        int limit = _settings.MaximumToolResultCharacters;

        return result.Length <= limit
            ? result
            : result[..limit] +
              $"\n...[tool result truncated at {limit} characters]";
    }

    private static bool TaskAllowsOverwrite(string task)
    {
        string lower = task.ToLowerInvariant();

        string[] overwriteHints =
        [
            "overwrite",
            "replace the file",
            "replace file",
            "update the file",
            "update file",
            "modify the file",
            "edit the file",
            "change the file"
        ];

        return overwriteHints.Any(
            hint => lower.Contains(hint, StringComparison.Ordinal)
        );
    }

    private static string ResolveDesktopPath(string relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            throw new InvalidOperationException(
                "Desktop-relative path cannot be empty."
            );
        }

        string desktop = Path.GetFullPath(
            Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
        );

        string candidate = Path.GetFullPath(
            Path.Combine(desktop, relativePath)
        );

        bool insideDesktop =
            candidate.Equals(desktop, StringComparison.OrdinalIgnoreCase)
            || candidate.StartsWith(
                desktop + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase
            );

        if (!insideDesktop)
        {
            throw new InvalidOperationException(
                "Path is outside the allowed Desktop directory."
            );
        }

        return candidate;
    }

    private static JsonElement ParseArguments(
        FunctionCallResponseItem call)
    {
        try
        {
            return JsonDocument.Parse(
                call.FunctionArguments
            ).RootElement.Clone();
        }
        catch
        {
            return JsonDocument.Parse("{}").RootElement.Clone();
        }
    }

    private static string S(
        JsonElement arguments,
        string propertyName)
    {
        try
        {
            if (
                arguments.ValueKind == JsonValueKind.Object
                && arguments.TryGetProperty(propertyName, out JsonElement value)
                && value.ValueKind == JsonValueKind.String
            )
            {
                return value.GetString() ?? "";
            }
        }
        catch
        {
        }

        return "";
    }

    private static int I(
        JsonElement arguments,
        string propertyName)
    {
        try
        {
            if (
                arguments.ValueKind == JsonValueKind.Object
                && arguments.TryGetProperty(propertyName, out JsonElement value)
                && value.ValueKind == JsonValueKind.Number
                && value.TryGetInt32(out int result)
            )
            {
                return result;
            }
        }
        catch
        {
        }

        return 0;
    }

    private static bool B(
        JsonElement arguments,
        string propertyName)
    {
        try
        {
            if (
                arguments.ValueKind == JsonValueKind.Object
                && arguments.TryGetProperty(propertyName, out JsonElement value)
            )
            {
                return value.ValueKind switch
                {
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    _ => false
                };
            }
        }
        catch
        {
        }

        return false;
    }
}

#pragma warning restore OPENAI001
