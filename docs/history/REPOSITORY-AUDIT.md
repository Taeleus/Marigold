# Repository audit — October 24, 2026

Scope: current working tree on `dev-launcher-compatibility`, based on master `f0635cd`, including uncommitted 0.14.9 compatibility changes. This is an inventory and source review, not a PluginHub approval or complete in-game validation. No production fixes, deployment, commits or pushes were performed by this audit.

## Readiness

Not ready for submission yet. The intended destination is normally **StarCpt/PluginHub**, which registers Marigold for Pulsar; it does not require merging Marigold's source into the Pulsar launcher repository. Confirm the current hub contribution procedure before preparing a PR.

### Blockers and necessary decisions

1. **License missing.** No tracked repository LICENSE was found. Choose an explicit open-source license before distribution/submission. Review copied skill licensing separately; do not silently assign a new license to third-party content.
3. **Release runtime mismatch.** `prepare-release.ps1` always appends `Runtimes=NETFramework`. This would exclude Interim even after the dual-runtime builds. Make runtime declaration an explicit release decision and avoid duplicate manifest elements. Validate required fields and the exact pinned source through Pulsar.
4. **Runtime validation is incomplete.** The net10 build currently references the installed game's Bin64 assemblies. Compilation does not establish compatibility with the actual Interim game's patched assemblies or Harmony host. Test actual Interim loading, rendering, UI, clipboard, reconnect and unload. Correct the System.Xml.Linq reference warning first.
5. **Two reported product issues remain open.** Recovery button/cursor alignment and repair recognition after blueprint baseline import. Neither cause is proven by this audit. Reproduce and fix or explicitly qualify before release. Blueprint conversion creates healthy expected samples and acceptance binds them; that alone does not prove the reported repair path works.

## Repository inventory

- 242 tracked files at audit time; **168 are copied `.agents/skills` files**.
- Production: 36 C# files, about 3,631 lines, plus the project file. Major modules: SchematicOverlay (593 lines), ControlPanel (449), UiText (227), SchematicControls (168), ProfileStore (164).
- Tests: lifecycle/API doubles, snapshot comparison, geometry/fill budgets, orientation, profiles/recovery, imports, classification, settings and health bars. Doubles are not actual loader/UI tests.
- Root: manifest, build/source-check/release scripts, README, roadmap/checklist, original proposal and research review.
- Docs: user commands, API/format notes, release notes, old milestone designs/checklists and detailed testing history.
- Local ignored folders: `.cache`, `artifacts`, `research`; artifacts contain builds, logs and rollback backups. **Keep backups.** No tracked DLL, EXE or PDB was found by the extension scan. No tracked CI workflow or LICENSE was found.

### Cleanup

- `.agents/` is ignored but still tracked. Ignore rules do not remove existing tracked files. Untrack the copied skills while preserving local copies. This removes unrelated example C# and large dependency locks from the submission repository. Manifest SourceDirectories already excludes them from production, but the repository remains cluttered.
- Move Original-proposal.txt and Research-and-plan-review.md into an explicitly historical docs area, or remove only after preserving useful decisions. Keep user test history.
- Consolidate historical milestone docs (CONTROLS-0.2.1, SCHEMATIC-0.2, OUTLINE-0.3, SNAPSHOTS-0.4, PROFILES-0.5, old IN-GAME-CHECKLIST) behind a history index. They are not current instructions.
- docs/RELEASE.md still ends with 0.14.1 readiness information and earlier untested claims. Replace its current release procedure with a concise accurate one; move chronological notes to development history.
- README starts with 0.14.8/Legacy-only language and appends 0.14.9 Interim development instructions. Unify the introduction and installation guidance once support is validated. ROADMAP similarly needs one clear current/stable distinction.
- New Power labels and pulse controls are English strings without matching Spanish entries. Add localization and check truncation, especially narrow Power sections.
- No need to delete artifacts/backups or rewrite Git history for this cleanup.

## Build and release tooling

