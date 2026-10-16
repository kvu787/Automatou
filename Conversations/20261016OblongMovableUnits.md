# Oblong Movable Units

Date: 2026-10-16

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
Prefix these commits with `[cnv]`.
If I attach images to prompts, save and record these in the conversation logs.

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

</INSTRUCTIONS><environment_context>
  <cwd>C:\Users\k\Repository\Automatou</cwd>
  <shell>powershell</shell>
  <current_date>2026-10-16</current_date>
  <timezone>America/Los_Angeles</timezone>
  <filesystem><workspace_roots><root>C:\Users\k\Repository\Automatou</root><root>C:\Users\k\.codex\visualizations\2026\10\16\01a1448a-69b0-7f32-9393-7d05415ab8c9</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry><entry access="write"><path>C:\Users\k\Repository\Automatou</path></entry><entry access="write"><path>C:\Users\k\.codex\visualizations\2026\10\16\01a1448a-69b0-7f32-9393-7d05415ab8c9</path></entry><entry access="write"><special>:slash_tmp</special></entry><entry access="write"><special>:tmpdir</special></entry><entry access="read"><path>C:\Users\k\Repository\Automatou\.git</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\16\01a1448a-69b0-7f32-9393-7d05415ab8c9\.git</path></entry><entry access="read"><path>C:\Users\k\Repository\Automatou\.agents</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\16\01a1448a-69b0-7f32-9393-7d05415ab8c9\.agents</path></entry><entry access="read"><path>C:\Users\k\Repository\Automatou\.codex</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\16\01a1448a-69b0-7f32-9393-7d05415ab8c9\.codex</path></entry><entry access="read"><path>C:\Users\k\Repository\Automatou\.aws</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\16\01a1448a-69b0-7f32-9393-7d05415ab8c9\.aws</path></entry></file_system></permission_profile></filesystem>
</environment_context>

## User

<external_codex_apps_open_page>{"page_id":null}</external_codex_apps_open_page>

## User

what has been decided regarding moveable units that have oblong dimensions instead of square-ish or circle-ish dimensions?

## Assistant

I’ll look through the project notes and conversation history for decisions about oblong movable units.

## Assistant

The current rule excludes oblong footprints: units use regular hexagonal footprints, and turning leaves their occupied cells unchanged. Rectangular units were explored in a separate experiment. I’m checking whether any decision also constrained the proportions of the visible model.

## Assistant

We decided **against oblong logical footprints for movable units**. Originally, units had to occupy square `N×N` areas. After switching to hexes, this became regular hexagonal footprints: a center cell plus complete rings, occupying **1, 7, 19, 37… cells**. Both [Automatou1’s mechanics](C:/Users/k/Repository/Automatou/Automatou1/Documentation/Mechanics.md:54) and [Automatou2’s specification](C:/Users/k/Repository/Automatou/Automatou2/Specification.md:84) retain this rule.

The distinction is between **footprint and visible model**. Oblong-looking models have not been explicitly banned, and harmless visual overhang is allowed. But we never settled how very long models should fit that compact footprint. Coiling serpents, foreshortening, and making models shorter and broader were suggestions, not adopted requirements. See [art direction](C:/Users/k/Repository/Automatou/Automatou1/Documentation/ArtDirection.md:72).

Turning preserves the occupied cells, with **no swept collision checks**. Rectangular footprints and their turning mechanics remain in the separate [RectangularGridMotion experiment](C:/Users/k/Repository/Automatou/RectangularGridMotion/README.md:5); they have not been adopted into the main game’s footprint rules.
