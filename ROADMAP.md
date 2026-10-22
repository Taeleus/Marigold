# Marigold roadmap to feature complete

Updated October 22, 2026, after the latest 0.9 testing notes.

## 0.10 implementation update

Implemented; runtime checks are appended to TESTING-TODO.md:

| Area | Delivered | Still open |
| --- | --- | --- |
| Feedback | Punctuation/press-to-bind, middle-left preview, cockpit-free recovery candidates and cell adjustments | Recovery may need repeating after reconnect; arbitrary alignment rotation beyond generated candidates |
| HUD | Position, size, opacity, colors/pulse, manual rotation, extra shortcuts, named presets | Live layout/input tests; freehand pan |
| Systems | Multiple categories; hidden/wireframe/faint/highlighted hull styles | Station applies visibility but not category brightness; mod-type audit |
| Profiles | Per-profile display saves, rename, named baseline libraries, archive before revision load | Cross-anchor restore, source/time metadata and active-revision identity. Capture/import replace the active baseline unless saved as a named revision first |
| Blueprint/report | Cached Workshop XML, named omitted definitions, structural completeness | PB-only/archive support and complex import coverage |
| Validation | Recovery rotations/loss/ties/cancellation and settings migration checks | Live tests, scale-specific performance, source-loader acceptance and release |

This is progress toward feature complete, not a claim that every item is finished.

## What feature complete means

A player can enter a ship, see its damage against a persistent capture or blueprint, recover the reference after severe damage or a server transfer, and customize an unobtrusive engineering HUD through the UI. Routine use should not require remembering chat commands. The player controls baseline replacement and uncertain profile matches.

This roadmap combines the original project plan with the later decisions: comparison-only display, full-health captures, cockpit-relative views, numeric profile IDs, conservative automatic association, and connector-only station mode. Mechanical subgrids remain excluded. Implementation completion and release acceptance are separate milestones.

## Already implemented and reported working

The latest user checks confirm these workflows on the tested ships and servers; they are not a claim of compatibility with every server or blueprint.

- Controlled-grid switching, live ship names, cockpit-relative views, rotation, static isometric view, zoom and compact/expanded layouts.

- One-command capture that creates an anchor and assumes full health; persistent numeric profiles; load, clear, delete and detach.

- Damage colors, repairs, replacements, additions, missing-region outlines across marker pages, and baseline retention through tested grid splits.

- Reconnect/restart/Nexus reassociation, delayed-streaming retries, manual override and persistent auto-off.

- Single-category isolation, damage-state filters, reports and marker pagination.

- Per-profile station mode, off by default; connector traversal and docked-ship comparisons against matching profiles.

- Local/nested-folder and cloud XML blueprints, automatic preparation, preview/cancel/accept, creation without a prior capture, and explicit omission of unavailable mod blocks.

- Native control panel, small live preview, saved preferences and three configurable shortcuts. Punctuation keys still need work.

- Incremental observation, stale-result rejection, hidden/resumed updates and the performance checks marked passed by the user.

The original test log stays intact in [TESTING-TODO.md](TESTING-TODO.md). Later passes supersede earlier failed expectations where behavior changed. Old notes about separate anchor/capture commands, live/reference modes, missing-mod rejection or globally saved station mode are historical, not current requirements.

## 1. Finish the latest feedback fixes

| Work | Completion criteria |

| --- | --- |

| Punctuation shortcuts | Accept apostrophe and other supported punctuation keys, including modifiers. Display readable names, persist them, reject duplicates and ignore input in chat/menus. Prefer a press-to-bind control so users need not know internal key names. No default conflicts introduced. |

| Preview placement | Move the panel's miniature schematic to middle-left. Check its caption and bounds at normal resolutions/UI scales so it does not overlap controls or important HUD elements. |

| Recovery after the anchor cockpit is destroyed | Add an explicit **Force apply profile** action in Profiles and a chat equivalent. Select a saved profile from another seat and find a best-fit rotation/translation using surviving block definitions, positions and orientations. Do not require the old cockpit to survive or treat the new seat as the old anchor automatically. |

