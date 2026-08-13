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
    // WINDOWS UI
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
    // GENERIC LOCATORS
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
    // ROLE-BASED TARGETING
    // =========================================================

    private static readonly FunctionTool BrowserRoleFindTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_role_find",
            functionDescription:
                "Find elements by ARIA role and accessible name. Common roles include button, link, textbox, searchbox, checkbox, radio, combobox, heading, dialog, tab, option.",
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
                "Wait for an element identified by ARIA role and accessible name. States: visible, hidden, attached, detached.",
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
    // VERSION 0.6F
    // VISUAL INSPECTION
    // =========================================================

    private static readonly FunctionTool BrowserVisualInspectTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_visual_inspect",
            functionDescription:
                "Observe the current browser page visually using a screenshot and image understanding. Use this as a fallback when DOM, role, text, label, or other structured browser tools do not provide enough information. This tool only observes; it does not click or change the page.",
            functionParameters: BinaryData.FromString(
                """
                {
                  "type": "object",
                  "properties": {
                    "question": {
                      "type": "string",
                      "description":
                        "What should be visually inspected or identified in the current browser screenshot."
                    },
                    "full_page": {
                      "type": "boolean",
                      "description":
                        "True to capture the full scrollable page; false to inspect only the current viewport."
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
    // SCREENSHOTS
    // =========================================================

    private static readonly FunctionTool BrowserScreenshotTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_screenshot",
            functionDescription:
                "Capture the current browser page as an image under Desktop\\OperatorScreenshots.",
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
    // ELEMENT INSPECTION
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
                "Read the current value of an input or textarea.",
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
    // WAITING
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

    private static readonly FunctionTool BrowserWaitForUrlTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_wait_for_url",
            functionDescription:
                "Wait for the current browser URL to match a URL or Playwright glob pattern such as **/orders/**.",
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
                "Wait for visible text to appear on the browser page.",
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
    // CLICK / FILL / TYPE / KEYBOARD
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
    // SCROLLING
    // =========================================================

    private static readonly FunctionTool BrowserScrollTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_scroll",
            functionDescription:
                "Scroll the current browser page vertically. Positive values scroll down and negative values scroll up.",
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
    // CHECKBOX / RADIO
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
    // DROPDOWN
    // =========================================================

    private static readonly FunctionTool BrowserSelectOptionTool =
        ResponseTool.CreateFunctionTool(
            functionName: "browser_select_option",
            functionDescription:
                "Select an option from a standard HTML select control. selection_type may be value, label, or index.",
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
    // UPLOAD / DOWNLOAD
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
                "Click an element expected to trigger a download and save the resulting file under Desktop\\OperatorDownloads.",
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
    // NAVIGATION / TABS
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
                "Close the Operator AI Chromium browser while retaining persistent browser session data.",
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

        // Vision calls can add some latency.
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
                            CORE RULES
                            =================================================

                            - Use tools for real computer and browser actions.
                            - Never claim that an action succeeded unless a tool confirms it.
                            - Verify important outcomes whenever practical.
                            - Never invent webpage contents, controls, files, URLs, or computer state.
                            - Do not repeat a failing strategy indefinitely.
                            - Prefer deterministic structured automation over guesses.

                            =================================================
                            WINDOWS
                            =================================================

                            - Use open_application for supported Windows applications.
                            - Use list_windows when you need the actual window title.
                            - Use focus_window before desktop interaction when appropriate.
                            - Use inspect_window when desktop UI state is unclear.
                            - Use type_text for desktop text entry.
                            - Use press_key for Windows keyboard shortcuts.
                            - Prefer save_active_document_as_desktop_file for reliable Desktop saving.

                            =================================================
                            BROWSER SESSION
                            =================================================

                            - Operator AI uses a dedicated persistent Chromium profile.
                            - Cookies and site state may survive browser restarts.
                            - Never assume the user is logged in.
                            - Inspect the current page when authentication state matters.
                            - Use browser_session_info when useful.

                            =================================================
                            TARGETING PRIORITY
                            =================================================

                            Prefer browser targeting in this order when practical:

                            1. ARIA role + accessible name
                            2. label
                            3. placeholder
                            4. exact visible text
                            5. testid
                            6. title or alt text
                            7. stable CSS

                            Avoid brittle complex CSS when semantic targeting exists.

                            =================================================
                            ROLE-BASED TARGETING
                            =================================================

                            - Use browser_role_find to identify semantic controls.
                            - Use browser_role_click for buttons, links, tabs and similar controls.
                            - Use browser_role_fill for textboxes and searchboxes.
                            - Use browser_role_wait when a semantic control may appear later.
                            - Use browser_role_get_text for specific semantic text.

                            Typical roles include:
                            button
                            link
                            textbox
                            searchbox
                            checkbox
                            radio
                            combobox
                            heading
                            dialog
                            tab
                            option

                            =================================================
                            VISUAL INSPECTION - VERSION 0.6F
                            =================================================

                            browser_visual_inspect visually analyzes a screenshot
                            of the current browser page.

                            IMPORTANT:
                            - It is observation only.
                            - It does not click, type, submit, or change anything.
                            - Use structured DOM and role tools first when they are sufficient.
                            - Use vision when the page is visually understandable but
                              structured tools do not expose enough information.
                            - Examples include custom dashboards, unusual controls,
                              poorly accessible websites, popup placement, visual warnings,
                              canvas-like content, or identifying what is blocking interaction.

                            A good recovery pattern is:

                            browser_list_elements
                            -> browser_role_find / browser_find
                            -> structured interaction

                            If structured inspection is insufficient:
                            -> browser_visual_inspect
                            -> use its observations to choose a structured locator or strategy
                            -> verify with DOM/role/page tools

                            Do not treat an approximate visual position as proof that a
                            structured locator points to that element.

                            Do not claim that visual inspection performed an action.

                            Avoid visual inspection on pages visibly containing passwords,
                            payment-card details, authentication secrets, private messages,
                            confidential documents, or similarly sensitive information
                            unless the user explicitly requested visual analysis of that page.

                            =================================================
                            PAGE INSPECTION
                            =================================================

                            - Use browser_get_page_info for title and URL.
                            - Use browser_read_page for broad visible text.
                            - Use browser_list_elements to inspect the interactive structure.
                            - Use browser_get_text for a specific element.
                            - Use browser_get_attribute for attributes such as href or aria-label.
                            - Use browser_get_value to verify form values.
                            - Use browser_is_visible for immediate visibility checks.

                            =================================================
                            SCREENSHOTS
                            =================================================

                            - Use browser_screenshot when the user asks for a screenshot
                              or when a persistent visual record is useful.
                            - Screenshots are stored under Desktop\OperatorScreenshots.
                            - browser_visual_inspect automatically captures its own screenshot.
                            - A screenshot being created does not by itself mean its
                              visual content has been understood.

                            =================================================
                            WAITING
                            =================================================

                            - Use browser_wait for generic element state.
                            - Use browser_role_wait for semantic controls.
                            - Use browser_wait_for_url after navigation when useful.
                            - Use browser_wait_for_text for dynamic content.
                            - Prefer waiting for an explicit state rather than blind retries.

                            =================================================
                            FORMS
                            =================================================

                            - Prefer browser_role_fill when an accessible textbox name is known.
                            - Otherwise use browser_fill.
                            - Use browser_get_value to verify text input.
                            - Use browser_set_checked and browser_get_checked for checkbox/radio state.
                            - Use browser_select_option for standard HTML select elements.
                            - Use browser_press for element-specific Enter, Tab, Escape, etc.

                            =================================================
                            SCROLLING
                            =================================================

                            - Use browser_scroll to move vertically through a long page.
                            - Positive values scroll downward.
                            - Negative values scroll upward.
                            - Use browser_scroll_to when a known element should be brought into view.

                            =================================================
                            FILES
                            =================================================

                            - browser_upload_desktop_file can upload only files under Desktop.
                            - Never invent a filename.
                            - browser_download stores downloads under Desktop\OperatorDownloads.
                            - Use browser_list_downloads when additional verification is useful.

                            =================================================
                            NAVIGATION
                            =================================================

                            - Use start_browser before browser work when necessary.
                            - Use browser_navigate to visit URLs.
                            - Use browser_back, browser_forward, and browser_reload when appropriate.
                            - Use browser_new_tab for a separate page.
                            - Use browser_list_tabs when tab numbers are uncertain.
                            - Use browser_switch_tab to change tabs.
                            - Avoid unnecessary tabs.

                            =================================================
                            RECOVERY
                            =================================================

                            ERROR, NOT_FOUND and BLOCKED mean the attempted strategy failed.

                            When a locator fails:
                            - inspect browser_list_elements,
                            - try role targeting,
                            - try label, placeholder, exact text, testid,
                            - then use stable CSS if necessary.

                            When structured inspection cannot explain the visible page:
                            - use browser_visual_inspect.

                            After visual inspection:
                            - return to structured browser tools for actual interaction
                              whenever possible.

                            Do not repeatedly issue the exact same failing tool call.

                            =================================================
                            CONSEQUENTIAL ACTIONS
                            =================================================

                            Reading, browsing, research, screenshots, ordinary form
                            preparation, uploads and downloads may proceed when requested.

                            Do not finalize purchases, financial transactions,
                            account deletion, password changes, final legal submissions,
                            or similarly consequential actions unless the user's request
                            clearly authorizes that exact final action.

                            =================================================
                            COMPLETION
                            =================================================

                            Do not declare completion until the requested outcome
                            has been reasonably verified.

                            For browser navigation:
                            verify page title, URL, or visible state.

                            For form input:
                            verify field values or state where practical.

                            For downloads:
                            verify the resulting file.

                            For screenshots:
                            verify creation.

                            For visually identified information:
                            distinguish visual observations from verified DOM facts.

                            If completion is impossible, clearly explain the unresolved step.
                            """
                    };

                // =================================================
                // WINDOWS
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
                // SEMANTIC TARGETING
                // =================================================

                options.Tools.Add(BrowserRoleFindTool);
                options.Tools.Add(BrowserRoleClickTool);
                options.Tools.Add(BrowserRoleFillTool);
                options.Tools.Add(BrowserRoleWaitTool);
                options.Tools.Add(BrowserRoleGetTextTool);
                options.Tools.Add(BrowserExactTextTool);

                // =================================================
                // 0.6F VISION
                // =================================================

                options.Tools.Add(BrowserVisualInspectTool);

                // =================================================
                // SCREENSHOTS / INSPECTION
                // =================================================

                options.Tools.Add(BrowserScreenshotTool);
                options.Tools.Add(BrowserListScreenshotsTool);
                options.Tools.Add(BrowserGetTextTool);
                options.Tools.Add(BrowserGetAttributeTool);
                options.Tools.Add(BrowserGetValueTool);
                options.Tools.Add(BrowserIsVisibleTool);

                // =================================================
                // WAIT / INPUT / SCROLL
                // =================================================

                options.Tools.Add(BrowserWaitTool);
                options.Tools.Add(BrowserWaitForUrlTool);
                options.Tools.Add(BrowserWaitForTextTool);

                options.Tools.Add(BrowserClickTool);
                options.Tools.Add(BrowserFillTool);
                options.Tools.Add(BrowserTypeTool);
                options.Tools.Add(BrowserPressTool);
                options.Tools.Add(BrowserPageKeyTool);

                options.Tools.Add(BrowserScrollTool);
                options.Tools.Add(BrowserScrollToTool);

                // =================================================
                // FORM / FILE
                // =================================================

                options.Tools.Add(BrowserSetCheckedTool);
                options.Tools.Add(BrowserGetCheckedTool);
                options.Tools.Add(BrowserSelectOptionTool);
                options.Tools.Add(BrowserUploadTool);
                options.Tools.Add(BrowserDownloadTool);
                options.Tools.Add(BrowserListDownloadsTool);

                // =================================================
                // NAVIGATION / TABS
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

            string stepLimitMessage =
                "Agent stopped because the maximum number of planning steps was reached.";

            log?.Invoke(
                $"[ERROR] {stepLimitMessage}"
            );

            return
                stepLimitMessage;
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

                return
                    message;
            }

            string timeoutMessage =
                "TIMEOUT: Task exceeded the 10-minute limit and was stopped.";

            log?.Invoke(
                $"[TIMEOUT] {timeoutMessage}"
            );

            return
                timeoutMessage;
        }
        catch (Exception ex)
        {
            string error =
                $"Agent failure: {ex.Message}";

            log?.Invoke(
                $"[ERROR] {error}"
            );

            return
                error;
        }
    }

    // =========================================================
    // TOOL EXECUTION
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

            case "open_application":
                {
                    return
                        WindowsTools.OpenApplication(
                            GetStringArgument(
                                arguments,
                                "application"
                            )
                        );
                }

            case "create_desktop_folder":
                {
                    return
                        WindowsTools.CreateDesktopFolder(
                            GetStringArgument(
                                arguments,
                                "folder_name"
                            )
                        );
                }

            case "create_desktop_file":
                {
                    return
                        WindowsTools.CreateDesktopFile(
                            GetStringArgument(
                                arguments,
                                "relative_path"
                            ),
                            GetStringArgument(
                                arguments,
                                "content"
                            )
                        );
                }

            case "read_desktop_file":
                {
                    return
                        WindowsTools.ReadDesktopFile(
                            GetStringArgument(
                                arguments,
                                "relative_path"
                            )
                        );
                }

            case "desktop_file_exists":
                {
                    return
                        WindowsTools.DesktopFileExists(
                            GetStringArgument(
                                arguments,
                                "relative_path"
                            )
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
                    return
                        WindowsUiTools.InspectWindow(
                            GetStringArgument(
                                arguments,
                                "window_title"
                            )
                        );
                }

            case "focus_window":
                {
                    return
                        WindowsUiTools.FocusWindow(
                            GetStringArgument(
                                arguments,
                                "window_title"
                            )
                        );
                }

            case "type_text":
                {
                    return
                        WindowsUiTools.TypeText(
                            GetStringArgument(
                                arguments,
                                "window_title"
                            ),
                            GetStringArgument(
                                arguments,
                                "text"
                            )
                        );
                }

            case "press_key":
                {
                    return
                        WindowsInputTools.PressKey(
                            GetStringArgument(
                                arguments,
                                "keys"
                            )
                        );
                }

            case "save_active_document_as_desktop_file":
                {
                    return
                        WindowsWorkflowTools
                            .SaveActiveDocumentAsDesktopFile(
                                GetStringArgument(
                                    arguments,
                                    "relative_path"
                                )
                            );
                }

            // =================================================
            // BROWSER CORE
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
                    return
                        await BrowserTools.NavigateAsync(
                            GetStringArgument(
                                arguments,
                                "url"
                            )
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
                    return
                        await BrowserTools.FindElementsAsync(
                            GetStringArgument(
                                arguments,
                                "locator_type"
                            ),
                            GetStringArgument(
                                arguments,
                                "query"
                            )
                        );
                }

            // =================================================
            // ROLE TARGETING
            // =================================================

            case "browser_role_find":
                {
                    return
                        await BrowserTools.FindByRoleAsync(
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
                }

            case "browser_role_click":
                {
                    return
                        await BrowserTools.ClickRoleAsync(
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
                }

            case "browser_role_fill":
                {
                    return
                        await BrowserTools.FillRoleAsync(
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
                }

            case "browser_role_wait":
                {
                    return
                        await BrowserTools.WaitForRoleAsync(
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
                }

            case "browser_role_get_text":
                {
                    return
                        await BrowserTools.GetRoleTextAsync(
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
                }

            case "browser_exact_text":
                {
                    return
                        await BrowserTools.FindElementsAsync(
                            "exact_text",
                            GetStringArgument(
                                arguments,
                                "text"
                            )
                        );
                }

            // =================================================
            // 0.6F VISUAL INSPECTION
            // =================================================

            case "browser_visual_inspect":
                {
                    string question =
                        GetStringArgument(
                            arguments,
                            "question"
                        );

                    bool fullPage =
                        GetBoolArgument(
                            arguments,
                            "full_page"
                        );

                    return
                        await BrowserVisionTools
                            .InspectCurrentPageAsync(
                                question,
                                fullPage,
                                cancellationToken
                            );
                }

            // =================================================
            // SCREENSHOT
            // =================================================

            case "browser_screenshot":
                {
                    return
                        await BrowserTools.ScreenshotAsync(
                            GetStringArgument(
                                arguments,
                                "relative_path"
                            ),
                            GetBoolArgument(
                                arguments,
                                "full_page"
                            )
                        );
                }

            case "browser_list_screenshots":
                {
                    return
                        BrowserTools.ListScreenshots();
                }

            // =================================================
            // INSPECTION
            // =================================================

            case "browser_get_text":
                {
                    return
                        await BrowserTools.GetElementTextAsync(
                            GetStringArgument(
                                arguments,
                                "locator_type"
                            ),
                            GetStringArgument(
                                arguments,
                                "query"
                            )
                        );
                }

            case "browser_get_attribute":
                {
                    return
                        await BrowserTools.GetAttributeAsync(
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
                }

            case "browser_get_value":
                {
                    return
                        await BrowserTools.GetValueAsync(
                            GetStringArgument(
                                arguments,
                                "locator_type"
                            ),
                            GetStringArgument(
                                arguments,
                                "query"
                            )
                        );
                }

            case "browser_is_visible":
                {
                    return
                        await BrowserTools.IsVisibleAsync(
                            GetStringArgument(
                                arguments,
                                "locator_type"
                            ),
                            GetStringArgument(
                                arguments,
                                "query"
                            )
                        );
                }

            // =================================================
            // WAIT
            // =================================================

            case "browser_wait":
                {
                    return
                        await BrowserTools.WaitForElementAsync(
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
                }

            case "browser_wait_for_url":
                {
                    return
                        await BrowserTools.WaitForUrlAsync(
                            GetStringArgument(
                                arguments,
                                "url_pattern"
                            ),
                            GetIntArgument(
                                arguments,
                                "timeout_seconds"
                            )
                        );
                }

            case "browser_wait_for_text":
                {
                    return
                        await BrowserTools.WaitForTextAsync(
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
                }

            // =================================================
            // INPUT
            // =================================================

            case "browser_click":
                {
                    return
                        await BrowserTools.ClickAsync(
                            GetStringArgument(
                                arguments,
                                "locator_type"
                            ),
                            GetStringArgument(
                                arguments,
                                "query"
                            )
                        );
                }

            case "browser_fill":
                {
                    return
                        await BrowserTools.FillAsync(
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
                }

            case "browser_type":
                {
                    return
                        await BrowserTools.TypeAsync(
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
                }

            case "browser_press":
                {
                    return
                        await BrowserTools.PressAsync(
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
                }

            case "browser_page_key":
                {
                    return
                        await BrowserTools.PressPageKeyAsync(
                            GetStringArgument(
                                arguments,
                                "key"
                            )
                        );
                }

            // =================================================
            // SCROLL
            // =================================================

            case "browser_scroll":
                {
                    return
                        await BrowserTools.ScrollPageAsync(
                            GetIntArgument(
                                arguments,
                                "delta_y"
                            )
                        );
                }

            case "browser_scroll_to":
                {
                    return
                        await BrowserTools.ScrollToElementAsync(
                            GetStringArgument(
                                arguments,
                                "locator_type"
                            ),
                            GetStringArgument(
                                arguments,
                                "query"
                            )
                        );
                }

            // =================================================
            // CHECKBOX / RADIO
            // =================================================

            case "browser_set_checked":
                {
                    return
                        await BrowserTools.SetCheckedAsync(
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
                }

            case "browser_get_checked":
                {
                    return
                        await BrowserTools.GetCheckedStateAsync(
                            GetStringArgument(
                                arguments,
                                "locator_type"
                            ),
                            GetStringArgument(
                                arguments,
                                "query"
                            )
                        );
                }

            // =================================================
            // DROPDOWN
            // =================================================

            case "browser_select_option":
                {
                    return
                        await BrowserTools.SelectOptionAsync(
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
                }

            // =================================================
            // UPLOAD / DOWNLOAD
            // =================================================

            case "browser_upload_desktop_file":
                {
                    return
                        await BrowserTools.UploadDesktopFileAsync(
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
                }

            case "browser_download":
                {
                    return
                        await BrowserTools.DownloadByClickAsync(
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
                        await BrowserTools.BackAsync();
                }

            case "browser_forward":
                {
                    return
                        await BrowserTools.ForwardAsync();
                }

            case "browser_reload":
                {
                    return
                        await BrowserTools.ReloadAsync();
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
                            await BrowserTools.NewTabAsync();
                    }

                    return
                        await BrowserTools.NewTabAsync(
                            url
                        );
                }

            case "browser_list_tabs":
                {
                    return
                        await BrowserTools.ListTabsAsync();
                }

            case "browser_switch_tab":
                {
                    return
                        await BrowserTools.SwitchTabAsync(
                            GetIntArgument(
                                arguments,
                                "tab_number"
                            )
                        );
                }

            case "browser_close_tab":
                {
                    return
                        await BrowserTools.CloseTabAsync(
                            GetIntArgument(
                                arguments,
                                "tab_number"
                            )
                        );
                }

            case "stop_browser":
                {
                    return
                        await BrowserTools.StopBrowserAsync();
                }

            default:
                {
                    return
                        $"ERROR: Unknown tool '{call.FunctionName}'.";
                }
        }
    }

    // =========================================================
    // JSON STRING
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
    // JSON INTEGER
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

    // =========================================================
    // JSON BOOLEAN
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