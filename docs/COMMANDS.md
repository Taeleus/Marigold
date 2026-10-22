# Marigold commands — 0.8

All commands use `/mg`; `/marigold` is an alias. `/m` belongs to the game. Commands are handled locally.

## Start and save a baseline

Sit in the cockpit you want as the permanent alignment anchor. Run `/mg capture`; it creates the cockpit anchor automatically and assumes full health. Profiles are stored outside the game folders in `%APPDATA%/Marigold/profiles.bin`, with a previous-version backup. Capturing again intentionally replaces that profile's baseline.

Automatic matching is enabled by default. After reconnect or world transfer, control the ship and allow several seconds for stable observations. Marigold searches up to 16 cockpit candidates for a unique saved layout match and announces its selection. If it cannot choose, sit in the corresponding cockpit, run `/mg profiles` or `/mg profile suggest`, then `/mg profile load ID`. Suggestions measure structural overlap around your seated cockpit. They do not establish ship identity; identical builds can tie. Automatic matching requires at least 8 blocks, 90% structural overlap and an 8-percentage-point lead over alternative profile/cockpit matches. Two matching observations must be unchanged, and the current sample is rechecked before acceptance. Names and runtime IDs do not determine the choice. This is client-side layout recognition, not a Nexus identity API; an unsaved identical copy can still match. Actual Nexus compatibility needs in-game testing.

`/mg clear` removes the selected baseline but keeps its profile/anchor. `/mg profile detach` unbinds the live ship and suppresses automatic selection for it until session unload or `/mg auto retry`. `/mg profile delete ID` deletes that saved profile. `/mg recover` explicitly restores the database backup after a load problem; it clears live bindings so you can reselect.

## Import a blueprint baseline

1. Sit in the intended live anchor cockpit. A prior profile or capture is optional.
2. `/mg blueprint load "C:\full path\bp.sbc"`
3. `/mg blueprint grids`, then `/mg blueprint grid N` for the intended grid.
4. `/mg blueprint anchors`, then `/mg blueprint preview N` for the matching cockpit.
5. Inspect top, bottom and reset views and `/mg report`. The preview has a purple reference outline and an explicit unsaved caption.
6. `/mg blueprint accept` saves the intended layout. `/mg blueprint cancel` leaves the original baseline intact. After cancel, the prepared grid remains available for another preview.

Import uses installed block definitions for exact multi-cell bounds and cockpit orientation. It treats the intended blocks as fully built. The cockpit definition and grid size must match. Blueprint scripts, inventories and ownership are never applied. Only the selected grid is imported; attached grids need future construct support. Unavailable mod definitions are skipped with an explicit count. Compound blocks, legacy quaternion-only orientations and supergrids remain unsupported. Files can be plain or gzip-compressed SBC, with bounded XML/file sizes. Blueprint files are read-only.

## Display and diagnostics

| Command | Result |
| --- | --- |
| `/mg show`, `hide` | Show/hide schematic; hiding pauses diagram observation; automatic matching still runs |
| `/mg spin`, `reset` | Toggle rotation; restore default rotating view |
| `/mg left`, `right`, `top`, `bottom` | Snap relative to the active cockpit |
| `/mg zoom 0.25..3`, `in`, `out` | Set/change scale |
| `/mg mode compare` | Comparison is always enabled; old live/reference commands also resolve to compare |
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

`/mg station on|off|status` enables connector-linked grids for this ship/profile, off by default. Locked connector chains are followed without cycles; rotor/hinge/piston-only links are excluded. Docked ships use their own uniquely matched saved baselines for damage outlines and separate report counts. Matches are retained during the session, so later damage does not erase a matched baseline. Unmatched ships remain context only. Capture always covers just the controlled grid. Limits: 8 additional grids and 100,000 context blocks.

`/mg auto on|off|status|retry` controls automatic profile reassociation. ON is the default. Matching is retried while an unbound controlled grid loads, including after world reload/login. It never captures, overwrites or deletes a baseline, never overrides an existing selection, and refuses to reuse a profile still bound to another live grid. Low-confidence, ambiguous, missing-cockpit and heavily altered ships remain manual. `retry` clears this ship's manual-detach suppression; it does not turn a disabled matcher on or replace a current binding.

Station changes save to the selected ship profile, or remain session-local until capture creates one. Auto and language changes save display preferences. Station is never restored from global display settings. `/mg auto off` persists and retains an existing binding.

## Graphical interface (0.9)

Use `/mg ui` for Display, Profiles, Blueprints and Views. Capture creates an anchor and full-health baseline in one step. Profiles have stable numeric IDs; old GUIDs and `profile select` remain accepted. Clear removes only the baseline; Delete removes the profile.

Local discovery includes nested folders (depth 16, 2,000 folders, 500 results) and skips linked folders. Cloud lists game-service XML `bp.sbc` files, including gzip; protobuf-only and Workshop downloads are not supported. Choose Load, a grid if needed, a matching cockpit, Preview, then Save profile. Lists update after preparation and a small live schematic stays visible beside the panel.

Capture/clear/delete/recover and blueprint acceptance require a second identical panel click. Chat commands execute directly. Ship changes close the panel.

Views lets you assign shortcuts for cycle-system, cycle-marker and expanded layout. Enter a key name such as `Ctrl+Shift+F6`, or blank to unbind. Defaults are unbound; Marigold does not consume the game's bindings. Shortcuts are saved separately in `%APPDATA%/Marigold/hotkeys.txt` and ignored while chat or a menu is open.

Spin off snaps to a fixed isometric view. Reset restores rotation. Missing regions remain outlined regardless of individual marker pagination; category/state filters still apply.

`/mg language en|es` changes UI label language. Newly added and diagnostic messages may remain English.
