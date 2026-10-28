# Marigold commands — 0.16.0 development

All commands use `/mg`; `/marigold` is an alias. `/m` belongs to the game. Commands are handled locally.

## Start and save a baseline

Sit in the cockpit you want as the permanent alignment anchor. Run `/mg capture`; it creates the cockpit anchor automatically and assumes full health. Profiles are stored in `<game user data>/Storage/Marigold/profiles.bin`, with a previous-version backup. The default is `%APPDATA%/SpaceEngineers/Storage/Marigold/profiles.bin`; a custom `-appdata` folder is honored. Capturing again intentionally replaces that profile's baseline after preserving the old reference in Saved versions.

Automatic matching is enabled by default. After reconnect or world transfer, control the ship and allow several seconds for stable observations. Marigold prefers the occupied cockpit; when there is no occupied cockpit candidate it considers up to 16 cockpits for a unique saved layout match and announces its selection. If it cannot choose, sit in the corresponding cockpit, run `/mg profiles` or `/mg profile suggest`, then `/mg profile load ID`. Suggestions measure structural overlap around your seated cockpit. They do not establish ship identity; identical builds can tie. Automatic matching requires at least 8 blocks, 75% structural overlap and an 8-percentage-point lead over alternative profile/cockpit matches. Two matching observations must retain the same structure, and the current sample is rechecked before acceptance. Health-only changes and enumeration order do not reset structural stability. Names and runtime IDs do not determine the choice. This is client-side layout recognition, not a Nexus identity API; an unsaved identical copy can still match. Actual Nexus compatibility needs in-game testing.

`/mg clear` removes the selected baseline but keeps its profile/anchor. `/mg profile detach` unbinds the live ship and suppresses automatic selection for it until session unload or `/mg auto retry`. `/mg profile delete ID` deletes that saved profile. `/mg recover` explicitly restores the database backup after a load problem; it clears live bindings so you can reselect.

## Import a blueprint baseline

1. Sit in the intended live anchor cockpit. A prior profile or capture is optional.

2. `/mg blueprint load "C:\full path\bp.sbc"`

3. `/mg blueprint grids`, then `/mg blueprint grid N` for the intended grid.

4. `/mg blueprint anchors`, then `/mg blueprint preview N` for the matching cockpit.

5. Inspect top, bottom and reset views and `/mg report`. The preview has a purple reference outline and an explicit unsaved caption.

6. `/mg blueprint accept` saves the intended layout. `/mg blueprint cancel` leaves the original baseline intact. After cancel, the prepared grid remains available for another preview.

Import uses installed block definitions for exact multi-cell bounds and cockpit orientation. It treats the intended blocks as fully built. The cockpit definition and grid size must match. Blueprint scripts, inventories and ownership are never applied. Only the selected grid is imported; mechanical subgrids are excluded. Unavailable mod definitions are skipped with an explicit count. Compound blocks, legacy quaternion-only orientations and supergrids remain unsupported. Files can be plain or gzip-compressed SBC, with bounded XML/file sizes. Blueprint files are read-only.

## Display and diagnostics

| Command | Result |

| --- | --- |

| `/mg show`, `hide` | Show/hide the entire HUD, including health bars; automatic matching still runs |

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

Categories: `all armor conveyors thrusters weapons power tanks production cargo control tools other`. Category labels use consistent title case in the UI; `weapons` is displayed as Offensive and `other` as Utility. Gas tanks and generators appear under Tanks. Mechanical blocks are grouped under Utility, not as a separate filter. Unusual/modded definitions may also fall back to Utility.

States: `all faults missing damaged broken added replaced unknown`. `broken` means structurally non-functional; `faults` includes missing, damaged, broken and unknown. Power loss and a disabled switch do not count as structural damage.

Yellow is faint damage, orange is non-functional, pulsing red is absent from the observed grid, green is added, purple is replaced, and gray is unreadable integrity data. Reference/preview data never changes the game world.

Preferences live in `<game user data>/Storage/Marigold/display.txt`; saving retains the previous file as `.bak`. Corrupt preferences are preserved and logged, with defaults used at startup. Saving is explicit. View orientation/spin and marker page are transient so each launch starts with the familiar default orientation.

## Station mode and automatic association

`/mg station on|off|status` enables connector-linked grids for this ship/profile, off by default. Locked connector chains are followed without cycles; rotor/hinge/piston-only links are excluded. Docked ships use their own uniquely matched saved baselines for damage outlines and separate report counts. Matches are retained during the session, so later damage does not erase a matched baseline. Unmatched ships remain context only. Capture always covers just the controlled grid. Limits: 8 additional grids and 100,000 context blocks.

