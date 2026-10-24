# Marigold roadmap

Current build: **0.14.8**. Updated October 24, 2026.

The main feature set is implemented and has extensive user-reported in-game passes. Two reported issues and final release validation remain. See the [active testing checklist](TESTING-TODO.md) and [detailed test history](docs/TESTING-HISTORY-through-0.14.8.md).

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

1. **Recovery control hitboxes.** Reproduce and fix the reported cursor offset/overlapping buttons across UI scales and aspect ratios. Keep camera inspection separate from baseline adjustments.
2. **Blueprint baseline repair behavior.** Investigate the reported failure to recognize repairs after import. Verify the expected baseline survives repairs, reloads and recovery without recapture.

## Before public release

- Complete the active performance, fresh-install and final source-loader checks.
- Review current PluginHub rules and connector-linked grid visibility/access behavior.
- Finalize release documentation, package and pinned source manifest. Publishing or submitting to PluginHub is separate from updating master.

## Known limits and deferred work

- Protobuf-only and archived blueprints are unsupported; use XML bp.sbc. See [blueprint formats](docs/BLUEPRINT-FORMATS.md).
- Schematics use block bounds, not meshes. Dense merged highlights can include gaps; this is not unlimited per-block detail.
- Fresh alerts apply to the main ship, not connector-docked context. Merged regions pulse together.
- Health bars count baseline blocks equally, not hit points, output capacity or combat survivability.
- Automatic matching requires sufficient unique evidence. Heavy changes, symmetric remnants or identical ships may need manual recovery. One recovery alignment is remembered per profile.
- Version libraries hold 16 entries and 250,000 blocks total. Replacing a version needs a free slot to preserve its previous contents.
- Profile format 3 cannot be read by 0.12.6. Rollback requires both the older plugin and a pre-upgrade data backup.
- Drag positioning, freehand pan, more custom bar groups and full diagnostic translation are optional future improvements.
- Mechanical subgrids, textured meshes, automatic Workshop downloads, repair automation and server components remain outside scope.

Target: **Pulsar Legacy / .NET Framework 4.8**. Automated checks use .NET 10; the plugin does not.
