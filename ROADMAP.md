# Marigold roadmap

## Current position — 0.8.0

The single-grid engineering HUD now includes persistent cockpit-anchored profiles, capture and blueprint baselines, damage comparison, system/state filters, marker pagination, display modes, a larger display layout, saved preferences and manual profile suggestions. Source is published at https://github.com/Taeleus/Marigold on master.

The user confirmed earlier orientation and missing-marker fixes. Versions 0.5–0.8 still need in-game testing. Development proceeds independently of that backlog: append checks to [TESTING-TODO.md](TESTING-TODO.md) without overwriting user notes. Automated checks do not complete in-game checkboxes.

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
| Observation optimization | Event-priority sampling, bounded per-update sweeps and unchanged geometry reuse implemented | Live large-grid benchmarks; membership enumeration/copies still have costs |
| Profile reassociation | Automatic conservative layout matching, manual selection and suggestions implemented 0.7 | Actual Nexus/login tests; identical unsaved copies remain structurally indistinguishable |
| Connector station mode | Implemented 0.7; OFF by default | Dock/undock, mixed grid size, chain/cycle and performance tests |
| Mechanical subgrids | Excluded at user request | Rotor/hinge/piston baseline support is outside current scope |
| Engineering UI | Native settings/profile/blueprint/view panel and English/Spanish labels implemented 0.8 | Runtime layout/input checks; diagnostic text remains English |
| Release readiness | Binary and source-only compilation checks, pin-generation script and rules review prepared | Actual Pulsar source loading, in-game acceptance and hub submission |

## Remaining release gates

All currently scoped implementation slices are present. Complete the queued in-game checks and fix any failures before release: UI layout/input, large-grid and dock performance, persistence/blueprints, multiplayer and Nexus reassociation, and actual Pulsar source loading. Use `verify-source.ps1` and `prepare-release.ps1` for repeatable preparation; see docs/RELEASE.md.

Mechanical subgrids remain excluded. Additional languages and translated diagnostics are optional future polish. Automatic matching is still a layout heuristic and cannot distinguish an unsaved identical copy from the original. Runtime validation has deliberately been deferred to the user's editable testing checklist.
