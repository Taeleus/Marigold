# Legacy release procedure

Current version: **0.16.1**. The redesigned UI and both follow-up review fixes passed their local-package in-game checks. PluginHub publication targets **Legacy/CLR only**. Interim and dual-package publication are deferred.

Building locally requires a .NET 10 SDK, .NET Framework 4.8 reference assemblies and the installed Space Engineers Bin64 folder. The build scripts accept `-Bin64 'D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64'` if you need to specify the game location.

1. Run `./build.ps1 -Runtime Legacy` and `./verify-source.ps1 -Runtime Legacy` against the tested code. Preserve warnings accurately; unavailable vulnerability data is not a clean dependency audit.
2. Confirm final regression acceptance in TESTING-TODO.md. Package only plugin.dll/plugin.pdb/plugin.xml; exclude game DLLs, caches, skill files and backups.
3. Commit/push dev, merge/push master, then run `./prepare-release.ps1 -Commit HEAD` to generate a full-SHA-pinned Legacy manifest. It must have exactly one runtime entry (`CLR`).
4. Load the exact source pin through Pulsar Legacy with the local binary disabled; test a fresh install and existing profiles, retaining complete backups.
5. Follow [PluginHub plan](PLUGINHUB-PLAN.md): validate current upstream requirements and update the open descriptor PR #285 to the new tested source pin. No further product or packaging decisions are required for this Legacy scope.

See [test checklist](../TESTING-TODO.md), [historical notes](history/RELEASE-HISTORY.md), [documentation index](INDEX.md).
