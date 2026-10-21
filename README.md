# Marigold

Space Engineers 1 client engineering HUD prototype. Start with [ROADMAP.md](ROADMAP.md) for milestones and acceptance gates.

## Current implementation

Version 0.6.0 targets Pulsar Legacy / .NET Framework 4.8, compiled against the local SE 1.210.014 assemblies. While controlling a ship controller, the schematic shows only the live grid name beneath the outline. The old observer notification, block/line counts, detail label and chat hint have been removed. Diagnostics remain available with `/mg stats`; help remains available with `/mg`.

Controller changes, closed grids, session unload and disposal clear the schematic. Grid name changes update without waiting for geometry rebuilding. The view defaults and cockpit-relative view commands are unchanged.

A rotating hull-envelope outline is now included, with coplanar seams removed and collinear edges merged. Persistent ship profiles, cockpit anchors and reference capture/comparison are included; see [Profiles and anchors](docs/PROFILES-0.5.md). Local blueprint import with an explicit alignment preview, system/state filters, live/reference/compare modes, expanded layout and saved display preferences are included. See [current commands](docs/COMMANDS.md) and the editable [testing to-do](TESTING-TODO.md). Full block meshes, a graphical settings editor, mechanical construct comparison and automatic Nexus reassociation are not implemented. No custom networking or world edits. The game may suppress normal notifications in some UI/HUD modes. Remote control, multiplayer behavior and exact rendering appearance still require in-game checks.

## Build and checks

For current controls see [Control correction 0.2.1](docs/CONTROLS-0.2.1.md). Original Ctrl+Alt shortcuts are removed; use local chat /mg commands. For current geometry and performance controls see [Outline 0.3](docs/OUTLINE-0.3.md). The plugin uses Pulsar's already-installed Harmony 2.4.2; a second Harmony DLL is deliberately not shipped in the local package.

From this folder in PowerShell:

```powershell
.\build.ps1
# Another installation:
.\build.ps1 -Bin64 'D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64'
```

Requires a .NET SDK able to build net48 (the local machine has SDK 10.0.402 and Framework reference assemblies). The console lifecycle checks target net10.0; the plugin targets net48. The build never deploys or launches the game. All output and package caches are inside this workspace.

Validation on October 21, 2026:

- Release plugin build against installed game DLLs: passed, zero warnings/errors.
- 13 lifecycle checks using small game API doubles: passed. These exercise notification reuse/text refresh, switching, exit, closed grids, unload/reload, failure recovery and idempotent cleanup.
- Game source inspection: see [docs/API-EVIDENCE.md](docs/API-EVIDENCE.md).
- Plugin and game-code skill preparation: completed successfully (both reported DONE). Game-code property search and plugin registry search smoke checks passed. Optional Graphify generation was disabled.
- Version 0.1 user-confirmed in-game: loading, name/count display on Lemon Drop (2,252 blocks) and OPS Miner (396 blocks), and immediate cockpit rename updates. Other checklist items remain pending.
- Version 0.2: 24 geometry checks pass in addition to the 13 observer checks. User confirmed rotating display and seamless grid switching in 0.2. The old keybinds failed and were replaced in 0.2.1. User subsequently confirmed chat controls and cockpit-relative snap views in 0.2.2; performance remains pending.

## Local test package

The build produces `artifacts/Marigold/plugin.dll`, `plugin.pdb` and `plugin.xml`. No Keen DLLs are copied into the package. Metadata is for local development; it is not a completed PluginHub submission and has no repository/commit pin. Runtime restrictions are omitted so the local loader can infer the Framework runtime from the DLL; only Legacy is supported/test-targeted at present.

For a later manual test, with the game closed, place that package directory under Pulsar's `Legacy/Local` directory, enable **Marigold (prototype)** in the plugin list, and launch Legacy. For the previously observed installation this would be `%APPDATA%/Pulsar/Legacy/Local/MarigoldDev`. Verify the directory against your active Pulsar installation before copying. Avoid enabling both a binary package and source-folder version simultaneously. The development installation is MarigoldDev. Restart the game after replacing the package; live reload is not supported.

After a session, look for `[Marigold]` entries in the normal game log. Disable the plugin to stop it loading. Follow [docs/IN-GAME-CHECKLIST.md](docs/IN-GAME-CHECKLIST.md) before marking milestone 1 complete.

