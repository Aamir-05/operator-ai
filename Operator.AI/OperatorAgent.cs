using OpenAI.Responses;
using Operator.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Operator.AI;

#pragma warning disable OPENAI001

public sealed class OperatorAgent
{
    private readonly ResponsesClient _client;

    // =========================================================
    // APPLICATION TOOLS
    // =========================================================

    private static readonly FunctionTool OpenApplicationTool =
        ResponseTool.CreateFunctionTool(
            functionName: "open_application",
            functionDescription:
                "Open an approved Windows application. Currently allowed: notepad, calculator, edge.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "application": {
                      "type": "string"
                    }
                  },
                  "required": ["application"],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    // =========================================================
    // DESKTOP FILE TOOLS
    // =========================================================

    private static readonly FunctionTool CreateFolderTool =
        ResponseTool.CreateFunctionTool(
            functionName: "create_desktop_folder",
            functionDescription:
                "Create a folder on the Windows Desktop.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "folder_name": {
                      "type": "string"
                    }
                  },
                  "required": ["folder_name"],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool CreateFileTool =
        ResponseTool.CreateFunctionTool(
            functionName: "create_desktop_file",
            functionDescription:
                "Create or overwrite a text file inside the Windows Desktop. relative_path may include subfolders.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "relative_path": {
                      "type": "string"
                    },
                    "content": {
                      "type": "string"
                    }
                  },
                  "required": [
                    "relative_path",
                    "content"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool ReadFileTool =
        ResponseTool.CreateFunctionTool(
            functionName: "read_desktop_file",
            functionDescription:
                "Read a text file located inside the Windows Desktop.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "relative_path": {
                      "type": "string"
                    }
                  },
                  "required": ["relative_path"],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool FileExistsTool =
        ResponseTool.CreateFunctionTool(
            functionName: "desktop_file_exists",
            functionDescription:
                "Check whether a file exists inside the Windows Desktop.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "relative_path": {
                      "type": "string"
                    }
                  },
                  "required": ["relative_path"],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool ListDesktopTool =
        ResponseTool.CreateFunctionTool(
            functionName: "list_desktop",
            functionDescription:
                "List files and folders currently on the user's Desktop.",
            functionParameters: null,
            strictModeEnabled: false
        );

    // =========================================================
    // WINDOWS UI TOOLS
    // =========================================================

    private static readonly FunctionTool ListWindowsTool =
        ResponseTool.CreateFunctionTool(
            functionName: "list_windows",
            functionDescription:
                "List visible top-level Windows application windows.",
            functionParameters: null,
            strictModeEnabled: false
        );

    private static readonly FunctionTool InspectWindowTool =
        ResponseTool.CreateFunctionTool(
            functionName: "inspect_window",
            functionDescription:
                "Inspect UI controls inside a visible Windows application window.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "window_title": {
                      "type": "string"
                    }
                  },
                  "required": ["window_title"],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool FocusWindowTool =
        ResponseTool.CreateFunctionTool(
            functionName: "focus_window",
            functionDescription:
                "Focus a visible Windows application window.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "window_title": {
                      "type": "string"
                    }
                  },
                  "required": ["window_title"],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool TypeTextTool =
        ResponseTool.CreateFunctionTool(
            functionName: "type_text",
            functionDescription:
                "Type or paste text into the editable area of a visible Windows application window.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "window_title": {
                      "type": "string"
                    },
                    "text": {
                      "type": "string"
                    }
                  },
                  "required": [
                    "window_title",
                    "text"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    // =========================================================
    // KEYBOARD TOOL
    // =========================================================

    private static readonly FunctionTool PressKeyTool =
        ResponseTool.CreateFunctionTool(
            functionName: "press_key",
            functionDescription:
                "Press a Windows keyboard key or shortcut. Examples: CTRL+S, CTRL+A, CTRL+SHIFT+S, ALT+F4, ENTER, TAB, ESC, LEFT, RIGHT, UP, DOWN.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "keys": {
                      "type": "string"
                    }
                  },
                  "required": ["keys"],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    // =========================================================
    // RELIABLE SAVE WORKFLOW
    // =========================================================

    private static readonly FunctionTool SaveActiveDocumentTool =
        ResponseTool.CreateFunctionTool(
            functionName:
                "save_active_document_as_desktop_file",
            functionDescription:
                "Reliably save the currently active document to a file inside the Windows Desktop. Uses Save As, waits for Windows, retries when necessary, and verifies that the file was created. Prefer this tool over manually reproducing the Save As sequence.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "relative_path": {
                      "type": "string",
                      "description":
                        "Path relative to Desktop, for example operations-report.txt or Reports\\daily-report.txt"
                    }
                  },
                  "required": ["relative_path"],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public OperatorAgent()
    {
        string? apiKey =
            Environment.GetEnvironmentVariable(
                "OPENAI_API_KEY"
            );

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException(
                "OPENAI_API_KEY was not found."
            );
        }

        _client =
            new ResponsesClient(
                apiKey
            );
    }

    // =========================================================
    // VERSION 0.5E-3
    // RELIABLE AGENT LOOP
    // =========================================================

    public async Task<string> RunAsync(
        string task,
        Action<string>? log = null,
        CancellationToken cancellationToken = default)
    {
        List<ResponseItem> inputItems =
        [
            ResponseItem.CreateUserMessageItem(
                task
            )
        ];

        AgentRunGuard guard =
            new AgentRunGuard
            {
                MaximumRepeatedToolCalls = 3,
                MaximumConsecutiveErrors = 5
            };

        // -----------------------------------------------------
        // Overall task timeout
        // -----------------------------------------------------

        using CancellationTokenSource timeoutSource =
            CancellationTokenSource
                .CreateLinkedTokenSource(
                    cancellationToken
                );

        timeoutSource.CancelAfter(
            TimeSpan.FromMinutes(3)
        );

        CancellationToken token =
            timeoutSource.Token;

        try
        {
            for (int step = 1;
                 step <= 30;
                 step++)
            {
                token.ThrowIfCancellationRequested();

                log?.Invoke(
                    $"[PLAN] Planning step {step}..."
                );

                // =================================================
                // CREATE MODEL REQUEST
                // =================================================

                CreateResponseOptions options =
                    new(
                        "gpt-5.6",
                        inputItems
                    )
                    {
                        Instructions =
                            """
                            You are Operator AI, a Windows automation agent.

                            Your job is to complete real tasks on the user's
                            Windows computer using the available tools.

                            GENERAL RULES

                            - Use tools for real computer actions.
                            - Never claim an action happened unless a tool confirms it.
                            - Verify important results whenever practical.
                            - Never invent a successful result.
                            - Stay within permissions exposed by available tools.
                            - Do not repeat failed actions indefinitely.
                            - Prefer reliable high-level tools over long fragile
                              sequences of individual keystrokes.

                            WINDOWS APPLICATION RULES

                            - Use open_application to launch supported applications.
                            - After opening an application, use list_windows when
                              necessary to discover its actual window title.
                            - Use focus_window before interacting with an application.
                            - Use inspect_window when you need to understand the
                              current Windows UI state.
                            - Use type_text for normal text entry.

                            KEYBOARD RULES

                            - Use press_key for shortcuts such as:
                              CTRL+A
                              CTRL+S
                              CTRL+SHIFT+S
                              ENTER
                              TAB
                              ESC
                              ALT+F4

                            - If a keyboard action changes the UI, inspect the
                              resulting state when necessary.

                            SAVING RULES

                            - When the user asks to save an active document to
                              Desktop, prefer
                              save_active_document_as_desktop_file.

                            - Do not manually reproduce the Save As workflow with
                              many press_key calls when the reliable save workflow
                              can perform the task.

                            - The save workflow accepts paths relative to Desktop.

                            - After saving, verify the requested file exists.

                            - If the user requests content verification, read the
                              saved file back.

                            FILE RULES

                            - Use desktop_file_exists to verify files.
                            - Use read_desktop_file to verify file contents.
                            - Use create_desktop_file when direct file creation is
                              appropriate.

                            - If the user explicitly wants an application such as
                              Notepad to create/save the document, use the application
                              UI instead of directly creating the file.

                            RECOVERY RULES

                            - ERROR, NOT_FOUND, and BLOCKED results mean the attempted
                              action did not succeed.

                            - Do not immediately give up after one recoverable error.

                            - Inspect the current state and try a reasonable
                              alternative strategy.

                            - If a tool reports that a repeated call was blocked,
                              do not make the exact same call again.

                            - Change the arguments or choose another strategy.

                            - Never enter an infinite loop.

                            - Do not repeatedly perform:
                              inspect -> inspect -> inspect
                              focus -> focus -> focus
                              type -> type -> type
                              or the same failed action without progress.

                            - After several unsuccessful recovery attempts,
                              stop safely and explain the unresolved problem.

                            COMPLETION RULES

                            - Finish only when the important requested outcome
                              has been confirmed.

                            - File creation and file saving tasks require
                              verification.

                            - If content verification was requested, the file
                              must be read back.

                            - If the task cannot be completed, clearly identify
                              the action that failed.
                            """
                    };

                // =================================================
                // REGISTER TOOLS
                // =================================================

                options.Tools.Add(
                    OpenApplicationTool
                );

                options.Tools.Add(
                    CreateFolderTool
                );

                options.Tools.Add(
                    CreateFileTool
                );

                options.Tools.Add(
                    ReadFileTool
                );

                options.Tools.Add(
                    FileExistsTool
                );

                options.Tools.Add(
                    ListDesktopTool
                );

                options.Tools.Add(
                    ListWindowsTool
                );

                options.Tools.Add(
                    InspectWindowTool
                );

                options.Tools.Add(
                    FocusWindowTool
                );

                options.Tools.Add(
                    TypeTextTool
                );

                options.Tools.Add(
                    PressKeyTool
                );

                options.Tools.Add(
                    SaveActiveDocumentTool
                );

                // =================================================
                // ASK GPT FOR NEXT ACTION
                // =================================================

                ResponseResult response =
                    await _client.CreateResponseAsync(
                        options,
                        token
                    );

                token.ThrowIfCancellationRequested();

                inputItems.AddRange(
                    response.OutputItems
                );

                bool toolCalled =
                    false;

                // =================================================
                // PROCESS TOOL CALLS
                // =================================================

                foreach (
                    FunctionCallResponseItem functionCall
                    in response.OutputItems
                        .OfType<FunctionCallResponseItem>())
                {
                    token.ThrowIfCancellationRequested();

                    toolCalled =
                        true;

                    // IMPORTANT FIX:
                    // FunctionArguments is BinaryData.
                    // Convert it to a string for AgentRunGuard.
                    string argumentsText =
                        functionCall
                            .FunctionArguments
                            .ToString();

                    string result;

                    // =============================================
                    // LOOP PROTECTION
                    // =============================================

                    if (!guard.CanExecuteTool(
                            functionCall.FunctionName,
                            argumentsText,
                            out string blockReason))
                    {
                        result =
                            $"BLOCKED: {blockReason}";

                        log?.Invoke(
                            $"[RETRY] {blockReason}"
                        );
                    }
                    else
                    {
                        log?.Invoke(
                            $"[ACTION] {functionCall.FunctionName}"
                        );

                        result =
                            ExecuteTool(
                                functionCall,
                                null
                            );
                    }

                    token.ThrowIfCancellationRequested();

                    // =============================================
                    // REGISTER RESULT
                    // =============================================

                    guard.RegisterResult(
                        result
                    );

                    if (AgentRunGuard.IsFailure(
                            result))
                    {
                        log?.Invoke(
                            $"[ERROR] {result}"
                        );
                    }
                    else
                    {
                        log?.Invoke(
                            $"[SUCCESS] {result}"
                        );
                    }

                    // =============================================
                    // RETURN TOOL RESULT TO GPT
                    // =============================================

                    inputItems.Add(
                        new FunctionCallOutputResponseItem(
                            functionCall.CallId,
                            result
                        )
                    );

                    // =============================================
                    // TOO MANY CONSECUTIVE ERRORS
                    // =============================================

                    if (guard.TooManyErrors(
                            out string failureReason))
                    {
                        log?.Invoke(
                            $"[ERROR] {failureReason}"
                        );

                        return failureReason;
                    }
                }

                // =================================================
                // GPT FINISHED WITHOUT ANOTHER TOOL CALL
                // =================================================

                if (!toolCalled)
                {
                    string finalAnswer =
                        response.GetOutputText();

                    if (string.IsNullOrWhiteSpace(
                            finalAnswer))
                    {
                        finalAnswer =
                            "Task completed.";
                    }

                    log?.Invoke(
                        $"[COMPLETE] {finalAnswer}"
                    );

                    return finalAnswer;
                }
            }

            // =====================================================
            // PLANNING LIMIT
            // =====================================================

            string stepLimitMessage =
                "Agent stopped because the maximum number of planning steps was reached.";

            log?.Invoke(
                $"[ERROR] {stepLimitMessage}"
            );

            return stepLimitMessage;
        }

        // =========================================================
        // CANCELLATION / TIMEOUT
        // =========================================================

        catch (OperationCanceledException)
        {
            if (cancellationToken
                .IsCancellationRequested)
            {
                string message =
                    "CANCELLED: Task stopped by the user.";

                log?.Invoke(
                    $"[CANCELLED] {message}"
                );

                return message;
            }

            string timeoutMessage =
                "TIMEOUT: Task exceeded the 3-minute limit and was stopped.";

            log?.Invoke(
                $"[TIMEOUT] {timeoutMessage}"
            );

            return timeoutMessage;
        }

        // =========================================================
        // UNEXPECTED FAILURE
        // =========================================================

        catch (Exception ex)
        {
            string error =
                $"Agent failure: {ex.Message}";

            log?.Invoke(
                $"[ERROR] {error}"
            );

            return error;
        }
    }

    // =========================================================
    // TOOL EXECUTION
    // =========================================================

    private static string ExecuteTool(
        FunctionCallResponseItem call,
        Action<string>? log)
    {
        JsonElement arguments;

        try
        {
            arguments =
                JsonDocument
                    .Parse(
                        call.FunctionArguments
                    )
                    .RootElement
                    .Clone();
        }
        catch
        {
            arguments =
                JsonDocument
                    .Parse("{}")
                    .RootElement
                    .Clone();
        }

        switch (call.FunctionName)
        {
            // =================================================
            // OPEN APPLICATION
            // =================================================

            case "open_application":
                {
                    string app =
                        GetStringArgument(
                            arguments,
                            "application"
                        );

                    log?.Invoke(
                        $"AI requested: Open application '{app}'"
                    );

                    string result =
                        WindowsTools.OpenApplication(
                            app
                        );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // CREATE DESKTOP FOLDER
            // =================================================

            case "create_desktop_folder":
                {
                    string folder =
                        GetStringArgument(
                            arguments,
                            "folder_name"
                        );

                    log?.Invoke(
                        $"AI requested: Create folder '{folder}'"
                    );

                    string result =
                        WindowsTools.CreateDesktopFolder(
                            folder
                        );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // CREATE DESKTOP FILE
            // =================================================

            case "create_desktop_file":
                {
                    string path =
                        GetStringArgument(
                            arguments,
                            "relative_path"
                        );

                    string content =
                        GetStringArgument(
                            arguments,
                            "content"
                        );

                    log?.Invoke(
                        $"AI requested: Create file '{path}'"
                    );

                    string result =
                        WindowsTools.CreateDesktopFile(
                            path,
                            content
                        );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // READ DESKTOP FILE
            // =================================================

            case "read_desktop_file":
                {
                    string path =
                        GetStringArgument(
                            arguments,
                            "relative_path"
                        );

                    log?.Invoke(
                        $"AI requested: Read file '{path}'"
                    );

                    string result =
                        WindowsTools.ReadDesktopFile(
                            path
                        );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // VERIFY DESKTOP FILE
            // =================================================

            case "desktop_file_exists":
                {
                    string path =
                        GetStringArgument(
                            arguments,
                            "relative_path"
                        );

                    log?.Invoke(
                        $"AI requested: Verify file '{path}'"
                    );

                    string result =
                        WindowsTools.DesktopFileExists(
                            path
                        );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // LIST DESKTOP
            // =================================================

            case "list_desktop":
                {
                    log?.Invoke(
                        "AI requested: List Desktop"
                    );

                    string result =
                        WindowsTools.ListDesktop();

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // LIST WINDOWS
            // =================================================

            case "list_windows":
                {
                    log?.Invoke(
                        "AI requested: List Windows"
                    );

                    string result =
                        WindowsUiTools.ListWindows();

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // INSPECT WINDOW
            // =================================================

            case "inspect_window":
                {
                    string title =
                        GetStringArgument(
                            arguments,
                            "window_title"
                        );

                    log?.Invoke(
                        $"AI requested: Inspect window '{title}'"
                    );

                    string result =
                        WindowsUiTools.InspectWindow(
                            title
                        );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // FOCUS WINDOW
            // =================================================

            case "focus_window":
                {
                    string title =
                        GetStringArgument(
                            arguments,
                            "window_title"
                        );

                    log?.Invoke(
                        $"AI requested: Focus window '{title}'"
                    );

                    string result =
                        WindowsUiTools.FocusWindow(
                            title
                        );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // TYPE TEXT
            // =================================================

            case "type_text":
                {
                    string title =
                        GetStringArgument(
                            arguments,
                            "window_title"
                        );

                    string text =
                        GetStringArgument(
                            arguments,
                            "text"
                        );

                    log?.Invoke(
                        $"AI requested: Type text into '{title}'"
                    );

                    string result =
                        WindowsUiTools.TypeText(
                            title,
                            text
                        );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // PRESS KEY
            // =================================================

            case "press_key":
                {
                    string keys =
                        GetStringArgument(
                            arguments,
                            "keys"
                        );

                    log?.Invoke(
                        $"AI requested: Press '{keys}'"
                    );

                    string result =
                        WindowsInputTools.PressKey(
                            keys
                        );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // RELIABLE SAVE WORKFLOW
            // =================================================

            case "save_active_document_as_desktop_file":
                {
                    string path =
                        GetStringArgument(
                            arguments,
                            "relative_path"
                        );

                    log?.Invoke(
                        $"AI requested: Save active document as '{path}'"
                    );

                    log?.Invoke(
                        "Running reliable Save As workflow..."
                    );

                    string result =
                        WindowsWorkflowTools
                            .SaveActiveDocumentAsDesktopFile(
                                path
                            );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // UNKNOWN TOOL
            // =================================================

            default:
                {
                    string result =
                        $"ERROR: Unknown tool '{call.FunctionName}'.";

                    log?.Invoke(result);

                    return result;
                }
        }
    }

    // =========================================================
    // SAFE JSON ARGUMENT READER
    // =========================================================

    private static string GetStringArgument(
        JsonElement arguments,
        string propertyName)
    {
        try
        {
            if (arguments.ValueKind !=
                JsonValueKind.Object)
            {
                return "";
            }

            if (!arguments.TryGetProperty(
                    propertyName,
                    out JsonElement value))
            {
                return "";
            }

            return value.GetString()
                ?? "";
        }
        catch
        {
            return "";
        }
    }
}

#pragma warning restore OPENAI001