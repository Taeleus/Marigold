# Marigold roadmap

Updated October 26, 2026. Release candidate: **0.14.16**, targeting **Pulsar Legacy / .NET Framework 4.8**.

## Completed and user-verified

Ship schematic and health bars; Power sections; damage fills, outlines and pulse controls; profiles and baseline versions; XML blueprint imports and repairs; recovery controls; automatic matching with the 75% minimum; reusable profile numbers; performance options under Preferences. User reports final regression checks working.

Heavy-damage performance has been measured. Outline modes provide lower cost; further marginal fill optimization is deferred to preserve current appearance and behavior. [Results](docs/PERFORMANCE-RESULTS.md).

## PluginHub review follow-up — 0.14.16

- Use the game's selected user-data folder for every Marigold data file and the game's canonical local blueprint folder for Local search.
- Use only the selected game-data root. No migration, old-folder detection or fallback remains; other folders are ignored.
- Remove routine per-rebuild log messages while retaining requested statistics and error logging.
- Remove the descriptor's redundant Harmony declaration; retain the local build dependency.
- Verify ordinary launch, persistence, Local blueprint search and quiet logging using the short [review-fix checklist](TESTING-TODO.md).

Core review-fix tests are user-reported passed: startup, save/restart persistence and Local blueprints. Log inspection confirmed only startup and requested statistics messages, with no rebuild spam or Marigold errors. Automated build, regression and Legacy source checks passed. The review fixes are being promoted to master and [PluginHub PR #285](https://github.com/StarCpt/PluginHub/pull/285). Final pinned-source loading through Legacy remains a separate check; no additional in-game custom `-appdata` test is claimed.

## Deferred / known limits

Interim builds compile but are untested in-game and excluded from initial publication. Dual-package release work is deferred. XML-only blueprints, block-envelope shapes, conservative matching and manual handling of ambiguous layouts remain documented limitations. Mechanical subgrids are outside scope. Further rendering optimization, drag positioning/freehand pan and complete diagnostic translation are optional future work.

Earlier roadmap and test evidence: [roadmap history](docs/history/ROADMAP-HISTORY-through-0.14.14.md), [test history](docs/history/TESTING-HISTORY-through-0.14.8.md).
