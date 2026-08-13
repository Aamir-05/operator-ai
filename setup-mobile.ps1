$ErrorActionPreference = "Stop"
Set-Location (Join-Path $PSScriptRoot "Operator.Mobile")
npm install
npx expo install --fix
Write-Host "Operator AI Mobile dependencies are ready." -ForegroundColor Green
