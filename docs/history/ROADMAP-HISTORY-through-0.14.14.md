# Marigold roadmap

Stable: **0.14.8**. Development: **0.14.9**. Updated October 24, 2026.

The main feature set is implemented and has extensive user-reported in-game passes. Two reported issues and final release validation remain. See the [active testing checklist](../../TESTING-TODO.md) and [detailed test history](TESTING-HISTORY-through-0.14.8.md).

## Implemented

- Cockpit-relative ship schematic with rotation, zoom, filters, configurable colors and independent visibility/size.
- Persistent captured or XML-blueprint baselines, saved ships, named baseline versions and conservative reconnect/Nexus matching.
- Recovery alignment with translation and quarter-turn rotation, an independent draggable/zoomable preview, and safe accept/cancel behavior.
- Whole-ship damage outlines and primary-state fills independent of marker pages. Dense same-state regions merge instead of disappearing.
- Optional Overall, Armor, Power, Thrusters, Utility, Cargo, Offensive and Tools health bars. Power can separate Reactors, Batteries and Solar; unselected sources stay in Other.
- Synchronized four-second fresh alerts for orange/red damage, with a separate settings toggle. Yellow stays steady with fainter lines and fill; ordinary missing-block pulse remains independently configurable.
- HUD layouts, defaults/per-ship saves, presets, clipboard sharing, shortcuts and English/Spanish controls with some English fallback.
- Optional connector-docked context and matched damage outlines. Station mode defaults off; mechanical subgrids are excluded.

## Next development work

1. **Recovery controls — passed on tested setup.** Rectangular styling resolves the reported overlap/click problem per October 25 user testing. Additional UI-scale coverage is optional.
2. **Blueprint baseline repair behavior.** Investigate the reported failure to recognize repairs after import. Verify the expected baseline survives repairs, reloads and recovery without recapture.

## Before public release

- Complete the active performance, fresh-install and final source-loader checks.
- Review current PluginHub contribution requirements.
- Finalize release documentation, package and pinned source manifest. Publishing or submitting to PluginHub is separate from updating master.

## Known limits and deferred work

- Protobuf-only and archived blueprints are unsupported; use XML bp.sbc. See [blueprint formats](../BLUEPRINT-FORMATS.md).
- Schematics use block bounds, not meshes. Dense merged highlights can include gaps; this is not unlimited per-block detail.
- Fresh alerts apply to the main ship, not connector-docked context. Merged regions pulse together.
- Health bars count baseline blocks equally, not hit points, output capacity or combat survivability.
- Automatic matching requires sufficient unique evidence. Heavy changes, symmetric remnants or identical ships may need manual recovery. One recovery alignment is remembered per profile.
- Version libraries hold 16 entries and 250,000 blocks total. Replacing a version needs a free slot to preserve its previous contents.
- Profile format 3 cannot be read by 0.12.6. Rollback requires both the older plugin and a pre-upgrade data backup.
- Drag positioning, freehand pan, more custom bar groups and full diagnostic translation are optional future improvements.
- Mechanical subgrids, textured meshes, automatic Workshop downloads, repair automation and server components remain outside scope.

Targets: **Windows Legacy / .NET Framework 4.8 and Interim / .NET 10**. Interim runtime acceptance remains pending.

## 0.14.9 development — launcher compatibility

Separate Windows Legacy/net48 and Interim/net10.0 packages now build from the same source. Packaged manifests restrict the runtime; source remains shared. Both compilation paths and regressions are checked, but Interim loading, Harmony patches, UI, clipboard and shared profile behavior require in-game validation. The reported 0x80131515 log identifies Legacy and indicates downloaded-file blocking, not an Interim incompatibility. Master remains on 0.14.8 while this work is tested.

## Submission readiness audit

See [repository audit](REPOSITORY-AUDIT.md) for inventory, licensing/repository cleanup, dual-runtime release tooling and prioritized optimization candidates. These gates precede a PluginHub submission.

## Release milestones

1. **Both launchers:** finish actual Legacy and Interim source/binary loading tests, shared profiles, UI, patches and unload. Build/source checks now pass per target; source wrapper and XML reference conflict are fixed.
2. **Recovery and import:** verify rectangular recovery-button sizing across screen/UI scales; investigate blueprint repair behavior.
3. **Measured performance:** follow [repeatable test protocol](../PERFORMANCE-TEST.md), capture three paired trials per scenario and optimize repeatable costs.
4. **PluginHub submission:** choose license, finish fresh-install tests and clean packaging, update Legacy-only pin generator for validated runtimes, commit the tested source pin, check current rules, then submit Taeleus/Marigold to StarCpt/PluginHub once ready.

October 25: recovery controls passed; initial small-grid and ~15k-grid performance observations are recorded in [performance results](../PERFORMANCE-RESULTS.md). Repeated heavy-damage/dock tests and Interim acceptance remain.

**Active-damage performance priority:** October 25 large-grid HUD averages 4.30–4.75 ms with observed peaks up to 7.80 ms and ~10k lines. Profile and reduce fault rendering/result-adoption costs while preserving every damage region; compare active versus stopped damage and bars-only. [Evidence](../PERFORMANCE-RESULTS.md).

## 0.14.10 schematic performance

High is the new default (saved preferences retained). Heavy-damage testing showed ~85 FPS with the ship display versus ~125 hidden. First optimization removes redundant primary fault overview geometry, skips identical same-state edges, computes projection axes once per frame and shares pulse-phase calculations. Fault fills remain fully covered. Build/regression and source checks precede in-game comparison; no FPS improvement is claimed until retested.

Auto-reassociation follow-up: damaged station required manual recovery despite correct first candidate. Investigate score/ambiguity/stability or existing-live-binding rejection; accepting recovery records a remnant signature for subsequent entries. Capture `/mg auto status` before altering binding on reproduction. Do not assume matching failure has been fixed by the successful manual alignment.

## 0.14.11 matching and configurable rendering

Automatic score/size minimum is 75%, retaining the 8-point ambiguity margin, minimum evidence and stable observations. HUD performance modes allow skipping fills and/or healthy hull context while keeping all filtered faults. Save/preset/clipboard support included. Await in-game comparison.

## 0.14.12 corrections

Auto-match uses the occupied cockpit when available and ignores health-only changes/order during structural stability checks. 75% minimum, ambiguity and existing-live-binding guards remain. Performance and both pulse controls moved to Customize > Preferences. Outline modes now build merged fault contours with no shaded marker geometry; stats reports submitted fills for verification. Await in-game retest; 84% alone does not prove which old rejection gate fired.

October 25 rendering-mode results: Full 85/142 FPS, Outlines 133/142, Faults with fills 90/142, Fault outlines 139/142 (shown/hidden). Prioritize reducing translucent fill cost; outline modes now provide user-tested low-cost options. Profile-number reuse passed.

## 0.14.14 fill optimization

Screen-space same-state fills are conservatively unioned before sprite submission using reusable row bitmasks. Coverage is retained independently for yellow/orange/red, with separate active pulse treatment. Adaptive raster resolution keeps the submission bound without dropping regions. Outlines and health counts remain unchanged. Overlapping fills shade once; outward cell rounding can fill tiny gaps. In-game FPS comparison pending.

**Performance stopping point (October 25):** 0.14.14 fill union produced no noticeable FPS improvement per user. Keep current visuals and coverage; defer further rendering optimization unless a measured substantial gain is available without changing appearance/behavior. Do not treat synthetic fill reduction as a proven FPS gain. Existing outline modes remain the practical performance choice.
