# Marigold roadmap

Updated October 25, 2026. Submitted candidate: **0.14.14**. Review fixes: **0.14.15 on dev**, targeting **Pulsar Legacy / .NET Framework 4.8**.

## Completed and user-verified

Ship schematic and health bars; Power sections; damage fills, outlines and pulse controls; profiles and baseline versions; XML blueprint imports and repairs; recovery controls; automatic matching with the 75% minimum; reusable profile numbers; performance options under Preferences. User reports final regression checks working.

Heavy-damage performance has been measured. Outline modes provide lower cost; further marginal fill optimization is deferred to preserve current appearance and behavior. [Results](docs/PERFORMANCE-RESULTS.md).

## PluginHub review follow-up — 0.14.15 on dev

- Use the game's selected user-data folder for every Marigold data file and the game's canonical local blueprint folder for Local search.
- Migrate the complete old data tree safely for the default game-data location only when the new destination is absent. Preserve originals and keep custom `-appdata` setups isolated.
- Remove routine per-rebuild log messages while retaining requested statistics and error logging.
- Remove the descriptor's redundant Harmony declaration; retain the local build dependency.
- Verify migration, custom data/blueprint paths and log behavior using the [review-fix checklist](TESTING-TODO.md).

Master and [PluginHub PR #285](https://github.com/StarCpt/PluginHub/pull/285) remain on 0.14.14 until the dev fixes are validated. Then commit the tested fixes, merge when requested, update the source pin and report the validation results. The final pinned-source load through Legacy and fresh-install smoke test remain distribution checks.

## Deferred / known limits

Interim builds compile but are untested in-game and excluded from initial publication. Dual-package release work is deferred. XML-only blueprints, block-envelope shapes, conservative matching and manual handling of ambiguous layouts remain documented limitations. Mechanical subgrids are outside scope. Further rendering optimization, drag positioning/freehand pan and complete diagnostic translation are optional future work.

Earlier roadmap and test evidence: [roadmap history](docs/history/ROADMAP-HISTORY-through-0.14.14.md), [test history](docs/history/TESTING-HISTORY-through-0.14.8.md).