`/mg auto on|off|status|retry` controls automatic profile reassociation. ON is the default. Matching is retried while an unbound controlled grid loads, including after world reload/login. It never captures, overwrites or deletes a baseline, never overrides an existing selection, and refuses to reuse a profile still bound to another live grid. Low-confidence, ambiguous, missing-cockpit and heavily altered ships remain manual. `retry` clears this ship's manual-detach suppression; it does not turn a disabled matcher on or replace a current binding.

Station changes save to the selected ship profile, or remain session-local until capture creates one. Auto and language changes save display preferences. Station is never restored from global display settings. `/mg auto off` persists and retains an existing binding.

## Graphical interface

Use `/mg ui` for Ship, Baselines, Ship display, Health bars, Preferences and Save & share. The navigation stays visible while forms scroll. Preview HUD opens the actual layout without discarding editor fields or a custom-bar draft; Return to editor resumes editing. Capture creates an anchor and full-health baseline in one step. Profiles have stable numeric IDs; old GUIDs and `profile select` remain accepted. Clear removes only the baseline; Delete removes the profile.

Local discovery includes nested folders (depth 16, 2,000 folders, 500 results) and skips linked folders. Cloud lists game-service XML `bp.sbc` files, including gzip; protobuf-only and Workshop downloads are not supported. Choose Load, a grid if needed, a matching cockpit, Preview, then Use baseline. Lists update after preparation and a small live schematic stays visible beside the panel.

Capture/clear/delete/recover and blueprint acceptance require a second identical panel click. Chat commands execute directly. Ship changes close the panel.

Preferences > Keyboard shortcuts lets you assign shortcuts for cycle-system, cycle-marker and expanded layout. Enter a key name such as `Ctrl+Shift+F6`, or blank to unbind. Defaults are unbound; Marigold does not consume the game's bindings. Shortcuts are saved separately in `<game user data>/Storage/Marigold/hotkeys.txt` and ignored while chat or a menu is open.

Spin off snaps to a fixed isometric view. Reset restores rotation. Missing regions remain outlined regardless of individual marker pagination; category/state filters still apply.

`/mg language en|es` changes UI label language. Controls, headings and menu descriptions are translated; dynamic diagnostic messages may remain English.

## Recovery, versions and customization

`/mg profile force NUMBER` searches surviving-block alignments without requiring the original cockpit. Wait for a preview; use `/mg recovery next`, `accept` or `cancel`. UI Alignment tools adjust translation by live-grid cells and rotate by 90 degrees about live-grid X/Y/Z axes around the baseline origin. If no candidate is found, a clearly unverified manual starting point is available. Acceptance requires three exact matches and never recaptures the baseline. Acceptance remembers the alignment and surviving layout locally for conservative automatic matching after reconnect. This requires auto matching on, at least eight exact matches and a verifiable remembered layout; ambiguity or further major layout changes may require recovery again. Only the latest accepted recovery is retained per profile, tied to its baseline identity.

`/mg isolate thrusters,power,tanks` combines categories; legacy `gas` settings/commands migrate to `tanks`, and `mechanical` migrates to `other`. `none` hides all. Systems offers hidden/wireframe/faint/highlighted hull styles. Damage markers retain state colors; station hulls use the same category visibility and brightness styles.

Focusing on selected systems now retains the others as faint outlines. **Ship > Systems and filters > Other systems opacity** adjusts this context; zero hides it. Hidden system styles remain hidden, and fault-only rendering modes omit context.

Ship display accepts normalized position (0-1), size (0.12-0.7 of screen height) and opacity (0-1). Ship display > State colors edits one state at a time. Preferences > Performance options contains both missing-block and fresh-damage pulse toggles. Save global writes defaults; Save ship writes the bound profile's display. Ship/preset loads preserve auto-match/language and station settings. Unsaved edits may be lost on grid switches. Preset names use up to 40 letters/digits/spaces/hyphens/underscores.

In **Ship display > State colors**, **Choose color visually...** opens a hue strip and a saturation/brightness square for the selected state. The swatches show the draft and original colors. **Set RGB** updates the picker from an exact six-digit value; **Use color** applies the selection, while Back/Cancel discards it. **Save display** keeps an applied color. The same picker is available under **Health bars > Bar colors**; applying an independent bar color disables outline matching.