| Recovery review and fallback | Show a preview and useful match evidence: matched surviving blocks, competing candidates and uncertainty. Let the player accept/cancel or adjust alignment when the result is ambiguous. Insufficient evidence must produce an explanation instead of a silent guess. |

| Preserve the recovered baseline | Recovery changes the live binding, never recaptures or rewrites missing blocks. Retain alignment while moving seats; define and test recovery after reconnect. Keep ordinary automatic matching conservative. Bound expensive matching work and discard stale results. |

Acceptance: test the original cockpit removed, a differently oriented/different-type replacement seat, heavy losses, a split remnant, repeated/symmetric armor patterns and the wrong selected profile. Missing sections must remain red after acceptance. A normal capture/load must still work.

## 2. Complete HUD customization and input

These were in the original plan but are not all present in the current prototype.

- Adjustable compact HUD position and size, with screen-bound clamping and a reset-to-default action.

- Separate panel-preview placement from the in-flight HUD layout.

- Background, outline and fill opacity controls; configurable state colors and missing-state pulse strength/on-off. Keep the current yellow/orange/red scheme as the default.

- Manual model rotation in the expanded engineering view, with existing snap/reset controls retained. Add pan only if useful without interfering with flight controls.

- Configurable show/hide shortcut alongside expanded view, cycle-system and cycle-state shortcuts. Add panel/view actions where they remove routine chat use.

- Save named display presets and restore a known-good default. Keep UI labels and shortcut names understandable.

Acceptance: settings survive restart, off-screen layouts can always be reset, and UI capture does not steer the ship or trigger shortcuts while typing. Confirm normal and larger/wider displays.

## 3. Complete system filtering and rendering controls

- Allow multiple categories at once instead of only one isolated category.

- Give categories independent visible/hidden, transparent, highlighted and wireframe presentation where supported by the schematic renderer.

- Keep a quick isolate action and useful presets, such as damage overview, propulsion and conveyors through transparent armor.

- Apply category choices consistently to live outlines, missing baseline geometry, markers and station context. Reports must make filtered counts distinct from full totals.

- Review modded-block classification and provide a clear fallback for unknown types.

- Assess the current simplified outline against representative ships. Improve misleading shapes or lost detail within the rendering budget; document that it is a schematic. Full textured game-block meshes are not required for the first complete release.

Acceptance: combine categories, inspect internal systems and missing blocks, switch presets during a pending rebuild, and retain whole-ship orientation/scale without stale or misleading geometry.

## 4. Finish profiles and reference management

- Store display preferences per ship/profile, with global defaults for new profiles. Station mode already works this way; extend the behavior to the agreed layout/filter/zoom preferences.

- Add clear profile rename and management actions while keeping stable numeric IDs.

- Support multiple named baseline revisions under one profile, such as Original, Retrofit and Pre-combat, with one active revision.

- Record reference name, source, creation time and available blueprint identity. A database backup is not a user-selectable reference revision.

- Make capture/import replacement explicit: create a new revision or replace the chosen one. Automatic matching and recovery must never change reference contents.

- Migrate existing single-baseline profiles without losing numbers, anchors, station preferences or baselines. Retain atomic saves and backup recovery.

- Ensure revision selection does not make automatic matching treat one ship's own revisions as competing ship identities.

Acceptance: upgrade existing data, create/switch/rename revisions, reconnect, recover a backup and compare against an older revision. An old baseline must remain unchanged after damage or a new revision.

## 5. Finish blueprint coverage and diagnostics

- Add browsing/import of locally available Workshop blueprints where the installed game exposes a readable layout. Normal use must not require a new external download service.

- Investigate protobuf-only blueprint support through verified game readers. If safe, read-only support is unavailable, explicitly identify unsupported files and document the XML route; do not imply all game blueprint formats are supported.

- Finish edge-case checks for multiple grids/cockpits, rotated multi-cell blocks, missing mods, changed mods, failed cloud requests and profile/ship switches during preview.

- Make omitted definitions inspectable by name/count before acceptance, not only a total skipped-block count.

