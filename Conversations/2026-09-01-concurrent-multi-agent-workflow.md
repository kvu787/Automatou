# Concurrent multi-agent workflow

Date: 2026-09-01

## User

> Currently, I'm mostly using a one-prompt-at-a-time workflow, which feels
> inefficient. Let's explore ways to improve or rearchitect.

## Assistant

Assessed the current AI-driven development process and recommended retaining
the Kernel/Shell product architecture while adding a repository-native
coordination layer. Identified `WorldKernel.cs` and the Godot `main.gd` as hot
files that would limit parallel development even with separate branches.

Recommended a persistent coordinator, two bounded implementation lanes, and a
serial review/integration gate. Proposed committed product, roadmap, decision,
visual, and task-packet documents so agents receive scope and acceptance
criteria from the repository instead of reconstructing them from conversation
history. Recommended splitting the two hot files along stable change axes
before attempting broad parallel implementation.

## User

> Would multiple different branches or clones of the repo help or hurt?

## Assistant

Recommended one repository with short-lived branches checked out in separate
Git worktrees. Explained that branches in a single checkout organize work but
do not permit simultaneous files and builds, while full clones duplicate Git
state and tend to drift. Proposed a clean `main` integration worktree and one
worktree per independent task, with explicit ownership boundaries and a small
work-in-progress limit.

Noted that branches postpone rather than eliminate conflicts, so parallel work
should not target the same hot files or cross-layer contracts. Also noted that
uncommitted changes do not transfer automatically into a new worktree.

## User

> Explain what this means: Branch per Git worktree—best option: isolated
> files/build outputs, shared Git history, easy comparison and integration.

## Assistant

Explained that a worktree is an additional physical checkout linked to the same
Git repository. Each worktree has its own files, active branch, staging area,
untracked files, and build outputs, while commits, refs, remotes, tags, and
history are shared. Illustrated separate main, sprite, Kernel, and Shell
directories and showed how a completed task branch would be merged from the
main worktree.

## User

> Provide a starter guide for understanding Git worktrees for someone who has
> used Git for many years but only used branches and mutually exclusive clone
> folders.

## Assistant

Provided a practical worktree guide covering the branch/worktree/clone mental
model, shared versus isolated state, creation and removal commands, daily use,
integration, the one-branch-per-worktree rule, detached worktrees, differences
from full clones, dirty-change behavior, the repository-wide stash, stale
metadata cleanup, runtime-resource collisions, a proposed Automapolis folder
layout, and a command reference.

## User

> There's currently another agent working on something, so only update/commit
> the conversation file until I tell you otherwise.
>
> Okay, this sounds promising. To implement a safe, concurrent multi-agent
> workflow, tell me what would be added to the repo.

## Assistant

Respected the active agent's ownership of all implementation files and changed
only this conversation record. Proposed a minimal first phase consisting of
concurrency rules in `AGENTS.md`, a development-workflow document, repository-
native task packets, lightweight decision records, safe worktree lifecycle
scripts, and one non-interactive verification entry point. Recommended that
task packets pin their base commit and declare owned paths, exclusions,
dependencies, acceptance commands, and expected evidence.

Proposed a later behavior-preserving partition of the Kernel and Godot hot
files, followed by a trial with two concurrent implementation lanes and a
single integrator. Deferred CI, scheduling, and more elaborate orchestration
until the basic workflow has been exercised and measured.
