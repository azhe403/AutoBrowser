## Weekly NuGet Update Check

Schedule a recurring weekly task to check for NuGet package updates in AutoBrowser.

Steps:
1. Run `dotnet list AutoBrowser.slnx package --outdated` to check all projects in the solution at once.
2. Review outdated packages (especially WPF UI, Serilog, .NET SDK).
3. Skip patch/revision-only bumps (x.y.Z changes). Update only minor or major version changes.
4. Rebuild + run `dotnet test` to confirm no regressions.
5. Commit updates separately with clear message.
6. Sync memory after any changes.

Note: AGENTS.md forbids committing/pushing without explicit user permission — request before committing.
