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

public sealed class RealWriteAgent07D
{
    private readonly ResponsesClient _client;

    // =========================================================
    // TOP-LEVEL WINDOWS
    // =========================================================

    private static readonly FunctionTool ListWindowsTool =
        ResponseTool.CreateFunctionTool(
            functionName: "windows_list_windows",
            functionDescription:
                "List visible titled top-level Windows using the robust Win32 window discovery layer.",
            functionParameters: null,
            strictModeEnabled: false
        );

    private static readonly FunctionTool WaitForWindowTool =
        ResponseTool.CreateFunctionTool(
            functionName: "windows_wait_for_top_window",
            functionDescription:
                "Wait for a top-level Windows window whose title contains the supplied title.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "window_title": {
                      "type": "string"
                    },
                    "timeout_seconds": {
                      "type": "integer"
                    }
                  },
                  "required": [
                    "window_title",
                    "timeout_seconds"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool FocusWindowTool =
        ResponseTool.CreateFunctionTool(
            functionName: "windows_focus_window",
            functionDescription:
                "Bring a visible top-level Windows window to the foreground using its title.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "window_title": {
                      "type": "string"
                    }
                  },
                  "required": [
                    "window_title"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool VerifyForegroundTool =
        ResponseTool.CreateFunctionTool(
            functionName: "windows_verify_foreground",
            functionDescription:
                "Verify that the current foreground Windows window matches the expected title.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "expected_title": {
                      "type": "string"
                    }
                  },
                  "required": [
                    "expected_title"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    // =========================================================
    // NATIVE UI INSPECTION
    // =========================================================

    private static readonly FunctionTool ListControlsTool =
        ResponseTool.CreateFunctionTool(
            functionName: "windows_list_controls",
            functionDescription:
                "List native UI Automation controls inside the current foreground window.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "maximum_controls": {
                      "type": "integer"
                    }
                  },
                  "required": [
                    "maximum_controls"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool FindControlAnyTool =
        ResponseTool.CreateFunctionTool(
            functionName: "windows_find_control_any",
            functionDescription:
                "Find a native control in the current foreground window by accessible Name or AutomationId without requiring a control type.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "control_query": {
                      "type": "string"
                    }
                  },
                  "required": [
                    "control_query"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    // =========================================================
    // REAL NOTEPAD DOCUMENT
    //
    // These tools deliberately wrap the verified 0.7D strategy:
    //
    // native ValuePattern first
    // verified text fallback second
    // =========================================================

    private static readonly FunctionTool ReplaceDocumentTextTool =
        ResponseTool.CreateFunctionTool(
            functionName: "windows_replace_document_text",
            functionDescription:
                "Replace the entire text of the currently targeted real Notepad document. Uses native UI Automation when available and a verified text-input fallback only when required.",
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

    private static readonly FunctionTool ReadDocumentTextTool =
        ResponseTool.CreateFunctionTool(
            functionName: "windows_read_document_text",
            functionDescription:
                "Read and verify text from the current real Notepad document through native UI Automation.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "expected_text": {
                      "type": "string"
                    }
                  },
                  "required": [
                    "expected_text"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool SaveDocumentTool =
        ResponseTool.CreateFunctionTool(
            functionName: "windows_save_document",
            functionDescription:
                "Save the currently active existing document using Ctrl+S. Use only after the correct Notepad window has been focused and verified.",
            functionParameters: null,
            strictModeEnabled: false
        );

    // =========================================================
    // FILESYSTEM
    // =========================================================

    private static readonly FunctionTool FileExistsTool =
        ResponseTool.CreateFunctionTool(
            functionName: "desktop_file_exists",
            functionDescription:
                "Check whether a file exists under the Windows Desktop.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "relative_path": {
                      "type": "string"
                    }
                  },
                  "required": [
                    "relative_path"
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
                "Read the exact contents of a text file under the Windows Desktop.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "relative_path": {
                      "type": "string"
                    }
                  },
                  "required": [
                    "relative_path"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public RealWriteAgent07D()
    {
        string? apiKey =
            Environment.GetEnvironmentVariable(
                "OPENAI_API_KEY"
            );

        if (string.IsNullOrWhiteSpace(
                apiKey))
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
    // RUN
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

        using CancellationTokenSource timeoutSource =
            CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken
            );

        timeoutSource.CancelAfter(
            TimeSpan.FromMinutes(5)
        );

        CancellationToken token =
            timeoutSource.Token;

        try
        {
            for (int step = 1;
                 step <= 45;
                 step++)
            {
                token.ThrowIfCancellationRequested();

                log?.Invoke(
                    $"[PLAN] 0.7D autonomous planning step {step}..."
                );

                CreateResponseOptions options =
                    new(
                        "gpt-5.6",
                        inputItems
                    )
                    {
                        Instructions =
                            """
                            You are Operator AI performing a controlled
                            autonomous REAL Windows write/save workflow.

                            The workflow uses real Notepad and real File
                            Explorer.

                            Use only the tools provided in this run.

                            =================================================
                            WINDOW SAFETY
                            =================================================

                            Before interacting with a specific application:

                            1. identify the correct top-level window,
                            2. focus it,
                            3. verify it became foreground,
                            4. inspect its native controls.

                            Never manipulate a window whose identity has not
                            been verified.

                            =================================================
                            NOTEPAD EDITING
                            =================================================

                            The test file already exists and is already open
                            in real Notepad.

                            To replace its document contents use:

                            windows_replace_document_text

                            After editing, verify the exact text using:

                            windows_read_document_text

                            Do not claim the edit succeeded only because the
                            write action returned success.

                            =================================================
                            SAVE
                            =================================================

                            The file already has a real path.

                            After verifying the correct Notepad document and
                            its edited contents, use:

                            windows_save_document

                            Then independently confirm the actual saved file
                            using filesystem tools.

                            =================================================
                            FILESYSTEM
                            =================================================

                            After saving:

                            1. desktop_file_exists
                            2. read_desktop_file

                            Verify the exact expected content is present.

                            =================================================
                            FILE EXPLORER
                            =================================================

                            After filesystem verification, switch to the
                            dedicated File Explorer test folder.

                            Verify Explorer became foreground.

                            Inspect its controls.

                            Find the saved file through native UI Automation.

                            If the full filename is not exposed because
                            Explorer hides known extensions, try the filename
                            without its extension.

                            Do not open the file.

                            Do not rename it.

                            Do not move it.

                            Do not delete it.

                            =================================================
                            RETURN TO NOTEPAD
                            =================================================

                            Return to Notepad.

                            Verify it became foreground.

                            Read the Notepad document text again.

                            Confirm the final document still contains exactly
                            the requested text.

                            =================================================
                            PROHIBITED
                            =================================================

                            Do not use browser automation.

                            Do not use screen coordinates.

                            Do not delete or rename anything.

                            Do not modify files other than the dedicated test
                            document explicitly identified by the task.

                            Do not use arbitrary keyboard shortcuts.

                            windows_save_document is the only save action
                            allowed.

                            =================================================
                            COMPLETION
                            =================================================

                            Do not claim success until:

                            - the Notepad document was edited,
                            - the editor text was verified,
                            - the document was saved,
                            - the real filesystem content was verified,
                            - the saved file was located in Explorer,
                            - Notepad was revisited,
                            - final editor text was verified.
                            """
                    };

                options.Tools.Add(
                    ListWindowsTool
                );

                options.Tools.Add(
                    WaitForWindowTool
                );

                options.Tools.Add(
                    FocusWindowTool
                );

                options.Tools.Add(
                    VerifyForegroundTool
                );

                options.Tools.Add(
                    ListControlsTool
                );

                options.Tools.Add(
                    FindControlAnyTool
                );

                options.Tools.Add(
                    ReplaceDocumentTextTool
                );

                options.Tools.Add(
                    ReadDocumentTextTool
                );

                options.Tools.Add(
                    SaveDocumentTool
                );

                options.Tools.Add(
                    FileExistsTool
                );

                options.Tools.Add(
                    ReadFileTool
                );

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

                foreach (
                    FunctionCallResponseItem functionCall
                    in response.OutputItems
                        .OfType<FunctionCallResponseItem>())
                {
                    token.ThrowIfCancellationRequested();

                    toolCalled =
                        true;

                    string argumentsText =
                        functionCall
                            .FunctionArguments
                            .ToString();

                    string result;

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
                            await ExecuteToolAsync(
                                functionCall,
                                token
                            );
                    }

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

                    inputItems.Add(
                        new FunctionCallOutputResponseItem(
                            functionCall.CallId,
                            result
                        )
                    );

                    if (guard.TooManyErrors(
                            out string failureReason))
                    {
                        return
                            failureReason;
                    }
                }

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

                    return
                        finalAnswer;
                }
            }

            return
                "ERROR: Maximum 0.7D autonomous planning steps reached.";
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return
                    "CANCELLED: Autonomous real-write task stopped.";
            }

            return
                "TIMEOUT: Autonomous real-write task exceeded the time limit.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Autonomous real-write agent failure: {ex.Message}";
        }
    }

    // =========================================================
    // EXECUTE TOOL
    // =========================================================

    private static async Task<string> ExecuteToolAsync(
        FunctionCallResponseItem call,
        CancellationToken cancellationToken)
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
            // WINDOWS
            // =================================================

            case "windows_list_windows":
                return await Task.Run(
                    () =>
                        WindowsWindowTools.ListWindows(),
                    cancellationToken
                );

            case "windows_wait_for_top_window":
                return await Task.Run(
                    () =>
                        WindowsWindowTools.WaitForWindow(
                            GetStringArgument(
                                arguments,
                                "window_title"
                            ),
                            GetIntArgument(
                                arguments,
                                "timeout_seconds"
                            )
                        ),
                    cancellationToken
                );

            case "windows_focus_window":
                return await Task.Run(
                    () =>
                        WindowsWindowTools.FocusWindow(
                            GetStringArgument(
                                arguments,
                                "window_title"
                            )
                        ),
                    cancellationToken
                );

            case "windows_verify_foreground":
                return await Task.Run(
                    () =>
                        WindowsWindowTools.VerifyForegroundWindow(
                            GetStringArgument(
                                arguments,
                                "expected_title"
                            )
                        ),
                    cancellationToken
                );

            // =================================================
            // NATIVE CONTROLS
            // =================================================

            case "windows_list_controls":
                return await Task.Run(
                    () =>
                        WindowsControlTools.ListControls(
                            "__FOREGROUND__",
                            GetIntArgument(
                                arguments,
                                "maximum_controls"
                            )
                        ),
                    cancellationToken
                );

            case "windows_find_control_any":
                return await Task.Run(
                    () =>
                        WindowsControlTools.FindControlInfo(
                            "__FOREGROUND__",
                            GetStringArgument(
                                arguments,
                                "control_query"
                            )
                        ),
                    cancellationToken
                );

            // =================================================
            // NOTEPAD DOCUMENT
            // =================================================

            case "windows_replace_document_text":
                return await ReplaceDocumentTextAsync(
                    GetStringArgument(
                        arguments,
                        "window_title"
                    ),
                    GetStringArgument(
                        arguments,
                        "text"
                    ),
                    cancellationToken
                );

            case "windows_read_document_text":
                return await ReadDocumentTextAsync(
                    GetStringArgument(
                        arguments,
                        "expected_text"
                    ),
                    cancellationToken
                );

            case "windows_save_document":
                return await Task.Run(
                    () =>
                        WindowsInputTools.PressKey(
                            "CTRL+S"
                        ),
                    cancellationToken
                );

            // =================================================
            // FILESYSTEM
            // =================================================

            case "desktop_file_exists":
                return WindowsTools.DesktopFileExists(
                    GetStringArgument(
                        arguments,
                        "relative_path"
                    )
                );

            case "read_desktop_file":
                return WindowsTools.ReadDesktopFile(
                    GetStringArgument(
                        arguments,
                        "relative_path"
                    )
                );

            default:
                return
                    $"ERROR: Unknown 0.7D tool '{call.FunctionName}'.";
        }
    }

    // =========================================================
    // REPLACE REAL NOTEPAD DOCUMENT
    //
    // Native ValuePattern first.
    // Verified fallback second.
    // =========================================================

    private static async Task<string> ReplaceDocumentTextAsync(
        string windowTitle,
        string text,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(
                windowTitle))
        {
            return
                "ERROR: Window title cannot be empty.";
        }

        (string Type, string Name, bool Exact)[] candidates =
        [
            (
                "edit",
                "Text editor",
                false
            ),
            (
                "document",
                "Text editor",
                false
            ),
            (
                "edit",
                "Text Editor",
                false
            ),
            (
                "document",
                "Text Editor",
                false
            ),
            (
                "edit",
                "",
                false
            ),
            (
                "document",
                "",
                false
            )
        ];

        string lastNativeResult =
            "";

        foreach (
            (string Type, string Name, bool Exact) candidate
            in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string setResult =
                await Task.Run(
                    () =>
                        WindowsControlTools.SetControlValue(
                            "__FOREGROUND__",
                            candidate.Type,
                            candidate.Name,
                            candidate.Exact,
                            text
                        ),
                    cancellationToken
                );

            lastNativeResult =
                setResult;

            if (!IsFailure(
                    setResult))
            {
                string verify =
                    await ReadDocumentTextAsync(
                        text,
                        cancellationToken
                    );

                if (
                    !IsFailure(
                        verify)
                    &&
                    verify.Contains(
                        text,
                        StringComparison.Ordinal)
                )
                {
                    return
                        "SUCCESS: Real Notepad document replaced using native ValuePattern.\n" +
                        setResult;
                }
            }
        }

        // =====================================================
        // VERIFIED FALLBACK
        // =====================================================

        string foreground =
            WindowsWindowTools.VerifyForegroundWindow(
                windowTitle
            );

        if (IsFailure(
                foreground))
        {
            return
                "ERROR: Cannot use Notepad text fallback because the requested document is not foreground.\n" +
                foreground;
        }

        string selectAll =
            WindowsInputTools.PressKey(
                "CTRL+A"
            );

        if (IsFailure(
                selectAll))
        {
            return
                "ERROR: Native ValuePattern failed and document selection fallback failed.\n" +
                $"Last native result:\n{lastNativeResult}\n" +
                $"Selection result:\n{selectAll}";
        }

        await Task.Delay(
            150,
            cancellationToken
        );

        string typeResult =
            WindowsUiTools.TypeText(
                windowTitle,
                text
            );

        if (IsFailure(
                typeResult))
        {
            return
                "ERROR: Native ValuePattern and verified text fallback both failed.\n" +
                $"Last native result:\n{lastNativeResult}\n" +
                $"Text result:\n{typeResult}";
        }

        await Task.Delay(
            250,
            cancellationToken
        );

        string fallbackVerification =
            await ReadDocumentTextAsync(
                text,
                cancellationToken
            );

        if (
            IsFailure(
                fallbackVerification)
            ||
            !fallbackVerification.Contains(
                text,
                StringComparison.Ordinal)
        )
        {
            return
                "ERROR: Notepad fallback wrote text but verification failed.\n" +
                fallbackVerification;
        }

        return
            "SUCCESS: Real Notepad document replaced using verified fallback.\n" +
            typeResult;
    }

    // =========================================================
    // READ REAL NOTEPAD DOCUMENT
    // =========================================================

    private static async Task<string> ReadDocumentTextAsync(
        string expectedText,
        CancellationToken cancellationToken)
    {
        (string Type, string Name, bool Exact)[] candidates =
        [
            (
                "edit",
                "Text editor",
                false
            ),
            (
                "document",
                "Text editor",
                false
            ),
            (
                "edit",
                "Text Editor",
                false
            ),
            (
                "document",
                "Text Editor",
                false
            ),
            (
                "document",
                "",
                false
            ),
            (
                "edit",
                "",
                false
            )
        ];

        string lastResult =
            "";

        foreach (
            (string Type, string Name, bool Exact) candidate
            in candidates)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string result =
                await Task.Run(
                    () =>
                        WindowsControlTools.GetControlValue(
                            "__FOREGROUND__",
                            candidate.Type,
                            candidate.Name,
                            candidate.Exact
                        ),
                    cancellationToken
                );

            lastResult =
                result;

            if (
                !IsFailure(
                    result)
                &&
                result.Contains(
                    expectedText,
                    StringComparison.Ordinal)
            )
            {
                return
                    "SUCCESS: Real Notepad document text verified.\n" +
                    result;
            }
        }

        return
            "NOT_FOUND: Expected real Notepad document text was not found through UI Automation.\n" +
            $"Expected: {expectedText}\n" +
            $"Last result:\n{lastResult}";
    }

    // =========================================================
    // ARGUMENTS
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

            if (value.ValueKind !=
                JsonValueKind.String)
            {
                return "";
            }

            return
                value.GetString()
                ?? "";
        }
        catch
        {
            return "";
        }
    }

    private static int GetIntArgument(
        JsonElement arguments,
        string propertyName)
    {
        try
        {
            if (arguments.ValueKind !=
                JsonValueKind.Object)
            {
                return 0;
            }

            if (!arguments.TryGetProperty(
                    propertyName,
                    out JsonElement value))
            {
                return 0;
            }

            if (
                value.ValueKind ==
                JsonValueKind.Number
                &&
                value.TryGetInt32(
                    out int result)
            )
            {
                return result;
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }

    // =========================================================
    // FAILURE CHECK
    // =========================================================

    private static bool IsFailure(
        string result)
    {
        if (string.IsNullOrWhiteSpace(
                result))
        {
            return true;
        }

        return
            result.StartsWith(
                "ERROR",
                StringComparison.OrdinalIgnoreCase)
            ||
            result.StartsWith(
                "NOT_FOUND",
                StringComparison.OrdinalIgnoreCase)
            ||
            result.StartsWith(
                "BLOCKED",
                StringComparison.OrdinalIgnoreCase)
            ||
            result.StartsWith(
                "TIMEOUT",
                StringComparison.OrdinalIgnoreCase)
            ||
            result.StartsWith(
                "CANCELLED",
                StringComparison.OrdinalIgnoreCase);
    }
}

#pragma warning restore OPENAI001