# Current acceptance — October 25, 2026

Legacy release candidate: **0.14.14**.

- [x] Blueprint baseline repairs: each fixed block returns to healthy (user confirmed).
- [x] Automatic matching: user reports working after the occupied-seat/stability corrections.
- [x] Final regression checks: user reports all working, including rendering modes/pulses and profile number reuse.
- [x] Performance comparison completed; retain outline alternatives and defer marginal optimization.
- [ ] Final committed source pin loaded through Pulsar Legacy with local binary disabled.
- [ ] Fresh-install test of final release package without overwriting existing profiles.
- [ ] PluginHub descriptor validation and PR submission after readiness.
- [ ] Interim runtime testing — deferred, outside initial Legacy release.

This acceptance summary supersedes overlapping unchecked historical entries below. Original test detail is preserved for reference.

---

# Marigold testing checklist

Current build: **0.14.8**, deployed October 24, 2026.

Your detailed results, comments and original checkboxes are preserved in the [testing history](TESTING-HISTORY-through-0.14.8.md). Later results supersede older overlapping failures and expectations; untested cases are not automatically passed.

## Reported issues to investigate

- [x] **Recovery preview controls:** user confirms 0.14.9 rectangular buttons are much better and work correctly (October 25). Other resolutions/UI scales remain additional coverage.
- [ ] **Blueprint baseline repairs:** import and accept a blueprint, damage/remove blocks, then weld/replace them. Check repaired blocks become healthy without recapturing; repeat after reload and recovery. Record blueprint source and exact steps if it fails.

## Release checks still open

- [ ] Record performance on approximately 1k/10k/50k-block ships, including sustained heavy damage and station mode on/off at the dock context limit. Include worker/HUD timings and visible coverage.
- [ ] Fresh-install smoke test using the final package. Existing-profile migration has earlier passing results.
- [ ] Load the final pinned source through Pulsar Legacy with the local binary disabled. Check dependencies, UI and unload; source compilation alone is not runtime acceptance.
- [ ] Recheck current PluginHub rules. Prepare the final release package and source-pinned manifest. Publication is a separate step.

## Latest user-reported passes

- [x] Recovery preview layout, drag inspection, wheel zoom, cancellation cleanup and saved HUD isolation (0.14.1–0.14.3). Later button-alignment report above remains open.
- [x] Damage outlines and primary fills remain visible through sustained combat and across marker pages (0.14.4–0.14.5).
- [x] Power bar sections, all checkbox combinations, damage attribution, layout and persistence (0.14.6).
- [x] Fresh-alert toggle, timing, shared rhythm, loading/preview behavior and persistence (0.14.7).
- [x] Yellow stays steady and faint; orange/red remain prominent with synchronized fresh alerts (0.14.8). This replaces the earlier yellow-pulse expectation.

Automated build, regression and source-compilation checks passed for 0.14.8. They do not replace the release checks above. Consult the history for older edge-case evidence before closing an item.

## 0.14.9 development — dual-runtime packages

- [ ] Back up profiles; test the Legacy package on Legacy and Interim package on Interim. Confirm the log identifies the intended runtime and Marigold initializes without dependency or patch errors.
- [ ] On each runtime, test /mg ui, capture, damage rendering, Power sections, pulses, clipboard configuration, blueprint import, recovery, reconnect and unload. Shared profile data must survive switching runtimes.
- [ ] Confirm packaged runtime restrictions prevent loading a package on the wrong runtime. Test a trusted downloaded ZIP after unblocking and extraction; distinguish download blocking from runtime failures.

Both builds compile; actual Interim game loading remains pending. No new binaries have been deployed by this work.

## Cleanup / recovery controls

- [ ] Test recovery hover/click locations and label spacing at 1080p, 1440p and your usual UI scale. Narrow controls now use rectangular styling instead of default button texture sizing. Check X/Y/Z, quarter-turns, candidate, views, zoom, accept/cancel and Back/Close.
- [ ] Run the [performance protocol](../PERFORMANCE-TEST.md), preserving saves/presets and three paired trial results. Compare Legacy and Interim separately.
- [ ] Test both packages and actual source loading; compilation is not runtime acceptance. Verify documentation matches the install steps.

- [ ] 0.14.9 launcher listing regression: restart Legacy and confirm Marigold appears in Add plugin and loads; repeat with Interim package. Pulsar 2.4.3 runtime tags are CLR/CoreCLR (corrected from framework names).

## October 25 results

- [x] Legacy 0.14.9 appears/loads after runtime XML correction. Recovery controls user-verified.
- [x] Initial 769-block full HUD / bars-only / disabled comparison: all reported 165 FPS. Approximately 15k-grid stats also captured. [Measurements and limits](../PERFORMANCE-RESULTS.md).
- [ ] Complete repeated large-grid, sustained-damage and dock trials; record hardware, settings and FPS cap. These initial observations do not close all performance coverage.

