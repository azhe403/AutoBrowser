# Commit Strategy — CRITICAL

## Golden Rule
**NEVER commit or push without explicit user permission.** Wait to be asked. This is the #1 rule — violating it is unacceptable. Even if the user says "fix" or "help", always ask "commit?" before running git commit.

## Split Per Logical Context — ALWAYS
**Always split changes per logical context.** Group related edits together and separate unrelated changes into distinct commits or edits. Each commit/edit should represent one cohesive, meaningful change (e.g., a single feature, a bug fix, a refactor). Avoid mixing unrelated changes in the same commit or edit.

This applies to:
- Code commits — one logical change per commit
- Memory updates — update only the relevant memory topic
- File edits — keep edits focused on a single concern

## Natural Timing
Add a random delay gap of 3 to 5 minutes between commits to make the commit history look natural (simulates human work pace).

## How to implement
- Group related files/changes into separate commits
- Use `Start-Sleep -Seconds (Get-Random -Minimum 180 -Maximum 300)` in PowerShell between commits
- Write meaningful commit messages for each logical change