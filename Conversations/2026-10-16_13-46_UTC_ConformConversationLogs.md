# Conform Conversation Logs

Migration note: Historical filename timestamps use each file's first recorded Git author timestamp, converted to UTC. These are recording times; original conversation start times are unavailable. Existing transcript wording is preserved; attachment link destinations are updated for renamed or restored images. This transcript contains the user and assistant messages verbatim.

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
<environment_context>
  <cwd>C:\Users\k\Repository\Automatou</cwd>
  <shell>powershell</shell>
  <current_date>2026-10-16</current_date>
  <timezone>America/Los_Angeles</timezone>
  <filesystem><workspace_roots><root>C:\Users\k\Repository\Automatou</root><root>C:\Users\k\.codex\visualizations\2026\10\16\01a144f4-f949-7a81-bf8b-0fb59b20f52a</root></workspace_roots><permission_profile type="managed"><file_system type="restricted"><entry access="read"><special>:root</special></entry><entry access="write"><path>C:\Users\k\Repository\Automatou</path></entry><entry access="write"><path>C:\Users\k\.codex\visualizations\2026\10\16\01a144f4-f949-7a81-bf8b-0fb59b20f52a</path></entry><entry access="write"><special>:slash_tmp</special></entry><entry access="write"><special>:tmpdir</special></entry><entry access="read"><path>C:\Users\k\Repository\Automatou\.git</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\16\01a144f4-f949-7a81-bf8b-0fb59b20f52a\.git</path></entry><entry access="read"><path>C:\Users\k\Repository\Automatou\.agents</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\16\01a144f4-f949-7a81-bf8b-0fb59b20f52a\.agents</path></entry><entry access="read"><path>C:\Users\k\Repository\Automatou\.codex</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\16\01a144f4-f949-7a81-bf8b-0fb59b20f52a\.codex</path></entry><entry access="read"><path>C:\Users\k\Repository\Automatou\.aws</path></entry><entry access="read"><path>C:\Users\k\.codex\visualizations\2026\10\16\01a144f4-f949-7a81-bf8b-0fb59b20f52a\.aws</path></entry></file_system></permission_profile></filesystem>
</environment_context>

## User

<external_codex_apps_open_page>{"page_id":null}</external_codex_apps_open_page>

## User

Fix the Conversations folder to conform with this updated agents.md section:
## Conversations

Record verbatim and commit all conversations in a folder named `Conversations` located at the root of this Git repo.
Use one file per conversation.
Prefix these files with the PowerShell evaluation of `[DateTime]::UtcNow.ToString("yyyy-MM-dd_HH-mm") + "_UTC"`.
Prefix these commits with `[cnv]`.
If I attach images to prompts, save and record these in the conversation logs.
If the conversation begins with `dnr`, then do not record the conversation.


## Assistant

I’ll check the existing conversation logs and repository instructions, fix any naming or layout issues, and commit the changes with the `[cnv]` prefix.

## Assistant

The logs use date-only filenames. I’ll use each file’s first recorded Git timestamp, converted to UTC, for the new prefix; the original conversation times aren’t available in the logs. I also found three attached images recoverable from Git history. I’ll restore them and update links affected by the renames.

## Assistant

All 84 existing files now have UTC timestamp prefixes. The three recovered images are restored, and all 24 relative image links resolve. I verified that transcript text and existing image bytes were preserved, apart from the necessary link updates. I’m recording this conversation and committing the changes now.

## Assistant

Renamed all 84 existing files with UTC prefixes, restored three missing images, and verified image links. Historical timestamps come from first-recorded Git dates. Committed the migration and this conversation with a `[cnv]` prefix.
