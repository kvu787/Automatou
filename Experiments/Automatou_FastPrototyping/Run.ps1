param(
    [switch]$Verify,
    [switch]$VerifyInterface,
    [switch]$BuildOnly
)

$ErrorActionPreference = 'Stop'
$experimentRoot = $PSScriptRoot
$sessionDirectory = Join-Path $experimentRoot ('MyLogOutput\' + (Get-Date -Format 'yyyy-MM-dd_HH-mm-ss'))
New-Item -ItemType Directory -Force -Path $sessionDirectory | Out-Null
Start-Transcript -Path (Join-Path $sessionDirectory 'Launcher.log') | Out-Null
try {
    $godotExecutable = $env:GODOT_EXE
    if (-not $godotExecutable) {
        $godotExecutable = Join-Path $env:UserProfile 'Program\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe'
    }
    if (-not (Test-Path -LiteralPath $godotExecutable -PathType Leaf)) {
        throw 'Install Godot 4.7.2 .NET x64 in your Program folder, or set GODOT_EXE to its console executable.'
    }
    $godotVersion = & $godotExecutable --version
    if ($LASTEXITCODE -ne 0 -or $godotVersion -notmatch '^4\.7\.2\..*mono') { throw 'GODOT_EXE must point to Godot 4.7.2 .NET.' }
    Push-Location $experimentRoot
    try {
        # Godot ships its own exact-version NuGet packages. Use those without a network dependency.
        $packageDirectory = Join-Path (Split-Path -Parent $godotExecutable) 'GodotSharp\Tools\nupkgs'
        if (-not (Test-Path -LiteralPath $packageDirectory)) { throw 'The Godot .NET package directory is missing.' }
        $escapedPackageDirectory = [System.Security.SecurityElement]::Escape($packageDirectory)
        $packageConfiguration = Join-Path $sessionDirectory 'NuGet.Config'
        '<configuration><packageSources><clear/><add key="GodotLocal" value="' + $escapedPackageDirectory + '" /></packageSources><config><add key="signatureValidationMode" value="accept" /></config></configuration>' | Set-Content -LiteralPath $packageConfiguration -Encoding UTF8
        & dotnet restore Automatou.csproj --configfile $packageConfiguration -p:NuGetAudit=false
        if ($LASTEXITCODE -ne 0) { throw 'Package restore failed.' }
        & dotnet build Automatou.csproj --no-restore --configuration Debug
        if ($LASTEXITCODE -ne 0) { throw 'The game did not build.' }
        if ($Verify) {
            & dotnet run --project Tests/SimulationTests.csproj --configuration Release -p:NuGetAudit=false -- --output $sessionDirectory
            if ($LASTEXITCODE -ne 0) { throw 'Simulation verification failed.' }
        }
        if ($BuildOnly) { Write-Host 'Build completed.' }
        else {
            $gameArguments = @('--path', $experimentRoot, '--log-file', (Join-Path $sessionDirectory 'Godot.log'))
            if ($VerifyInterface) { $gameArguments += @('--', '--verify-interface', ('--session-log=' + $sessionDirectory)) }
            else { $gameArguments += @('--', ('--session-log=' + $sessionDirectory)) }
            & $godotExecutable @gameArguments
            if ($LASTEXITCODE -ne 0) { throw 'The game reported a failure. Review the session logs.' }
        }
    } finally { Pop-Location }
    Write-Host ('Session logs: ' + $sessionDirectory)
} catch {
    Write-Host ('Could not run Automatou: ' + $_.Exception.Message) -ForegroundColor Red
    exit 1
} finally {
    Stop-Transcript | Out-Null
}