- [x] Captured large-grid active heavy-damage timings: ~10k lines, HUD mean 4.30–4.75 ms, peak 7.80 ms. This exposes an optimization target; it is not a performance acceptance pass.
- [ ] Repeat the damaged grid after fire stops, then bars-only with the same damage. Record FPS/stutters and stats for each; recheck after optimization without losing damage coverage.

## 0.14.10 — first schematic performance reduction

- [ ] Repeat the same damaged large-grid show/hide test (previously ~85/~125 FPS). Compare FPS and /mg stats mean/max, worker and geometry/rendered line counts. Hiding bars alone previously had little effect. Record identical view/size and damage state.
- [ ] Verify all yellow/orange/red fills and outlines still cover missing/damaged blocks across pages and filters. Shared same-state edges now draw once, and redundant primary overview outlines are removed. Check merged bounds and pulse expiry/repair.
- [ ] Verify top/bottom/left/right, reset, spinning, fine rotation and recovery inspection orientation remain identical after linear projection optimization.
- [ ] Fresh/default-reset display uses High detail; previously saved explicit detail preferences still load. No automatic overwrite of saved layouts.

- [ ] Auto-reassociation follow-up (October 25): user entered damaged station and no automatic binding occurred; first manual recovery candidate was correct and accepted successfully. Cause not yet isolated. On re-entry after leaving the seat/reconnect, run `/mg auto status` before manual recovery and record the message. Confirm whether the newly remembered recovery now associates automatically.

## 0.14.11 — matching and performance modes

- [ ] Automatic matching accepts a unique stable 75–89% overlap; under 75%, identical alternatives and close ties must still wait. Check accepted profile and original baseline denominator after heavy losses. Automatic matching is layout evidence, not identity proof.
- [ ] Customize > HUD layout > Performance options: compare Full, Outlines only, Faults with fills, Fault outlines only on the same damaged save. Record FPS and /mg stats. Health bars/report counts must stay identical; no filtered fault should disappear.
- [ ] Save defaults/ship/preset and clipboard round trip for each rendering mode. Old configs load Full. Recovery preview must retain full rendering regardless of the HUD performance choice.

## 0.14.12 — auto matching and performance corrections

- [ ] Enter a unique ~84% station match from the intended seat: auto matching now uses the occupied cockpit instead of rejecting due to alternate seats. Damage-only health changes no longer prevent structural stability; actual block/orientation changes still reset it. Record `/mg auto status` if it still waits (ambiguity or another live binding can still reject).
- [ ] Customize > Preferences > Performance options contains rendering mode and Missing/Fresh pulse toggles. Save/restore and restart; verify both pulses independently. Colors links there; HUD layout no longer owns these options.
- [ ] Outlines only and Fault outlines only must report **0 fills** in `/mg stats` and display merged contours without translucent marker faces. Faults with fills removes healthy hull context but retains fills; Full restores both. Inspect red/yellow/orange coverage after switching while a rebuild is pending.
- [ ] Repeat the same damage-state performance comparison for all modes. Recovery inspection retains full rendering. Check original health bars and denominator do not change.

## 0.14.13 — reusable profile numbers

- [ ] Delete all saved profiles, restart and capture: new profile should be 1. With profiles 1/2/3, delete 2 and capture another: it should reuse 2, retaining 1/3 and their data. Old high numbers are preserved for existing profiles. Reusing a number must not reuse deleted profile identity/layout/version data.

## October 25 — 0.14.13 results

- [x] User confirms new baselines reuse free profile numbers.
- [x] All four performance modes tested: Full 85/142 FPS shown/hidden; Outlines 133/142; Faults with fills 90/142; Fault outlines 139/142. [Recorded results](../PERFORMANCE-RESULTS.md). Rendering-mode cost changes are now user-verified; persistence, detailed fault coverage and auto-match checks remain as separately listed.

## 0.14.14 — merged translucent fills

- [ ] Repeat Full and Faults with fills on the same heavily damaged grid; record FPS and `/mg stats` (submitted fills now shown alongside pre-merge count). Compare with 85/142 Full and 90/142 Faults with fills from 0.14.13.
- [ ] Verify all yellow/orange/red regions remain filled across pages, views, zoom, filters and active damage. Fill union is separate per state, so no category can exhaust another's coverage.
- [ ] Same-state overlapping fills now shade once and may look less dark. Fill edges conservatively snap outward to a fine screen grid; tiny gaps can merge. Outlines are unchanged. Check small isolated blocks remain visible and panel clipping holds.
- [ ] Verify fresh pulses and steady regions remain distinct and expire normally. Outline-only modes must still show zero fill submissions. Health counts unchanged.

- [x] 0.14.14 performance comparison completed: user reports about the same FPS. No significant improvement claimed. Further fine-tuning deferred at user preference unless a substantial appearance-preserving improvement is identified. Detailed coverage/pulse checks remain separate from this performance result.
