param(
    [string]$Runtime = "win-x64",
    [switch]$SelfContained
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Project = Join-Path $Root "Operator.Desktop\Operator.Desktop.csproj"
$PublishDir = Join-Path $Root "Operator.Desktop\bin\publish\OperatorAI-1.0-$Runtime"
$SelfContainedValue = if ($SelfContained.IsPresent) { "true" } else { "false" }

if (Test-Path $PublishDir) { Remove-Item $PublishDir -Recurse -Force }

Push-Location $Root
try {
    dotnet clean
    dotnet publish $Project `
        -c Release `
        -r $Runtime `
        --self-contained $SelfContainedValue `
        -p:PublishSingleFile=false `
        -o $PublishDir

    Write-Host "SUCCESS: Operator AI 1.0 published to $PublishDir" -ForegroundColor Green
}
finally {
    Pop-Location
}
