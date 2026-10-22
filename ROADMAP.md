# Marigold roadmap

## Current position — 0.7.0

The single-grid engineering HUD now includes persistent cockpit-anchored profiles, capture and blueprint baselines, damage comparison, system/state filters, marker pagination, display modes, a larger display layout, saved preferences and manual profile suggestions. Source is published at https://github.com/Taeleus/Marigold on master.

The user confirmed earlier orientation and missing-marker fixes. Versions 0.5–0.7 still need in-game testing. Development proceeds independently of that backlog: append checks to [TESTING-TODO.md](TESTING-TODO.md) without overwriting user notes. Automated checks do not complete in-game checkboxes.

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
| Profile reassociation | Automatic conservative layout matching, manual selection and suggestions implemented 0.7 | Actual Nexus/login tests; identical unsaved copies remain structurally indistinguishable |
| Connector station mode | Implemented 0.7; OFF by default | Dock/undock, mixed grid size, chain/cycle and performance tests |
| Mechanical subgrids | Excluded at user request | Rotor/hinge/piston baseline support is outside current scope |
| Engineering UI | Chat workflows and compact/expanded panels implemented | Graphical settings/profile/blueprint picker and localized text |
| Release readiness | Local package builds and automated checks available | In-game performance/compatibility, PluginHub source compilation and pinned manifest/rules review |

## Next development slices

1. Validate connector station context and automatic reassociation using the queued tests. Keep manual control and never change saved baselines during matching. Mechanical subgrids are out of scope.
2. Incremental observations: reduce full-grid sampling cost, bound queued work and retain periodic reconciliation for damage events not exposed to clients. Measure in the actual game before promising frame-time limits.
3. Graphical settings/profile picker: reuse existing commands and explicit blueprint preview/accept workflow. Retain readable chat help and compact ship-name-only caption.
4. Release packaging: confirm PluginHub rules and source compile against the target loader, prepare a pinned source manifest, and run the accumulated in-game checklist before calling the plugin release-ready.

Automatic layout matching now attempts reassociation across reconnects and transfers, but live Nexus compatibility is unverified. It requires unique, high-overlap, stable observations and preserves manual overrides. It cannot establish a unique persistent identity for an unsaved identical copy. Mechanical subgrids remain excluded.
