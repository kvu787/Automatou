# Repository Reorganization Request Review

## User

review this request.
do not execute the request.

---

# Request

Reroganize this repo so that it contains a subfolder for each wholly self-contained project, with
with several "global" files and folders at the root that apply to multiple projects.

# Files

After reorganization, these should be the only files at the repo root:

.editorconfig
.gitattributes
AGENTS.md
CLAUDE.md
LICENSE.txt

# Folders

After reorganization, these should be the only folders at the repo root.

These are "global folders" that may apply the multiple projects:

- Blender/
- Conversations/
- Lore/

These are "project folders" that each contain a complete, wholly self-contained project:

- RectangularGridMotion/
- Automatou1/
- Automatou2/

Automatou1 must contain the original project, which is essentially the repo excluding the `./Experiments/` subfolder
Automatou2 must contain `./Experiments/Automatou_FastPrototyping`

## Assistant

I’ll inspect the repository layout and review the request for ambiguities and migration risks, without carrying out the reorganization.

## Assistant

The layout is workable. The main gaps are the missing rule for `.gitignore`, the implicit source of `RectangularGridMotion`, and the meaning of “self-contained.” I’ll record this review in `Conversations` as required by `AGENTS.md`; the reorganization itself will remain unexecuted.

## Assistant

The request is workable, but I’d clarify these points before implementation:

1. **Explicitly map all three projects.** Add `Experiments/RectangularGridMotion/ → RectangularGridMotion/`. State that the *contents* of `Experiments/Automatou_FastPrototyping/` move directly into `Automatou2/`, without an extra nested directory.

2. **Define the exceptions for Automatou1.** “Essentially the repo excluding Experiments” also includes the global folders and retained root files. Say: move all remaining original-project files and folders into `Automatou1/`, excluding the explicitly retained root files, global folders, and Git metadata.

3. **Specify what happens to `.gitignore`.** It currently exists at the root but is absent from your permitted list. Either permit it there or distribute its rules into project-local `.gitignore` files, preserving relevant ignore coverage for global folders too.

4. **Exempt `.git/` from the root-folder restriction.** Describe the list as the allowed repository content, excluding Git metadata.

5. **Define “wholly self-contained.”** I suggest: each project contains its own source, assets, build configuration, tests, and launch scripts, and can build and run without sibling projects. Clarify whether runtime/build dependencies on `Blender/` or other global folders are allowed, and whether independence must hold when a project folder is copied outside this repository.

6. **Require migration verification.** Update affected paths, documentation links, build settings, launch scripts, and ignore rules; verify each project’s build, existing tests, and launch behavior. Preserve application behavior and leave `Lore/` untouched. Historical conversation logs should remain verbatim.

7. **Set the launcher scope.** The current root `Run.cmd` delegates to a shell launcher, and RectangularGridMotion embeds substantial batch logic. Specify whether this task should also bring these into the prescribed `Run.cmd` → `Run.ps1` pattern or leave launcher refactoring outside scope.

Also fix “Reroganize,” the repeated “with,” and “apply the multiple projects.”

No reorganization was performed. Only the required conversation record was added.

