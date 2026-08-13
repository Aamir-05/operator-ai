param(
    [Parameter(Mandatory = $true)]
    [string]$ProjectRef
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command supabase -ErrorAction SilentlyContinue)) {
    throw "Supabase CLI was not found. Install it first."
}

Push-Location $PSScriptRoot
try {
    supabase link --project-ref $ProjectRef
    supabase db push
    supabase functions deploy operator-pair --no-verify-jwt
    supabase functions deploy operator-device --no-verify-jwt
    supabase functions deploy operator-command
    supabase functions deploy transcribe-command

    if ($env:OPENAI_API_KEY) {
        supabase secrets set "OPENAI_API_KEY=$env:OPENAI_API_KEY"
    }
    else {
        Write-Warning "OPENAI_API_KEY is not set. Text commands work, but mobile voice transcription is disabled until this secret is configured."
    }

    Write-Host "SUCCESS: Operator AI Cloud deployed." -ForegroundColor Green
}
finally {
    Pop-Location
}