- Legacy and Interim binary builds passed in the preceding compatibility work. Current regression suites passed again during this audit.
- `verify-source.ps1` produced a failed combined multi-target build (NU1900 warnings, no compiler errors reported). Explicit `-f net48` and `-f net10.0` source builds both passed. Reproduce/fix the wrapper or explicitly build/check each framework with separate results; do not advertise the combined script as passing.
- Interim source build emits MSB3243 for `System.Xml.Linq` because the Framework-era explicit reference conflicts with runtime references. Condition/remove that reference appropriately for net10 and retest.
- NU1900 means vulnerability data was unavailable; these runs establish no dependency vulnerability audit.
- Build writes into an existing package directory without verifying its full inventory. Old extra files could remain in a future ZIP. Package from a fresh staging directory and assert an allowlist; preserve rollback folders separately.
- `prepare-release.ps1` checks production/manifest dirtiness, but build/release scripts and dependency settings can also affect the result. Check the relevant whole working tree and ensure the selected pin contains all intended changes.
- Production source discovery in `verify-source.ps1` is top-level only. Future nested folders could be missed. Match Pulsar's source inclusion behavior explicitly and exclude generated bin/obj files.
- Runtime assembly version is duplicated between csproj and Plugin.cs and build only updates manifest display name. Add a consistency check so an edited project version cannot ship with stale source assembly metadata.
- No CI is present. A reproducible local check script is the immediate priority; public CI needs a lawful plan for obtaining game reference assemblies without committing them.

## Performance candidates (profile before changing behavior)

1. `DamagePulse.Start` scans every active alert for each fill region on the game thread when a worker result is accepted. Up to 3,072 regions and thousands of hits can create millions of containment checks. Associate region membership on the worker or use a spatial lookup; retain aggregate-region coverage and four-second timing tests.
2. `MarkerFill.Build` recomputes scan bands every draw, within a 4,096-band budget; projection also touches all geometry each frame. Measure on damaged large ships and while static before choosing caching or adaptive refresh. Never regain performance by dropping fault categories.
3. `HealthBarRenderer` allocates PowerSections and repeatedly constructs labels during drawing; `ObservationSchedule.Take` allocates a list per update. Cache immutable results/reuse buffers where measurements support it.
4. Snapshot acceptance logs two lines for every rebuild, which can happen repeatedly during combat. Gate detailed timings behind stats/debug or throttle routine logs; preserve failures.
5. Station sampling walks grid blocks/connectors, copies snapshots, collects anchors and profile arrays on the game thread. Bound traversal cost as well as drawn output. Existing limits: eight extra grids, 100k sampled context blocks.
6. Blueprint title search rescans and parses catalog files per search. Worker execution avoids direct UI blocking; cache metadata with file-change invalidation only if library measurements justify it.

## Source quality and safeguards

Positive evidence: net48 retained, implicit usings disabled, scoped HUD postfix patch, no detected PatchAll/reverse-patch calls, no detected updater/process launching/assembly-location loading in production. Assembly access in UI reads version only. XML imports prohibit DTD/entities and bound size/depth/block counts. Profile writes use temporary files and replacement backups; invalid primary/backup data disables writes. Observer Dispose removes subscriptions; recovery has cancellation/generation protections. These are useful foundations, not a complete security certification.

Maintainability: highly compressed multi-statement lines make UI, lifecycle and persistence code harder to review. Reformat high-risk paths first; split rendering, worker-result adoption and UI page construction by responsibility only with behavior-preserving checks. Avoid a wholesale rewrite before submission.

Known product limits remain deliberate: block-envelope shapes, XML-only imports, no mechanical subgrids, conservative identity matching, bounded baseline libraries and main-ship-only fresh pulses. Document them; they are not all unfinished features.

## Validation performed and limits

Audit logs: `artifacts/audit-checks.log`, `audit-source.log`, `audit-source-net48.log`, `audit-source-net10.log`. Full regression run passed; both explicit source target builds passed. No new in-game tests were claimed. Live wiki fetch failed during this audit, so current PluginHub rules were not freshly certified; recheck them directly before submission.

## Suggested order

1. License choice, untrack copied skills, current docs/release script and dual-target source-check cleanup.
2. The two reported functional issues.
3. Actual Interim/Legacy source-loader tests, performance measurements and targeted optimizations.
4. Fresh install, final clean package/pin, current rules review, then a PluginHub PR.

## Cleanup update

Local dev and master now untrack the copied skill files; local copies remain. The source check explicitly builds both frameworks, and System.Xml.Linq is referenced only for net48. Recovery buttons use a rectangular style to avoid default texture sizing limits; runtime hitbox verification remains pending. Current docs are indexed, historical records labeled, and a repeatable performance procedure added. Earlier observations describe the pre-cleanup tree.
