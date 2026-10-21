# Marigold roadmap

## Scope and guardrails

Client-only Space Engineers 1 engineering HUD, beginning on Pulsar Legacy / .NET Framework 4.8. Never change the world, send custom network messages, or infer destruction from unavailable data. The game installation and SpaceEngineers roaming directory remain read-only during development. Build outputs stay in this workspace; deployment and in-game validation are separate steps.

## Milestones and acceptance gates

| Milestone | Deliverable | Gate before moving on | Status |
| --- | --- | --- | --- |
| 0 — Evidence and setup | Installed skills, rules review, runtime identification, verified local API sources | Build against installed assemblies; record unverified behavior | Complete for milestone 1; renderer research remains in milestone 2 |
| 1 — Ship observer | Minimal plugin showing controlled main-grid name and slim-block count | Build succeeds; cockpit/remote transitions, edits, disconnect, reload and teardown pass in game | User confirmed loading, two ships and live rename; remaining in-game checks pending |
| 2 — Renderer proof | Independent schematic HUD with rotation, zoom and visibility toggle | No gameplay entities; clipping/input behavior verified; 1k/10k/50k-block timing recorded | 0.3 outline/detail implemented; synthetic checks pass; user confirmed 0.2.2 views and controls; 0.3 appearance/performance gate pending |
| 3 — Snapshot comparison | Immutable local reference; missing/added/replaced/damaged/unknown states | Pure comparison tests cover orientation, multi-cell blocks, incomplete observations and grid splits | 0.4 initial implementation; pure tests pass; in-game split/repair/multiplayer validation pending |
| 4 — Categories and observation quality | System filters, isolation, bounded incremental updates | Modded-definition fallback; multiplayer unknown state verified; no per-frame full scan | Planned |
| 5 — Blueprints and profiles | Safe local import, discrete alignment, versioned profiles | Alignment ambiguity requires confirmation; identical ships remain separate profiles; import bounds tested | Planned |
| 6 — Mechanical constructs | Independent grid coordinates and moving transforms | Rotor/hinge/piston motion, detachment and connector exclusion verified | Planned |
| 7 — Engineering interface and release | Expanded UI, settings, profiling and PluginHub packaging | Release source compilation, cleanup, multiplayer tests and rules review pass | Planned |

## First implementation slice

- One small plugin project; introduce an independent Core library when comparison behavior exists.
- Use documented game interfaces and verify their implementation in the installed version.
- Observe controller changes cheaply; read the verified constant-time BlocksCount property every 15 updates. Add block event tracking when geometry caching needs it.
- Show a standard game HUD notification initially. This verifies the observer, not the future 3D renderer.
- Clear the display and detach handlers on ship changes, closed grids, world changes and disposal.
- No Harmony, publicizer, custom networking, auto-updater, automatic deployment, or persistence in this slice.

## Validation ledger

Record build results in README.md and API evidence in docs/API-EVIDENCE.md. In-game validation stays explicitly pending until actually exercised. Each milestone is complete only when its acceptance gate passes; compiling alone does not establish runtime success.

## Deferred decisions

Independent 3D viewport/render-target feasibility, exact multiplayer integrity freshness, SDX2 behavior, compressed/cloud Workshop imports, automatic subgrid reassociation, and Interim support remain research tasks. Full block meshes are optional until the schematic renderer meets its performance budget.

