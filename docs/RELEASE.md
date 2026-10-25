# Legacy release procedure

Release candidate: **0.14.14**. Initial PluginHub submission targets **Legacy/CLR only**. Interim and dual-package publication are deferred.

1. Run `./build.ps1 -Runtime Legacy` and `./verify-source.ps1 -Runtime Legacy` against the tested code. Preserve warnings accurately; unavailable vulnerability data is not a clean dependency audit.
2. Confirm final regression acceptance in TESTING-TODO.md. Package only plugin.dll/plugin.pdb/plugin.xml; exclude game DLLs, caches, skill files and backups.
3. Commit/push dev, merge/push master, then run `./prepare-release.ps1 -Commit HEAD` to generate a full-SHA-pinned Legacy manifest. It must have exactly one runtime entry (`CLR`).
4. Load the exact source pin through Pulsar Legacy with the local binary disabled; test a fresh install and existing profiles, retaining complete backups.
5. Follow [PluginHub plan](PLUGINHUB-PLAN.md): validate current upstream requirements and submit the descriptor PR when ready. No further product or packaging decisions are required for this Legacy scope.

See [test checklist](../TESTING-TODO.md), [historical notes](history/RELEASE-HISTORY.md), [documentation index](INDEX.md).
