# Commit Strategy

## Split Large Changes
When there are too many changes, split the commit into multiple logical commits instead of one large commit.

## Natural Timing
Add a random delay gap of 3 to 5 minutes between commits to make the commit history look natural (simulates human work pace).

## How to implement
- Group related files/changes into separate commits
- Use `Start-Sleep -Seconds (Get-Random -Minimum 180 -Maximum 300)` in PowerShell between commits
- Write meaningful commit messages for each logical change