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

public sealed class RealAppAgent07C
{
    private readonly ResponsesClient _client;

    // =========================================================
    // WINDOW DISCOVERY
    // =========================================================

    private static readonly FunctionTool ListWindowsTool =
        ResponseTool.CreateFunctionTool(
            functionName: "windows_list_windows",
            functionDescription:
                "List visible titled top-level Windows using the robust Win32 window layer.",
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
                "Bring a top-level Windows window to the foreground by title.",
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
                "Verify the currently active foreground window matches the expected title.",
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

    private static readonly FunctionTool ForegroundInfoTool =
        ResponseTool.CreateFunctionTool(
            functionName: "windows_get_foreground_window",
            functionDescription:
                "Read the currently active foreground Windows window.",
            functionParameters: null,
            strictModeEnabled: false
        );

    // =========================================================
    // UI AUTOMATION
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
                "Find a native control in the current foreground window by accessible Name or AutomationId without requiring the control type.",
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
    // FILE VERIFICATION
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
                "Read a text file located under the Windows Desktop.",
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

    public RealAppAgent07C()
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
                 step <= 40;
                 step++)
            {
                token.ThrowIfCancellationRequested();

                log?.Invoke(
                    $"[PLAN] Real-app planning step {step}..."
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
                            automation test using REAL Windows applications.

                            The applications used in this test are Notepad
                            and File Explorer.

                            Use only the tools provided in this run.

                            =================================================
                            WINDOW SAFETY
                            =================================================

                            Always:

                            1. discover or wait for the desired window,
                            2. focus the intended window,
                            3. verify it became foreground,
                            4. inspect its controls before reasoning about
                               application contents.

                            Do not manipulate the wrong foreground window.

                            =================================================
                            NOTEPAD
                            =================================================

                            The required document is already open in Notepad.

                            Verify the correct Notepad document primarily by
                            its window title and native control structure.

                            Do not edit or save the document in this test.

                            =================================================
                            FILE EXPLORER
                            =================================================

                            A dedicated test folder is already open in
                            File Explorer.

                            Inspect native controls and locate the expected
                            file by its accessible name.

                            Use windows_find_control_any when the exact
                            Explorer control type is uncertain.

                            Do not delete, rename, move, or open the file.

                            =================================================
                            FILESYSTEM VERIFICATION
                            =================================================

                            desktop_file_exists and read_desktop_file may
                            independently verify the real file and content.

                            These tools do not replace the requirement to
                            inspect File Explorer itself.

                            =================================================
                            PROHIBITED
                            =================================================

                            Do not:

                            - use browser tools,
                            - use keyboard automation,
                            - use screen coordinates,
                            - modify the test file,
                            - delete anything,
                            - rename anything,
                            - close unrelated applications.

                            =================================================
                            COMPLETION
                            =================================================

                            Do not claim completion until:

                            - the correct Notepad document has been verified,
                            - the File Explorer window has been verified,
                            - the file has been located in Explorer,
                            - the real filesystem file has been verified,
                            - the file content has been read,
                            - Notepad has been revisited and verified again.
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
                    ForegroundInfoTool
                );

                options.Tools.Add(
                    ListControlsTool
                );

                options.Tools.Add(
                    FindControlAnyTool
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
                "ERROR: Maximum real-application planning steps reached.";
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return
                    "CANCELLED: Real-application task stopped.";
            }

            return
                "TIMEOUT: Real-application task exceeded the time limit.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Real-application agent failure: {ex.Message}";
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

            case "windows_get_foreground_window":
                return await Task.Run(
                    () =>
                        WindowsWindowTools.GetForegroundWindowInfo(),
                    cancellationToken
                );

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
                    $"ERROR: Unknown real-app tool '{call.FunctionName}'.";
        }
    }

    // =========================================================
    // STRING ARGUMENT
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

    // =========================================================
    // INTEGER ARGUMENT
    // =========================================================

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
                return
                    result;
            }

            return 0;
        }
        catch
        {
            return 0;
        }
    }
}

#pragma warning restore OPENAI001