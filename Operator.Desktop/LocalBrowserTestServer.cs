using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Operator.Desktop;

public sealed class LocalBrowserTestServer :
    IAsyncDisposable
{
    private TcpListener? _listener;
    private CancellationTokenSource? _cancellation;
    private Task? _serverTask;

    public string BaseUrl
    {
        get;
        private set;
    } = "";

    // =========================================================
    // START
    // =========================================================

    public async Task<string> StartAsync()
    {
        try
        {
            if (_listener != null)
            {
                return
                    $"SUCCESS: Browser test server is already running at {BaseUrl}";
            }

            _cancellation =
                new CancellationTokenSource();

            _listener =
                new TcpListener(
                    IPAddress.Loopback,
                    0
                );

            _listener.Start();

            IPEndPoint endpoint =
                (IPEndPoint)_listener.LocalEndpoint;

            BaseUrl =
                $"http://127.0.0.1:{endpoint.Port}";

            _serverTask =
                RunServerAsync(
                    _cancellation.Token
                );

            await Task.Delay(
                100
            );

            return
                $"SUCCESS: Browser test server started at {BaseUrl}";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not start browser test server: {ex.Message}";
        }
    }

    // =========================================================
    // SERVER LOOP
    // =========================================================

    private async Task RunServerAsync(
        CancellationToken cancellationToken)
    {
        if (_listener == null)
        {
            return;
        }

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                TcpClient client =
                    await _listener.AcceptTcpClientAsync(
                        cancellationToken
                    );

                _ =
                    Task.Run(
                        async () =>
                        {
                            try
                            {
                                await HandleClientAsync(
                                    client,
                                    cancellationToken
                                );
                            }
                            catch
                            {
                                try
                                {
                                    client.Dispose();
                                }
                                catch
                                {
                                }
                            }
                        }
                    );
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (ObjectDisposedException)
            {
                break;
            }
            catch
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                try
                {
                    await Task.Delay(
                        50,
                        cancellationToken
                    );
                }
                catch
                {
                    break;
                }
            }
        }
    }

    // =========================================================
    // REQUEST
    // =========================================================

    private static async Task HandleClientAsync(
        TcpClient client,
        CancellationToken cancellationToken)
    {
        using (client)
        {
            using NetworkStream stream =
                client.GetStream();

            string request =
                await ReadRequestAsync(
                    stream,
                    cancellationToken
                );

            if (string.IsNullOrWhiteSpace(
                    request))
            {
                return;
            }

            string firstLine =
                request.Split(
                    "\r\n",
                    StringSplitOptions.None
                )[0];

            string[] parts =
                firstLine.Split(
                    ' ',
                    StringSplitOptions.RemoveEmptyEntries
                );

            string path =
                parts.Length >= 2
                    ? parts[1]
                    : "/";

            int queryIndex =
                path.IndexOf('?');

            if (queryIndex >= 0)
            {
                path =
                    path.Substring(
                        0,
                        queryIndex
                    );
            }

            if (path.Equals(
                    "/vision-fallback",
                    StringComparison.OrdinalIgnoreCase))
            {
                await SendVisionFallbackPageAsync(
                    stream,
                    cancellationToken
                );

                return;
            }

            if (path.Equals(
                    "/coordinate-canvas",
                    StringComparison.OrdinalIgnoreCase))
            {
                await SendCoordinateCanvasPageAsync(
                    stream,
                    cancellationToken
                );

                return;
            }

            if (path.Equals(
                    "/download",
                    StringComparison.OrdinalIgnoreCase))
            {
                await SendDownloadAsync(
                    stream,
                    cancellationToken
                );

                return;
            }

            if (path.Equals(
                    "/next",
                    StringComparison.OrdinalIgnoreCase))
            {
                await SendNextPageAsync(
                    stream,
                    cancellationToken
                );

                return;
            }

            if (path.Equals(
                    "/favicon.ico",
                    StringComparison.OrdinalIgnoreCase))
            {
                await SendEmptyResponseAsync(
                    stream,
                    cancellationToken
                );

                return;
            }

            await SendMainPageAsync(
                stream,
                cancellationToken
            );
        }
    }

    // =========================================================
    // READ HTTP REQUEST
    // =========================================================

    private static async Task<string> ReadRequestAsync(
        NetworkStream stream,
        CancellationToken cancellationToken)
    {
        byte[] buffer =
            new byte[4096];

        using MemoryStream data =
            new MemoryStream();

        while (data.Length < 16384)
        {
            int count =
                await stream.ReadAsync(
                    buffer.AsMemory(
                        0,
                        buffer.Length
                    ),
                    cancellationToken
                );

            if (count <= 0)
            {
                break;
            }

            data.Write(
                buffer,
                0,
                count
            );

            string current =
                Encoding.ASCII.GetString(
                    data.ToArray()
                );

            if (current.Contains(
                    "\r\n\r\n",
                    StringComparison.Ordinal))
            {
                break;
            }
        }

        return
            Encoding.ASCII.GetString(
                data.ToArray()
            );
    }

    // =========================================================
    // MAIN RELIABILITY PAGE
    // =========================================================

    private static async Task SendMainPageAsync(
        NetworkStream stream,
        CancellationToken cancellationToken)
    {
        string html =
            """
            <!DOCTYPE html>

            <html lang="en">

            <head>

                <meta charset="utf-8">

                <title>
                    Operator AI Browser Reliability Test
                </title>

                <style>

                    body {
                        font-family: Segoe UI, Arial, sans-serif;
                        max-width: 850px;
                        margin: 45px auto;
                        padding: 0 30px 100px 30px;
                        line-height: 1.5;
                    }

                    .card {
                        border: 1px solid #ccc;
                        border-radius: 8px;
                        padding: 22px;
                        margin-bottom: 20px;
                    }

                    label {
                        display: block;
                        margin-bottom: 8px;
                        font-weight: 600;
                    }

                    input,
                    select,
                    button,
                    a {
                        font-size: 16px;
                    }

                    input[type=text] {
                        width: 420px;
                        padding: 8px;
                    }

                    button {
                        padding: 9px 16px;
                    }

                    .status {
                        margin-top: 12px;
                        font-family: Consolas, monospace;
                    }

                    .hidden {
                        display: none;
                    }

                    .exact-target {
                        border: 1px dashed #777;
                        padding: 10px;
                    }

                    .spacer {
                        height: 1200px;
                    }

                    #bottomTarget {
                        border: 2px solid #555;
                        padding: 20px;
                        margin-bottom: 30px;
                    }

                </style>

            </head>

            <body>

                <h1>
                    Operator AI Browser Reliability Test
                </h1>

                <p>
                    Local deterministic browser test page.
                </p>

                <div class="card">

                    <label for="testInput">
                        Test input
                    </label>

                    <input
                        id="testInput"
                        type="text"
                        placeholder="Enter test value">

                    <div
                        id="inputStatus"
                        class="status">
                        Input: empty
                    </div>

                </div>

                <div class="card">

                    <div
                        id="exactTarget"
                        class="exact-target"
                        data-test-value="operator-ai-06e">
                        Exact Target 0.6E
                    </div>

                    <br>

                    <a
                        id="attributeLink"
                        href="/next"
                        data-purpose="navigation-test">
                        Attribute Test Link
                    </a>

                </div>

                <div class="card">

                    <button
                        id="revealButton"
                        type="button">
                        Reveal async message
                    </button>

                    <div
                        id="asyncMessage"
                        class="status hidden">
                        Async message ready
                    </div>

                </div>

                <div class="card">

                    <label for="enableAutomation">

                        <input
                            id="enableAutomation"
                            type="checkbox">

                        Enable automation

                    </label>

                    <div
                        id="checkboxStatus"
                        class="status">
                        Checkbox: disabled
                    </div>

                </div>

                <div class="card">

                    <label for="department">
                        Department
                    </label>

                    <select id="department">

                        <option value="finance">
                            Finance
                        </option>

                        <option value="operations">
                            Operations
                        </option>

                        <option value="maintenance">
                            Maintenance
                        </option>

                        <option value="engineering">
                            Engineering
                        </option>

                    </select>

                    <div
                        id="departmentStatus"
                        class="status">
                        Department: Finance
                    </div>

                </div>

                <div class="card">

                    <label for="uploadFile">
                        Upload file
                    </label>

                    <input
                        id="uploadFile"
                        type="file">

                    <div
                        id="uploadStatus"
                        class="status">
                        Uploaded: none
                    </div>

                </div>

                <div class="card">

                    <a
                        id="downloadReport"
                        href="/download">
                        Download Test Report
                    </a>

                </div>

                <h2>
                    Scroll Test
                </h2>

                <div class="spacer">
                </div>

                <div id="bottomTarget">
                    Bottom Target Reached
                </div>

                <a
                    id="nextLink"
                    href="/next">
                    Go to next page
                </a>

                <script>

                    const testInput =
                        document.getElementById(
                            "testInput"
                        );

                    const inputStatus =
                        document.getElementById(
                            "inputStatus"
                        );

                    testInput.addEventListener(
                        "input",
                        () => {
                            inputStatus.textContent =
                                "Input: " +
                                testInput.value;
                        }
                    );

                    const revealButton =
                        document.getElementById(
                            "revealButton"
                        );

                    const asyncMessage =
                        document.getElementById(
                            "asyncMessage"
                        );

                    revealButton.addEventListener(
                        "click",
                        () => {
                            setTimeout(
                                () => {
                                    asyncMessage.classList.remove(
                                        "hidden"
                                    );
                                },
                                600
                            );
                        }
                    );

                    const checkbox =
                        document.getElementById(
                            "enableAutomation"
                        );

                    const checkboxStatus =
                        document.getElementById(
                            "checkboxStatus"
                        );

                    checkbox.addEventListener(
                        "change",
                        () => {
                            checkboxStatus.textContent =
                                checkbox.checked
                                    ? "Checkbox: enabled"
                                    : "Checkbox: disabled";
                        }
                    );

                    const department =
                        document.getElementById(
                            "department"
                        );

                    const departmentStatus =
                        document.getElementById(
                            "departmentStatus"
                        );

                    department.addEventListener(
                        "change",
                        () => {

                            const option =
                                department.options[
                                    department.selectedIndex
                                ];

                            departmentStatus.textContent =
                                "Department: " +
                                option.text;
                        }
                    );

                    const upload =
                        document.getElementById(
                            "uploadFile"
                        );

                    const uploadStatus =
                        document.getElementById(
                            "uploadStatus"
                        );

                    upload.addEventListener(
                        "change",
                        () => {

                            if (
                                upload.files &&
                                upload.files.length > 0
                            ) {
                                uploadStatus.textContent =
                                    "Uploaded: " +
                                    upload.files[0].name;
                            }
                            else {
                                uploadStatus.textContent =
                                    "Uploaded: none";
                            }
                        }
                    );

                </script>

            </body>

            </html>
            """;

        await SendResponseAsync(
            stream,
            "200 OK",
            "text/html; charset=utf-8",
            Encoding.UTF8.GetBytes(
                html
            ),
            null,
            cancellationToken
        );
    }

    // =========================================================
    // 0.6F VISION FALLBACK PAGE
    // =========================================================

    private static async Task SendVisionFallbackPageAsync(
        NetworkStream stream,
        CancellationToken cancellationToken)
    {
        string html =
            """
            <!DOCTYPE html>

            <html lang="en">

            <head>

                <meta charset="utf-8">

                <title>
                    Operator AI Vision Fallback Test
                </title>

                <style>

                    body {
                        font-family: Segoe UI, Arial, sans-serif;
                        background: #f6f7f9;
                    }

                    .page {
                        width: 920px;
                        margin: 55px auto;
                    }

                    .panel {
                        background: white;
                        border: 1px solid #ccc;
                        border-radius: 12px;
                        padding: 34px;
                    }

                    #mysteryButtons {
                        display: flex;
                        gap: 22px;
                        justify-content: center;
                        margin: 30px 0;
                    }

                    .visual-choice {
                        width: 210px;
                        height: 64px;
                        border: 0;
                        background-color: transparent;
                        background-repeat: no-repeat;
                        background-position: center;
                        background-size: 210px 64px;
                        cursor: pointer;
                    }

                    #mysteryButtons button:nth-child(1) {
                        background-image:
                            url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='210' height='64'%3E%3Crect width='210' height='64' rx='8' fill='%23eeeeee' stroke='%23777'/%3E%3Ctext x='105' y='39' font-family='Segoe UI,Arial' font-size='18' text-anchor='middle' fill='%23111'%3ECancel%3C/text%3E%3C/svg%3E");
                    }

                    #mysteryButtons button:nth-child(2) {
                        background-image:
                            url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='210' height='64'%3E%3Crect width='210' height='64' rx='8' fill='%23dce9ff' stroke='%23506ca8'/%3E%3Ctext x='105' y='39' font-family='Segoe UI,Arial' font-size='18' text-anchor='middle' fill='%23111'%3EContinue%20Review%3C/text%3E%3C/svg%3E");
                    }

                    #mysteryButtons button:nth-child(3) {
                        background-image:
                            url("data:image/svg+xml,%3Csvg xmlns='http://www.w3.org/2000/svg' width='210' height='64'%3E%3Crect width='210' height='64' rx='8' fill='%23eeeeee' stroke='%23777'/%3E%3Ctext x='105' y='39' font-family='Segoe UI,Arial' font-size='18' text-anchor='middle' fill='%23111'%3EDefer%3C/text%3E%3C/svg%3E");
                    }

                    #fallbackStatus {
                        margin-top: 25px;
                        padding: 14px;
                        border: 1px solid #aaa;
                        font-family: Consolas, monospace;
                    }

                </style>

            </head>

            <body>

                <div class="page">

                    <h1>
                        Operator AI Vision Fallback Test
                    </h1>

                    <div class="panel">

                        <p>
                            Choose the visually appropriate action.
                        </p>

                        <div id="mysteryButtons">

                            <button
                                class="visual-choice"
                                type="button">
                            </button>

                            <button
                                class="visual-choice"
                                type="button">
                            </button>

                            <button
                                class="visual-choice"
                                type="button">
                            </button>

                        </div>

                        <div id="fallbackStatus">
                            Result: no action yet
                        </div>

                    </div>

                </div>

                <script>

                    const buttons =
                        document.querySelectorAll(
                            "#mysteryButtons button"
                        );

                    const status =
                        document.getElementById(
                            "fallbackStatus"
                        );

                    buttons[0].addEventListener(
                        "click",
                        () => {
                            status.textContent =
                                "Result: cancel selected";
                        }
                    );

                    buttons[1].addEventListener(
                        "click",
                        () => {
                            status.textContent =
                                "Result: review mode activated";
                        }
                    );

                    buttons[2].addEventListener(
                        "click",
                        () => {
                            status.textContent =
                                "Result: defer selected";
                        }
                    );

                </script>

            </body>

            </html>
            """;

        await SendResponseAsync(
            stream,
            "200 OK",
            "text/html; charset=utf-8",
            Encoding.UTF8.GetBytes(
                html
            ),
            null,
            cancellationToken
        );
    }

    // =========================================================
    // VERSION 0.6F-4
    // CANVAS-ONLY COORDINATE TEST
    //
    // There are NO DOM buttons inside the canvas.
    // The controls exist only as drawn pixels.
    // =========================================================

    private static async Task SendCoordinateCanvasPageAsync(
        NetworkStream stream,
        CancellationToken cancellationToken)
    {
        string html =
            """
            <!DOCTYPE html>

            <html lang="en">

            <head>

                <meta charset="utf-8">

                <title>
                    Operator AI Canvas Coordinate Test
                </title>

                <style>

                    html,
                    body {
                        margin: 0;
                        padding: 0;
                    }

                    body {
                        font-family: Segoe UI, Arial, sans-serif;
                        background: #eef1f5;
                        color: #111;
                    }

                    .page {
                        width: 1040px;
                        margin: 38px auto;
                    }

                    h1 {
                        margin: 0 0 8px 0;
                        font-size: 32px;
                    }

                    .subtitle {
                        color: #555;
                        margin-bottom: 18px;
                    }

                    .panel {
                        background: white;
                        border: 1px solid #c8ccd2;
                        border-radius: 12px;
                        padding: 28px 38px 30px 38px;
                        box-shadow: 0 3px 15px rgba(0,0,0,0.08);
                    }

                    .instruction {
                        border-left: 5px solid #536a91;
                        background: #f7f9fc;
                        padding: 13px 16px;
                        margin-bottom: 18px;
                    }

                    #visualCanvas {
                        width: 960px;
                        height: 420px;
                        display: block;
                        border: 1px solid #888;
                        background: white;
                    }

                    #canvasStatus {
                        margin-top: 18px;
                        padding: 13px;
                        border: 1px solid #999;
                        background: #fafafa;
                        font-family: Consolas, monospace;
                        font-size: 16px;
                    }

                </style>

            </head>

            <body>

                <div class="page">

                    <h1>
                        Operator AI Canvas Coordinate Test
                    </h1>

                    <div class="subtitle">
                        Version 0.6F visual coordinate interaction
                    </div>

                    <div class="panel">

                        <div class="instruction">

                            The action controls below are rendered entirely
                            inside a canvas.

                            Choose the action that continues the review
                            workflow.

                        </div>

                        <canvas
                            id="visualCanvas"
                            width="960"
                            height="420">
                        </canvas>

                        <div id="canvasStatus">
                            Result: no canvas action yet
                        </div>

                    </div>

                </div>

                <script>

                    const canvas =
                        document.getElementById(
                            "visualCanvas"
                        );

                    const ctx =
                        canvas.getContext(
                            "2d"
                        );

                    const status =
                        document.getElementById(
                            "canvasStatus"
                        );

                    const actions = [
                        {
                            x: 70,
                            y: 210,
                            width: 240,
                            height: 90,
                            label: "Cancel"
                        },
                        {
                            x: 360,
                            y: 210,
                            width: 240,
                            height: 90,
                            label: "Continue Review"
                        },
                        {
                            x: 650,
                            y: 210,
                            width: 240,
                            height: 90,
                            label: "Defer"
                        }
                    ];

                    function roundedRect(
                        x,
                        y,
                        width,
                        height,
                        radius)
                    {
                        ctx.beginPath();

                        ctx.roundRect(
                            x,
                            y,
                            width,
                            height,
                            radius
                        );

                        ctx.closePath();
                    }

                    function draw()
                    {
                        ctx.clearRect(
                            0,
                            0,
                            canvas.width,
                            canvas.height
                        );

                        ctx.fillStyle =
                            "#ffffff";

                        ctx.fillRect(
                            0,
                            0,
                            canvas.width,
                            canvas.height
                        );

                        ctx.fillStyle =
                            "#1f2937";

                        ctx.font =
                            "bold 27px Segoe UI, Arial";

                        ctx.textAlign =
                            "center";

                        ctx.fillText(
                            "Approval Review",
                            480,
                            62
                        );

                        ctx.fillStyle =
                            "#4b5563";

                        ctx.font =
                            "18px Segoe UI, Arial";

                        ctx.fillText(
                            "Select the action required to continue.",
                            480,
                            99
                        );

                        ctx.strokeStyle =
                            "#d1d5db";

                        ctx.lineWidth =
                            2;

                        ctx.beginPath();

                        ctx.moveTo(
                            80,
                            135
                        );

                        ctx.lineTo(
                            880,
                            135
                        );

                        ctx.stroke();

                        actions.forEach(
                            (action, index) => {

                                if (index === 1)
                                {
                                    ctx.fillStyle =
                                        "#dbeafe";

                                    ctx.strokeStyle =
                                        "#3b63a3";
                                }
                                else
                                {
                                    ctx.fillStyle =
                                        "#f3f4f6";

                                    ctx.strokeStyle =
                                        "#777777";
                                }

                                ctx.lineWidth =
                                    2;

                                roundedRect(
                                    action.x,
                                    action.y,
                                    action.width,
                                    action.height,
                                    12
                                );

                                ctx.fill();
                                ctx.stroke();

                                ctx.fillStyle =
                                    "#111827";

                                ctx.font =
                                    index === 1
                                        ? "bold 21px Segoe UI, Arial"
                                        : "20px Segoe UI, Arial";

                                ctx.textAlign =
                                    "center";

                                ctx.textBaseline =
                                    "middle";

                                ctx.fillText(
                                    action.label,
                                    action.x +
                                    action.width / 2,
                                    action.y +
                                    action.height / 2
                                );
                            }
                        );

                        ctx.fillStyle =
                            "#6b7280";

                        ctx.font =
                            "15px Segoe UI, Arial";

                        ctx.textAlign =
                            "center";

                        ctx.textBaseline =
                            "alphabetic";

                        ctx.fillText(
                            "Canvas-rendered controls — no DOM buttons exist here",
                            480,
                            370
                        );
                    }

                    draw();

                    canvas.addEventListener(
                        "click",
                        event => {

                            const rect =
                                canvas.getBoundingClientRect();

                            const x =
                                event.clientX -
                                rect.left;

                            const y =
                                event.clientY -
                                rect.top;

                            if (
                                x >= 70 &&
                                x <= 310 &&
                                y >= 210 &&
                                y <= 300
                            )
                            {
                                status.textContent =
                                    "Result: canvas cancel selected";

                                return;
                            }

                            if (
                                x >= 360 &&
                                x <= 600 &&
                                y >= 210 &&
                                y <= 300
                            )
                            {
                                status.textContent =
                                    "Result: canvas review activated";

                                return;
                            }

                            if (
                                x >= 650 &&
                                x <= 890 &&
                                y >= 210 &&
                                y <= 300
                            )
                            {
                                status.textContent =
                                    "Result: canvas defer selected";

                                return;
                            }

                            status.textContent =
                                "Result: canvas background clicked";
                        }
                    );

                </script>

            </body>

            </html>
            """;

        await SendResponseAsync(
            stream,
            "200 OK",
            "text/html; charset=utf-8",
            Encoding.UTF8.GetBytes(
                html
            ),
            null,
            cancellationToken
        );
    }

    // =========================================================
    // NEXT PAGE
    // =========================================================

    private static async Task SendNextPageAsync(
        NetworkStream stream,
        CancellationToken cancellationToken)
    {
        string html =
            """
            <!DOCTYPE html>

            <html lang="en">

            <head>

                <meta charset="utf-8">

                <title>
                    Operator AI Navigation Complete
                </title>

            </head>

            <body
                style="
                    font-family:Segoe UI,Arial,sans-serif;
                    max-width:760px;
                    margin:80px auto;
                    padding:30px;
                ">

                <h1>
                    Navigation Complete
                </h1>

                <p id="navigationResult">
                    Operator AI successfully reached the next page.
                </p>

                <a href="/">
                    Return to test page
                </a>

            </body>

            </html>
            """;

        await SendResponseAsync(
            stream,
            "200 OK",
            "text/html; charset=utf-8",
            Encoding.UTF8.GetBytes(
                html
            ),
            null,
            cancellationToken
        );
    }

    // =========================================================
    // DOWNLOAD
    // =========================================================

    private static async Task SendDownloadAsync(
        NetworkStream stream,
        CancellationToken cancellationToken)
    {
        string report =
            """
            Operator AI Browser Controls Test Report

            Version: 0.6F

            Result: Browser download system is working.
            """;

        byte[] bytes =
            Encoding.UTF8.GetBytes(
                report
            );

        await SendResponseAsync(
            stream,
            "200 OK",
            "text/plain; charset=utf-8",
            bytes,
            "attachment; filename=\"test-report.txt\"",
            cancellationToken
        );
    }

    // =========================================================
    // EMPTY
    // =========================================================

    private static async Task SendEmptyResponseAsync(
        NetworkStream stream,
        CancellationToken cancellationToken)
    {
        await SendResponseAsync(
            stream,
            "204 No Content",
            "text/plain",
            Array.Empty<byte>(),
            null,
            cancellationToken
        );
    }

    // =========================================================
    // RESPONSE
    // =========================================================

    private static async Task SendResponseAsync(
        NetworkStream stream,
        string status,
        string contentType,
        byte[] body,
        string? contentDisposition,
        CancellationToken cancellationToken)
    {
        StringBuilder headers =
            new StringBuilder();

        headers.Append(
            $"HTTP/1.1 {status}\r\n"
        );

        headers.Append(
            $"Content-Type: {contentType}\r\n"
        );

        headers.Append(
            $"Content-Length: {body.Length}\r\n"
        );

        headers.Append(
            "Cache-Control: no-store\r\n"
        );

        headers.Append(
            "Connection: close\r\n"
        );

        if (!string.IsNullOrWhiteSpace(
                contentDisposition))
        {
            headers.Append(
                $"Content-Disposition: {contentDisposition}\r\n"
            );
        }

        headers.Append(
            "\r\n"
        );

        byte[] headerBytes =
            Encoding.ASCII.GetBytes(
                headers.ToString()
            );

        await stream.WriteAsync(
            headerBytes.AsMemory(),
            cancellationToken
        );

        if (body.Length > 0)
        {
            await stream.WriteAsync(
                body.AsMemory(),
                cancellationToken
            );
        }

        await stream.FlushAsync(
            cancellationToken
        );
    }

    // =========================================================
    // STOP
    // =========================================================

    public async Task StopAsync()
    {
        try
        {
            _cancellation?.Cancel();
        }
        catch
        {
        }

        try
        {
            _listener?.Stop();
        }
        catch
        {
        }

        if (_serverTask != null)
        {
            try
            {
                await _serverTask;
            }
            catch
            {
            }
        }

        _listener = null;
        _serverTask = null;

        if (_cancellation != null)
        {
            try
            {
                _cancellation.Dispose();
            }
            catch
            {
            }

            _cancellation = null;
        }

        BaseUrl = "";
    }

    // =========================================================
    // DISPOSE
    // =========================================================

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
    }
}