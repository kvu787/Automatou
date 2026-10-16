# Reorganize Projects

## User

execute this request:

# Request

Reorganize this repo so that it contains a subfolder for each wholly self-contained project, with
several "global" files and folders at the root that apply to multiple projects.

# Files

After reorganization, these should be the only files at the repo root:

.editorconfig
.gitattributes
AGENTS.md
CLAUDE.md
LICENSE.txt

# Folders

After reorganization, these should be the only folders at the repo root.

These are "global folders" that may apply to multiple projects:

- Blender/
- Conversations/
- Lore/

These are "project folders" that each contain a complete, wholly self-contained project:

- RectangularGridMotion/
- Automatou1/
- Automatou2/

RectangularGridMotion contains `./Experiments/RectangularGridMotion`
Automatou1 contains the original project, which is essentially the repo excluding the `./Experiments/` subfolder
Automatou2 contains `./Experiments/Automatou_FastPrototyping`

## Assistant

I’ll inspect the repository, move each project into its requested folder, and update affected paths. I’ll leave `Lore/` untouched and commit the reorganization separately from the conversation log.

## Assistant

The projects already keep their build and runtime dependencies within their own folders. I’ll preserve that structure, move `.gitignore` rules into each project, and update documentation that refers to the old repository root. Existing local data and build files will move with their projects.

## Assistant

The root now matches the requested layout, excluding Git metadata. The motion tests pass, and Automatou2 builds successfully. Build/export checks also hit access restrictions in the sandbox; I’m rerunning the affected checks with access to the installed tools’ working directories.

## Assistant

All existing tests passed: 12 Kernel tests, 12 terminal tests, 236 shell checks, 36 Automatou2 checks, and both rectangular-motion suites. Both export checks passed too. I’m finishing the path review and launch checks, then committing the changes and conversation record.

## Assistant

Reorganized and committed as `26dc2b4`. The root matches your requested layout, apart from `.git` metadata. Documentation and project-local ignore rules are updated; `Lore/` is unchanged.

Builds, existing tests, exports, and headless launch checks passed for all three projects.

Conversation recorded in a separate `[cnv]` commit. Working tree is clean.
