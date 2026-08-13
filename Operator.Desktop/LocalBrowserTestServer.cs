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
    // START SERVER
    // =========================================================

    public async Task<string> StartAsync()
    {
        try
        {
            if (_listener != null)
            {
                return
                    $"SUCCESS: Browser controls test server is already running at {BaseUrl}";
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
                (IPEndPoint)
                _listener.LocalEndpoint;

            BaseUrl =
                $"http://127.0.0.1:{endpoint.Port}";

            _serverTask =
                RunServerAsync(
                    _cancellation.Token
                );

            // Give the listener a moment to settle.
            await Task.Delay(
                100
            );

            return
                $"SUCCESS: Browser controls test server started at {BaseUrl}";
        }
        catch (Exception ex)
        {
            return
                $"ERROR: Could not start browser controls test server: {ex.Message}";
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

        while (!cancellationToken
            .IsCancellationRequested)
        {
            try
            {
                TcpClient client =
                    await _listener
                        .AcceptTcpClientAsync(
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
                if (cancellationToken
                    .IsCancellationRequested)
                {
                    break;
                }

                await Task.Delay(
                    50,
                    cancellationToken
                );
            }
        }
    }

    // =========================================================
    // HANDLE REQUEST
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

            if (path.StartsWith(
                    "/download",
                    StringComparison.OrdinalIgnoreCase))
            {
                await SendDownloadAsync(
                    stream,
                    cancellationToken
                );

                return;
            }

            if (path.StartsWith(
                    "/favicon.ico",
                    StringComparison.OrdinalIgnoreCase))
            {
                await SendEmptyResponseAsync(
                    stream,
                    cancellationToken
                );

                return;
            }

            await SendTestPageAsync(
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
    // TEST PAGE
    // =========================================================

    private static async Task SendTestPageAsync(
        NetworkStream stream,
        CancellationToken cancellationToken)
    {
        string html =
            """
            <!DOCTYPE html>
            <html lang="en">
            <head>
                <meta charset="utf-8">
                <title>Operator AI Browser Controls Test</title>

                <style>
                    body {
                        font-family: Segoe UI, Arial, sans-serif;
                        max-width: 760px;
                        margin: 50px auto;
                        padding: 0 30px;
                        line-height: 1.5;
                    }

                    h1 {
                        margin-bottom: 8px;
                    }

                    .subtitle {
                        color: #555;
                        margin-bottom: 32px;
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
                    a.download-button {
                        font-size: 16px;
                    }

                    select,
                    input[type=file] {
                        margin-bottom: 12px;
                    }

                    .status {
                        margin-top: 10px;
                        font-family: Consolas, monospace;
                    }

                    a.download-button {
                        display: inline-block;
                        padding: 10px 18px;
                        border: 1px solid #777;
                        border-radius: 5px;
                        color: black;
                        text-decoration: none;
                        background: #eee;
                    }
                </style>
            </head>

            <body>

                <h1>Operator AI Browser Controls Test</h1>

                <div class="subtitle">
                    Local Version 0.6D automation test page
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

                    <div>
                        Test report download
                    </div>

                    <br>

                    <a
                        id="downloadReport"
                        class="download-button"
                        href="/download">
                        Download Test Report
                    </a>

                </div>

                <script>

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
    // DOWNLOAD RESPONSE
    // =========================================================

    private static async Task SendDownloadAsync(
        NetworkStream stream,
        CancellationToken cancellationToken)
    {
        string report =
            """
            Operator AI Browser Controls Test Report

            Version: 0.6D
            Result: Browser download system is working.

            This file was downloaded automatically
            by Operator AI through Playwright.
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
    // EMPTY RESPONSE
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
    // SEND RESPONSE
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
    // STOP SERVER
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