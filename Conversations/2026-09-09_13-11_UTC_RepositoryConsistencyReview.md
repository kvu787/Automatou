# Repository consistency review

## User

i reviewed and edited mechanics.md. do a pass through the repo and fix and consistency issues.

## Assistant

I’ll read the updated mechanics document, check the rest of the repo for inconsistencies, and fix them without editing `Lore`.

## Assistant

I found a leftover TODO about traversable building interiors that contradicts your new rule, plus descriptions of terrain symbols that no longer match the color-only shell. I’ll align those documents and clarify which spatial rules are still awaiting implementation.

## Assistant

The documentation now agrees on non-enterable buildings, fixed building orientation, and color-only terrain. I also clarified the rectangular experiment’s separate rules. All 15 local links in the changed documents resolve, and `git diff --check` passes. I’m committing the fixes and conversation log separately.

## Assistant

Fixed inconsistencies across six documents:

- Removed TODOs contradicting non-enterable buildings.
- Clarified fixed building orientation and prototype limitations.
- Corrected terrain-color descriptions.
- Distinguished rectangular experiment rules from current mechanics.

Verified 15 local links and clean diffs. Documentation-only changes; no runtime tests needed. `Lore` untouched. Fixes and conversation committed separately.