- Add a concise engineering summary: expected blocks, matching blocks, missing/damaged/non-functional/added/replaced blocks and structural completeness.

- Define completeness explicitly as matching expected positions/definitions out of the baseline. Keep it separate from health/integrity; show unavailable health as unknown rather than inventing a precise percentage.

- Keep station diagnostics separate per docked grid and expose loading/display limits clearly.

Acceptance: the imported baseline and report agree, skipped blocks are understandable, cancel leaves saved data untouched, and no blueprint scripts, inventories or ownership changes are applied.

## 6. Hardening and remaining validation

User-reported passes remain recorded. This is the remaining coverage and regression work, not a request to redo every successful test immediately.

- Test ambiguous/near-identical saved profiles and symmetric structures. A layout match is not proof of ship identity; preserve manual selection and auto-off.

- Verify cockpit, flight-seat and remote-control transitions, including destroyed controllers and leaving control during pending work.

- Validate blueprint preview isolation/cancellation and rotated multi-cell alignment beyond the already-passed basic import flow.

- Record reproducible live performance on approximately 1k, 10k and 50k-block ships, plus a large connector dock. Prior performance passes are useful; scale-specific timing and worst-case matching/capture/import costs still need measurement.

- Add bounded refresh/performance controls if profiling shows a useful tradeoff. Avoid full-grid work per frame and keep game-object reads on the game thread.

- Exercise dock display limits, mixed grid sizes, long sessions, reconnects and repeated grid destruction. Check memory, subscriptions and queued-worker cleanup.

- Verify partial/delayed multiplayer observations, unavailable integrity data and heavily modded block definitions without weakening missing-block semantics.

- Finish English/Spanish label persistence and new-control coverage. Full translation of diagnostic responses is optional; document the English fallback.

- Keep every new runtime test in TESTING-TODO.md. Automated tests verify logic and compilation; only observed game results complete runtime checks.

## 7. Release acceptance

Feature complete means sections 1–5 are delivered, with conditional format support resolved and documented. Release-ready additionally requires the relevant section 6 checks and these gates:

1. Build the binary package and pass automated regression checks.

2. Compile the manifest-selected source without local-build-only assumptions.

3. Actually load that pinned source through Pulsar Legacy with the binary copy disabled; verify version, dependencies, commands, panel and unload behavior. A source compilation check alone does not establish this.

4. Finish a fresh-install and existing-profile-upgrade smoke test; document known limitations, backup/recovery and installation steps.

5. Reconcile README, commands, plugin description and this roadmap with shipped behavior. Publish versioned release notes and package contents without game DLLs or duplicate Harmony.

6. Recheck PluginHub rules against the final source and prepare the pinned manifest. Hub submission/review is a distribution step; acceptance is not guaranteed by local testing.

Current supported target remains Pulsar Legacy / .NET Framework 4.8. A .NET 10/Interim plugin build needs its own API, build and runtime validation before support is claimed; the .NET 10 test executable is not that build.

## Outside the first feature-complete release

- Mechanical rotor/hinge/piston subgrid rendering or baselines: explicitly excluded by user decision. Connector-only station mode remains in scope.

- Separate live-only/reference-only display modes: replaced by comparison-only behavior at user request.

- Fully textured block meshes, a second client-mod implementation and an additional runtime port.

- Damage history, combat timelines, conveyor connectivity analysis, power-network simulation, repair priorities and other advanced engineering diagnostics.

- Automatic Workshop downloading and additional UI languages/full diagnostic translation.

- Server components, world edits, repair automation or bypassing normal client replication: outside the product's read-only design.

## Recommended order of work

1. Punctuation shortcuts and middle-left preview, then cockpit-loss recovery with alignment preview/fallback.

2. HUD customization and multi-category filters/presets.

3. Per-profile display preferences and named baseline revisions.

4. Blueprint coverage, inspectable import warnings and engineering statistics.

5. Remaining hardening, source-loader acceptance and release packaging.

Current build: 0.10.0 prototype. New runtime acceptance is pending.
