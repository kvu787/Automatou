# Base template

## External tools

You may use the tools in `%UserProfile%\Program`.
You may refer to local copies of source repos in `%UserProfile%\Repository\External`.

## Godot

If you create a Godot project, include a "Run.cmd" file that builds and launches the standalone exe of the Godot project by double-clicking the Run.cmd from File Explorer.

## Git

When implementing stuff, avoid difficult-to-review "mega-commits".
Split large work into multiple commits to make it easier to review.
Separate commits that record conversations from other commits.

## Mathematical notation in Markdown

Any mathematical notation in Markdown files (LaTeX, KaTeX, MathJax, etc) must display properly in VSCode's Markdown previewer, GitHub.com's Markdown displayer, and the markdown viewer in the Windows 11 ChatGPT app.

# Base template additions

## Conversations

Record and commit all conversations in a `Conversations` folder located at the root of this Git repo.
Use one file per conversation.
Prefix these commits with `[record]`.
If I attach images to prompts, you save and record these in the conversation logs.

## Compatibility

Do not attempt to maintain any sort of application compatibility between different commits of the repo. This creates unwanted complexity.

# Repository-specific

Do not edit anything in the Lore folder.
