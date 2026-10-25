# Marigold roadmap

Updated October 25, 2026. Release candidate: **0.14.14**, targeting **Pulsar Legacy / .NET Framework 4.8**.

## Completed and user-verified

Ship schematic and health bars; Power sections; damage fills, outlines and pulse controls; profiles and baseline versions; XML blueprint imports and repairs; recovery controls; automatic matching with the 75% minimum; reusable profile numbers; performance options under Preferences. User reports final regression checks working.

Heavy-damage performance has been measured. Outline modes provide lower cost; further marginal fill optimization is deferred to preserve current appearance and behavior. [Results](docs/PERFORMANCE-RESULTS.md).

## Legacy release / PluginHub

1. Commit and push tested changes to dev, merge into master.
2. Generate the Legacy-only descriptor pinned to the final full commit SHA; retain the existing plugin ID and Taeleus author/repository details.
3. Test that exact pin through Pulsar Legacy with the local binary disabled, and smoke-test a fresh installation. Preserve existing data/backups.
4. Validate the descriptor against current StarCpt/PluginHub requirements and prepare a PR adding `Plugins/Marigold.xml`.
5. Submit when ready, address review findings, and keep the tested source pin stable during review.

No additional product decisions are needed to draft the PR. [Submission plan](docs/PLUGINHUB-PLAN.md), [active acceptance](TESTING-TODO.md).

## Deferred / known limits

Interim builds compile but are untested in-game and excluded from initial publication. Dual-package release work is deferred. XML-only blueprints, block-envelope shapes, conservative matching and manual handling of ambiguous layouts remain documented limitations. Mechanical subgrids are outside scope. Further rendering optimization, drag positioning/freehand pan and complete diagnostic translation are optional future work.

Earlier roadmap and test evidence: [roadmap history](docs/history/ROADMAP-HISTORY-through-0.14.14.md), [test history](docs/history/TESTING-HISTORY-through-0.14.8.md).
