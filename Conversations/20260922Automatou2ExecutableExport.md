# Automatou2 Executable Export

## User

C:\Users\k\Repository\Automatou\Automatou2

does the build and run process do any export to EXE?

## Assistant

I’ll check Automatou2’s launch scripts and build configuration for an EXE export step.

## Assistant

No. The current process:

- Builds the C# project in Debug mode using `dotnet build` ([Run.ps1, line 32](../Automatou2/Run.ps1#L32)).
- Launches the project through the installed Godot executable with `--path` ([Run.ps1, line 42](../Automatou2/Run.ps1#L42)).

It does not export a standalone game EXE or run `dotnet publish`.
