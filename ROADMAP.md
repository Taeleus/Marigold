# Marigold roadmap

Updated October 23, 2026. Stable `master`: **0.12.6**. Development `dev`: **0.14.0**.

0.12.6 has user-reported runtime passes. 0.13.0 is installed but not yet tested. 0.14.0 is built for testing; compilation is not in-game acceptance. Original feedback and results remain in [TESTING-TODO.md](TESTING-TODO.md).

## Implemented scope

| Area | Current behavior |
| --- | --- |
| Ship display | Cockpit-relative views, corrected default orientation, rotation/zoom, independent show/hide/size, damage colors/fills, missing-region outlines and marker pages |
| Health bars | Eight optional, independently positioned/configured bars: Overall, Armor, Power, Thrusters, Utility, Cargo, Offensive and Tools |
| Customization | State colors/pulse, opacity, compact/expanded layouts, multi-system filters, hull styles, shortcuts, defaults/per-ship saves/presets and clipboard config sharing |
| Profiles | Persistent numbered profiles, capture/clear/load/detach/delete, main-cockpit selection and conservative client-side reconnect/Nexus matching |
| Recovery | Surviving-block alignment previews, translation and manual 90-degree turns about live-grid axes, accept/cancel and a manual starting point if no candidate is found |
| Persistent recovery | Latest accepted alignment remembered per profile; reconnect matching supports partial repairs and changed cockpit type when sufficient structure still matches |
| Baseline versions | Automatic preservation before capture/import/anchor changes/restores, named versions, rename, use/delete, explicit replace with prior-copy preservation, source/date/blueprint metadata and cross-anchor previews |
| Blueprints | Search any combination of local, locally cached Workshop and Cloud XML; select grid/cockpit, inspect omitted definitions, preview/accept/cancel |
| Station mode | Off by default; connector-linked grids show independent matched references and reports, respecting system visibility and hull brightness/styles |
| Reports | Expected/observed/matching counts, damage states, structural completeness distinct from health, plus rendering/performance statistics |
| Languages | English and Spanish controls, page titles and menu descriptions; unlocalized dynamic diagnostics fall back to English |

These features cover the agreed first-release development scope. Runtime validation and fixes remain before release readiness.

## 0.14.0 closes the remaining development items

- Recovery matching now requires at least 90% of the remembered structure to remain and at least 90% of observed blocks to match the full baseline for a repair-based match. Both exact-match counts must be at least eight. Cockpit definition equality is no longer required for an explicitly remembered recovery. Competing profiles/alignments still require a clear lead and stable observations.
- Alignment tools can turn the baseline by 90 degrees around X/Y/Z at its origin. Block grids require axis-aligned rotations, not arbitrary angles. Translation is separate. No automatic candidate means an explicitly unverified starting point, not automatic acceptance; manual acceptance still requires three exact matches.
- Saved versions offers **Save new version**, **Rename version** and **Replace version**. Replace copies the active baseline into the selected slot, preserving its previous contents in another slot. A full library refuses changes rather than deleting history.
- Docked hulls use the same normal/faint/highlighted styles as the main ship. Their combined hull budget remains 768 lines per docked grid; damage outlines retain their existing separate budgets. Coarse outer outlines are clamped to actual block-envelope extents.
- Protobuf/archive support is explicitly deferred. The inspected game convenience loaders can write PB caches or use Workshop state; this release keeps its bounded, read-only XML reader. Direct PB deserialization exists but is not integrated or validated here. See [blueprint format notes](docs/BLUEPRINT-FORMATS.md).
- Roadmap/help and Spanish labels now reflect the current UI rather than historical page names.

## Remaining release work

1. **0.13/0.14 in-game acceptance.** Profile upgrades and backups; partial/full repairs; cockpit-type changes; manual alignment; revision rename/replace/restore; Spanish text fit; station styles and outlines. Fix findings without marking unchecked tests passed.
2. **Multiplayer edge cases.** Ambiguous/near-identical ships, symmetric remnants, controller loss, grid splits, delayed observations, reconnect/Nexus transfer, auto-off/manual detach and changing grids during pending work.
3. **Blueprint edge cases.** Multiple grids/cockpits, rotated multi-cell blocks, missing/changed mods, cloud failures and cancel/switch during preview. Confirm XML-only guidance is useful for unsupported files.
4. **Performance and cleanup.** Record live costs on approximately 1k/10k/50k-block ships and large connector docks. Check long sessions, repeated destruction/reconnect, observer subscriptions and worker cleanup. Add performance controls only if measurements justify them.
5. **Installation and loader acceptance.** Fresh install and existing-data upgrade; actually load the pinned source through Pulsar Legacy with the binary disabled. Source compilation alone does not prove loader behavior.
6. **Distribution.** Final docs/known limitations, versioned release package without game DLLs or duplicate Harmony, current PluginHub rules review and pinned manifest. Hub submission is a separate distribution step.

## Limits and optional follow-up

- One remembered recovery per profile; significant further losses, different seat placement/orientation or ambiguous structures can still require manual recovery. Layout similarity is not identity proof.
- Profile format 3 is incompatible with 0.12.6. Restore a pre-upgrade copy of the complete Marigold data folder when rolling back.
- Schematic block envelopes are not meshes. Slopes are box-like and coarse detail may merge small gaps. High detail helps but is still bounded.
- Baseline libraries allow 16 entries and 250,000 total blocks. Replacing a version needs one free slot to preserve its previous contents.
- Drag positioning, freehand pan, custom health-bar groups and full diagnostic translation are optional.
- Mechanical rotor/hinge/piston subgrids, textured meshes, automatic Workshop downloads, advanced engineering simulation, repair automation and server components remain outside this release.
- Target: Pulsar Legacy / .NET Framework 4.8. The .NET 10 regression executable does not establish support for a .NET 10 plugin runtime.
