# Oblong Movable Units Continuation

## User

# AGENTS.md instructions for C:\Users\k\Repository\Automatou

<INSTRUCTIONS>
# Base template

## Platform compatibility

Assume that everything is running on Windows 11 x64 and only targets Windows 11 x64.

## Folder and file naming

This only applies to things that we have the freedom to name as wanted.
Use CamelCase.
Use complete proper words. Don't use typical shortenings. Good: Source, Documentation. Bad: src, docs.

## External tools

You may use the tools in `%UserProfile%\Program`.
You may refer to local copies of source repos in `%UserProfile%\Repository\External`.

## Git

Unless explicitly told not to by the user, commit your changes like a normal developer would.
When implementing stuff, avoid difficult-to-review "mega-commits".
Split large work into multiple commits to make it easier to review.
Separate commits that record conversations from other commits.

## Markdown tables

Tables in Markdown must be padded and aligned in a way to make them easy to read in a plaintext editor, not only in a Markdown viewer.

## Mathematical notation in Markdown

Any mathematical notation in Markdown files (LaTeX, KaTeX, MathJax, etc) must display properly in VSCode's Markdown previewer, GitHub.com's Markdown displayer, and the markdown viewer in the Windows 11 ChatGPT app.

## Applications

### Running

If you create a runnable application, create a `Run.cmd` file that builds and launches the application when Run.cmd is double-clicked from File Explorer.
Run.cmd must be located at the root of the application's folder in the git repo.
Run.cmd must be a simple wrapper for a PowerShell script named `Run.ps1` that contains the actual logic to minimize the amount of batch code written.

### Logging

When creating an application, create a folder called `MyLogOutput` at the root of the application's folder in the git repo.
For each run of the application, a folder must be created in MyLogOutput and named with the current timestamp. This PowerShell code shows what the name of the folder should be:

```powershell
$logFolderPath = "$env:UserProfile\Repository\Godot\VsyncStutterTest\MyLogOutput\$(Get-Date -Format "yyyy-MM-dd_HH-mm-ss")"
New-Item -ItemType "Directory" -Path $logFolderPath
```

Any logs for that application session must be put in that log folder.
`MyLogOutput/` must be gitignored.

# Base template additions

## Conversations

Record verbatim and commit all conversations in a folder named `Conversations` located at the root of this Git repo.
Use one file per conversation.
Prefix these files with the PowerShell evaluation of `[DateTime]::UtcNow.ToString("yyyy-MM-dd_HH-mm") + "_UTC"`.
Prefix these commits with `[cnv]`.
If I attach images to prompts, save and record these in the conversation logs.
If the conversation begins with `dnr`, then do not record the conversation.

## Application compatibility

Do not attempt to maintain any sort of application compatibility between different commits of the repo. This creates unwanted complexity.

# Repository-specific

Do not edit anything in the Lore folder.

## Repository layout

The repository root contains only the shared `.editorconfig`, `.gitattributes`,
`AGENTS.md`, `CLAUDE.md`, and `LICENSE.txt` files, the global `Blender`,
`Conversations`, and `Lore` folders, and these independent project folders
(excluding Git metadata):

- `Automatou1`: the original Kernel and Shells application.
- `Automatou2`: the standalone fast-prototyping application.
- `RectangularGridMotion`: the integer-only rectangular motion experiment.

Keep each project's source, assets, build configuration, tests, launchers, and
`.gitignore` inside its own folder. Projects must build and run without sibling
projects. Run development commands from the relevant project folder.

</INSTRUCTIONS>

## User

<external_codex_apps_open_page>{"page_id":null}</external_codex_apps_open_page>

## User

treat this conversation as a continuation of C:\Users\k\Repository\Automatou\Conversations\2026-10-16_11-50_UTC_OblongMovableUnits.md

## Assistant

I’ll read that conversation and the repository instructions to pick up where it left off.

## Assistant

I’ve read the previous conversation and will use it as context. The established decisions are regular hexagonal footprints, turning without changing occupied cells or swept collision checks, and no explicit ban on oblong visual models. How very long models fit those footprints remains unresolved.

## User

help me brainstorm approaches to oblong units. let's use an aircraft carrier and a squiggly serpent as examples

## Assistant

I’ll check the footprint and art-direction rules, then use the carrier and serpent to explore the tradeoffs.

## Assistant

