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
    // WINDOWS APPLICATION TOOLS
    // =========================================================

    private static readonly FunctionTool OpenApplicationTool =
        ResponseTool.CreateFunctionTool(
            functionName: "open_application",
            functionDescription:
                "Open an approved Windows application. Currently allowed applications include notepad, calculator, and edge.",
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
                "List files and folders currently on the Windows Desktop.",
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
    // WINDOWS KEYBOARD TOOL
    // =========================================================

    private static readonly FunctionTool PressKeyTool =
        ResponseTool.CreateFunctionTool(
            functionName: "press_key",
            functionDescription:
                "Press a Windows keyboard key or shortcut. Examples include CTRL+S, CTRL+A, CTRL+SHIFT+S, ALT+F4, ENTER, TAB, ESC, LEFT, RIGHT, UP, and DOWN.",
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
    // RELIABLE WINDOWS SAVE WORKFLOW
    // =========================================================

    private static readonly FunctionTool SaveActiveDocumentTool =
        ResponseTool.CreateFunctionTool(
            functionName:
                "save_active_document_as_desktop_file",
            functionDescription:
                "Reliably save the currently active Windows document to a file inside the Desktop. Uses Save As, waits for Windows, retries when needed, and verifies creation.",
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
    // VERSION 0.6C
    // BROWSER TOOLS
    // =========================================================

    private static readonly FunctionTool StartBrowserTool =
        ResponseTool.CreateFunctionTool(
            functionName: "start_browser",
            functionDescription:
                "Start the Operator AI Chromium browser if it is not already running.",
            functionParameters: null,
            strictModeEnabled: false
        );

    private static readonly FunctionTool BrowserNavigateTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_navigate",
            functionDescription:
                "Navigate the current browser tab to a URL.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "url": {
                      "type": "string"
                    }
                  },
                  "required": ["url"],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool BrowserPageInfoTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_get_page_info",
            functionDescription:
                "Get the current browser page title and URL.",
            functionParameters: null,
            strictModeEnabled: false
        );

    private static readonly FunctionTool BrowserReadPageTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_read_page",
            functionDescription:
                "Read visible text from the current browser page. Use this to understand or summarize page contents.",
            functionParameters: null,
            strictModeEnabled: false
        );

    private static readonly FunctionTool BrowserListLinksTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_list_links",
            functionDescription:
                "List links visible in the current browser page, including link text and href values.",
            functionParameters: null,
            strictModeEnabled: false
        );

    private static readonly FunctionTool BrowserListElementsTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_list_elements",
            functionDescription:
                "Inspect interactive elements on the current page such as links, buttons, inputs, textareas, and selects. Use this when unsure how to interact with a webpage.",
            functionParameters: null,
            strictModeEnabled: false
        );

    private static readonly FunctionTool BrowserFindTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_find",
            functionDescription:
                "Find matching elements on the current webpage. Supported locator types: css, text, label, placeholder, title, testid.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "locator_type": {
                      "type": "string"
                    },
                    "query": {
                      "type": "string"
                    }
                  },
                  "required": [
                    "locator_type",
                    "query"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool BrowserClickTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_click",
            functionDescription:
                "Click the first browser element matching the locator. Supported locator types: css, text, label, placeholder, title, testid.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "locator_type": {
                      "type": "string"
                    },
                    "query": {
                      "type": "string"
                    }
                  },
                  "required": [
                    "locator_type",
                    "query"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool BrowserFillTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_fill",
            functionDescription:
                "Replace the contents of a browser input or textarea with the supplied text. Supported locator types: css, text, label, placeholder, title, testid.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "locator_type": {
                      "type": "string"
                    },
                    "query": {
                      "type": "string"
                    },
                    "text": {
                      "type": "string"
                    }
                  },
                  "required": [
                    "locator_type",
                    "query",
                    "text"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool BrowserTypeTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_type",
            functionDescription:
                "Type text character-by-character into a browser input. Prefer browser_fill for ordinary form input unless simulated typing is necessary.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "locator_type": {
                      "type": "string"
                    },
                    "query": {
                      "type": "string"
                    },
                    "text": {
                      "type": "string"
                    }
                  },
                  "required": [
                    "locator_type",
                    "query",
                    "text"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool BrowserPressTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_press",
            functionDescription:
                "Press a keyboard key on a specific browser element. Examples: Enter, Tab, Escape, ArrowDown.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "locator_type": {
                      "type": "string"
                    },
                    "query": {
                      "type": "string"
                    },
                    "key": {
                      "type": "string"
                    }
                  },
                  "required": [
                    "locator_type",
                    "query",
                    "key"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool BrowserPageKeyTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_page_key",
            functionDescription:
                "Press a keyboard key globally on the current browser page.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "key": {
                      "type": "string"
                    }
                  },
                  "required": ["key"],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool BrowserBackTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_back",
            functionDescription:
                "Navigate the current browser tab backward in history.",
            functionParameters: null,
            strictModeEnabled: false
        );

    private static readonly FunctionTool BrowserForwardTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_forward",
            functionDescription:
                "Navigate the current browser tab forward in history.",
            functionParameters: null,
            strictModeEnabled: false
        );

    private static readonly FunctionTool BrowserReloadTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_reload",
            functionDescription:
                "Reload the current browser page.",
            functionParameters: null,
            strictModeEnabled: false
        );

    private static readonly FunctionTool BrowserNewTabTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_new_tab",
            functionDescription:
                "Open a new browser tab. Provide an empty string for url if a blank tab is desired.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "url": {
                      "type": "string"
                    }
                  },
                  "required": ["url"],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool BrowserListTabsTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_list_tabs",
            functionDescription:
                "List all currently open Operator AI browser tabs and indicate which one is current.",
            functionParameters: null,
            strictModeEnabled: false
        );

    private static readonly FunctionTool BrowserSwitchTabTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_switch_tab",
            functionDescription:
                "Switch to a browser tab by its 1-based tab number from browser_list_tabs.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "tab_number": {
                      "type": "integer"
                    }
                  },
                  "required": ["tab_number"],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool BrowserCloseTabTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_close_tab",
            functionDescription:
                "Close a browser tab by its 1-based tab number.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "tab_number": {
                      "type": "integer"
                    }
                  },
                  "required": ["tab_number"],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool StopBrowserTool =
        ResponseTool.CreateFunctionTool(
            functionName: "stop_browser",
            functionDescription:
                "Close the Operator AI Chromium browser and its tabs.",
            functionParameters: null,
            strictModeEnabled: false
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
    // AGENT LOOP
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

        // Browser tasks can take longer than simple Windows tasks.
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
                    $"[PLAN] Planning step {step}..."
                );

                CreateResponseOptions options =
                    new(
                        "gpt-5.6",
                        inputItems
                    )
                    {
                        Instructions =
                            """
                            You are Operator AI, a Windows and browser automation agent.

                            Your job is to complete real tasks on the user's
                            computer using the available tools.

                            =================================================
                            GENERAL RULES
                            =================================================

                            - Use tools for real computer or browser actions.
                            - Never claim an action happened unless a tool confirms it.
                            - Verify important outcomes whenever practical.
                            - Never invent successful results.
                            - Stay within the capabilities exposed by the tools.
                            - Do not repeat failed actions indefinitely.
                            - Prefer reliable structured tools over fragile UI guessing.

                            =================================================
                            WINDOWS RULES
                            =================================================

                            - Use open_application for supported Windows apps.
                            - Use list_windows if you need the actual window title.
                            - Use focus_window before interacting with a desktop app.
                            - Use inspect_window when you need the UI state.
                            - Use type_text for desktop text entry.
                            - Use press_key for desktop keyboard shortcuts.

                            =================================================
                            WINDOWS SAVING RULES
                            =================================================

                            - Prefer save_active_document_as_desktop_file when
                              saving an active Windows document to Desktop.
                            - Verify saved files with desktop_file_exists.
                            - Read files back when content verification is required.

                            =================================================
                            BROWSER RULES
                            =================================================

                            - Use start_browser before browser work when needed.
                            - Use browser_navigate to visit URLs.
                            - Use browser_get_page_info to confirm title and URL.
                            - Use browser_read_page to understand page content.
                            - Use browser_list_elements when you do not know
                              how to locate an interactive element.
                            - Use browser_find when you need to test a locator.

                            - Supported browser locator types are:
                              css
                              text
                              label
                              placeholder
                              title
                              testid

                            - Prefer human-readable locators such as:
                              label
                              placeholder
                              text

                              before using complicated CSS selectors.

                            - Use browser_fill for normal form fields.
                            - Use browser_type only when character-by-character
                              typing is specifically useful.
                            - Use browser_press to press Enter or another key
                              on a specific element.
                            - Use browser_page_key for page-wide keyboard actions.
                            - Use browser_click for buttons and links.

                            =================================================
                            BROWSER NAVIGATION RULES
                            =================================================

                            - Use browser_back and browser_forward for history.
                            - Use browser_reload when refreshing is appropriate.
                            - Use browser_new_tab when a task needs another page.
                            - Use browser_list_tabs before switching tabs if
                              you are uncertain which tab number to use.
                            - Use browser_switch_tab to change current tabs.
                            - Use browser_close_tab only when useful to the task.

                            =================================================
                            BROWSER RESEARCH RULES
                            =================================================

                            - When asked to find or understand information online,
                              navigate to appropriate pages and read their contents.
                            - Do not infer page content without reading it.
                            - If a page does not contain the needed information,
                              use links, navigation, or another appropriate webpage.
                            - Confirm the final page state before reporting completion.

                            =================================================
                            SENSITIVE / CONSEQUENTIAL ACTION RULES
                            =================================================

                            - Browsing, searching, reading, and ordinary navigation
                              may proceed automatically when requested.

                            - Do not submit purchases, financial transactions,
                              account deletions, password changes, or similarly
                              consequential actions unless the user explicitly
                              asked for that exact action.

                            - Do not infer permission for consequential actions
                              merely because you can see a button.

                            =================================================
                            RECOVERY RULES
                            =================================================

                            - ERROR, NOT_FOUND, and BLOCKED mean the strategy failed.
                            - Do not immediately give up after one recoverable failure.
                            - Inspect the current state and try a reasonable alternative.
                            - If browser_find fails, inspect interactive elements
                              and try a better locator.
                            - If a repeated tool call is blocked, do not issue
                              the identical call again.
                            - Change strategy or arguments instead.
                            - Do not enter loops.
                            - After several unsuccessful recovery attempts,
                              stop and explain what prevented completion.

                            =================================================
                            COMPLETION RULES
                            =================================================

                            - Finish only when the requested outcome is confirmed.
                            - For browser research, read enough page content to
                              support the final answer.
                            - For browser navigation tasks, confirm the final URL
                              or page state when practical.
                            - For file tasks, verify the resulting file.
                            - If completion is impossible, clearly state which
                              action remains unresolved.
                            """
                    };

                // =================================================
                // WINDOWS TOOLS
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
                // BROWSER TOOLS
                // =================================================

                options.Tools.Add(
                    StartBrowserTool
                );

                options.Tools.Add(
                    BrowserNavigateTool
                );

                options.Tools.Add(
                    BrowserPageInfoTool
                );

                options.Tools.Add(
                    BrowserReadPageTool
                );

                options.Tools.Add(
                    BrowserListLinksTool
                );

                options.Tools.Add(
                    BrowserListElementsTool
                );

                options.Tools.Add(
                    BrowserFindTool
                );

                options.Tools.Add(
                    BrowserClickTool
                );

                options.Tools.Add(
                    BrowserFillTool
                );

                options.Tools.Add(
                    BrowserTypeTool
                );

                options.Tools.Add(
                    BrowserPressTool
                );

                options.Tools.Add(
                    BrowserPageKeyTool
                );

                options.Tools.Add(
                    BrowserBackTool
                );

                options.Tools.Add(
                    BrowserForwardTool
                );

                options.Tools.Add(
                    BrowserReloadTool
                );

                options.Tools.Add(
                    BrowserNewTabTool
                );

                options.Tools.Add(
                    BrowserListTabsTool
                );

                options.Tools.Add(
                    BrowserSwitchTabTool
                );

                options.Tools.Add(
                    BrowserCloseTabTool
                );

                options.Tools.Add(
                    StopBrowserTool
                );

                // =================================================
                // MODEL REQUEST
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
                            await ExecuteToolAsync(
                                functionCall,
                                null
                            );
                    }

                    token.ThrowIfCancellationRequested();

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
                        log?.Invoke(
                            $"[ERROR] {failureReason}"
                        );

                        return failureReason;
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

                    return finalAnswer;
                }
            }

            string stepLimitMessage =
                "Agent stopped because the maximum number of planning steps was reached.";

            log?.Invoke(
                $"[ERROR] {stepLimitMessage}"
            );

            return stepLimitMessage;
        }
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
                "TIMEOUT: Task exceeded the 5-minute limit and was stopped.";

            log?.Invoke(
                $"[TIMEOUT] {timeoutMessage}"
            );

            return timeoutMessage;
        }
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

    private static async Task<string> ExecuteToolAsync(
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
            // WINDOWS - OPEN APPLICATION
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
            // WINDOWS - CREATE FOLDER
            // =================================================

            case "create_desktop_folder":
                {
                    string folder =
                        GetStringArgument(
                            arguments,
                            "folder_name"
                        );

                    string result =
                        WindowsTools.CreateDesktopFolder(
                            folder
                        );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // WINDOWS - CREATE FILE
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

                    string result =
                        WindowsTools.CreateDesktopFile(
                            path,
                            content
                        );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // WINDOWS - READ FILE
            // =================================================

            case "read_desktop_file":
                {
                    string path =
                        GetStringArgument(
                            arguments,
                            "relative_path"
                        );

                    string result =
                        WindowsTools.ReadDesktopFile(
                            path
                        );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // WINDOWS - FILE EXISTS
            // =================================================

            case "desktop_file_exists":
                {
                    string path =
                        GetStringArgument(
                            arguments,
                            "relative_path"
                        );

                    string result =
                        WindowsTools.DesktopFileExists(
                            path
                        );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // WINDOWS - LIST DESKTOP
            // =================================================

            case "list_desktop":
                {
                    string result =
                        WindowsTools.ListDesktop();

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // WINDOWS - LIST WINDOWS
            // =================================================

            case "list_windows":
                {
                    string result =
                        WindowsUiTools.ListWindows();

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // WINDOWS - INSPECT WINDOW
            // =================================================

            case "inspect_window":
                {
                    string title =
                        GetStringArgument(
                            arguments,
                            "window_title"
                        );

                    string result =
                        WindowsUiTools.InspectWindow(
                            title
                        );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // WINDOWS - FOCUS WINDOW
            // =================================================

            case "focus_window":
                {
                    string title =
                        GetStringArgument(
                            arguments,
                            "window_title"
                        );

                    string result =
                        WindowsUiTools.FocusWindow(
                            title
                        );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // WINDOWS - TYPE TEXT
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

                    string result =
                        WindowsUiTools.TypeText(
                            title,
                            text
                        );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // WINDOWS - PRESS KEY
            // =================================================

            case "press_key":
                {
                    string keys =
                        GetStringArgument(
                            arguments,
                            "keys"
                        );

                    string result =
                        WindowsInputTools.PressKey(
                            keys
                        );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // WINDOWS - SAVE WORKFLOW
            // =================================================

            case "save_active_document_as_desktop_file":
                {
                    string path =
                        GetStringArgument(
                            arguments,
                            "relative_path"
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
            // BROWSER - START
            // =================================================

            case "start_browser":
                {
                    string result =
                        await BrowserTools.StartBrowserAsync();

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // BROWSER - NAVIGATE
            // =================================================

            case "browser_navigate":
                {
                    string url =
                        GetStringArgument(
                            arguments,
                            "url"
                        );

                    string result =
                        await BrowserTools.NavigateAsync(
                            url
                        );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // BROWSER - PAGE INFO
            // =================================================

            case "browser_get_page_info":
                {
                    string result =
                        await BrowserTools.GetPageInfoAsync();

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // BROWSER - READ PAGE
            // =================================================

            case "browser_read_page":
                {
                    string result =
                        await BrowserTools.ReadPageTextAsync();

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // BROWSER - LIST LINKS
            // =================================================

            case "browser_list_links":
                {
                    string result =
                        await BrowserTools.ListLinksAsync();

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // BROWSER - LIST INTERACTIVE ELEMENTS
            // =================================================

            case "browser_list_elements":
                {
                    string result =
                        await BrowserTools
                            .ListInteractiveElementsAsync();

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // BROWSER - FIND
            // =================================================

            case "browser_find":
                {
                    string locatorType =
                        GetStringArgument(
                            arguments,
                            "locator_type"
                        );

                    string query =
                        GetStringArgument(
                            arguments,
                            "query"
                        );

                    string result =
                        await BrowserTools.FindElementsAsync(
                            locatorType,
                            query
                        );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // BROWSER - CLICK
            // =================================================

            case "browser_click":
                {
                    string locatorType =
                        GetStringArgument(
                            arguments,
                            "locator_type"
                        );

                    string query =
                        GetStringArgument(
                            arguments,
                            "query"
                        );

                    string result =
                        await BrowserTools.ClickAsync(
                            locatorType,
                            query
                        );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // BROWSER - FILL
            // =================================================

            case "browser_fill":
                {
                    string locatorType =
                        GetStringArgument(
                            arguments,
                            "locator_type"
                        );

                    string query =
                        GetStringArgument(
                            arguments,
                            "query"
                        );

                    string text =
                        GetStringArgument(
                            arguments,
                            "text"
                        );

                    string result =
                        await BrowserTools.FillAsync(
                            locatorType,
                            query,
                            text
                        );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // BROWSER - TYPE
            // =================================================

            case "browser_type":
                {
                    string locatorType =
                        GetStringArgument(
                            arguments,
                            "locator_type"
                        );

                    string query =
                        GetStringArgument(
                            arguments,
                            "query"
                        );

                    string text =
                        GetStringArgument(
                            arguments,
                            "text"
                        );

                    string result =
                        await BrowserTools.TypeAsync(
                            locatorType,
                            query,
                            text
                        );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // BROWSER - PRESS ON ELEMENT
            // =================================================

            case "browser_press":
                {
                    string locatorType =
                        GetStringArgument(
                            arguments,
                            "locator_type"
                        );

                    string query =
                        GetStringArgument(
                            arguments,
                            "query"
                        );

                    string key =
                        GetStringArgument(
                            arguments,
                            "key"
                        );

                    string result =
                        await BrowserTools.PressAsync(
                            locatorType,
                            query,
                            key
                        );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // BROWSER - PAGE KEY
            // =================================================

            case "browser_page_key":
                {
                    string key =
                        GetStringArgument(
                            arguments,
                            "key"
                        );

                    string result =
                        await BrowserTools.PressPageKeyAsync(
                            key
                        );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // BROWSER - BACK
            // =================================================

            case "browser_back":
                {
                    string result =
                        await BrowserTools.BackAsync();

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // BROWSER - FORWARD
            // =================================================

            case "browser_forward":
                {
                    string result =
                        await BrowserTools.ForwardAsync();

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // BROWSER - RELOAD
            // =================================================

            case "browser_reload":
                {
                    string result =
                        await BrowserTools.ReloadAsync();

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // BROWSER - NEW TAB
            // =================================================

            case "browser_new_tab":
                {
                    string url =
                        GetStringArgument(
                            arguments,
                            "url"
                        );

                    string result;

                    if (string.IsNullOrWhiteSpace(url))
                    {
                        result =
                            await BrowserTools.NewTabAsync();
                    }
                    else
                    {
                        result =
                            await BrowserTools.NewTabAsync(
                                url
                            );
                    }

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // BROWSER - LIST TABS
            // =================================================

            case "browser_list_tabs":
                {
                    string result =
                        await BrowserTools.ListTabsAsync();

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // BROWSER - SWITCH TAB
            // =================================================

            case "browser_switch_tab":
                {
                    int tabNumber =
                        GetIntArgument(
                            arguments,
                            "tab_number"
                        );

                    string result =
                        await BrowserTools.SwitchTabAsync(
                            tabNumber
                        );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // BROWSER - CLOSE TAB
            // =================================================

            case "browser_close_tab":
                {
                    int tabNumber =
                        GetIntArgument(
                            arguments,
                            "tab_number"
                        );

                    string result =
                        await BrowserTools.CloseTabAsync(
                            tabNumber
                        );

                    log?.Invoke(result);

                    return result;
                }

            // =================================================
            // BROWSER - STOP
            // =================================================

            case "stop_browser":
                {
                    string result =
                        await BrowserTools.StopBrowserAsync();

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
    // SAFE STRING ARGUMENT READER
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

            return value.GetString()
                ?? "";
        }
        catch
        {
            return "";
        }
    }

    // =========================================================
    // SAFE INTEGER ARGUMENT READER
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

            if (value.ValueKind ==
                JsonValueKind.Number &&
                value.TryGetInt32(
                    out int result))
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
}

#pragma warning restore OPENAI001