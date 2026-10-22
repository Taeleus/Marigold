# Marigold commands — 0.7

All commands use `/mg`; `/marigold` is an alias. `/m` belongs to the game. Commands are handled locally.

## Start and save a baseline

Sit in the cockpit you want as the permanent alignment anchor. Run `/mg anchor`, then `/mg capture`. Profiles are stored outside the game folders in `%APPDATA%/Marigold/profiles.bin`, with a previous-version backup. Capturing again intentionally replaces that profile's baseline.

Automatic matching is enabled by default. After reconnect or world transfer, control the ship and allow several seconds for stable observations. Marigold searches up to 16 cockpit candidates for a unique saved layout match and announces its selection. If it cannot choose, sit in the corresponding cockpit, run `/mg profiles` or `/mg profile suggest`, then `/mg profile select ID`. Suggestions measure structural overlap around your seated cockpit. They do not establish ship identity; identical builds can tie. Automatic matching requires at least 8 blocks, 90% structural overlap and an 8-percentage-point lead over alternative profile/cockpit matches. Two matching observations must be unchanged, and the current sample is rechecked before acceptance. Names and runtime IDs do not determine the choice. This is client-side layout recognition, not a Nexus identity API; an unsaved identical copy can still match. Actual Nexus compatibility needs in-game testing.

`/mg clear` removes the selected baseline but keeps its profile/anchor. `/mg profile detach` unbinds the live ship and suppresses automatic selection for it until session unload or `/mg auto retry`. `/mg profile delete ID` deletes that saved profile. `/mg recover` explicitly restores the database backup after a load problem; it clears live bindings so you can reselect.

## Import a blueprint baseline

1. Select or create the live ship's profile while in its intended anchor cockpit.
2. `/mg blueprint load "C:\full path\bp.sbc"`
3. `/mg blueprint grids`, then `/mg blueprint grid N` for the intended grid.
4. `/mg blueprint anchors`, then `/mg blueprint preview N` for the matching cockpit.
5. Inspect top, bottom and reset views and `/mg report`. The preview has a purple reference outline and an explicit unsaved caption.
6. `/mg blueprint accept` saves the intended layout. `/mg blueprint cancel` leaves the original baseline intact. After cancel, select the blueprint grid again to prepare another preview.

Import uses installed block definitions for exact multi-cell bounds and cockpit orientation. It treats the intended blocks as fully built. The cockpit definition and grid size must match. Blueprint scripts, inventories and ownership are never applied. Only the selected grid is imported; attached grids need future construct support. Missing mods, compound blocks, legacy quaternion-only orientations and supergrids are rejected instead of silently approximated. Files can be plain or gzip-compressed SBC, with bounded XML/file sizes. Blueprint files are read-only.

## Display and diagnostics

| Command | Result |
| --- | --- |
| `/mg show`, `hide` | Show/hide schematic; hiding pauses diagram observation; automatic matching still runs |
| `/mg spin`, `reset` | Toggle rotation; restore default rotating view |
| `/mg left`, `right`, `top`, `bottom` | Snap relative to the active cockpit |
| `/mg zoom 0.25..3`, `in`, `out` | Set/change scale |
| `/mg mode live`, `reference`, `compare` | Current outline, saved outline, or change markers |
| `/mg isolate CATEGORY` | Select a system, retaining whole-ship scale |
| `/mg filter STATE` | Limit markers; report totals still cover the entire grid |
| `/mg page next`, `prev`, `N` | Browse up to 128 filtered markers per page, highest severity first |
| `/mg layout compact`, `expanded` | Small side panel or larger centered panel |
| `/mg detail low`, `medium`, `high` | Outline geometry budget |
| `/mg report` | Baseline status, all change counts and visible marker page |
| `/mg stats` | Sample, worker and HUD CPU timing; excludes GPU cost |
| `/mg settings save`, `load`, `reset` | Explicitly save/reload preferences, or reset in-memory defaults |
| `/mg help display`, `profiles`, `blueprint` | Topic-specific help |

Categories: `all armor conveyors thrusters weapons power gas mechanical production cargo control tools other`. Classification uses block types; unusual/modded definitions may fall back to `other`. `mechanical` isolates rotor/piston blocks on the observed grid, not their attached grids.

States: `all faults missing damaged broken added replaced unknown`. `broken` means structurally non-functional; `faults` includes missing, damaged, broken and unknown. Power loss and a disabled switch do not count as structural damage.

Yellow is faint damage, orange is non-functional, pulsing red is absent from the observed grid, green is added, purple is replaced, and gray is unreadable integrity data. Reference/preview data never changes the game world.

Preferences live in `%APPDATA%/Marigold/display.txt`; saving retains the previous file as `.bak`. Corrupt preferences are preserved and logged, with defaults used at startup. Saving is explicit. View orientation/spin and marker page are transient so each launch starts with the familiar default orientation.

## Station mode and automatic association

`/mg station on|off|status` controls connector context. It defaults OFF. Locked connectors are followed recursively, with cycles removed; rotor/hinge/piston-only links are excluded. Connected grids are drawn at their relative position/rotation and grid scale, using bounded low-detail outlines. They are context only: capture, blueprint baselines, filters' comparison totals and damage markers still refer to the controlled grid. Context is refreshed about once per second while visible. Limits are 8 additional grids and 100,000 context blocks, flagged in `/mg report`. In reference mode the controlled outline is saved data but station context is still live.

`/mg auto on|off|status|retry` controls automatic profile reassociation. ON is the default. Matching is retried while an unbound controlled grid loads, including after world reload/login. It never captures, overwrites or deletes a baseline, never overrides an existing selection, and refuses to reuse a profile still bound to another live grid. Low-confidence, ambiguous, missing-cockpit and heavily altered ships remain manual. `retry` clears this ship's manual-detach suppression; it does not turn a disabled matcher on or replace a current binding.

Station and auto on/off commands save the current display preferences immediately, including any other unsaved display choices. Older preferences migrate with station OFF and automatic matching ON. `/mg settings reset` returns to those defaults in memory. `/mg auto off` persists across logouts and restarts; it retains any existing binding, which you can separately detach.
