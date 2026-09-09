@echo off
setlocal
set "EXPERIMENT_ROOT=%~dp0"
set "EXPERIMENT_GODOT=%GODOT_EXE%"
if not defined EXPERIMENT_GODOT set "EXPERIMENT_GODOT=%USERPROFILE%\Program\Godot_v4.7.2-stable_win64.exe\Godot_v4.7.2-stable_win64_console.exe"
if not exist "%EXPERIMENT_GODOT%" goto :missing
"%EXPERIMENT_GODOT%" --version | findstr /b /c:"4.7.2." >nul
if errorlevel 1 goto :missing
"%EXPERIMENT_GODOT%" --headless --path "%EXPERIMENT_ROOT%" --editor --import
if errorlevel 1 goto :failed
"%EXPERIMENT_GODOT%" --headless --path "%EXPERIMENT_ROOT%" --script Verify.gd
if errorlevel 1 goto :failed
if not exist "%EXPERIMENT_ROOT%Build" mkdir "%EXPERIMENT_ROOT%Build"
"%EXPERIMENT_GODOT%" --headless --path "%EXPERIMENT_ROOT%" --export-release "Windows Desktop" "%EXPERIMENT_ROOT%Build\RectangularGridMotion.exe"
if errorlevel 1 goto :failed
start "Rectangular Grid Motion" "%EXPERIMENT_ROOT%Build\RectangularGridMotion.exe"
exit /b 0
:missing
echo Set GODOT_EXE to a Godot 4.7.2 console executable. Windows export templates are required.
:failed
echo Build failed. Review the output above.
pause
exit /b 1
