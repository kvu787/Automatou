# Create TerminalShell

## User

create a new shell called the "TerminalShell" in C:\Users\k\Repository\CSharp\Automapolis\Shells.
it is a c# command line application
double clicking Run.cmd should launch a terminal and you should be able to "play the game" by sending commands that inspect state, edit world state, and advance the simulation.

## Assistant

I’ll inspect the existing shells and simulation API, then build TerminalShell with state inspection, world editing, simulation advancement, and a double-clickable Run.cmd.

## Assistant

The Kernel already supports the needed world edits and explicit turns. I’ll have TerminalShell call it directly, with readable commands, a hex map, sector and force details, and in-terminal help. The launcher will build it without requiring Godot.

## Assistant

The shell and launcher are in place. World edits report the resulting sector state, and `advance` shows the updated map. I’m adding checks for command parsing, invalid-input recovery, and matching the Kernel’s results, then I’ll test the build and launcher.

## Assistant

The full solution builds without warnings, and all 12 TerminalShell tests pass. `Run.cmd -BuildOnly` also publishes successfully from outside the repository. I’m checking the interactive launcher and existing Kernel tests before committing the implementation and conversation log separately.

## Assistant

Interactive play through `Run.cmd` works: inspection, world edits, turn advancement, and clean exit all passed. The 12 Kernel tests also pass, and the published executable includes its runtime. Both implementation commits are complete; I’m recording the conversation in its required separate commit.

## Assistant

Created [TerminalShell](C:/Users/k/Repository/CSharp/Automapolis/Shells/TerminalShell/README.md). Double-click [Run.cmd](C:/Users/k/Repository/CSharp/Automapolis/Shells/TerminalShell/Run.cmd) to play.

Try:

```text
help
inspect 2 3
channel 2 3 25
deploy 2 3
advance 5
```

Edits leave time unchanged; `advance` resolves turns. State lasts for the session.

Verified the interactive launcher, standalone build, and all 24 tests. Implementation and conversation committed separately.