The useful distinction is **looking long versus behaving long**. A carrier might need to fit through a narrow strait; a serpent might need to wrap around a rock. Those behaviors put different demands on the spatial rules.

These are possibilities to explore, not changes to the existing decisions.

| Approach                          | Aircraft carrier                                                                                                | Squiggly serpent                                                               | Main tradeoff                                                                                                                |
| --------------------------------- | --------------------------------------------------------------------------------------------------------------- | ------------------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------------- |
| **Compact silhouette**            | A stylized, shorter and broader hull fits inside a regular hexagonal footprint.                                 | A coiled body fills the footprint; the head establishes facing.                | Preserves the current rules, but reduces the sense of physical length.                                                       |
| **Reserve space around the body** | A long hull sits inside a large regular hexagonal footprint. Empty space beside it represents maneuvering room. | A winding body fits inside the footprint and changes its pose during movement. | Preserves length and current rules, but blocks considerable apparently empty ground.                                         |
| **Rigid oblong footprint**        | The occupied hexes follow the hull and rotate with it.                                                          | The entire curved body rotates as one fixed shape.                             | Length affects navigation, but the serpent behaves like a bent rigid object.                                                 |
| **Articulated footprint**         | Several connected sections approximate the hull, with their relative positions constrained to keep it straight. | Connected body sections follow the head and bend around obstacles.             | Supports physical length and flexibility, but introduces body configuration, self-collision, and more demanding pathfinding. |

**For the carrier, a rigid footprint seems the most natural option if geography should matter.** It could pass lengthwise through a narrow strait while being unable to turn broadside there. You would need to choose its rotation pivot and turning rule:

- **Rotate in place:** accept a turn when the destination footprint is clear. This can retain the existing decision against swept collision checks, although the occupied cells would now change.
- **Turn while advancing:** use movement actions that combine translation and a change of facing. This makes turning room a meaningful property of the ship.

The second approach offers stronger ship behavior, but a large 60-degree facing change may need carefully designed movement steps to feel convincing.

**For the serpent, decide whether its squiggles are animation or game state.** That is probably the most consequential choice here.

- **Animation:** it is logically one compact unit. Its body coils, stretches, and undulates inside its reserved area. Easier movement and clearer commands.
- **Game state:** its occupied body forms a connected chain. The head advances and the rest follows, releasing space behind the tail. It can snake through passages and span several locations at once.

The chain version creates interesting questions: can it block a doorway with its middle, surround another unit, reverse through its own body, or be attacked anywhere along its length? Those could become defining mechanics rather than incidental complications. It also needs a separate facing rule: the head's direction need not describe the orientation of the whole body.

My inclination is to explore **a rigid carrier and an articulated serpent** if physical shape is meant to produce tactics. They share occupied hexes and collision rules, while having different permitted movement actions. If the priority is keeping the present movement system simple, **a carrier with reserved maneuvering space and a visually coiled serpent** are the strongest starting points.

The deciding question is: **should the carrier's length and the serpent's bends change where they can go, or mainly how they look?**

## User

these are my current thoughts. they may change after experimentation:
Compact silhouette: Unacceptable because it impacts visual design too much.
Reserve space around the body: Possibly acceptable. I am concerned with how much "empty space" there could be in the hex footprint for oblong units.
Rigid oblong footprint: Unacceptable due to increased mechanics complexity.
Articulated footprint: Unacceptable due to increased mechanics complexity.

## Assistant

I’ll record these as provisional preferences and check where they fit in the design notes.

## Assistant

That narrows the exploration to **preserving the model’s proportions while keeping a regular hexagonal footprint**, with reserved empty space still an open concern.

The carrier is the harder case: its rigid hull leaves substantial space beside it. A serpent can change poses, but requiring it to coil just to fit would risk the same visual compromise you rejected.

A useful experiment would compare both models at several footprint sizes, showing:

- The occupied boundary alongside the actual body.
- Two units as close together as the rules permit.
- Movement beside a shoreline or obstacle.
- All six facings, including turn animations.

That would help distinguish whether the empty space feels acceptable in play, looks misleading, or prevents formations and passages you expect to work. **No acceptable amount of empty space has been decided yet.**

Recorded as provisional preferences in [ArtDirection.md](C:/Users/k/Repository/Automatou/Automatou1/Documentation/ArtDirection.md), committed as `5dfa4f6`.