Library saves independent named baselines. Capture/import and loading archive the previous active baseline when needed; deleting a revision leaves the active baseline intact. Versions show source/date and active identity. Cross-anchor restore opens a recovery preview and changes the active baseline only after acceptance. Limits: 16 revisions and 250,000 total blocks per library.

Preferences > Keyboard shortcuts accepts common punctuation/digits and press-to-bind Ctrl/Alt/Shift combinations. Escape cancels; blank unbinds. Show/hide and Open panel are new actions. The game still receives keys.

Workshop cache reads local bp.sbc files in the current Steam library without downloading; PB-only/archived items are unsupported. Omitted blocks lists unavailable definitions. Structural completeness measures matching expected structure, not health. Dynamic diagnostic responses may remain English.


## Health bars

Health bars provides eight independent bars: Overall, Armor, Power, Thrusters, Utility, Cargo, Offensive and Tools. Defaults are disabled. Each has enable/label toggles, normalized X/Y position, width (0.05-0.8 of screen width), height (0.005-0.08 of screen height), opacity (0-1), movement buttons and reset. Save global, Save ship and display presets include all layouts. Older six- and seven-bar settings retain their existing layouts; new bars start disabled.

Bars count baseline block states: healthy blue, damaged yellow, non-functional orange, missing/replaced expected blocks red, and unreadable gray. This is not summed integrity. New blocks are excluded; replacements retain the original system category. Tools includes welders, grinders, drills and ToolCore blocks; Utility covers categories without a dedicated bar. Filters/pagination and docked-grid context do not affect these controlled-grid totals. Preview baselines are labeled. No baseline or an empty system is shown as unavailable, never 100% healthy. Global Show/hide and normal HUD suppression also hide the bars.

**Health bars > Bar colors** changes the healthy segment color or links it to the healthy schematic outline. Other segments use the state colors. Power's reactor/battery/solar choices are under **Power sections**. Save display keeps these settings.

**Health bars > Custom bars** creates up to 16 bars for a named terminal block or an existing main-grid terminal group. Find selections by name; only currently present blocks matching the saved baseline become members. Damage and repairs update automatically: destroyed/replaced blocks stay red, and repaired blocks return healthy even if the terminal group disappears. Use **Update tracked blocks** only after adding/removing blocks in a terminal group and wanting this bar to follow the changed list. It requires confirmation and replaces the tracked list with the group's current matching baseline blocks; former members absent from that list stop counting.

Custom-bar edits preview immediately and use **Save bar**, independently from Save display. Leaving the editor discards unsaved edits. Members, names and layouts belong to the ship and its saved baseline versions, not shared display presets/clipboard layouts. Recovery and changing the main cockpit preserve the coordinate alignment. A new capture/import starts new custom-bar selections, while the previous ones remain in the archived baseline.


## Version controls and remembered matching

Baselines > Saved versions: **Save new version** creates a named copy of the active baseline. **Rename version** uses the name input to rename the selected copy without changing its blocks. **Replace version** copies the active baseline into the selected slot and preserves the overwritten copy in another slot. Use/rename/replace/delete require a second click. A full library refuses replacement before overwriting anything; remove an unwanted version first.

Remembered recovery can accept partial repairs when its remnant and expected-layout checks meet the 75% automatic threshold, with eight exact matches in each comparison. A changed cockpit type can match on surrounding structure. Ambiguity, auto-off, manual-detach and stable-observation safeguards remain. The original baseline and missing-block denominator never shrink to fit the remnant.

The renderer shows block envelopes rather than model meshes. Coarse detail may merge small gaps; outer extents are clamped to the actual hull. Docked-grid styles share a 768-line hull budget per grid. Damage markers retain state colors. See [blueprint formats](BLUEPRINT-FORMATS.md) for XML support and PB/archive limitations.


## Data location

Profiles, display preferences, shortcuts, presets, revisions, recovery data and backups all use `<MyFileSystem.UserDataPath>/Storage/Marigold`. The default is `%APPDATA%/SpaceEngineers/Storage/Marigold`; custom `-appdata` locations are honored. Local blueprint search uses the active game's `Blueprints/local` folder.

There is no migration or fallback to another data location. Marigold starts fresh when its active folder is absent. Existing files elsewhere are ignored.

Routine schematic rebuilds do not write snapshot/worker lines to the game log. `/mg stats` still reports timings when requested, and failures remain logged.
