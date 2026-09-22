[CmdletBinding()]
param(
    [switch]$BuildOnly
)

$ErrorActionPreference = 'Stop'

Push-Location -LiteralPath $PSScriptRoot
try {
    if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
        throw 'The x64 .NET 10 SDK is required to build TerminalShell. Install it and run again.'
    }

    $buildDirectory = Join-Path $PSScriptRoot 'Build'
    $shellExecutable = Join-Path $buildDirectory 'TerminalShell.exe'
    Write-Host 'Building TerminalShell for Windows x64...'
    & dotnet publish (Join-Path $PSScriptRoot 'TerminalShell.csproj') --configuration Release --runtime win-x64 --self-contained true --output $buildDirectory --nologo
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed with exit code $LASTEXITCODE. Close any running TerminalShell before rebuilding."
    }

    if (-not $BuildOnly) {
        # Run in the console opened by Run.cmd so stdin and stdout stay interactive.
        & $shellExecutable
        if ($LASTEXITCODE -ne 0) {
            throw "TerminalShell exited with code $LASTEXITCODE."
        }
    }
    exit 0
}
catch {
    Write-Host "TerminalShell: $($_.Exception.Message)" -ForegroundColor Red
    if (-not $BuildOnly) {
        Read-Host 'Press Enter to close' | Out-Null
    }
    exit 1
}
finally {
    Pop-Location
}
