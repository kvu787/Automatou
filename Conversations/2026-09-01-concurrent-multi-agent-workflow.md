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

## User

> Okay, this is straying away from the core issue. I want to be able to review
> the repo, playtest the game, and define tasks, directions, feedback, etc.
> without getting bogged down in coordination details. The AI agents should
> just be a compute resource continuously developing the game based on that.
> Me having to coordinate the agents is inefficient.

## Assistant

Accepted the correction and reframed the target as a self-driving game studio,
not a user-operated multi-agent workflow. Proposed one persistent Director as
the only human-facing interface. The Director would interpret product feedback,
maintain priorities, decompose work, allocate isolated workers, review their
results, integrate safe changes, keep `main` playable, and present concise
playtest checkpoints. Worktrees, branches, task briefs, merge ordering, and
worker supervision would remain internal machinery rather than user duties.

Recommended a continuous loop of direction, autonomous implementation and
verification, a playable checkpoint with a focused change summary, and new
human feedback. The system should continue useful independent work when one
creative decision is blocked, surface only high-value product questions, and
reserve user attention for reviewing the game and deciding its direction.

Proposed using a durable Codex goal or project-teammate task as the Director,
with a recurring heartbeat when continuous background progress is desired.
Recommended only a small amount of repository-visible state: durable game
direction, AI-maintained development state, a one-command non-interactive
verification pipeline, and a one-command review build. Detailed task queues and
worker coordination should be generated and managed by the Director, not by
the user.

## User

> This is interesting, but I'm wondering if there isn't already some solid
> off-the-shelf solutions that do this. I am 100% sure there are many other
> people looking for this workflow.

## Assistant

Confirmed that a rapidly developing product category now targets this workflow,
usually described as software factories, agent orchestration, managed agent
fleets, background agents, or agent mission control. Distinguished products
that merely present parallel task dashboards from systems that attempt the full
intake, planning, execution, review, and release loop.

Identified OpenAI Symphony as the clearest open-source expression of the desired
architecture: a long-running service that monitors project work, creates
isolated workspaces, dispatches Codex sessions, collects proof, and allows the
human to manage work rather than supervise agents. Noted that it is explicitly
an engineering preview, is issue-tracker driven, and lacks a polished product
direction or playtesting interface.

Identified Factory Software Factory as the closest commercial description of a
24/7 full-SDLC autonomous system; Devin managed sessions as the closest
conversational coordinator that decomposes work across isolated workers; Cursor
Automations and Cloud Agents as a strong always-on event/schedule platform; and
GitHub Copilot Agents as a mature backlog and mission-control surface. Noted
that Conductor and Vibe Kanban primarily make human-managed parallel agents
easier and therefore do not remove the coordination burden at the center of
the user's request.

Concluded that no evaluated product fully supplies continuous game-design
judgment and native Windows/Godot playtesting out of the box. Recommended
evaluating Symphony, Factory, and Devin before building orchestration, and
adding only an Automapolis-specific direction and playtest feedback layer on
top of the selected substrate.
