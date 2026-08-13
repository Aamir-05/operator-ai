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

public sealed class MultiWindowAgent07C
{
    private readonly ResponsesClient _client;

    // =========================================================
    // WINDOWS
    // LIST TOP-LEVEL WINDOWS
    // =========================================================

    private static readonly FunctionTool ListWindowsTool =
        ResponseTool.CreateFunctionTool(
            functionName:
                "windows_list_windows",
            functionDescription:
                "List visible titled top-level Windows using the robust Win32 window discovery layer.",
            functionParameters:
                null,
            strictModeEnabled:
                false
        );

    // =========================================================
    // WINDOWS
    // WAIT FOR TOP-LEVEL WINDOW
    // =========================================================

    private static readonly FunctionTool WaitForWindowTool =
        ResponseTool.CreateFunctionTool(
            functionName:
                "windows_wait_for_top_window",
            functionDescription:
                "Wait for a visible top-level Windows window whose title matches the supplied title.",
            functionParameters:
                BinaryData.FromString(
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
            strictModeEnabled:
                true
        );

    // =========================================================
    // WINDOWS
    // FOCUS WINDOW
    // =========================================================

    private static readonly FunctionTool FocusWindowTool =
        ResponseTool.CreateFunctionTool(
            functionName:
                "windows_focus_window",
            functionDescription:
                "Bring a visible top-level Windows window to the foreground using its title.",
            functionParameters:
                BinaryData.FromString(
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
            strictModeEnabled:
                true
        );

    // =========================================================
    // WINDOWS
    // GET FOREGROUND
    // =========================================================

    private static readonly FunctionTool GetForegroundWindowTool =
        ResponseTool.CreateFunctionTool(
            functionName:
                "windows_get_foreground_window",
            functionDescription:
                "Read the currently active foreground Windows window title, process ID, and HWND.",
            functionParameters:
                null,
            strictModeEnabled:
                false
        );

    // =========================================================
    // WINDOWS
    // VERIFY FOREGROUND
    // =========================================================

    private static readonly FunctionTool VerifyForegroundTool =
        ResponseTool.CreateFunctionTool(
            functionName:
                "windows_verify_foreground",
            functionDescription:
                "Verify that the current foreground Windows window matches an expected title.",
            functionParameters:
                BinaryData.FromString(
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
            strictModeEnabled:
                true
        );

    // =========================================================
    // WINDOWS UI AUTOMATION
    // LIST CONTROLS
    // =========================================================

    private static readonly FunctionTool ListControlsTool =
        ResponseTool.CreateFunctionTool(
            functionName:
                "windows_list_controls",
            functionDescription:
                "List native UI Automation controls in the currently active foreground window.",
            functionParameters:
                BinaryData.FromString(
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
            strictModeEnabled:
                true
        );

    // =========================================================
    // WINDOWS UI AUTOMATION
    // GET VALUE
    // =========================================================

    private static readonly FunctionTool GetControlValueTool =
        ResponseTool.CreateFunctionTool(
            functionName:
                "windows_get_control_value",
            functionDescription:
                "Read the value or text of a native control in the current foreground window.",
            functionParameters:
                BinaryData.FromString(
                    """
                    {
                      "type": "object",
                      "properties": {
                        "control_type": {
                          "type": "string"
                        },
                        "control_name": {
                          "type": "string"
                        },
                        "exact_name": {
                          "type": "boolean"
                        }
                      },
                      "required": [
                        "control_type",
                        "control_name",
                        "exact_name"
                      ],
                      "additionalProperties": false
                    }
                    """
                ),
            strictModeEnabled:
                true
        );

    // =========================================================
    // WINDOWS UI AUTOMATION
    // SET VALUE
    // =========================================================

    private static readonly FunctionTool SetControlValueTool =
        ResponseTool.CreateFunctionTool(
            functionName:
                "windows_set_control_value",
            functionDescription:
                "Set a native editable control in the current foreground window using ValuePattern.",
            functionParameters:
                BinaryData.FromString(
                    """
                    {
                      "type": "object",
                      "properties": {
                        "control_type": {
                          "type": "string"
                        },
                        "control_name": {
                          "type": "string"
                        },
                        "exact_name": {
                          "type": "boolean"
                        },
                        "value": {
                          "type": "string"
                        }
                      },
                      "required": [
                        "control_type",
                        "control_name",
                        "exact_name",
                        "value"
                      ],
                      "additionalProperties": false
                    }
                    """
                ),
            strictModeEnabled:
                true
        );

    // =========================================================
    // WINDOWS UI AUTOMATION
    // SET TOGGLE
    // =========================================================

    private static readonly FunctionTool SetToggleTool =
        ResponseTool.CreateFunctionTool(
            functionName:
                "windows_set_toggle",
            functionDescription:
                "Set a native checkbox or toggle in the current foreground window to ON or OFF.",
            functionParameters:
                BinaryData.FromString(
                    """
                    {
                      "type": "object",
                      "properties": {
                        "control_type": {
                          "type": "string"
                        },
                        "control_name": {
                          "type": "string"
                        },
                        "exact_name": {
                          "type": "boolean"
                        },
                        "checked": {
                          "type": "boolean"
                        }
                      },
                      "required": [
                        "control_type",
                        "control_name",
                        "exact_name",
                        "checked"
                      ],
                      "additionalProperties": false
                    }
                    """
                ),
            strictModeEnabled:
                true
        );

    // =========================================================
    // WINDOWS UI AUTOMATION
    // GET TOGGLE
    // =========================================================

    private static readonly FunctionTool GetToggleTool =
        ResponseTool.CreateFunctionTool(
            functionName:
                "windows_get_toggle",
            functionDescription:
                "Read the current state of a native checkbox or toggle in the foreground window.",
            functionParameters:
                BinaryData.FromString(
                    """
                    {
                      "type": "object",
                      "properties": {
                        "control_type": {
                          "type": "string"
                        },
                        "control_name": {
                          "type": "string"
                        },
                        "exact_name": {
                          "type": "boolean"
                        }
                      },
                      "required": [
                        "control_type",
                        "control_name",
                        "exact_name"
                      ],
                      "additionalProperties": false
                    }
                    """
                ),
            strictModeEnabled:
                true
        );

    // =========================================================
    // WINDOWS UI AUTOMATION
    // CONTROL INFO
    // =========================================================

    private static readonly FunctionTool ControlInfoTool =
        ResponseTool.CreateFunctionTool(
            functionName:
                "windows_get_control_info",
            functionDescription:
                "Inspect a native Windows control and report supported UI Automation patterns.",
            functionParameters:
                BinaryData.FromString(
                    """
                    {
                      "type": "object",
                      "properties": {
                        "control_type": {
                          "type": "string"
                        },
                        "control_name": {
                          "type": "string"
                        },
                        "exact_name": {
                          "type": "boolean"
                        }
                      },
                      "required": [
                        "control_type",
                        "control_name",
                        "exact_name"
                      ],
                      "additionalProperties": false
                    }
                    """
                ),
            strictModeEnabled:
                true
        );

    // =========================================================
    // WINDOWS UI AUTOMATION
    // CLICK / INVOKE
    // =========================================================

    private static readonly FunctionTool ClickControlTool =
        ResponseTool.CreateFunctionTool(
            functionName:
                "windows_click_control",
            functionDescription:
                "Activate a native control in the foreground window using its supported UI Automation pattern.",
            functionParameters:
                BinaryData.FromString(
                    """
                    {
                      "type": "object",
                      "properties": {
                        "control_type": {
                          "type": "string"
                        },
                        "control_name": {
                          "type": "string"
                        },
                        "exact_name": {
                          "type": "boolean"
                        }
                      },
                      "required": [
                        "control_type",
                        "control_name",
                        "exact_name"
                      ],
                      "additionalProperties": false
                    }
                    """
                ),
            strictModeEnabled:
                true
        );

    // =========================================================
    // CONSTRUCTOR
    // =========================================================

    public MultiWindowAgent07C()
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
    // RUN AUTONOMOUS MULTI-WINDOW TASK
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
            CancellationTokenSource
                .CreateLinkedTokenSource(
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
                    $"[PLAN] Multi-window planning step {step}..."
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
                            multi-window Windows automation task.

                            Use only the tools provided in this run.

                            =================================================
                            WINDOW DISCOVERY
                            =================================================

                            Start by listing top-level windows.

                            Do not assume a window is active merely because
                            it exists.

                            Before reading or changing controls in a specific
                            application:

                            1. focus the intended window,
                            2. verify it became foreground,
                            3. inspect its controls.

                            =================================================
                            WINDOW SWITCHING
                            =================================================

                            windows_focus_window changes the active top-level
                            window.

                            After each important switch, call
                            windows_verify_foreground.

                            Never manipulate controls in a window until the
                            foreground title has been verified.

                            =================================================
                            NATIVE CONTROLS
                            =================================================

                            windows_list_controls inspects only the CURRENT
                            FOREGROUND window.

                            Use exact accessible control names whenever known.

                            For editable fields use:
                            windows_set_control_value
                            then
                            windows_get_control_value

                            For checkboxes use:
                            windows_set_toggle
                            then
                            windows_get_toggle

                            For buttons:
                            inspect with windows_get_control_info,
                            then use windows_click_control.

                            =================================================
                            CROSS-WINDOW DATA
                            =================================================

                            When transferring information between windows:

                            - read the source value through a tool,
                            - retain the exact value,
                            - switch to the destination,
                            - write that exact value,
                            - verify the destination value,
                            - perform the destination action,
                            - verify the result.

                            Do not invent, reformat, or paraphrase values that
                            are meant to be copied exactly.

                            =================================================
                            VERIFICATION
                            =================================================

                            Never claim success based only on an action
                            returning success.

                            Verify resulting values and application state.

                            If asked to return to another window, actually
                            switch to it and verify it became foreground.

                            =================================================
                            PROHIBITED
                            =================================================

                            Do not use browser automation.

                            Do not use keyboard automation.

                            Do not use mouse coordinates.

                            Do not guess screen positions.

                            Do not interact with windows unrelated to the
                            controlled task.

                            =================================================
                            COMPLETION
                            =================================================

                            Complete only after the requested source and
                            destination states have been verified through
                            actual tools.
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
                    GetForegroundWindowTool
                );

                options.Tools.Add(
                    VerifyForegroundTool
                );

                options.Tools.Add(
                    ListControlsTool
                );

                options.Tools.Add(
                    GetControlValueTool
                );

                options.Tools.Add(
                    SetControlValueTool
                );

                options.Tools.Add(
                    SetToggleTool
                );

                options.Tools.Add(
                    GetToggleTool
                );

                options.Tools.Add(
                    ControlInfoTool
                );

                options.Tools.Add(
                    ClickControlTool
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
                "ERROR: Maximum multi-window planning steps reached.";
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken
                .IsCancellationRequested)
            {
                return
                    "CANCELLED: Multi-window task stopped.";
            }

            return
                "TIMEOUT: Multi-window task exceeded the time limit.";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Multi-window agent failure: {ex.Message}";
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

            case "windows_get_foreground_window":
                return await Task.Run(
                    () =>
                        WindowsWindowTools
                            .GetForegroundWindowInfo(),
                    cancellationToken
                );

            case "windows_verify_foreground":
                return await Task.Run(
                    () =>
                        WindowsWindowTools
                            .VerifyForegroundWindow(
                                GetStringArgument(
                                    arguments,
                                    "expected_title"
                                )
                            ),
                    cancellationToken
                );

            // =================================================
            // WINDOWS UI AUTOMATION
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

            case "windows_get_control_value":
                return await Task.Run(
                    () =>
                        WindowsControlTools.GetControlValue(
                            "__FOREGROUND__",
                            GetStringArgument(
                                arguments,
                                "control_type"
                            ),
                            GetStringArgument(
                                arguments,
                                "control_name"
                            ),
                            GetBoolArgument(
                                arguments,
                                "exact_name"
                            )
                        ),
                    cancellationToken
                );

            case "windows_set_control_value":
                return await Task.Run(
                    () =>
                        WindowsControlTools.SetControlValue(
                            "__FOREGROUND__",
                            GetStringArgument(
                                arguments,
                                "control_type"
                            ),
                            GetStringArgument(
                                arguments,
                                "control_name"
                            ),
                            GetBoolArgument(
                                arguments,
                                "exact_name"
                            ),
                            GetStringArgument(
                                arguments,
                                "value"
                            )
                        ),
                    cancellationToken
                );

            case "windows_set_toggle":
                return await Task.Run(
                    () =>
                        WindowsControlTools.SetToggleState(
                            "__FOREGROUND__",
                            GetStringArgument(
                                arguments,
                                "control_type"
                            ),
                            GetStringArgument(
                                arguments,
                                "control_name"
                            ),
                            GetBoolArgument(
                                arguments,
                                "exact_name"
                            ),
                            GetBoolArgument(
                                arguments,
                                "checked"
                            )
                        ),
                    cancellationToken
                );

            case "windows_get_toggle":
                return await Task.Run(
                    () =>
                        WindowsControlTools.GetToggleState(
                            "__FOREGROUND__",
                            GetStringArgument(
                                arguments,
                                "control_type"
                            ),
                            GetStringArgument(
                                arguments,
                                "control_name"
                            ),
                            GetBoolArgument(
                                arguments,
                                "exact_name"
                            )
                        ),
                    cancellationToken
                );

            case "windows_get_control_info":
                return await Task.Run(
                    () =>
                        WindowsControlTools.GetControlInfo(
                            "__FOREGROUND__",
                            GetStringArgument(
                                arguments,
                                "control_type"
                            ),
                            GetStringArgument(
                                arguments,
                                "control_name"
                            ),
                            GetBoolArgument(
                                arguments,
                                "exact_name"
                            )
                        ),
                    cancellationToken
                );

            case "windows_click_control":
                return await Task.Run(
                    () =>
                        WindowsControlTools.ClickControl(
                            "__FOREGROUND__",
                            GetStringArgument(
                                arguments,
                                "control_type"
                            ),
                            GetStringArgument(
                                arguments,
                                "control_name"
                            ),
                            GetBoolArgument(
                                arguments,
                                "exact_name"
                            )
                        ),
                    cancellationToken
                );

            default:
                return
                    $"ERROR: Unknown 0.7C tool '{call.FunctionName}'.";
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
            if (
                arguments.ValueKind !=
                JsonValueKind.Object
            )
            {
                return "";
            }

            if (!arguments.TryGetProperty(
                    propertyName,
                    out JsonElement value))
            {
                return "";
            }

            if (
                value.ValueKind !=
                JsonValueKind.String
            )
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
            if (
                arguments.ValueKind !=
                JsonValueKind.Object
            )
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
    // BOOLEAN ARGUMENT
    // =========================================================

    private static bool GetBoolArgument(
        JsonElement arguments,
        string propertyName)
    {
        try
        {
            if (
                arguments.ValueKind !=
                JsonValueKind.Object
            )
            {
                return false;
            }

            if (!arguments.TryGetProperty(
                    propertyName,
                    out JsonElement value))
            {
                return false;
            }

            if (
                value.ValueKind ==
                JsonValueKind.True
            )
            {
                return true;
            }

            if (
                value.ValueKind ==
                JsonValueKind.False
            )
            {
                return false;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}

#pragma warning restore OPENAI001