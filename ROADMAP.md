# Marigold roadmap

## Current position — 0.4.10

The usable prototype is in place: a cockpit-relative ship schematic, chat controls, persistent profile-based reference capture, comparison markers and translucent damage shading. The user confirmed missing-block behavior and the corrected default/reset orientation. Source is published at https://github.com/Taeleus/Marigold on master.

## Scope and guardrails

Client-only Space Engineers 1 engineering HUD for Pulsar Legacy / .NET Framework 4.8. Never change the world or send custom network messages. Red means absent from the observed ship relative to its baseline, including detached blocks; it does not assert destruction. Gray is reserved for unreadable block data. The game installation and SpaceEngineers roaming directory remain read-only development references.

## Milestones and acceptance gates

| Milestone | Status | Remaining verification or work |
| --- | --- | --- |
| Setup and API research | Established | Recheck rules and packaging before PluginHub submission |
| Controlled-ship observer | Implemented; switching and live naming confirmed | Broader remote-control, disconnect and teardown testing |
| Schematic renderer and controls | Implemented; user confirmed rotation, controls, shading and corrected orientation | Real in-game profiling on large ships; broader display/resolution checks |
| Snapshot comparison | Implemented for this world session; missing-state behavior confirmed | Verify orange functional threshold, repair/replacement cases, splits and multiplayer freshness in game |
| Persistent profiles and cockpit anchors | Implemented in 0.5.0; pure persistence/alignment tests pass | In-game save/reload and explicit profile reassociation checks; Nexus-specific validation |
| Blueprint baseline import | After persistent profiles | Bounded local blueprint parser, grid selection, cockpit pairing, alignment preview and acceptance |
| Categories and observation quality | Planned | System filters, isolation, incremental updates and multiplayer observation testing |
| Mechanical constructs | Planned | Rotor/hinge/piston grids, moving transforms, detachment and connector exclusion |
| Engineering UI and release | Planned | Settings/profile UI, profiling, PluginHub source compilation, cleanup and rules review |

## Implemented slice: persistent profiles and cockpit anchors

1. Save versioned profiles containing a stable profile ID, baseline blocks and an anchor record. Store them outside the read-only SpaceEngineers folders. Use atomic writes and recoverable backups; test malformed or incompatible files.
2. Implement `/mg anchor` while seated. Record the selected cockpit's grid-local position, orientation, definition and name. Keep this separate from the game's Main cockpit setting. Switching seats must not change baseline alignment.
3. Make capture update the selected profile explicitly, and provide profile list/select/remove commands. Distinguish clearing a baseline from deleting a saved profile.
4. Restore profiles after world reload or reconnect. Keep profile identity independent of display names and runtime grid IDs. Require explicit selection when multiple identical ships could match.
5. Research and test Nexus transfers before enabling automatic reassociation. Verify a candidate using its anchor and surrounding layout; retain manual reassociation when uncertain. Do not promise Nexus compatibility before this validation.

Acceptance: anchor and capture a ship, leave/reload the world, select or reliably reassociate its profile, and compare against the original baseline. Renames and cockpit switches preserve the anchor. Two identical ships remain distinct profiles. Missing anchors never silently attach a baseline to another ship.

## Following slice: blueprint baselines

Import a local blueprint into the same profile format used by captures. Select the intended grid and pair its cockpit with the live anchor. Use the cockpit orientation for rotation and its position for translation. Preview and accept the alignment before reporting differences. Multiple cockpit candidates require selection; mechanical subgrid alignment is deferred.

## Validation ledger

Build/check results are recorded in README.md; API evidence is in docs/API-EVIDENCE.md. Pure tests and compilation do not establish live runtime or multiplayer behavior. Historical implementation notes remain in docs; this roadmap supersedes their older milestone priorities. Persistent profiles and anchors are implemented in 0.5.0. Blueprint import and automatic Nexus reassociation are not implemented.

## Immediate next gate

Validate 0.5.0 in game: anchor, capture, reload, select the saved profile, and verify damage remains aligned. Test switching seats and selecting profiles for identically named ships. After that, implement bounded local blueprint import and an alignment preview. Automatic transfer matching stays gated on Nexus research and testing.