## Source boundaries

Only `src/Marigold.Plugin` belongs to the production source descriptor. Tests and downloaded research sources must never enter PluginHub production compilation. `bin`, `obj`, `research`, caches and artifacts are ignored. The inspected Pulsar local-folder fallback excludes bin/obj, while source publication must exclude generated files from Git.

Both `D:/SteamLibrary/steamapps/common/SpaceEngineers` and `%APPDATA%/SpaceEngineers` are read-only development references. Do not use builds or setup scripts that write there. Skill caches and decompiled sources are local research only and must not be shipped.




## 0.3.0 validation and deployment

October 21, 2026: Release build passed with zero warnings/errors. Passed 13 observer lifecycle checks, 6,973 outline/geometry assertions and 151 command/orientation checks. Synthetic solid-hull worker build samples: 1,000 blocks 1.05 ms; 10,000 blocks 20.89 ms; 50,000 blocks 9.32 ms. Single-run .NET 10 timings include runtime warm-up variation and are not production frame-time measurements.

Installed plugin.dll, plugin.pdb and plugin.xml into Pulsar Legacy/Local/MarigoldDev with matching SHA-256 hashes; installed assembly version 0.3.0.0. Previous package saved under artifacts/backup-before-0.3.0. In-game validation remains pending. See docs/OUTLINE-0.3.md for commands and test steps.

## 0.3.1 UI cleanup

Removed the legacy status notification and simplified the schematic caption to the live grid name. Existing observer lifecycle checks now verify overlay handoff/cleanup and the absence of legacy notifications. Outline and control checks remain unchanged. Runtime validation pending.

## 0.4.0 validation and deployment

Release build passed with zero warnings/errors. Passed 24 new snapshot comparison checks, 12 observer lifecycle checks, 6,973 geometry assertions and 151 command/orientation checks. Installed assembly 0.4.0.0 and verified SHA-256 matches for all three package files in MarigoldDev. Previous package preserved in artifacts/backup-before-0.4.0. Live diagnostics and split/multiplayer validation are pending; see docs/SNAPSHOTS-0.4.md for the test sequence.

## 0.4.2 shaded damage markers

Damaged/degraded markers now receive a translucent amber silhouette fill (alpha 60/255), drawn behind the existing outlines. The fill follows the rotated block envelope and is clipped to the viewport. Scanline bands are approximately one pixel high, adapting to a 4,096-fill-sprite budget across all displayed damaged markers. Overlapping markers can appear darker; this remains a transparent schematic without depth occlusion. Other marker colors remain outlines.

Release build passed with zero warnings/errors; all existing checks and five fill checks passed. Installed assembly 0.4.2.0 in MarigoldDev with package hash verification and backup under artifacts/backup-before-0.4.2. In-game appearance/performance pending.

## 0.4.3 command correction

Use `/mg` or `/marigold`. `/m` belongs to the game mute command and is no longer claimed. The installed MyChatCommands.cs registers `/m` as mute at line 230; `/mg` and `/marigold` are absent from that built-in registry. Third-party command conflicts are not exhaustively checked. Plugin details now use a short introduction, getting-started steps, color key and current limitations. Anchor/claim requests explain that persistent cockpit anchors remain unimplemented.

## 0.4.4 diagnostic colors

Yellow fill/outline marks damaged/degraded blocks. Orange fill/outline marks structurally non-functional functional blocks (including unfinished construction), using FatBlock.IsFunctional, not IsWorking or Enabled. For matched blocks orange takes precedence over yellow; added/replaced markers retain their baseline-change colors. Armor has no functional-state classification. Missing markers now have a red fill/outline with a gentle two-second alpha pulse (never fully off). Unknown remains gray. All fills share the existing 4,096-sprite budget.

Installed 0.4.4.0 with verified package hashes and backup under artifacts/backup-before-0.4.4. Release build: zero warnings/errors; all checks passed, including 29 snapshot comparison checks. Live color/functionality testing remains pending.

## 0.4.5 visual priority and gray-marker explanation

Damage yellow is muted (RGB 220/205/65): outline alpha 150 instead of 240, fill alpha 25 instead of 60. Orange and red are unchanged. `/mg report` now distinguishes observed split, multiplayer-client uncertainty, or both. Classification is unchanged: gray does not establish destruction. In-game removal confirmation and detached-grid tracking remain future work.

