@echo off
setlocal
set "AUTOMAPOLIS_ROOT=%~dp0"
set "AUTOMAPOLIS_GODOT=%GODOT_EXE%"
if not defined AUTOMAPOLIS_GODOT set "AUTOMAPOLIS_GODOT=%USERPROFILE%\Program\Godot_v4.7.2-stable_win64.exe\Godot_v4.7.2-stable_win64_console.exe"

if not exist "%AUTOMAPOLIS_GODOT%" (
  echo Godot 4.7.2 was not found.
  echo Set GODOT_EXE to the Godot 4.7.2 console executable and try again.
  pause
  exit /b 1
)

echo [1/5] Building the Kernel and tests...
dotnet build "%AUTOMAPOLIS_ROOT%Automapolis.slnx" --configuration Release
if errorlevel 1 goto :failed

echo [2/5] Running Kernel tests...
dotnet run --project "%AUTOMAPOLIS_ROOT%Tests\Automapolis.Kernel.Tests" --configuration Release --no-build
if errorlevel 1 goto :failed

echo [3/5] Exporting the Godot 4.7.2 standalone game...
if not exist "%AUTOMAPOLIS_ROOT%Shells\SimplePaint3DShell\Build" mkdir "%AUTOMAPOLIS_ROOT%Shells\SimplePaint3DShell\Build"
"%AUTOMAPOLIS_GODOT%" --headless --path "%AUTOMAPOLIS_ROOT%Shells\SimplePaint3DShell" --export-release "Windows Desktop" "%AUTOMAPOLIS_ROOT%Shells\SimplePaint3DShell\Build\SimplePaint3DShell.exe"
if errorlevel 1 goto :failed

echo [4/5] Publishing the standalone Kernel host...
dotnet publish "%AUTOMAPOLIS_ROOT%Source\Automapolis.Kernel.Host\Automapolis.Kernel.Host.csproj" --configuration Release --no-build --output "%AUTOMAPOLIS_ROOT%Shells\SimplePaint3DShell\Build\KernelHost"
if errorlevel 1 goto :failed

echo [5/5] Launching SimplePaint3DShell...
start "SimplePaint3DShell" "%AUTOMAPOLIS_ROOT%Shells\SimplePaint3DShell\Build\SimplePaint3DShell.exe"
exit /b 0

:failed
echo.
echo Build failed. Review the output above.
pause
exit /b 1
