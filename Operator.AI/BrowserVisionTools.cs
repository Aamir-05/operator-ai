using OpenAI.Files;
using OpenAI.Responses;
using Operator.Tools;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Operator.AI;

#pragma warning disable OPENAI001

public static class BrowserVisionTools
{
    // =========================================================
    // VERSION 0.6F
    // BROWSER VISUAL UNDERSTANDING
    // =========================================================

    public static async Task<string> InspectCurrentPageAsync(
        string question,
        bool fullPage = false,
        CancellationToken cancellationToken = default)
    {
        string? uploadedFileId =
            null;

        OpenAIFileClient? fileClient =
            null;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            // =================================================
            // API KEY
            // =========================================================

            string? apiKey =
                Environment.GetEnvironmentVariable(
                    "OPENAI_API_KEY"
                );

            if (string.IsNullOrWhiteSpace(
                    apiKey))
            {
                return
                    "ERROR: OPENAI_API_KEY was not found.";
            }

            // =================================================
            // CHECK BROWSER
            // =================================================

            string sessionInfo =
                await BrowserTools
                    .GetSessionInfoAsync();

            if (IsFailure(
                    sessionInfo))
            {
                return
                    "ERROR: Browser visual inspection requires an active browser page.\n" +
                    sessionInfo;
            }

            cancellationToken.ThrowIfCancellationRequested();

            // =================================================
            // CREATE SCREENSHOT NAME
            // =================================================

            string screenshotFileName =
                $"vision-{DateTime.Now:yyyyMMdd-HHmmss-fff}.png";

            string screenshotDirectory =
                BrowserTools
                    .GetScreenshotsDirectory();

            string screenshotPath =
                Path.Combine(
                    screenshotDirectory,
                    screenshotFileName
                );

            // =================================================
            // CAPTURE CURRENT PAGE
            // =================================================

            string screenshotResult =
                await BrowserTools
                    .ScreenshotAsync(
                        screenshotFileName,
                        fullPage
                    );

            if (IsFailure(
                    screenshotResult))
            {
                return
                    "ERROR: Could not capture browser screenshot.\n" +
                    screenshotResult;
            }

            if (!File.Exists(
                    screenshotPath))
            {
                return
                    $"ERROR: Browser screenshot was not found at {screenshotPath}";
            }

            cancellationToken.ThrowIfCancellationRequested();

            // =================================================
            // GET CURRENT PAGE INFORMATION
            // =================================================

            string pageInfo =
                await BrowserTools
                    .GetPageInfoAsync();

            // =================================================
            // PREPARE VISUAL QUESTION
            // =================================================

            string userQuestion =
                string.IsNullOrWhiteSpace(
                    question)
                    ?
                    """
                    Inspect this browser screenshot carefully.

                    Describe:
                    - what page or application is visible,
                    - the important visible text,
                    - important buttons, links, fields, menus, dialogs,
                      tabs, tables, cards, or other controls,
                    - anything unusual, blocked, disabled, hidden,
                      loading, or requiring user attention,
                    - the approximate visual location of important
                      controls when useful.

                    Do not claim that you clicked or changed anything.

                    This is visual inspection only.
                    """
                    :
                    question.Trim();

            string visualPrompt =
                $"""
                You are the visual inspection component of Operator AI.

                Analyze the attached screenshot of the current browser page.

                CURRENT STRUCTURED PAGE INFORMATION
                -----------------------------------
                {pageInfo}

                USER / AGENT QUESTION
                -----------------------------------
                {userQuestion}

                VISUAL INSPECTION RULES
                -----------------------------------

                - Base the answer on what is actually visible in the screenshot.
                - Do not invent text, controls, state, or actions.
                - If something is uncertain, say that it is uncertain.
                - Do not claim that an action was performed.
                - This tool only observes the page.
                - Mention visible error messages, warnings, login prompts,
                  CAPTCHAs, permission prompts, popups, or dialogs.
                - When useful, describe approximate locations such as:
                  top-left, top-center, top-right,
                  center-left, center, center-right,
                  bottom-left, bottom-center, bottom-right.
                - Prefer concise actionable observations.
                - If a requested element cannot be seen, explicitly say so.

                Return a clear visual inspection report.
                """;

            // =================================================
            // UPLOAD SCREENSHOT FOR VISION
            //
            // IMPORTANT:
            // The cancellable OpenAI SDK overload requires:
            //
            // Stream
            // filename
            // purpose
            // cancellation token
            // =================================================

            fileClient =
                new OpenAIFileClient(
                    apiKey
                );

            await using FileStream screenshotStream =
                new FileStream(
                    screenshotPath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.Read
                );

            OpenAIFile uploadedFile =
                await fileClient
                    .UploadFileAsync(
                        screenshotStream,
                        Path.GetFileName(
                            screenshotPath
                        ),
                        FileUploadPurpose.Vision,
                        cancellationToken
                    );

            uploadedFileId =
                uploadedFile.Id;

            cancellationToken.ThrowIfCancellationRequested();

            // =================================================
            // SEND IMAGE + TEXT TO RESPONSES API
            // =================================================

            ResponsesClient responsesClient =
                new ResponsesClient(
                    apiKey
                );

            ResponseItem visualMessage =
                ResponseItem
                    .CreateUserMessageItem(
                        [
                            ResponseContentPart
                                .CreateInputTextPart(
                                    visualPrompt
                                ),

                            ResponseContentPart
                                .CreateInputImagePart(
                                    uploadedFile.Id,
                                    ResponseImageDetailLevel.High
                                )
                        ]
                    );

            CreateResponseOptions options =
                new CreateResponseOptions(
                    "gpt-5.6",
                    [
                        visualMessage
                    ]
                )
                {
                    Instructions =
                        """
                        You visually inspect browser screenshots for Operator AI.

                        Report only what is supported by the screenshot.

                        Never claim to have clicked, typed, submitted,
                        downloaded, uploaded, purchased, deleted,
                        or otherwise changed the page.

                        Your output is observational information that another
                        automation agent may use when deciding what to do next.
                        """
                };

            ResponseResult response =
                await responsesClient
                    .CreateResponseAsync(
                        options,
                        cancellationToken
                    );

            cancellationToken.ThrowIfCancellationRequested();

            string analysis =
                response.GetOutputText();

            if (string.IsNullOrWhiteSpace(
                    analysis))
            {
                return
                    "ERROR: Visual inspection returned no text.";
            }

            // =================================================
            // RESULT
            // =================================================

            return
                "VISUAL INSPECTION\n" +
                $"Screenshot: {screenshotPath}\n" +
                $"Full page: {fullPage}\n\n" +
                analysis.Trim();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Browser visual inspection failed: {ex.Message}";
        }
        finally
        {
            // =================================================
            // DELETE TEMPORARY OPENAI FILE
            //
            // The local screenshot remains on Desktop.
            // Only the temporary OpenAI upload is removed.
            // =================================================

            if (
                fileClient != null
                &&
                !string.IsNullOrWhiteSpace(
                    uploadedFileId)
            )
            {
                try
                {
                    await fileClient
                        .DeleteFileAsync(
                            uploadedFileId
                        );
                }
                catch
                {
                    // Do not replace a successful inspection result
                    // merely because temporary-file cleanup failed.
                }
            }
        }
    }

    // =========================================================
    // SIMPLE PAGE DESCRIPTION
    // =========================================================

    public static async Task<string> DescribeCurrentPageAsync(
        CancellationToken cancellationToken = default)
    {
        return
            await InspectCurrentPageAsync(
                """
                Describe the current browser page visually.

                Identify:
                - the page or site,
                - main visible heading,
                - important buttons,
                - links,
                - text fields,
                - search fields,
                - menus,
                - dialogs,
                - alerts,
                - tables,
                - cards,
                - and anything blocking interaction.

                Include approximate screen locations for important controls.
                """,
                false,
                cancellationToken
            );
    }

    // =========================================================
    // FIND SOMETHING VISUALLY
    // =========================================================

    public static async Task<string> FindVisuallyAsync(
        string targetDescription,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(
                targetDescription))
        {
            return
                "ERROR: Visual target description cannot be empty.";
        }

        return
            await InspectCurrentPageAsync(
                $"""
                Look carefully at this screenshot.

                Find this target if it is visible:

                {targetDescription}

                Report:
                - whether it is visible,
                - its visible text or appearance,
                - its approximate position,
                - nearby labels or controls that help identify it,
                - whether it appears enabled, disabled, selected,
                  checked, expanded, hidden, or obstructed.

                Do not click it.
                """,
                false,
                cancellationToken
            );
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
                StringComparison.OrdinalIgnoreCase);
    }
}

#pragma warning restore OPENAI001