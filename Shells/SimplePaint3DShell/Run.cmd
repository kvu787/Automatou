@echo off
setlocal
set "SHELL_ROOT=%~dp0"
set "REPOSITORY_ROOT=%~dp0..\..\"
set "SHELL_GODOT=%GODOT_EXE%"
if not defined SHELL_GODOT set "SHELL_GODOT=%USERPROFILE%\Program\Godot_v4.7.2-stable_mono_win64\Godot_v4.7.2-stable_mono_win64_console.exe"

if not exist "%SHELL_GODOT%" goto :missing
"%SHELL_GODOT%" --version | findstr /b /c:"4.7.2.stable.mono." >nul
if errorlevel 1 goto :missing

echo [1/7] Building the C# shell, Kernel, and tests...
dotnet build "%REPOSITORY_ROOT%Automapolis.slnx" --configuration Debug
if errorlevel 1 goto :failed

echo [2/7] Running Kernel tests...
dotnet run --project "%REPOSITORY_ROOT%Tests\Automapolis.Kernel.Tests" --configuration Debug --no-build
if errorlevel 1 goto :failed

echo [3/7] Publishing the Kernel host for editor play and integration tests...
dotnet publish "%REPOSITORY_ROOT%Source\Automapolis.Kernel.Host\Automapolis.Kernel.Host.csproj" --configuration Release --output "%SHELL_ROOT%KernelHost"
if errorlevel 1 goto :failed

echo [4/7] Importing and testing SimplePaint3DShell in Godot 4.7.2 .NET...
"%SHELL_GODOT%" --headless --path "%SHELL_ROOT%." --editor --import
if errorlevel 1 goto :failed
"%SHELL_GODOT%" --headless --path "%SHELL_ROOT%." res://Tests/HexGridSmoke.tscn
if errorlevel 1 goto :failed

echo [5/7] Exporting the standalone C# shell...
if not exist "%SHELL_ROOT%Build" mkdir "%SHELL_ROOT%Build"
"%SHELL_GODOT%" --headless --path "%SHELL_ROOT%." --export-release "Windows Desktop" "%SHELL_ROOT%Build\SimplePaint3DShell.exe"
if errorlevel 1 goto :failed

echo [6/7] Publishing the standalone Kernel host...
dotnet publish "%REPOSITORY_ROOT%Source\Automapolis.Kernel.Host\Automapolis.Kernel.Host.csproj" --configuration Release --no-build --output "%SHELL_ROOT%Build\KernelHost"
if errorlevel 1 goto :failed

if /i "%~1"=="--build-only" exit /b 0
echo [7/7] Launching SimplePaint3DShell...
start "SimplePaint3DShell" "%SHELL_ROOT%Build\SimplePaint3DShell.exe"
exit /b 0

:missing
echo Godot 4.7.2 .NET was not found.
echo Set GODOT_EXE to its console executable. Matching .NET export templates are required.
goto :failed

:failed
echo.
echo Build failed. Review the output above. Close a running SimplePaint3DShell before exporting again.
if /i not "%~1"=="--build-only" pause
exit /b 1
