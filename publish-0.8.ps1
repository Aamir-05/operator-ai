param(
    [ValidateSet("win-x64", "win-arm64")]
    [string]$Runtime = "win-x64",

    [switch]$SelfContained,

    [switch]$SkipPlaywrightInstall
)

$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Project = Join-Path $Root "Operator.Desktop\Operator.Desktop.csproj"
$PublishDir = Join-Path $Root "Operator.Desktop\bin\publish\OperatorAI-0.8-$Runtime"
$SelfContainedValue = if ($SelfContained.IsPresent) { "true" } else { "false" }

Write-Host "Operator AI 0.8 publish" -ForegroundColor Cyan
Write-Host "Runtime: $Runtime"
Write-Host "Self-contained: $SelfContainedValue"
Write-Host "Output: $PublishDir"

if (Test-Path $PublishDir) {
    Remove-Item $PublishDir -Recurse -Force
}

Push-Location $Root

try {
    dotnet clean

    dotnet publish $Project `
        -c Release `
        -r $Runtime `
        --self-contained $SelfContainedValue `
        -p:PublishSingleFile=false `
        -o $PublishDir

    if (-not $SkipPlaywrightInstall.IsPresent) {
        $PlaywrightScript = Get-ChildItem `
            -Path (Join-Path $Root "Operator.Desktop\bin\Release") `
            -Filter "playwright.ps1" `
            -Recurse `
            -ErrorAction SilentlyContinue |
            Sort-Object LastWriteTime -Descending |
            Select-Object -First 1

        if ($null -ne $PlaywrightScript) {
            Write-Host "Installing Playwright Chromium..." -ForegroundColor Cyan

            & powershell.exe `
                -NoProfile `
                -ExecutionPolicy Bypass `
                -File $PlaywrightScript.FullName `
                install chromium

            if ($LASTEXITCODE -ne 0) {
                throw "Playwright Chromium installation failed with exit code $LASTEXITCODE."
            }
        }
        else {
            Write-Warning "playwright.ps1 was not found. Build once and install Chromium manually if needed."
        }
    }

    Write-Host "" 
    Write-Host "SUCCESS: Operator AI 0.8 published." -ForegroundColor Green
    Write-Host "Executable folder: $PublishDir"
}
finally {
    Pop-Location
}
