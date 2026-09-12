[CmdletBinding()]
param(
    [switch]$BuildOnly
)

$ErrorActionPreference = 'Stop'

function InvokeBuildCommand {
    param([string]$Executable, [string[]]$Arguments)

    & $Executable @Arguments
    if ($LASTEXITCODE -ne 0) {
        throw "$Executable failed with exit code $LASTEXITCODE."
    }
}

Push-Location -LiteralPath $PSScriptRoot
try {
    $repositoryRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
    $hostProject = Join-Path $repositoryRoot 'Source\Automatou.Kernel.Host\Automatou.Kernel.Host.csproj'
    $buildDirectory = Join-Path $PSScriptRoot 'Build'
    $shellExecutable = Join-Path $buildDirectory 'SimplePaint3DShell.exe'
    $godotExecutable = $env:GODOT_EXE
    if (-not $godotExecutable) {
        $godotExecutable = Join-Path $env:USERPROFILE 'Program\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
    }
    if (-not (Test-Path -LiteralPath $godotExecutable -PathType Leaf)) {
        throw 'Godot 4.7.2 .NET was not found. Set GODOT_EXE to its Windows x64 console executable.'
    }
    $godotVersion = & $godotExecutable --version
    if ($LASTEXITCODE -ne 0 -or $godotVersion -notmatch '^4\.7\.2\.stable\.mono\.') {
        throw 'Godot 4.7.2 .NET and matching Windows x64 .NET export templates are required.'
    }

    Write-Host '[1/6] Building the Windows x64 C# shell, Kernel, and tests...'
    InvokeBuildCommand 'dotnet' @('build', (Join-Path $repositoryRoot 'Automatou.slnx'), '--configuration', 'Debug')

    Write-Host '[2/6] Running Kernel tests...'
    InvokeBuildCommand 'dotnet' @('run', '--project', (Join-Path $repositoryRoot 'Tests\Automatou.Kernel.Tests'), '--configuration', 'Debug', '--no-build')

    Write-Host '[3/6] Publishing the Windows x64 Kernel host for editor play and integration tests...'
    $publishArguments = @('publish', $hostProject, '--configuration', 'Release', '--runtime', 'win-x64', '--self-contained', 'true')
    InvokeBuildCommand 'dotnet' ($publishArguments + @('--output', (Join-Path $PSScriptRoot 'KernelHost')))

    Write-Host '[4/6] Importing and testing SimplePaint3DShell in Godot 4.7.2 .NET...'
    InvokeBuildCommand $godotExecutable @('--headless', '--path', $PSScriptRoot, '--editor', '--import')
    InvokeBuildCommand $godotExecutable @('--headless', '--path', $PSScriptRoot, 'res://Tests/HexGridSmoke.tscn')

    Write-Host '[5/6] Exporting the standalone Windows x64 C# shell...'
    New-Item -ItemType Directory -Path $buildDirectory -Force | Out-Null
    InvokeBuildCommand $godotExecutable @('--headless', '--path', $PSScriptRoot, '--export-release', 'Windows Desktop', $shellExecutable)

    Write-Host '[6/6] Publishing the standalone Windows x64 Kernel host...'
    InvokeBuildCommand 'dotnet' ($publishArguments + @('--no-build', '--output', (Join-Path $buildDirectory 'KernelHost')))

    if (-not $BuildOnly) {
        Write-Host 'Launching SimplePaint3DShell...'
        Start-Process -FilePath $shellExecutable -WorkingDirectory $buildDirectory
    }
    exit 0
}
catch {
    Write-Host "Build failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host 'Close a running SimplePaint3DShell before exporting again.'
    if (-not $BuildOnly) {
        Read-Host 'Press Enter to close' | Out-Null
    }
    exit 1
}
finally {
    Pop-Location
}
