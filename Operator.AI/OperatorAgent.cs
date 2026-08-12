using OpenAI.Responses;
using Operator.Tools;
using System.Text.Json;

namespace Operator.AI;

#pragma warning disable OPENAI001

public sealed class OperatorAgent
{
    private readonly ResponsesClient _client;

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

    private static readonly FunctionTool ListWindowsTool =
        ResponseTool.CreateFunctionTool(
            functionName: "list_windows",
            functionDescription:
                "List visible top-level windows on the Windows desktop.",
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

    private static readonly FunctionTool PressKeyTool =
        ResponseTool.CreateFunctionTool(
            functionName: "press_key",
            functionDescription:
                "Press a Windows keyboard key or shortcut. Examples: CTRL+S, CTRL+A, ALT+F4, ENTER, TAB, ESC, LEFT, RIGHT, UP, DOWN.",
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
            new ResponsesClient(apiKey);
    }

    public async Task<string> RunAsync(
        string task,
        Action<string>? log = null)
    {
        List<ResponseItem> inputItems =
        [
            ResponseItem.CreateUserMessageItem(task)
        ];

        for (int step = 1; step <= 25; step++)
        {
            log?.Invoke(
                $"AI planning step {step}..."
            );

            CreateResponseOptions options =
                new("gpt-5.6", inputItems)
                {
                    Instructions =
                        """
                        You are Operator AI, a Windows automation agent.

                        Complete the user's task using the available tools.

                        Rules:
                        - Use tools for real computer actions.
                        - Never claim an action happened unless a tool confirms it.
                        - Verify important actions whenever practical.
                        - Only work within the permissions provided by the tools.
                        - Prefer structured Windows UI tools over guessing.
                        - If an application was opened and you need its real window title, use list_windows.
                        - Use inspect_window when you need to understand an application's available controls.
                        - Use focus_window before typing into a desktop application.
                        - Use type_text for text entry into Windows applications.
                        - Use press_key for Windows keyboard shortcuts such as CTRL+S, CTRL+A, ENTER, TAB, ESC and ALT+F4.
                        - After pressing a keyboard shortcut that opens a dialog, use list_windows or inspect_window to discover the new UI state.
                        - Never invent a successful result.
                        - If something is not possible with the available tools, explain clearly.
                        """
                };

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

            ResponseResult response =
                await _client.CreateResponseAsync(
                    options
                );

            inputItems.AddRange(
                response.OutputItems
            );

            bool toolCalled = false;

            foreach (
                FunctionCallResponseItem functionCall
                in response.OutputItems
                    .OfType<FunctionCallResponseItem>())
            {
                toolCalled = true;

                string result =
                    ExecuteTool(
                        functionCall,
                        log
                    );

                inputItems.Add(
                    new FunctionCallOutputResponseItem(
                        functionCall.CallId,
                        result
                    )
                );
            }

            if (!toolCalled)
            {
                string finalAnswer =
                    response.GetOutputText();

                return string.IsNullOrWhiteSpace(
                    finalAnswer)
                    ? "Task completed."
                    : finalAnswer;
            }
        }

        return
            "Agent stopped because the maximum number of steps was reached.";
    }

    private static string ExecuteTool(
        FunctionCallResponseItem call,
        Action<string>? log)
    {
        JsonElement arguments;

        try
        {
            arguments =
                JsonDocument
                    .Parse(call.FunctionArguments)
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
            case "open_application":
                {
                    string app =
                        arguments
                            .GetProperty("application")
                            .GetString()
                        ?? "";

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

            case "create_desktop_folder":
                {
                    string folder =
                        arguments
                            .GetProperty("folder_name")
                            .GetString()
                        ?? "";

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

            case "create_desktop_file":
                {
                    string path =
                        arguments
                            .GetProperty("relative_path")
                            .GetString()
                        ?? "";

                    string content =
                        arguments
                            .GetProperty("content")
                            .GetString()
                        ?? "";

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

            case "read_desktop_file":
                {
                    string path =
                        arguments
                            .GetProperty("relative_path")
                            .GetString()
                        ?? "";

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

            case "desktop_file_exists":
                {
                    string path =
                        arguments
                            .GetProperty("relative_path")
                            .GetString()
                        ?? "";

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

            case "inspect_window":
                {
                    string title =
                        arguments
                            .GetProperty("window_title")
                            .GetString()
                        ?? "";

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

            case "focus_window":
                {
                    string title =
                        arguments
                            .GetProperty("window_title")
                            .GetString()
                        ?? "";

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

            case "type_text":
                {
                    string title =
                        arguments
                            .GetProperty("window_title")
                            .GetString()
                        ?? "";

                    string text =
                        arguments
                            .GetProperty("text")
                            .GetString()
                        ?? "";

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

            case "press_key":
                {
                    string keys =
                        arguments
                            .GetProperty("keys")
                            .GetString()
                        ?? "";

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

            default:
                {
                    string result =
                        $"ERROR: Unknown tool '{call.FunctionName}'.";

                    log?.Invoke(result);

                    return result;
                }
        }
    }
}

#pragma warning restore OPENAI001