## 0.4.6 missing-state semantics

Supersedes the 0.4.5 uncertainty rule: any reference position absent from the observed ship is assumed missing and rendered pulsing red, including after splits and on multiplayer clients. Gray is reserved for invalid/unreadable block data. No recapture is needed to make a removed block red. Detached pieces need not be destroyed to count as missing from the original ship.

## 0.4.7 view correction

Swapped the top/bottom presets in response to in-game feedback; up/down aliases follow the corrected mappings. Both retain nose-up screen orientation and stop automatic spin without changing zoom. Left/right and reset are unchanged. Updated the existing 24-cockpit-orientation checks; in-game confirmation of corrected views is pending.

## 0.4.8 plan-view handedness

Top/bottom views now explicitly preserve cockpit left/right and nose-up screen coordinates. This corrects the horizontal reflection introduced by swapping the presets in 0.4.7. Hull lines, fault outlines and translucent fills share the corrected projection. Switching to spin, reset or a side view exits this plan-view convention. Added asymmetric rear-port landmark checks across all 24 cockpit orientations for both plan views.

## 0.4.9 default/reset alignment

Default/reset now starts at zero yaw instead of 0.6 radians. At the starting angle screen X equals cockpit-local X, independent of a block's fore/aft position. Automatic rotation, pitch, zoom, and the working plan views are unchanged. This is a starting-orientation correction; screen positions naturally change as automatic rotation continues. Regression checks cover forward/aft, left/right landmarks for fresh defaults and reset after a plan view across 24 cockpit orientations. In-game confirmation pending.

## 0.4.10 rotating-view horizontal correction

Replaces the insufficient 0.4.9 starting-angle adjustment with an explicit screen-X reflection for the default/reset rotating view. Hull outlines, markers and fills share this projection. Top/bottom and side presets are unchanged. Pausing spin retains the projection; resuming spin enables the rotating-view convention. Regression checks cover multiple yaw angles, unchanged Y/Z, pause/resume and plan-view isolation. In-game verification remains pending.

## In-game confirmation

The user confirmed the default/reset horizontal projection in 0.4.10 is correct. Current snapshots still last only for the world session; persistent profiles, cockpit anchors and blueprint import remain planned.

## 0.5.0 persistent profiles

Implemented `/mg anchor`, persistent `/mg capture`, `/mg profiles`, `/mg profile select ID`, `/mg profile detach`, `/mg profile delete ID`, and `/mg recover`. Data is stored outside the game folders in `%APPDATA%/Marigold/profiles.bin`, using versioned bounded records, atomic replacement and a previous-version backup. Profile selection after reload is explicit; automatic Nexus matching and blueprint import remain deferred. See docs/PROFILES-0.5.md for usage and verification. Historical session-only notes above describe older builds.

0.5.0 validation: Release build completed with zero warnings/errors. Passed 119 profile persistence/alignment checks plus the existing observer, comparison, fill, geometry and control checks. Installed assembly 0.5.0.0 into MarigoldDev with SHA-256 verification of all package files; previous package retained under artifacts/backup-before-0.5.0. In-game save/reload and Nexus transfer validation remain pending.

## 0.6.0 — Blueprint import and engineering display controls

Adds bounded plain/gzip SBC import, explicit grid/cockpit pairing, unsaved alignment preview and accepted persistent baselines. Adds system isolation, change filters, marker pagination, live/reference/compare modes, compact/expanded layouts, explicit saved preferences and cockpit-relative profile suggestions. Unchanged observations reuse geometry; game-thread snapshots still run periodically. Suggestions never select a profile automatically.

Release build passed with zero warnings/errors. Automated checks: 12 observer lifecycle, 16 blueprint parser/command, 45 display/settings/suggestion, 119 profile persistence/alignment, 5 fill, 29 snapshot comparison, 6,973 geometry and 403 command/orientation checks. Pure checks and API doubles do not validate the live renderer, imported game definition conversion, multiplayer replication or Nexus transfers. User testing is deferred in [TESTING-TODO.md](TESTING-TODO.md); no unchecked item is considered validated.

Installed assembly 0.6.0.0 into the authorized Pulsar `Legacy/Local/MarigoldDev` folder. DLL, PDB and metadata SHA-256 hashes match the built package. The previous package is preserved under `artifacts/backup-before-0.6.0`. Restart the game to load the new version.
