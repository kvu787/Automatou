# Rename and migrate SimplePaint3DShell

## User

Rename the folder and entire project in "C:\Users\k\Repository\CSharp\Automapolis\Shells\Godot" to SimplePaint3DShell.
Rewrite SimplePaint3DShell using Godot 4.7.2 .NET with C# instead of the current Godot 4.7.2 with gdscript.

## Assistant

I’ll inspect the Godot shell, rename it to SimplePaint3DShell, and migrate its scripts to C# while preserving its behavior. I’ll also verify the build and launcher and record this conversation as required by the repository instructions.

## Assistant

The shell has two GDScript files and an integration test covering hex geometry, input, protocol exchange, and rendering. Godot 4.7.2 .NET and its export templates are already installed. I’ll migrate all three scripts to C#, update the project and launchers, and preserve the current symbolic hex-grid presentation.

## Assistant

The folder and project rename are committed separately. I’m now porting the controls and hex drawing to C#, including the existing seam and highlight fixes. The Kernel will remain a separate process using the same JSON-lines protocol.

## Assistant

The C# shell builds with zero warnings. The first integration run passed the behavior checks for selection, resizing, keyboard input, commands, Unicode names, and 64-bit seeds. One test used the wrong name for Godot’s .NET feature flag; I’m correcting that assertion, then checking rendering and the standalone export.

## Assistant

All 12 Kernel tests and 236 C# integration checks pass. The rendered suite also passes all 275 checks, including seam and highlight checks across multiple outline widths and window sizes. The full launcher workflow exported SimplePaint3DShell.exe successfully; I’m checking its startup and finishing the documentation.

## Assistant

The exported application starts successfully and terminates its Kernel host when it closes. Debug and Release builds pass with zero warnings, and no GDScript files remain in SimplePaint3DShell. I’m committing the migration, test and launcher updates, and conversation log separately.

## Assistant

Renamed the folder and project to **SimplePaint3DShell** and migrated all shell scripts and tests to **Godot 4.7.2 .NET with C#**, preserving existing behavior.

Verified clean Debug/Release builds, 12 Kernel tests, 275 integration/rendering checks, and standalone startup.

Launch with [Run.cmd](C:/Users/k/Repository/CSharp/Automapolis/Shells/SimplePaint3DShell/Run.cmd). Changes and conversation are committed; the working tree is clean.
