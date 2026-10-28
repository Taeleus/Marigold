# Marigold roadmap

Updated October 28, 2026. Current version: **0.15.1**, targeting **Pulsar Legacy / .NET Framework 4.8**. Usability testing is complete; the PluginHub source pin is managed separately.

## 0.15.1 usability follow-up — complete

- Visual color selection for schematic states and healthy health bars, with a hue strip, saturation/brightness square, comparison swatches and exact RGB entry.
- Custom-bar search controls no longer share the same space with the Find button.
- **Update tracked blocks** explains group membership changes separately from automatic damage/repair updates.

User testing passed color selection, cancel/save persistence and the custom-bar page. The earlier menu-label and Power sections check also passed. Results are recorded in the [completed checklist](TESTING-TODO.md).

## 0.15.0 usability improvements

- Adjustable faint outlines retain the rest of the ship when focusing on selected systems, respecting hidden styles and fault-only rendering.
- Shared orange/red brightness pulses make fresh faults more visible; yellow remains steady.
- Healthy health-bar color can be chosen independently or linked to the schematic outline.
- Labels clarify dropdowns/textboxes; Power sections have a separate page without status-area overlap.
- Custom health bars track a selected block or terminal group, with saved membership, explicit refresh and individual layout controls.
- Profiles and saved baseline versions retain custom bars; recovery and cockpit re-anchoring preserve their coordinates.

Automated checks cover settings compatibility, fault states, membership persistence, revisions and recovery. User testing passed system context, pulse feedback, healthy colors, menu layout, custom-bar creation/repair and persistence/recovery.

## Completed and user-verified

Ship schematic and health bars; Power sections; damage fills, outlines and pulse controls; profiles and baseline versions; XML blueprint imports and repairs; recovery controls; automatic matching with the 75% minimum; reusable profile numbers; performance options under Preferences. User reports final regression checks working.

Heavy-damage performance has been measured. Outline modes provide lower cost; further marginal fill optimization is deferred to preserve current appearance and behavior. [Results](docs/PERFORMANCE-RESULTS.md).

## PluginHub review follow-up — 0.14.16

- Use the game's selected user-data folder for every Marigold data file and the game's canonical local blueprint folder for Local search.
- Use only the selected game-data root. No migration, old-folder detection or fallback remains; other folders are ignored.
- Remove routine per-rebuild log messages while retaining requested statistics and error logging.
- Remove the descriptor's redundant Harmony declaration; retain the local build dependency.
- Verify ordinary launch, persistence, Local blueprint search and quiet logging using the short [review-fix checklist](TESTING-TODO.md).

Core review-fix tests are user-reported passed: startup, save/restart persistence and Local blueprints. Log inspection confirmed only startup and requested statistics messages, with no rebuild spam or Marigold errors. Automated build, regression and Legacy source checks passed. These review fixes are on master and [PluginHub PR #285](https://github.com/StarCpt/PluginHub/pull/285). Final pinned-source loading through Legacy remains a separate check; no additional in-game custom `-appdata` test is claimed.

## Deferred / known limits

Interim builds compile but are untested in-game and excluded from initial publication. Dual-package release work is deferred. XML-only blueprints, block-envelope shapes, conservative matching and manual handling of ambiguous layouts remain documented limitations. Mechanical subgrids are outside scope. Further rendering optimization, drag positioning/freehand pan and complete diagnostic translation are optional future work.

Earlier roadmap and test evidence: [roadmap history](docs/history/ROADMAP-HISTORY-through-0.14.14.md), [test history](docs/history/TESTING-HISTORY-through-0.14.8.md).
