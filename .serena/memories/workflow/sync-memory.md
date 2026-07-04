# Sync Memory on Project Changes

Every time a project change is made (code edits, config changes, new files, etc.), the state must be synced to Serena memories so future tasks have accurate context.

## What to sync
- New features or components added
- Architecture decisions or refactors
- Configuration changes (package deps, settings, etc.)
- Files created, renamed, or deleted
- Commits made and their purpose

## How to sync
Use `serena_write_memory` with an appropriate topic path (e.g., `project/changes/YYYY-MM-DD` or update existing topic memories) to capture the delta. Overwrite existing memories when the content they track has changed.

## Why
Keeps the agent's context fresh across sessions and prevents stale/incorrect assumptions about the project.