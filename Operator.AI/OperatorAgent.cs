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
    // WINDOWS APPLICATION
    // =========================================================

    private static readonly FunctionTool OpenApplicationTool =
        ResponseTool.CreateFunctionTool(
            functionName: "open_application",
            functionDescription:
                "Open an approved Windows application such as notepad, calculator, or edge.",
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
    // DESKTOP FILES
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
                "Create or overwrite a text file under the Windows Desktop. relative_path may contain subfolders.",
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
    // EXISTING WINDOWS UI
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
                "Type or paste text into the editable area of a visible Windows application.",
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
                "Press a Windows keyboard key or shortcut such as CTRL+S, CTRL+A, ALT+F4, ENTER, TAB, ESC, LEFT, RIGHT, UP, or DOWN.",
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

    private static readonly FunctionTool SaveActiveDocumentTool =
        ResponseTool.CreateFunctionTool(
            functionName:
                "save_active_document_as_desktop_file",
            functionDescription:
                "Reliably save the currently active Windows document to a file under the Desktop.",
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
    // VERSION 0.7A
    // NATIVE WINDOWS UI AUTOMATION
    // =========================================================

    private static readonly FunctionTool WindowsListControlsTool =
        ResponseTool.CreateFunctionTool(
            functionName: "windows_list_controls",
            functionDescription:
                "List native Windows UI Automation controls inside a window. Use this before guessing control names. window_title may be an application title or __FOREGROUND__ for the active window.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "window_title": {
                      "type": "string"
                    },
                    "maximum_controls": {
                      "type": "integer"
                    }
                  },
                  "required": [
                    "window_title",
                    "maximum_controls"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool WindowsFindControlTool =
        ResponseTool.CreateFunctionTool(
            functionName: "windows_find_control",
            functionDescription:
                "Find a native Windows control by control type and accessible name or AutomationId.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "window_title": {
                      "type": "string"
                    },
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
                    "window_title",
                    "control_type",
                    "control_name",
                    "exact_name"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool WindowsGetControlInfoTool =
        ResponseTool.CreateFunctionTool(
            functionName: "windows_get_control_info",
            functionDescription:
                "Inspect a native Windows control and report its identity, bounds, enabled/focusable state, and supported UI Automation patterns.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "window_title": {
                      "type": "string"
                    },
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
                    "window_title",
                    "control_type",
                    "control_name",
                    "exact_name"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool WindowsSetControlValueTool =
        ResponseTool.CreateFunctionTool(
            functionName: "windows_set_control_value",
            functionDescription:
                "Set the value of a native Windows editable control through UI Automation ValuePattern.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "window_title": {
                      "type": "string"
                    },
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
                    "window_title",
                    "control_type",
                    "control_name",
                    "exact_name",
                    "value"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool WindowsGetControlValueTool =
        ResponseTool.CreateFunctionTool(
            functionName: "windows_get_control_value",
            functionDescription:
                "Read the current value or text from a native Windows control through UI Automation.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "window_title": {
                      "type": "string"
                    },
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
                    "window_title",
                    "control_type",
                    "control_name",
                    "exact_name"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool WindowsClickControlTool =
        ResponseTool.CreateFunctionTool(
            functionName: "windows_click_control",
            functionDescription:
                "Activate a native Windows control using its supported UI Automation pattern, preferring Invoke, SelectionItem, Toggle, or ExpandCollapse instead of mouse coordinates.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "window_title": {
                      "type": "string"
                    },
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
                    "window_title",
                    "control_type",
                    "control_name",
                    "exact_name"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool WindowsSetToggleTool =
        ResponseTool.CreateFunctionTool(
            functionName: "windows_set_toggle",
            functionDescription:
                "Set a native Windows checkbox or toggle control to a requested on/off state using TogglePattern.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "window_title": {
                      "type": "string"
                    },
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
                    "window_title",
                    "control_type",
                    "control_name",
                    "exact_name",
                    "checked"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool WindowsGetToggleTool =
        ResponseTool.CreateFunctionTool(
            functionName: "windows_get_toggle",
            functionDescription:
                "Read the current TogglePattern state of a native Windows checkbox or toggle control.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "window_title": {
                      "type": "string"
                    },
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
                    "window_title",
                    "control_type",
                    "control_name",
                    "exact_name"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool WindowsSelectControlTool =
        ResponseTool.CreateFunctionTool(
            functionName: "windows_select_control",
            functionDescription:
                "Select a native Windows tab item, list item, or other selectable control through SelectionItemPattern.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "window_title": {
                      "type": "string"
                    },
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
                    "window_title",
                    "control_type",
                    "control_name",
                    "exact_name"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool WindowsSetExpandedTool =
        ResponseTool.CreateFunctionTool(
            functionName: "windows_set_expanded",
            functionDescription:
                "Expand or collapse a native Windows ComboBox, menu, tree item, or other ExpandCollapsePattern control.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "window_title": {
                      "type": "string"
                    },
                    "control_type": {
                      "type": "string"
                    },
                    "control_name": {
                      "type": "string"
                    },
                    "exact_name": {
                      "type": "boolean"
                    },
                    "expanded": {
                      "type": "boolean"
                    }
                  },
                  "required": [
                    "window_title",
                    "control_type",
                    "control_name",
                    "exact_name",
                    "expanded"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool WindowsFocusControlTool =
        ResponseTool.CreateFunctionTool(
            functionName: "windows_focus_control",
            functionDescription:
                "Move keyboard focus to a native Windows control identified by type and accessible name.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "window_title": {
                      "type": "string"
                    },
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
                    "window_title",
                    "control_type",
                    "control_name",
                    "exact_name"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool WindowsWaitForWindowTool =
        ResponseTool.CreateFunctionTool(
            functionName: "windows_wait_for_window",
            functionDescription:
                "Wait for a native Windows application window or dialog to become available.",
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

    private static readonly FunctionTool WindowsWaitForControlTool =
        ResponseTool.CreateFunctionTool(
            functionName: "windows_wait_for_control",
            functionDescription:
                "Wait for a native Windows control to become available inside a window or dialog.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "window_title": {
                      "type": "string"
                    },
                    "control_type": {
                      "type": "string"
                    },
                    "control_name": {
                      "type": "string"
                    },
                    "exact_name": {
                      "type": "boolean"
                    },
                    "timeout_seconds": {
                      "type": "integer"
                    }
                  },
                  "required": [
                    "window_title",
                    "control_type",
                    "control_name",
                    "exact_name",
                    "timeout_seconds"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    // =========================================================
    // BROWSER CORE
    // =========================================================

    private static readonly FunctionTool StartBrowserTool =
        ResponseTool.CreateFunctionTool(
            functionName: "start_browser",
            functionDescription:
                "Start the persistent Operator AI Chromium browser.",
            functionParameters: null,
            strictModeEnabled: false
        );

    private static readonly FunctionTool BrowserSessionInfoTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_session_info",
            functionDescription:
                "Get browser session information including current URL, title, persistent profile, and open tabs.",
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
                "Read visible textual content from the current browser page.",
            functionParameters: null,
            strictModeEnabled: false
        );

    private static readonly FunctionTool BrowserListLinksTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_list_links",
            functionDescription:
                "List links visible on the current browser page.",
            functionParameters: null,
            strictModeEnabled: false
        );

    private static readonly FunctionTool BrowserListElementsTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_list_elements",
            functionDescription:
                "Inspect interactive elements on the current browser page.",
            functionParameters: null,
            strictModeEnabled: false
        );

    // =========================================================
    // GENERIC BROWSER LOCATORS
    // =========================================================

    private static readonly FunctionTool BrowserFindTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_find",
            functionDescription:
                "Find webpage elements. Supported locator types: css, text, exact_text, label, placeholder, title, testid, alt.",
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
    // BROWSER ROLE TARGETING
    // =========================================================

    private static readonly FunctionTool BrowserRoleFindTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_role_find",
            functionDescription:
                "Find elements by ARIA role and accessible name.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "role": {
                      "type": "string"
                    },
                    "name": {
                      "type": "string"
                    },
                    "exact": {
                      "type": "boolean"
                    }
                  },
                  "required": [
                    "role",
                    "name",
                    "exact"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool BrowserRoleClickTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_role_click",
            functionDescription:
                "Click an element by ARIA role and accessible name.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "role": {
                      "type": "string"
                    },
                    "name": {
                      "type": "string"
                    },
                    "exact": {
                      "type": "boolean"
                    }
                  },
                  "required": [
                    "role",
                    "name",
                    "exact"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool BrowserRoleFillTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_role_fill",
            functionDescription:
                "Fill an editable browser element using its ARIA role and accessible name.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "role": {
                      "type": "string"
                    },
                    "name": {
                      "type": "string"
                    },
                    "exact": {
                      "type": "boolean"
                    },
                    "text": {
                      "type": "string"
                    }
                  },
                  "required": [
                    "role",
                    "name",
                    "exact",
                    "text"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool BrowserRoleWaitTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_role_wait",
            functionDescription:
                "Wait for an element identified by ARIA role and accessible name.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "role": {
                      "type": "string"
                    },
                    "name": {
                      "type": "string"
                    },
                    "exact": {
                      "type": "boolean"
                    },
                    "state": {
                      "type": "string"
                    },
                    "timeout_seconds": {
                      "type": "integer"
                    }
                  },
                  "required": [
                    "role",
                    "name",
                    "exact",
                    "state",
                    "timeout_seconds"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool BrowserRoleGetTextTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_role_get_text",
            functionDescription:
                "Read text from a browser element identified by ARIA role and accessible name.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "role": {
                      "type": "string"
                    },
                    "name": {
                      "type": "string"
                    },
                    "exact": {
                      "type": "boolean"
                    }
                  },
                  "required": [
                    "role",
                    "name",
                    "exact"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool BrowserExactTextTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_exact_text",
            functionDescription:
                "Find a browser element whose visible text exactly matches the supplied text.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "text": {
                      "type": "string"
                    }
                  },
                  "required": ["text"],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    // =========================================================
    // BROWSER VISION
    // =========================================================

    private static readonly FunctionTool BrowserVisualInspectTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_visual_inspect",
            functionDescription:
                "Observe the current browser page visually using a screenshot and image understanding. This tool observes only. It does not click or modify the page.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "question": {
                      "type": "string"
                    },
                    "full_page": {
                      "type": "boolean"
                    }
                  },
                  "required": [
                    "question",
                    "full_page"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    // =========================================================
    // BROWSER SCREENSHOTS
    // =========================================================

    private static readonly FunctionTool BrowserScreenshotTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_screenshot",
            functionDescription:
                "Capture the browser page under Desktop\\OperatorScreenshots. Use full_page=false when a screenshot may be used for coordinate interaction.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "relative_path": {
                      "type": "string"
                    },
                    "full_page": {
                      "type": "boolean"
                    }
                  },
                  "required": [
                    "relative_path",
                    "full_page"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool BrowserListScreenshotsTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_list_screenshots",
            functionDescription:
                "List screenshots captured under Desktop\\OperatorScreenshots.",
            functionParameters: null,
            strictModeEnabled: false
        );

    // =========================================================
    // SAFE BROWSER COORDINATE CONTROL
    // =========================================================

    private static readonly FunctionTool BrowserGetViewportTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_get_viewport",
            functionDescription:
                "Read the current browser viewport width, height, scroll position, URL, and whether a recent screenshot is available for safe coordinate clicking.",
            functionParameters: null,
            strictModeEnabled: false
        );

    private static readonly FunctionTool BrowserElementBoxTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_element_box",
            functionDescription:
                "Read the viewport bounding box and center coordinate of a structured browser element.",
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

    private static readonly FunctionTool BrowserMouseMoveTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_mouse_move",
            functionDescription:
                "Move the browser mouse to a viewport coordinate without clicking.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "x": {
                      "type": "integer"
                    },
                    "y": {
                      "type": "integer"
                    }
                  },
                  "required": [
                    "x",
                    "y"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool BrowserMouseClickTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_mouse_click",
            functionDescription:
                "Perform a guarded visual coordinate click inside the browser viewport. Use only when structured targeting cannot identify the target.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "x": {
                      "type": "integer"
                    },
                    "y": {
                      "type": "integer"
                    }
                  },
                  "required": [
                    "x",
                    "y"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool BrowserMouseDoubleClickTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_mouse_double_click",
            functionDescription:
                "Perform a guarded visual coordinate double-click inside the current browser viewport.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "x": {
                      "type": "integer"
                    },
                    "y": {
                      "type": "integer"
                    }
                  },
                  "required": [
                    "x",
                    "y"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    // =========================================================
    // BROWSER ELEMENT INSPECTION
    // =========================================================

    private static readonly FunctionTool BrowserGetTextTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_get_text",
            functionDescription:
                "Read text from the first browser element matching a locator.",
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

    private static readonly FunctionTool BrowserGetAttributeTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_get_attribute",
            functionDescription:
                "Read an HTML attribute from the first matching browser element.",
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
                    "attribute_name": {
                      "type": "string"
                    }
                  },
                  "required": [
                    "locator_type",
                    "query",
                    "attribute_name"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool BrowserGetValueTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_get_value",
            functionDescription:
                "Read the current value of a browser input or textarea.",
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

    private static readonly FunctionTool BrowserIsVisibleTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_is_visible",
            functionDescription:
                "Check whether the first matching browser element is currently visible.",
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
    // BROWSER WAITING
    // =========================================================

    private static readonly FunctionTool BrowserWaitTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_wait",
            functionDescription:
                "Wait for a browser element state: visible, hidden, attached, or detached.",
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

    private static readonly FunctionTool BrowserWaitForUrlTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_wait_for_url",
            functionDescription:
                "Wait for the current browser URL to match a URL or Playwright glob pattern.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "url_pattern": {
                      "type": "string"
                    },
                    "timeout_seconds": {
                      "type": "integer"
                    }
                  },
                  "required": [
                    "url_pattern",
                    "timeout_seconds"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool BrowserWaitForTextTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_wait_for_text",
            functionDescription:
                "Wait for visible text to appear on the current browser page.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "text": {
                      "type": "string"
                    },
                    "exact": {
                      "type": "boolean"
                    },
                    "timeout_seconds": {
                      "type": "integer"
                    }
                  },
                  "required": [
                    "text",
                    "exact",
                    "timeout_seconds"
                  ],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    // =========================================================
    // BROWSER CLICK / FILL / TYPE
    // =========================================================

    private static readonly FunctionTool BrowserClickTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_click",
            functionDescription:
                "Click the first browser element matching a generic locator.",
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
                "Replace the contents of a browser field with text.",
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
                "Type text character by character into a browser field.",
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
    // BROWSER SCROLL
    // =========================================================

    private static readonly FunctionTool BrowserScrollTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_scroll",
            functionDescription:
                "Scroll the current browser page vertically.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "delta_y": {
                      "type": "integer"
                    }
                  },
                  "required": ["delta_y"],
                  "additionalProperties": false
                }
                """
            ),
            strictModeEnabled: true
        );

    private static readonly FunctionTool BrowserScrollToTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_scroll_to",
            functionDescription:
                "Scroll a matching browser element into view.",
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
    // BROWSER CHECKBOX
    // =========================================================

    private static readonly FunctionTool BrowserSetCheckedTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_set_checked",
            functionDescription:
                "Set a checkbox or radio control to checked or unchecked.",
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
                "Read whether a checkbox or radio control is currently checked.",
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
    // BROWSER DROPDOWN
    // =========================================================

    private static readonly FunctionTool BrowserSelectOptionTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_select_option",
            functionDescription:
                "Select an option from a standard HTML select control.",
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
    // BROWSER UPLOAD / DOWNLOAD
    // =========================================================

    private static readonly FunctionTool BrowserUploadTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_upload_desktop_file",
            functionDescription:
                "Upload a file located under the Windows Desktop into a webpage file input.",
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

    private static readonly FunctionTool BrowserDownloadTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_download",
            functionDescription:
                "Click an element expected to trigger a download and save it under Desktop\\OperatorDownloads.",
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
                "List files downloaded under Desktop\\OperatorDownloads.",
            functionParameters: null,
            strictModeEnabled: false
        );

    // =========================================================
    // BROWSER NAVIGATION / TABS
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
                "Switch to a browser tab by its 1-based tab number.",
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
                "Close the Operator AI browser while retaining persistent session data.",
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
            TimeSpan.FromMinutes(10)
        );

        CancellationToken token =
            timeoutSource.Token;

        try
        {
            for (int step = 1;
                 step <= 70;
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

                            =================================================
                            CORE
                            =================================================

                            - Use tools for real actions.
                            - Never claim success without tool confirmation.
                            - Verify important outcomes.
                            - Never invent files, URLs, windows, controls,
                              page contents, or computer state.
                            - Prefer deterministic structured automation.
                            - Do not repeatedly issue the exact same failing call.
                            - If a control name is uncertain, inspect first.

                            =================================================
                            WINDOWS TARGETING PRIORITY
                            =================================================

                            For native Windows applications prefer:

                            1. windows_list_controls
                            2. windows_find_control
                            3. windows_get_control_info
                            4. direct UI Automation pattern operations
                            5. keyboard automation only when structured
                               UI Automation is insufficient

                            Prefer native UI Automation over keyboard
                            navigation and over coordinate guessing.

                            =================================================
                            WINDOWS WINDOW DISCOVERY
                            =================================================

                            Use list_windows to discover top-level windows.

                            Use windows_wait_for_window after opening an
                            application or when a dialog is expected.

                            Use an explicit window title whenever known.

                            "__FOREGROUND__" may be used when interacting
                            with the currently active dialog or window and
                            its identity has already been established.

                            Do not assume the foreground window is correct
                            without first inspecting or verifying it.

                            =================================================
                            WINDOWS CONTROL DISCOVERY
                            =================================================

                            Use windows_list_controls before guessing native
                            control names.

                            Native control types include common values such as:

                            button
                            edit
                            checkbox
                            combobox
                            list
                            listitem
                            menu
                            menuitem
                            radiobutton
                            tab
                            tabitem
                            tree
                            treeitem
                            text

                            Prefer exact control names when available.

                            Set exact_name=false only when exact targeting is
                            not possible and the partial name is unambiguous.

                            =================================================
                            WINDOWS TEXTBOXES
                            =================================================

                            Prefer windows_set_control_value for an editable
                            native control that supports ValuePattern.

                            After changing important text, verify using:

                            windows_get_control_value

                            Do not use type_text merely because a textbox is
                            visible if structured ValuePattern works.

                            =================================================
                            WINDOWS BUTTONS
                            =================================================

                            Before activating an unfamiliar button, use:

                            windows_get_control_info

                            windows_click_control uses the supported native
                            UI Automation pattern instead of screen coordinates.

                            After clicking an important control, verify the
                            resulting state, window, text, or dialog.

                            =================================================
                            WINDOWS CHECKBOXES / TOGGLES
                            =================================================

                            Use:

                            windows_set_toggle
                            windows_get_toggle

                            Do not blindly click a checkbox when its requested
                            state can be set and verified deterministically.

                            =================================================
                            WINDOWS TABS / LIST ITEMS
                            =================================================

                            Use windows_select_control for controls that expose
                            SelectionItemPattern, such as tab items and many
                            list items.

                            Verify the resulting interface state afterward.

                            =================================================
                            WINDOWS COMBOBOX / EXPANDABLE CONTROLS
                            =================================================

                            Use windows_set_expanded to expand or collapse
                            controls supporting ExpandCollapsePattern.

                            Inspect newly exposed controls after expanding.

                            =================================================
                            WINDOWS WAITING
                            =================================================

                            Use:

                            windows_wait_for_window
                            windows_wait_for_control

                            rather than repeatedly retrying immediately.

                            =================================================
                            WINDOWS FALLBACK
                            =================================================

                            Existing tools remain available:

                            open_application
                            list_windows
                            inspect_window
                            focus_window
                            type_text
                            press_key
                            save_active_document_as_desktop_file

                            Use keyboard-based tools only when native control
                            automation is unavailable or insufficient.

                            =================================================
                            HIGH-CONSEQUENCE WINDOWS ACTIONS
                            =================================================

                            Do not finalize high-consequence actions such as:

                            - purchases
                            - payments
                            - money transfers
                            - permanent account deletion
                            - password/security changes
                            - irreversible administrative changes
                            - destructive data deletion
                            - final legal submissions

                            unless the user's instruction clearly authorizes
                            that exact final action.

                            For consequential controls, inspect the exact
                            control and verify the surrounding state before
                            activating it.

                            =================================================
                            BROWSER TARGETING PRIORITY
                            =================================================

                            Prefer:

                            1. ARIA role + accessible name
                            2. label
                            3. placeholder
                            4. exact visible text
                            5. testid
                            6. title / alt
                            7. stable CSS
                            8. visual coordinate interaction only as fallback

                            =================================================
                            BROWSER VISUAL INSPECTION
                            =================================================

                            browser_visual_inspect observes a screenshot.

                            It does not click.

                            Use it when structured DOM/role information is
                            insufficient.

                            Prefer full_page=false when visual information may
                            later be used for coordinate interaction.

                            =================================================
                            BROWSER COORDINATE INTERACTION
                            =================================================

                            Coordinate clicking is fallback-only.

                            Before browser_mouse_click or
                            browser_mouse_double_click:

                            1. Try structured targeting.
                            2. Establish structured targeting is insufficient.
                            3. Capture or inspect a fresh viewport screenshot.
                            4. Determine the target coordinate.
                            5. Verify the viewport if useful.
                            6. Click only when the target is clear.
                            7. Verify the resulting state.

                            Never:
                            - derive coordinates from a full-page screenshot,
                            - scroll after capture then click from stale data,
                            - navigate after capture then use stale coordinates,
                            - probe random coordinates.

                            =================================================
                            COMPLETION
                            =================================================

                            Do not declare completion until the requested
                            result has been reasonably verified.

                            Distinguish:
                            - actions actually performed,
                            - state verified through tools,
                            - visual inference,
                            - assumptions.
                            """
                    };

                // =================================================
                // WINDOWS BASIC
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
                // WINDOWS 0.7A NATIVE UI AUTOMATION
                // =================================================

                options.Tools.Add(WindowsListControlsTool);
                options.Tools.Add(WindowsFindControlTool);
                options.Tools.Add(WindowsGetControlInfoTool);
                options.Tools.Add(WindowsSetControlValueTool);
                options.Tools.Add(WindowsGetControlValueTool);
                options.Tools.Add(WindowsClickControlTool);
                options.Tools.Add(WindowsSetToggleTool);
                options.Tools.Add(WindowsGetToggleTool);
                options.Tools.Add(WindowsSelectControlTool);
                options.Tools.Add(WindowsSetExpandedTool);
                options.Tools.Add(WindowsFocusControlTool);
                options.Tools.Add(WindowsWaitForWindowTool);
                options.Tools.Add(WindowsWaitForControlTool);

                // =================================================
                // BROWSER CORE
                // =================================================

                options.Tools.Add(StartBrowserTool);
                options.Tools.Add(BrowserSessionInfoTool);
                options.Tools.Add(BrowserNavigateTool);
                options.Tools.Add(BrowserPageInfoTool);
                options.Tools.Add(BrowserReadPageTool);
                options.Tools.Add(BrowserListLinksTool);
                options.Tools.Add(BrowserListElementsTool);
                options.Tools.Add(BrowserFindTool);

                // =================================================
                // BROWSER SEMANTIC
                // =================================================

                options.Tools.Add(BrowserRoleFindTool);
                options.Tools.Add(BrowserRoleClickTool);
                options.Tools.Add(BrowserRoleFillTool);
                options.Tools.Add(BrowserRoleWaitTool);
                options.Tools.Add(BrowserRoleGetTextTool);
                options.Tools.Add(BrowserExactTextTool);

                // =================================================
                // BROWSER VISION
                // =================================================

                options.Tools.Add(BrowserVisualInspectTool);

                // =================================================
                // BROWSER SCREENSHOTS
                // =================================================

                options.Tools.Add(BrowserScreenshotTool);
                options.Tools.Add(BrowserListScreenshotsTool);

                // =================================================
                // BROWSER COORDINATES
                // =================================================

                options.Tools.Add(BrowserGetViewportTool);
                options.Tools.Add(BrowserElementBoxTool);
                options.Tools.Add(BrowserMouseMoveTool);
                options.Tools.Add(BrowserMouseClickTool);
                options.Tools.Add(BrowserMouseDoubleClickTool);

                // =================================================
                // BROWSER INSPECTION
                // =================================================

                options.Tools.Add(BrowserGetTextTool);
                options.Tools.Add(BrowserGetAttributeTool);
                options.Tools.Add(BrowserGetValueTool);
                options.Tools.Add(BrowserIsVisibleTool);

                // =================================================
                // BROWSER WAIT
                // =================================================

                options.Tools.Add(BrowserWaitTool);
                options.Tools.Add(BrowserWaitForUrlTool);
                options.Tools.Add(BrowserWaitForTextTool);

                // =================================================
                // BROWSER INPUT
                // =================================================

                options.Tools.Add(BrowserClickTool);
                options.Tools.Add(BrowserFillTool);
                options.Tools.Add(BrowserTypeTool);
                options.Tools.Add(BrowserPressTool);
                options.Tools.Add(BrowserPageKeyTool);

                // =================================================
                // BROWSER SCROLL
                // =================================================

                options.Tools.Add(BrowserScrollTool);
                options.Tools.Add(BrowserScrollToTool);

                // =================================================
                // BROWSER FORMS
                // =================================================

                options.Tools.Add(BrowserSetCheckedTool);
                options.Tools.Add(BrowserGetCheckedTool);
                options.Tools.Add(BrowserSelectOptionTool);

                // =================================================
                // BROWSER FILES
                // =================================================

                options.Tools.Add(BrowserUploadTool);
                options.Tools.Add(BrowserDownloadTool);
                options.Tools.Add(BrowserListDownloadsTool);

                // =================================================
                // BROWSER NAVIGATION
                // =================================================

                options.Tools.Add(BrowserBackTool);
                options.Tools.Add(BrowserForwardTool);
                options.Tools.Add(BrowserReloadTool);
                options.Tools.Add(BrowserNewTabTool);
                options.Tools.Add(BrowserListTabsTool);
                options.Tools.Add(BrowserSwitchTabTool);
                options.Tools.Add(BrowserCloseTabTool);
                options.Tools.Add(StopBrowserTool);

                // =================================================
                // MODEL
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
                "Agent stopped because the maximum number of planning steps was reached.";
        }
        catch (OperationCanceledException)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return
                    "CANCELLED: Task stopped by the user.";
            }

            return
                "TIMEOUT: Task exceeded the 10-minute limit and was stopped.";
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
            // WINDOWS BASIC
            // =================================================

            case "open_application":
                return WindowsTools.OpenApplication(
                    GetStringArgument(
                        arguments,
                        "application"
                    )
                );

            case "create_desktop_folder":
                return WindowsTools.CreateDesktopFolder(
                    GetStringArgument(
                        arguments,
                        "folder_name"
                    )
                );

            case "create_desktop_file":
                return WindowsTools.CreateDesktopFile(
                    GetStringArgument(
                        arguments,
                        "relative_path"
                    ),
                    GetStringArgument(
                        arguments,
                        "content"
                    )
                );

            case "read_desktop_file":
                return WindowsTools.ReadDesktopFile(
                    GetStringArgument(
                        arguments,
                        "relative_path"
                    )
                );

            case "desktop_file_exists":
                return WindowsTools.DesktopFileExists(
                    GetStringArgument(
                        arguments,
                        "relative_path"
                    )
                );

            case "list_desktop":
                return WindowsTools.ListDesktop();

            case "list_windows":
                return WindowsUiTools.ListWindows();

            case "inspect_window":
                return WindowsUiTools.InspectWindow(
                    GetStringArgument(
                        arguments,
                        "window_title"
                    )
                );

            case "focus_window":
                return WindowsUiTools.FocusWindow(
                    GetStringArgument(
                        arguments,
                        "window_title"
                    )
                );

            case "type_text":
                return WindowsUiTools.TypeText(
                    GetStringArgument(
                        arguments,
                        "window_title"
                    ),
                    GetStringArgument(
                        arguments,
                        "text"
                    )
                );

            case "press_key":
                return WindowsInputTools.PressKey(
                    GetStringArgument(
                        arguments,
                        "keys"
                    )
                );

            case "save_active_document_as_desktop_file":
                return WindowsWorkflowTools
                    .SaveActiveDocumentAsDesktopFile(
                        GetStringArgument(
                            arguments,
                            "relative_path"
                        )
                    );

            // =================================================
            // WINDOWS 0.7A NATIVE CONTROL TOOLS
            //
            // UI Automation calls run on a worker thread so the
            // WPF interface remains responsive.
            // =================================================

            case "windows_list_controls":
                return await Task.Run(
                    () =>
                        WindowsControlTools.ListControls(
                            GetStringArgument(
                                arguments,
                                "window_title"
                            ),
                            GetIntArgument(
                                arguments,
                                "maximum_controls"
                            )
                        ),
                    cancellationToken
                );

            case "windows_find_control":
                return await Task.Run(
                    () =>
                        WindowsControlTools.FindControl(
                            GetStringArgument(
                                arguments,
                                "window_title"
                            ),
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
                            GetStringArgument(
                                arguments,
                                "window_title"
                            ),
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
                            GetStringArgument(
                                arguments,
                                "window_title"
                            ),
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

            case "windows_get_control_value":
                return await Task.Run(
                    () =>
                        WindowsControlTools.GetControlValue(
                            GetStringArgument(
                                arguments,
                                "window_title"
                            ),
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
                            GetStringArgument(
                                arguments,
                                "window_title"
                            ),
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

            case "windows_set_toggle":
                return await Task.Run(
                    () =>
                        WindowsControlTools.SetToggleState(
                            GetStringArgument(
                                arguments,
                                "window_title"
                            ),
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
                            GetStringArgument(
                                arguments,
                                "window_title"
                            ),
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

            case "windows_select_control":
                return await Task.Run(
                    () =>
                        WindowsControlTools.SelectControl(
                            GetStringArgument(
                                arguments,
                                "window_title"
                            ),
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

            case "windows_set_expanded":
                return await Task.Run(
                    () =>
                        WindowsControlTools.SetExpandedState(
                            GetStringArgument(
                                arguments,
                                "window_title"
                            ),
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
                                "expanded"
                            )
                        ),
                    cancellationToken
                );

            case "windows_focus_control":
                return await Task.Run(
                    () =>
                        WindowsControlTools.FocusControl(
                            GetStringArgument(
                                arguments,
                                "window_title"
                            ),
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

            case "windows_wait_for_window":
                return await Task.Run(
                    () =>
                        WindowsControlTools.WaitForWindow(
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

            case "windows_wait_for_control":
                return await Task.Run(
                    () =>
                        WindowsControlTools.WaitForControl(
                            GetStringArgument(
                                arguments,
                                "window_title"
                            ),
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
                            GetIntArgument(
                                arguments,
                                "timeout_seconds"
                            )
                        ),
                    cancellationToken
                );

            // =================================================
            // BROWSER CORE
            // =================================================

            case "start_browser":
                return await BrowserTools.StartBrowserAsync();

            case "browser_session_info":
                return await BrowserTools.GetSessionInfoAsync();

            case "browser_navigate":
                return await BrowserTools.NavigateAsync(
                    GetStringArgument(
                        arguments,
                        "url"
                    )
                );

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
                    GetStringArgument(
                        arguments,
                        "locator_type"
                    ),
                    GetStringArgument(
                        arguments,
                        "query"
                    )
                );

            // =================================================
            // BROWSER ROLE
            // =================================================

            case "browser_role_find":
                return await BrowserTools.FindByRoleAsync(
                    GetStringArgument(
                        arguments,
                        "role"
                    ),
                    GetStringArgument(
                        arguments,
                        "name"
                    ),
                    GetBoolArgument(
                        arguments,
                        "exact"
                    )
                );

            case "browser_role_click":
                return await BrowserTools.ClickRoleAsync(
                    GetStringArgument(
                        arguments,
                        "role"
                    ),
                    GetStringArgument(
                        arguments,
                        "name"
                    ),
                    GetBoolArgument(
                        arguments,
                        "exact"
                    )
                );

            case "browser_role_fill":
                return await BrowserTools.FillRoleAsync(
                    GetStringArgument(
                        arguments,
                        "role"
                    ),
                    GetStringArgument(
                        arguments,
                        "name"
                    ),
                    GetBoolArgument(
                        arguments,
                        "exact"
                    ),
                    GetStringArgument(
                        arguments,
                        "text"
                    )
                );

            case "browser_role_wait":
                return await BrowserTools.WaitForRoleAsync(
                    GetStringArgument(
                        arguments,
                        "role"
                    ),
                    GetStringArgument(
                        arguments,
                        "name"
                    ),
                    GetBoolArgument(
                        arguments,
                        "exact"
                    ),
                    GetStringArgument(
                        arguments,
                        "state"
                    ),
                    GetIntArgument(
                        arguments,
                        "timeout_seconds"
                    )
                );

            case "browser_role_get_text":
                return await BrowserTools.GetRoleTextAsync(
                    GetStringArgument(
                        arguments,
                        "role"
                    ),
                    GetStringArgument(
                        arguments,
                        "name"
                    ),
                    GetBoolArgument(
                        arguments,
                        "exact"
                    )
                );

            case "browser_exact_text":
                return await BrowserTools.FindElementsAsync(
                    "exact_text",
                    GetStringArgument(
                        arguments,
                        "text"
                    )
                );

            // =================================================
            // BROWSER VISION
            // =================================================

            case "browser_visual_inspect":
                return await BrowserVisionTools
                    .InspectCurrentPageAsync(
                        GetStringArgument(
                            arguments,
                            "question"
                        ),
                        GetBoolArgument(
                            arguments,
                            "full_page"
                        ),
                        cancellationToken
                    );

            // =================================================
            // BROWSER SCREENSHOTS
            // =================================================

            case "browser_screenshot":
                return await BrowserTools.ScreenshotAsync(
                    GetStringArgument(
                        arguments,
                        "relative_path"
                    ),
                    GetBoolArgument(
                        arguments,
                        "full_page"
                    )
                );

            case "browser_list_screenshots":
                return BrowserTools.ListScreenshots();

            // =================================================
            // BROWSER COORDINATES
            // =================================================

            case "browser_get_viewport":
                return await BrowserTools.GetViewportInfoAsync();

            case "browser_element_box":
                return await BrowserTools.GetElementBoxAsync(
                    GetStringArgument(
                        arguments,
                        "locator_type"
                    ),
                    GetStringArgument(
                        arguments,
                        "query"
                    )
                );

            case "browser_mouse_move":
                return await BrowserTools.MouseMoveAsync(
                    GetIntArgument(
                        arguments,
                        "x"
                    ),
                    GetIntArgument(
                        arguments,
                        "y"
                    )
                );

            case "browser_mouse_click":
                return await BrowserTools.MouseClickAsync(
                    GetIntArgument(
                        arguments,
                        "x"
                    ),
                    GetIntArgument(
                        arguments,
                        "y"
                    )
                );

            case "browser_mouse_double_click":
                return await BrowserTools.MouseDoubleClickAsync(
                    GetIntArgument(
                        arguments,
                        "x"
                    ),
                    GetIntArgument(
                        arguments,
                        "y"
                    )
                );

            // =================================================
            // BROWSER INSPECTION
            // =================================================

            case "browser_get_text":
                return await BrowserTools.GetElementTextAsync(
                    GetStringArgument(
                        arguments,
                        "locator_type"
                    ),
                    GetStringArgument(
                        arguments,
                        "query"
                    )
                );

            case "browser_get_attribute":
                return await BrowserTools.GetAttributeAsync(
                    GetStringArgument(
                        arguments,
                        "locator_type"
                    ),
                    GetStringArgument(
                        arguments,
                        "query"
                    ),
                    GetStringArgument(
                        arguments,
                        "attribute_name"
                    )
                );

            case "browser_get_value":
                return await BrowserTools.GetValueAsync(
                    GetStringArgument(
                        arguments,
                        "locator_type"
                    ),
                    GetStringArgument(
                        arguments,
                        "query"
                    )
                );

            case "browser_is_visible":
                return await BrowserTools.IsVisibleAsync(
                    GetStringArgument(
                        arguments,
                        "locator_type"
                    ),
                    GetStringArgument(
                        arguments,
                        "query"
                    )
                );

            // =================================================
            // BROWSER WAIT
            // =================================================

            case "browser_wait":
                return await BrowserTools.WaitForElementAsync(
                    GetStringArgument(
                        arguments,
                        "locator_type"
                    ),
                    GetStringArgument(
                        arguments,
                        "query"
                    ),
                    GetStringArgument(
                        arguments,
                        "state"
                    ),
                    GetIntArgument(
                        arguments,
                        "timeout_seconds"
                    )
                );

            case "browser_wait_for_url":
                return await BrowserTools.WaitForUrlAsync(
                    GetStringArgument(
                        arguments,
                        "url_pattern"
                    ),
                    GetIntArgument(
                        arguments,
                        "timeout_seconds"
                    )
                );

            case "browser_wait_for_text":
                return await BrowserTools.WaitForTextAsync(
                    GetStringArgument(
                        arguments,
                        "text"
                    ),
                    GetBoolArgument(
                        arguments,
                        "exact"
                    ),
                    GetIntArgument(
                        arguments,
                        "timeout_seconds"
                    )
                );

            // =================================================
            // BROWSER INPUT
            // =================================================

            case "browser_click":
                return await BrowserTools.ClickAsync(
                    GetStringArgument(
                        arguments,
                        "locator_type"
                    ),
                    GetStringArgument(
                        arguments,
                        "query"
                    )
                );

            case "browser_fill":
                return await BrowserTools.FillAsync(
                    GetStringArgument(
                        arguments,
                        "locator_type"
                    ),
                    GetStringArgument(
                        arguments,
                        "query"
                    ),
                    GetStringArgument(
                        arguments,
                        "text"
                    )
                );

            case "browser_type":
                return await BrowserTools.TypeAsync(
                    GetStringArgument(
                        arguments,
                        "locator_type"
                    ),
                    GetStringArgument(
                        arguments,
                        "query"
                    ),
                    GetStringArgument(
                        arguments,
                        "text"
                    )
                );

            case "browser_press":
                return await BrowserTools.PressAsync(
                    GetStringArgument(
                        arguments,
                        "locator_type"
                    ),
                    GetStringArgument(
                        arguments,
                        "query"
                    ),
                    GetStringArgument(
                        arguments,
                        "key"
                    )
                );

            case "browser_page_key":
                return await BrowserTools.PressPageKeyAsync(
                    GetStringArgument(
                        arguments,
                        "key"
                    )
                );

            // =================================================
            // BROWSER SCROLL
            // =================================================

            case "browser_scroll":
                return await BrowserTools.ScrollPageAsync(
                    GetIntArgument(
                        arguments,
                        "delta_y"
                    )
                );

            case "browser_scroll_to":
                return await BrowserTools.ScrollToElementAsync(
                    GetStringArgument(
                        arguments,
                        "locator_type"
                    ),
                    GetStringArgument(
                        arguments,
                        "query"
                    )
                );

            // =================================================
            // BROWSER CHECKBOX
            // =================================================

            case "browser_set_checked":
                return await BrowserTools.SetCheckedAsync(
                    GetStringArgument(
                        arguments,
                        "locator_type"
                    ),
                    GetStringArgument(
                        arguments,
                        "query"
                    ),
                    GetBoolArgument(
                        arguments,
                        "checked"
                    )
                );

            case "browser_get_checked":
                return await BrowserTools.GetCheckedStateAsync(
                    GetStringArgument(
                        arguments,
                        "locator_type"
                    ),
                    GetStringArgument(
                        arguments,
                        "query"
                    )
                );

            // =================================================
            // BROWSER SELECT
            // =================================================

            case "browser_select_option":
                return await BrowserTools.SelectOptionAsync(
                    GetStringArgument(
                        arguments,
                        "locator_type"
                    ),
                    GetStringArgument(
                        arguments,
                        "query"
                    ),
                    GetStringArgument(
                        arguments,
                        "selection_type"
                    ),
                    GetStringArgument(
                        arguments,
                        "selection"
                    )
                );

            // =================================================
            // BROWSER FILES
            // =================================================

            case "browser_upload_desktop_file":
                return await BrowserTools.UploadDesktopFileAsync(
                    GetStringArgument(
                        arguments,
                        "locator_type"
                    ),
                    GetStringArgument(
                        arguments,
                        "query"
                    ),
                    GetStringArgument(
                        arguments,
                        "relative_path"
                    )
                );

            case "browser_download":
                return await BrowserTools.DownloadByClickAsync(
                    GetStringArgument(
                        arguments,
                        "locator_type"
                    ),
                    GetStringArgument(
                        arguments,
                        "query"
                    ),
                    GetStringArgument(
                        arguments,
                        "preferred_relative_path"
                    )
                );

            case "browser_list_downloads":
                return BrowserTools.ListDownloads();

            // =================================================
            // BROWSER NAVIGATION
            // =================================================

            case "browser_back":
                return await BrowserTools.BackAsync();

            case "browser_forward":
                return await BrowserTools.ForwardAsync();

            case "browser_reload":
                return await BrowserTools.ReloadAsync();

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
                            await BrowserTools.NewTabAsync();
                    }

                    return
                        await BrowserTools.NewTabAsync(
                            url
                        );
                }

            case "browser_list_tabs":
                return await BrowserTools.ListTabsAsync();

            case "browser_switch_tab":
                return await BrowserTools.SwitchTabAsync(
                    GetIntArgument(
                        arguments,
                        "tab_number"
                    )
                );

            case "browser_close_tab":
                return await BrowserTools.CloseTabAsync(
                    GetIntArgument(
                        arguments,
                        "tab_number"
                    )
                );

            case "stop_browser":
                return await BrowserTools.StopBrowserAsync();

            default:
                return
                    $"ERROR: Unknown tool '{call.FunctionName}'.";
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