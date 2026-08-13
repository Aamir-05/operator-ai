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
    // WINDOWS SAVE WORKFLOW
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

    // =========================================================
    // BROWSER BASIC TOOLS
    // =========================================================

    private static readonly FunctionTool StartBrowserTool =
        ResponseTool.CreateFunctionTool(
            functionName: "start_browser",
            functionDescription:
                "Start the persistent Operator AI Chromium browser if it is not already running.",
            functionParameters: null,
            strictModeEnabled: false
        );

    private static readonly FunctionTool BrowserSessionInfoTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_session_info",
            functionDescription:
                "Get information about the current persistent browser session, including profile location, current URL, title, and number of tabs.",
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
                "Read visible text from the current browser page.",
            functionParameters: null,
            strictModeEnabled: false
        );

    private static readonly FunctionTool BrowserListLinksTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_list_links",
            functionDescription:
                "List links in the current browser page.",
            functionParameters: null,
            strictModeEnabled: false
        );

    private static readonly FunctionTool BrowserListElementsTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_list_elements",
            functionDescription:
                "List interactive elements including buttons, inputs, links, textareas, and select elements.",
            functionParameters: null,
            strictModeEnabled: false
        );

    private static readonly FunctionTool BrowserFindTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_find",
            functionDescription:
                "Find webpage elements using locator types css, text, label, placeholder, title, or testid.",
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

    // =========================================================
    // 0.6D WAIT TOOL
    // =========================================================

    private static readonly FunctionTool BrowserWaitTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_wait",
            functionDescription:
                "Wait for a browser element to reach a state. States: visible, hidden, attached, detached.",
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
                    "state": {
                      "type": "string"
                    },
                    "timeout_seconds": {
                      "type": "integer"
                    }
                  },
                  "required": [
                    "locator_type",
                    "query",
                    "state",
                    "timeout_seconds"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    // =========================================================
    // CLICK / FILL / TYPE / PRESS
    // =========================================================

    private static readonly FunctionTool BrowserClickTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_click",
            functionDescription:
                "Click the first matching browser element.",
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
                "Replace the contents of a browser form field with text.",
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
                "Type text character-by-character into a browser field.",
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
                "Press a keyboard key on a specific browser element.",
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

    // =========================================================
    // 0.6D CHECKBOX / RADIO TOOLS
    // =========================================================

    private static readonly FunctionTool BrowserSetCheckedTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_set_checked",
            functionDescription:
                "Set a checkbox or radio element to checked or unchecked and verify the resulting state.",
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
                    "checked": {
                      "type": "boolean"
                    }
                  },
                  "required": [
                    "locator_type",
                    "query",
                    "checked"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool BrowserGetCheckedTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_get_checked",
            functionDescription:
                "Read whether a checkbox or radio element is currently checked.",
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

    // =========================================================
    // 0.6D DROPDOWN TOOL
    // =========================================================

    private static readonly FunctionTool BrowserSelectOptionTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_select_option",
            functionDescription:
                "Choose an option from a standard HTML select element. selection_type may be value, label, or index.",
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
                    "selection_type": {
                      "type": "string"
                    },
                    "selection": {
                      "type": "string"
                    }
                  },
                  "required": [
                    "locator_type",
                    "query",
                    "selection_type",
                    "selection"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    // =========================================================
    // 0.6D FILE UPLOAD
    // =========================================================

    private static readonly FunctionTool BrowserUploadTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_upload_desktop_file",
            functionDescription:
                "Upload a file from the user's Desktop into a webpage file input. The file path must be relative to Desktop.",
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
                    "relative_path": {
                      "type": "string"
                    }
                  },
                  "required": [
                    "locator_type",
                    "query",
                    "relative_path"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    // =========================================================
    // 0.6D DOWNLOAD TOOLS
    // =========================================================

    private static readonly FunctionTool BrowserDownloadTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_download",
            functionDescription:
                "Click a webpage element that starts a download and save the file under Desktop\\OperatorDownloads. preferred_relative_path may be an empty string to use the website's suggested filename.",
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
                    "preferred_relative_path": {
                      "type": "string"
                    }
                  },
                  "required": [
                    "locator_type",
                    "query",
                    "preferred_relative_path"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool BrowserListDownloadsTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_list_downloads",
            functionDescription:
                "List files downloaded by Operator AI under Desktop\\OperatorDownloads.",
            functionParameters: null,
            strictModeEnabled: false
        );

    // =========================================================
    // BROWSER NAVIGATION / TAB TOOLS
    // =========================================================

    private static readonly FunctionTool BrowserBackTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_back",
            functionDescription:
                "Navigate backward in browser history.",
            functionParameters: null,
            strictModeEnabled: false
        );

    private static readonly FunctionTool BrowserForwardTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_forward",
            functionDescription:
                "Navigate forward in browser history.",
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
                "Open a new browser tab. Pass an empty URL for a blank tab.",
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
                "List currently open Operator AI browser tabs.",
            functionParameters: null,
            strictModeEnabled: false
        );

    private static readonly FunctionTool BrowserSwitchTabTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_switch_tab",
            functionDescription:
                "Switch to a browser tab using its 1-based tab number.",
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
                "Close a browser tab using its 1-based tab number.",
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
                "Close the Operator AI Chromium browser while retaining its persistent session data.",
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

        timeoutSource.CancelAfter(
            TimeSpan.FromMinutes(7)
        );

        CancellationToken token =
            timeoutSource.Token;

        try
        {
            for (int step = 1;
                 step <= 50;
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

                            Complete the user's requested task using the available tools.

                            GENERAL

                            - Use tools for real actions.
                            - Never claim success unless a tool confirms it.
                            - Verify important results whenever practical.
                            - Prefer reliable structured browser tools over guessing.
                            - Do not repeat failed actions indefinitely.
                            - Never invent webpage contents or computer state.

                            WINDOWS

                            - Use open_application for supported Windows applications.
                            - Use list_windows when you need an actual window title.
                            - Use focus_window before desktop interaction.
                            - Use inspect_window when desktop UI state is unclear.
                            - Use type_text for desktop text.
                            - Use press_key for desktop shortcuts.
                            - Prefer save_active_document_as_desktop_file for reliable Desktop saving.

                            BROWSER SESSION

                            - Operator AI uses a persistent dedicated browser profile.
                            - Login cookies and site state may survive browser restarts.
                            - Never assume a user is logged in; inspect the page.
                            - Use browser_session_info when browser session state is useful.

                            BROWSER NAVIGATION

                            - Use start_browser before browser work when needed.
                            - Use browser_navigate to visit URLs.
                            - Use browser_get_page_info to verify title and URL.
                            - Use browser_read_page to understand page contents.
                            - Use browser_list_elements when you are unsure how to interact.
                            - Use browser_find to validate a locator.

                            LOCATORS

                            Supported locator types:
                            - css
                            - text
                            - label
                            - placeholder
                            - title
                            - testid

                            Prefer label, placeholder, or visible text when practical.
                            Use CSS when a stable human-readable locator is unavailable.

                            WAITING

                            - Use browser_wait when a page is loading dynamically or an element may appear later.
                            - Valid states are visible, hidden, attached, detached.
                            - Prefer waiting for a meaningful UI state rather than blindly repeating an action.

                            FORMS

                            - Use browser_fill for ordinary text inputs.
                            - Use browser_type only when character-by-character typing is required.
                            - Use browser_set_checked for checkboxes and radio controls.
                            - Use browser_get_checked if state verification is important.
                            - Use browser_select_option for standard HTML select dropdowns.
                            - Use browser_press for Enter, Tab, Escape, or other element-specific keys.

                            FILE UPLOADS

                            - browser_upload_desktop_file can upload only files located under Desktop.
                            - Never invent a filename.
                            - Verify the requested Desktop file exists first when useful.
                            - Uploading a file is not the same as submitting a form.

                            DOWNLOADS

                            - Use browser_download when clicking an element is expected to download a file.
                            - Downloads are stored under Desktop\OperatorDownloads.
                            - Use browser_list_downloads when verification is needed.
                            - A successful download tool result already verifies the saved file exists.

                            TABS

                            - Use browser_new_tab for a separate page.
                            - Use browser_list_tabs when tab numbers are uncertain.
                            - Use browser_switch_tab to change current page.
                            - Avoid unnecessary tabs.

                            RECOVERY

                            - ERROR, NOT_FOUND, and BLOCKED mean the strategy failed.
                            - After a locator failure, inspect browser elements or try a different locator.
                            - Do not repeat the exact same failing call.
                            - Use browser_wait for delayed elements.
                            - Stop after several unsuccessful recovery attempts and explain what failed.

                            CONSEQUENTIAL ACTIONS

                            - Reading, navigation, searching, typing into ordinary non-sensitive fields, and preparing forms may proceed when requested.
                            - Do not complete purchases, transfers, account deletion, password changes, final legal submissions, or similarly consequential actions unless the user explicitly requested that exact action.
                            - If a webpage presents a final submit/pay/delete/confirm action with major consequences and the user's request did not clearly authorize it, stop before that action.

                            COMPLETION

                            - Confirm the requested outcome before declaring completion.
                            - For research tasks, read sufficient page content.
                            - For downloads, verify the file.
                            - For uploads, confirm the file was attached when practical.
                            - For forms, verify values/state when practical.
                            - If the task cannot be completed, state the exact unresolved step.
                            """
                    };

                // =================================================
                // REGISTER WINDOWS TOOLS
                // =================================================

                options.Tools.Add(OpenApplicationTool);
                options.Tools.Add(CreateFolderTool);
                options.Tools.Add(CreateFileTool);
                options.Tools.Add(ReadFileTool);
                options.Tools.Add(FileExistsTool);
                options.Tools.Add(ListDesktopTool);
                options.Tools.Add(ListWindowsTool);
                options.Tools.Add(InspectWindowTool);
                options.Tools.Add(FocusWindowTool);
                options.Tools.Add(TypeTextTool);
                options.Tools.Add(PressKeyTool);
                options.Tools.Add(SaveActiveDocumentTool);

                // =================================================
                // REGISTER BROWSER TOOLS
                // =================================================

                options.Tools.Add(StartBrowserTool);
                options.Tools.Add(BrowserSessionInfoTool);
                options.Tools.Add(BrowserNavigateTool);
                options.Tools.Add(BrowserPageInfoTool);
                options.Tools.Add(BrowserReadPageTool);
                options.Tools.Add(BrowserListLinksTool);
                options.Tools.Add(BrowserListElementsTool);
                options.Tools.Add(BrowserFindTool);
                options.Tools.Add(BrowserWaitTool);
                options.Tools.Add(BrowserClickTool);
                options.Tools.Add(BrowserFillTool);
                options.Tools.Add(BrowserTypeTool);
                options.Tools.Add(BrowserPressTool);
                options.Tools.Add(BrowserPageKeyTool);
                options.Tools.Add(BrowserSetCheckedTool);
                options.Tools.Add(BrowserGetCheckedTool);
                options.Tools.Add(BrowserSelectOptionTool);
                options.Tools.Add(BrowserUploadTool);
                options.Tools.Add(BrowserDownloadTool);
                options.Tools.Add(BrowserListDownloadsTool);
                options.Tools.Add(BrowserBackTool);
                options.Tools.Add(BrowserForwardTool);
                options.Tools.Add(BrowserReloadTool);
                options.Tools.Add(BrowserNewTabTool);
                options.Tools.Add(BrowserListTabsTool);
                options.Tools.Add(BrowserSwitchTabTool);
                options.Tools.Add(BrowserCloseTabTool);
                options.Tools.Add(StopBrowserTool);

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
                "TIMEOUT: Task exceeded the 7-minute limit and was stopped.";

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
            // WINDOWS
            // =================================================

            case "open_application":
                {
                    string app =
                        GetStringArgument(
                            arguments,
                            "application"
                        );

                    return
                        WindowsTools.OpenApplication(
                            app
                        );
                }

            case "create_desktop_folder":
                {
                    string folder =
                        GetStringArgument(
                            arguments,
                            "folder_name"
                        );

                    return
                        WindowsTools.CreateDesktopFolder(
                            folder
                        );
                }

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

                    return
                        WindowsTools.CreateDesktopFile(
                            path,
                            content
                        );
                }

            case "read_desktop_file":
                {
                    string path =
                        GetStringArgument(
                            arguments,
                            "relative_path"
                        );

                    return
                        WindowsTools.ReadDesktopFile(
                            path
                        );
                }

            case "desktop_file_exists":
                {
                    string path =
                        GetStringArgument(
                            arguments,
                            "relative_path"
                        );

                    return
                        WindowsTools.DesktopFileExists(
                            path
                        );
                }

            case "list_desktop":
                {
                    return
                        WindowsTools.ListDesktop();
                }

            case "list_windows":
                {
                    return
                        WindowsUiTools.ListWindows();
                }

            case "inspect_window":
                {
                    string title =
                        GetStringArgument(
                            arguments,
                            "window_title"
                        );

                    return
                        WindowsUiTools.InspectWindow(
                            title
                        );
                }

            case "focus_window":
                {
                    string title =
                        GetStringArgument(
                            arguments,
                            "window_title"
                        );

                    return
                        WindowsUiTools.FocusWindow(
                            title
                        );
                }

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

                    return
                        WindowsUiTools.TypeText(
                            title,
                            text
                        );
                }

            case "press_key":
                {
                    string keys =
                        GetStringArgument(
                            arguments,
                            "keys"
                        );

                    return
                        WindowsInputTools.PressKey(
                            keys
                        );
                }

            case "save_active_document_as_desktop_file":
                {
                    string path =
                        GetStringArgument(
                            arguments,
                            "relative_path"
                        );

                    return
                        WindowsWorkflowTools
                            .SaveActiveDocumentAsDesktopFile(
                                path
                            );
                }

            // =================================================
            // BROWSER BASIC
            // =================================================

            case "start_browser":
                {
                    return
                        await BrowserTools
                            .StartBrowserAsync();
                }

            case "browser_session_info":
                {
                    return
                        await BrowserTools
                            .GetSessionInfoAsync();
                }

            case "browser_navigate":
                {
                    string url =
                        GetStringArgument(
                            arguments,
                            "url"
                        );

                    return
                        await BrowserTools
                            .NavigateAsync(
                                url
                            );
                }

            case "browser_get_page_info":
                {
                    return
                        await BrowserTools
                            .GetPageInfoAsync();
                }

            case "browser_read_page":
                {
                    return
                        await BrowserTools
                            .ReadPageTextAsync();
                }

            case "browser_list_links":
                {
                    return
                        await BrowserTools
                            .ListLinksAsync();
                }

            case "browser_list_elements":
                {
                    return
                        await BrowserTools
                            .ListInteractiveElementsAsync();
                }

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

                    return
                        await BrowserTools
                            .FindElementsAsync(
                                locatorType,
                                query
                            );
                }

            // =================================================
            // BROWSER WAIT
            // =================================================

            case "browser_wait":
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

                    string state =
                        GetStringArgument(
                            arguments,
                            "state"
                        );

                    int timeoutSeconds =
                        GetIntArgument(
                            arguments,
                            "timeout_seconds"
                        );

                    return
                        await BrowserTools
                            .WaitForElementAsync(
                                locatorType,
                                query,
                                state,
                                timeoutSeconds
                            );
                }

            // =================================================
            // BROWSER INPUT
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

                    return
                        await BrowserTools
                            .ClickAsync(
                                locatorType,
                                query
                            );
                }

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

                    return
                        await BrowserTools
                            .FillAsync(
                                locatorType,
                                query,
                                text
                            );
                }

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

                    return
                        await BrowserTools
                            .TypeAsync(
                                locatorType,
                                query,
                                text
                            );
                }

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

                    return
                        await BrowserTools
                            .PressAsync(
                                locatorType,
                                query,
                                key
                            );
                }

            case "browser_page_key":
                {
                    string key =
                        GetStringArgument(
                            arguments,
                            "key"
                        );

                    return
                        await BrowserTools
                            .PressPageKeyAsync(
                                key
                            );
                }

            // =================================================
            // CHECKBOX / RADIO
            // =================================================

            case "browser_set_checked":
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

                    bool checkedState =
                        GetBoolArgument(
                            arguments,
                            "checked"
                        );

                    return
                        await BrowserTools
                            .SetCheckedAsync(
                                locatorType,
                                query,
                                checkedState
                            );
                }

            case "browser_get_checked":
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

                    return
                        await BrowserTools
                            .GetCheckedStateAsync(
                                locatorType,
                                query
                            );
                }

            // =================================================
            // DROPDOWN
            // =================================================

            case "browser_select_option":
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

                    string selectionType =
                        GetStringArgument(
                            arguments,
                            "selection_type"
                        );

                    string selection =
                        GetStringArgument(
                            arguments,
                            "selection"
                        );

                    return
                        await BrowserTools
                            .SelectOptionAsync(
                                locatorType,
                                query,
                                selectionType,
                                selection
                            );
                }

            // =================================================
            // UPLOAD
            // =================================================

            case "browser_upload_desktop_file":
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

                    string relativePath =
                        GetStringArgument(
                            arguments,
                            "relative_path"
                        );

                    return
                        await BrowserTools
                            .UploadDesktopFileAsync(
                                locatorType,
                                query,
                                relativePath
                            );
                }

            // =================================================
            // DOWNLOAD
            // =================================================

            case "browser_download":
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

                    string preferredRelativePath =
                        GetStringArgument(
                            arguments,
                            "preferred_relative_path"
                        );

                    return
                        await BrowserTools
                            .DownloadByClickAsync(
                                locatorType,
                                query,
                                preferredRelativePath
                            );
                }

            case "browser_list_downloads":
                {
                    return
                        BrowserTools.ListDownloads();
                }

            // =================================================
            // HISTORY / TABS
            // =================================================

            case "browser_back":
                {
                    return
                        await BrowserTools
                            .BackAsync();
                }

            case "browser_forward":
                {
                    return
                        await BrowserTools
                            .ForwardAsync();
                }

            case "browser_reload":
                {
                    return
                        await BrowserTools
                            .ReloadAsync();
                }

            case "browser_new_tab":
                {
                    string url =
                        GetStringArgument(
                            arguments,
                            "url"
                        );

                    if (string.IsNullOrWhiteSpace(
                            url))
                    {
                        return
                            await BrowserTools
                                .NewTabAsync();
                    }

                    return
                        await BrowserTools
                            .NewTabAsync(
                                url
                            );
                }

            case "browser_list_tabs":
                {
                    return
                        await BrowserTools
                            .ListTabsAsync();
                }

            case "browser_switch_tab":
                {
                    int tabNumber =
                        GetIntArgument(
                            arguments,
                            "tab_number"
                        );

                    return
                        await BrowserTools
                            .SwitchTabAsync(
                                tabNumber
                            );
                }

            case "browser_close_tab":
                {
                    int tabNumber =
                        GetIntArgument(
                            arguments,
                            "tab_number"
                        );

                    return
                        await BrowserTools
                            .CloseTabAsync(
                                tabNumber
                            );
                }

            case "stop_browser":
                {
                    return
                        await BrowserTools
                            .StopBrowserAsync();
                }

            // =================================================
            // UNKNOWN TOOL
            // =================================================

            default:
                {
                    return
                        $"ERROR: Unknown tool '{call.FunctionName}'.";
                }
        }
    }

    // =========================================================
    // JSON HELPERS
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

    private static bool GetBoolArgument(
        JsonElement arguments,
        string propertyName)
    {
        try
        {
            if (arguments.ValueKind !=
                JsonValueKind.Object)
            {
                return false;
            }

            if (!arguments.TryGetProperty(
                    propertyName,
                    out JsonElement value))
            {
                return false;
            }

            if (value.ValueKind ==
                JsonValueKind.True)
            {
                return true;
            }

            if (value.ValueKind ==
                JsonValueKind.False)
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