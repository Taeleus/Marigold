# Marigold roadmap

## Current position — 0.6.0

The single-grid engineering HUD now includes persistent cockpit-anchored profiles, capture and blueprint baselines, damage comparison, system/state filters, marker pagination, display modes, a larger display layout, saved preferences and manual profile suggestions. Source is published at https://github.com/Taeleus/Marigold on master.

The user confirmed earlier orientation and missing-marker fixes. Versions 0.5 and 0.6 still need in-game testing. Development proceeds independently of that backlog: append checks to [TESTING-TODO.md](TESTING-TODO.md) without overwriting user notes. Automated checks do not complete in-game checkboxes.

## Scope and guardrails

Client-only Space Engineers 1 HUD for Pulsar Legacy / .NET Framework 4.8. No world edits or custom network messages. Game installation and SpaceEngineers roaming folders are read-only. Profile/settings data goes in `%APPDATA%/Marigold`. Red means absent from the selected grid relative to its baseline, including detached blocks; gray means unreadable integrity data.

## Milestones

| Milestone | Implementation | Remaining |
| --- | --- | --- |
| Controlled-grid observer | Implemented; switching/live names previously confirmed | Remote control, multiplayer and unload regressions |
| Outline and orientation | Implemented; default/reset orientation previously confirmed | Broader displays, ship sizes and live profiling |
| Damage comparison | Implemented for selected grid; colors and missing behavior previously confirmed | Functional thresholds, repairs, replacements and multiplayer freshness |
| Persistent profiles and anchors | Implemented 0.5 | User save/reload, cockpit switching and transfer tests |
| Blueprint baseline import | Implemented 0.6 | User tests of multi-cell anchors, rotated/modded blocks and preview acceptance |
| System filters and display modes | Implemented 0.6 | User visual/filter/page tests; unusual mod types may classify as other |
| Observation optimization | Unchanged geometry reused in 0.6 | Large-grid game-thread sampling benchmarks; true incremental dirty-block sampling |
| Profile reassociation | Manual selection and layout suggestions implemented | Nexus testing; automatic matching must not confuse identical ships |
| Mechanical constructs | Not implemented | Stable component identity, per-grid baselines, moving transforms, detachment, connector exclusion |
| Engineering UI | Chat workflows and compact/expanded panels implemented | Graphical settings/profile/blueprint picker and localized text |
| Release readiness | Local package builds and automated checks available | In-game performance/compatibility, PluginHub source compilation and pinned manifest/rules review |

## Next development slices

1. Mechanical constructs: represent component grids and their attachment topology separately from display coordinates. Preserve each component's block coordinates while rotor/hinge/piston transforms move. Compare against persistent component baselines, distinguish detachment from motion, and exclude connector-only docked ships. Add tests before exposing construct capture; the existing single-grid format must remain readable.
2. Incremental observations: reduce full-grid sampling cost, bound queued work and retain periodic reconciliation for damage events not exposed to clients. Measure in the actual game before promising frame-time limits.
3. Graphical settings/profile picker: reuse existing commands and explicit blueprint preview/accept workflow. Retain readable chat help and compact ship-name-only caption.
4. Release packaging: confirm PluginHub rules and source compile against the target loader, prepare a pinned source manifest, and run the accumulated in-game checklist before calling the plugin release-ready.

No automated Nexus matching or mechanical construct compatibility is claimed. The explicit profile workflow is available across worlds without relying on runtime entity IDs or grid names.
