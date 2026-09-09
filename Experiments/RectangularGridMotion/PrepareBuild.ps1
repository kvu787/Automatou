param([Parameter(Mandatory = $true)][string]$ExecutablePath)

$ErrorActionPreference = 'Stop'
try {
    $experimentExecutable = [System.IO.Path]::GetFullPath($ExecutablePath)
    $experimentName = [System.IO.Path]::GetFileNameWithoutExtension($experimentExecutable)
    foreach ($experimentProcess in @(Get-Process -Name $experimentName -ErrorAction SilentlyContinue)) {
        if ($experimentProcess.Path -ine $experimentExecutable) { continue }
        if ($experimentProcess.HasExited) { continue }
        Write-Host 'Closing the previous experiment window before rebuilding...'
        if (-not $experimentProcess.CloseMainWindow()) {
            if ($experimentProcess.HasExited) { continue }
            throw 'Close the previous experiment window, then run Run.cmd again.'
        }
        if (-not $experimentProcess.WaitForExit(5000)) {
            throw 'The previous experiment is still closing. Close it, then run Run.cmd again.'
        }
    }
    exit 0
} catch {
    Write-Host ('Cannot prepare the experiment build: ' + $_.Exception.Message)
    exit 1
}
