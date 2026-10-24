# Marigold testing checklist

Current build: **0.14.8**, deployed October 24, 2026.

Your detailed results, comments and original checkboxes are preserved in the [testing history](docs/TESTING-HISTORY-through-0.14.8.md). Later results supersede older overlapping failures and expectations; untested cases are not automatically passed.

## Reported issues to investigate

- [ ] **Recovery preview controls:** reproduce cursor/button misalignment or overlapping X/Y/Z, view and zoom controls. Record resolution and UI scale, then retest hitboxes after the fix. Earlier preview tests passed, but this later report remains open.
- [ ] **Blueprint baseline repairs:** import and accept a blueprint, damage/remove blocks, then weld/replace them. Check repaired blocks become healthy without recapturing; repeat after reload and recovery. Record blueprint source and exact steps if it fails.

## Release checks still open

- [ ] Record performance on approximately 1k/10k/50k-block ships, including sustained heavy damage and station mode on/off at the dock context limit. Include worker/HUD timings and visible coverage.
- [ ] Fresh-install smoke test using the final package. Existing-profile migration has earlier passing results.
- [ ] Load the final pinned source through Pulsar Legacy with the local binary disabled. Check dependencies, UI and unload; source compilation alone is not runtime acceptance.
- [ ] Recheck current PluginHub rules, including what station mode reveals about connector-linked grids. Prepare the final release package and source-pinned manifest. Publication is a separate step.

## Latest user-reported passes

- [x] Recovery preview layout, drag inspection, wheel zoom, cancellation cleanup and saved HUD isolation (0.14.1–0.14.3). Later button-alignment report above remains open.
- [x] Damage outlines and primary fills remain visible through sustained combat and across marker pages (0.14.4–0.14.5).
- [x] Power bar sections, all checkbox combinations, damage attribution, layout and persistence (0.14.6).
- [x] Fresh-alert toggle, timing, shared rhythm, loading/preview behavior and persistence (0.14.7).
- [x] Yellow stays steady and faint; orange/red remain prominent with synchronized fresh alerts (0.14.8). This replaces the earlier yellow-pulse expectation.

Automated build, regression and source-compilation checks passed for 0.14.8. They do not replace the release checks above. Consult the history for older edge-case evidence before closing an item.
