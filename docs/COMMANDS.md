# Marigold commands

All Marigold commands are handled locally. Use `/mg` or `/marigold`. Space Engineers uses `/m` for whispers. This guide covers the PluginHub 0.17.15 Legacy release and the 0.17.16 development build. The [station access rules](#station-mode-and-automatic-association) apply to 0.17.16.

## Start and save a baseline

Sit in the cockpit you want as the alignment anchor. Run `/mg capture` to create a profile and record the blocks currently present as a full-health baseline. If you already have a profile, capture preserves its anchor and archives its previous baseline in **Saved versions** before replacing it. Use **Baselines → Recovery tools → Set main cockpit**, or `/mg anchor`, to change the anchor without recapturing. See [Data location](#data-location) for profile files and backups.

Automatic matching is enabled by default. After reconnecting or transferring worlds, control the ship and allow a few seconds. If you are sitting in a cockpit, Marigold checks that one. Otherwise it considers up to 16 cockpits. Automatic selection needs at least eight blocks, 75% structural overlap, stable observations, and a lead of at least eight percentage points over the next candidate. Marigold announces the selected profile. If it cannot choose, sit in the corresponding cockpit, run `/mg profiles` or `/mg profile suggest`, then `/mg profile load NUMBER`. Identical builds can tie or match the same saved layout across sessions. Matching uses the ship's blocks and cockpit layout and is independent of the ship's name and Nexus identity.

`/mg clear` removes the selected baseline but keeps its profile/anchor. `/mg profile detach` unbinds the live ship and suppresses automatic selection for it until session unload or `/mg auto retry`. `/mg profile delete ID` deletes that saved profile. `/mg recover` explicitly restores the database backup after a load problem; it clears live bindings so you can reselect.

## Import a blueprint baseline

1. Sit in the intended live anchor cockpit. A prior profile or capture is optional.

2. `/mg blueprint load "C:\full path\bp.sbc"`. Wait for the load to finish.

3. `/mg blueprint grids`, then `/mg blueprint grid N` for the intended grid. Wait while Marigold prepares its block definitions.

4. `/mg blueprint anchors`, then `/mg blueprint preview N` for the matching cockpit.

5. Inspect top, bottom and reset views and `/mg report`. The preview has a purple reference outline and an explicit unsaved caption.

6. `/mg blueprint accept` saves the intended layout. Use `/mg blueprint cancel`, **Escape**, or **Close** to discard the draft and keep the previous baseline. The prepared grid remains available for another preview.

Import uses installed block definitions for multi-cell bounds and cockpit orientation. The blueprint cockpit must match the profile's anchor type, or your occupied cockpit type for a new profile. Grid sizes must match. Unavailable mod definitions are skipped with an explicit count. Marigold imports the selected grid as a full-health reference, without spawning blocks or applying scripts, inventories, or ownership. See [Blueprint formats](BLUEPRINT-FORMATS.md) for supported files and search sources.

## Display and diagnostics

| Command | Result |
| --- | --- |
| `/mg show`, `hide` | Show or hide all Marigold HUD elements, including health bars. Automatic matching continues. |
| `/mg spin`, `reset` | Toggle between rotation and a fixed isometric view, or restore the default rotating view and zoom |
| `/mg left`, `right`, `top`, `bottom` | Snap relative to the controller you are using. The saved anchor still aligns the baseline. |
| `/mg zoom 0.25..3`, `in`, `out` | Set or change view zoom |
| `/mg mode compare` | Comparison is always enabled; old live/reference commands also resolve to compare |
| `/mg isolate CATEGORY` | Select a system, retaining whole-ship scale |
| `/mg filter STATE` | Limit visible markers while report totals still cover the entire grid |
| `/mg page next`, `prev`, `N` | Browse up to 128 detailed markers per page, highest severity first. Whole-ship fault indicators remain visible. |
| `/mg layout compact`, `expanded` | Small side panel or larger centered panel |
| `/mg detail low`, `medium`, `high` | Outline geometry budget |
| `/mg report` | Baseline status, all change counts and visible marker page |
| `/mg stats` | Geometry and HUD CPU timings, plus renderer, preview size and update rate for Textured Ship |
| `/mg settings save`, `load`, `reset` | Explicitly save/reload preferences, or reset in-memory defaults |
| `/mg help display`, `profiles`, `blueprint` | Topic-specific help |

Categories: `all armor conveyors thrusters weapons power tanks production cargo control tools other`. Category labels use consistent title case in the UI; `weapons` is displayed as Offensive and `other` as Utility. Gas tanks and generators appear under Tanks. Mechanical blocks are grouped under Utility, not as a separate filter. Unusual/modded definitions may also fall back to Utility.

States: `all faults missing damaged broken added replaced unknown`. `broken` means structurally non-functional; `faults` includes missing, damaged, broken and unknown. Power loss and a disabled switch do not count as structural damage.

Yellow is faint damage, orange is non-functional, pulsing red is absent from the observed grid, green is added, purple is replaced, and gray is unreadable integrity data. Reference/preview data never changes the game world.

To keep the bars and hide only the model, turn off **Ship display → Show ship schematic**. Use **Ship display → Rendering mode** to select **HD Wireframe**, **SD Wireframe**, **Outlines only**, or **Textured Ship**. Wireframe displays block bounds, while Textured Ship uses available models, paint, and textures with simpler shapes where needed. Under **Preferences → Performance → Textured Ship renderer**, choose **CPU** if the experimental GPU view cannot draw your ship. Your renderer choice saves for all ships. If a GPU frame is still loading or unavailable, the wireframe and damage marks remain visible.

Display preferences live in `display.txt` under the [Marigold data folder](#data-location). Saving retains the previous file as `.bak`. Marigold logs unreadable preferences and uses defaults at startup. View angle, spin, and marker page reset each launch.

## Station mode and automatic association

`/mg station on|off|status` controls docked context for the current profile. Station mode is off by default and can be enabled from an eligible static grid.

For each grid, Marigold counts blocks that support ownership, including unowned blocks. The grid qualifies if you own more than half of those blocks or if more than half are owned by members of your faction and set to **Share with my faction**. Each option needs its own majority. Armor without an ownership setting is outside the count. Private blocks owned by other players and blocks set to **Share with all** do not count as faction shared.

Marigold follows locked connectors only through eligible grids. If a grid loses access, it disappears along with any grids reachable only through it. Rotor, hinge and piston links are excluded. Docked grids can be mobile. The display covers up to eight additional grids and 100,000 docked blocks.

Docked ships can use their own matched baselines for damage outlines and separate report counts. Ordinary undocking preserves a ship's match for eligible redocking in the same session. An unmatched ship appears as context. Capture covers only the controlled grid.

`/mg auto on|off|status|retry` controls automatic profile reassociation. ON is the default. Matching is retried while an unbound controlled grid loads, including after world reload/login. It never captures, overwrites or deletes a baseline, never overrides an existing selection, and refuses to reuse a profile still bound to another live grid. Low-confidence, ambiguous, missing-cockpit and heavily altered ships remain manual. `retry` clears this ship's manual-detach suppression; it does not turn a disabled matcher on or replace a current binding.

Station changes save to the selected ship profile, or remain session-local until capture creates one. Auto and language changes save display preferences. Station is never restored from global display settings. `/mg auto off` persists and retains an existing binding.

## Graphical interface

Use `/mg ui` for **Ship**, **Baselines**, **Ship display**, **Health bars**, **Preferences**, and **Save & share**. Switch to **Advanced** on a section for more controls. **Preview HUD** shows the layout without discarding editor fields or a custom-bar draft. **Return to editor** resumes editing. Each saved ship has a profile number. The chat commands also accept the profile's GUID, and `profile select` is an alias for `profile load`.

To browse blueprints, open **Baselines → Import blueprint → Find blueprint**. Select any combination of Local, Workshop, and Cloud, enter part of a name, and select a search result to load it. Prepare the target grid, choose the corresponding cockpit, and preview before choosing **Use baseline**. To locate a specific file, enter its full path under **Import blueprint** instead.

Capture/clear/delete/recover and blueprint acceptance require a second identical panel click. Chat commands execute directly. Ship changes close the panel.

**Preferences → Keyboard shortcuts** lets you assign shortcuts for cycling systems or block states, toggling the expanded layout, showing or hiding the HUD, and opening the panel. Record a combination or type a binding such as `Ctrl+Shift+F6`. Escape cancels recording. **Remove shortcut** or a blank binding clears it. Bindings start empty and save immediately in `hotkeys.txt`. The game still receives these keys, so choose unused combinations. Marigold ignores shortcuts while chat or an interactive menu is open.

`/mg language en|es` changes UI label language. Controls, headings and menu descriptions are translated; dynamic diagnostic messages may remain English.

## Recovery, versions and customization

`/mg profile force NUMBER` searches surviving blocks for an alignment when the original cockpit is gone. Wait for a preview, then use `/mg recovery next`, `/mg recovery accept`, or `/mg recovery cancel`. In **Recovery tools → Preview recovery**, drag the model or scroll to inspect it. The X/Y/Z buttons move the baseline by one cell or rotate it by 90 degrees around the baseline origin. If the search finds no alignment, the preview gives you an unverified starting point. Acceptance requires three exact matches and saves the alignment without recapturing. Closing an unaccepted preview discards its changes. Automatic matching can reuse the latest accepted alignment for that baseline when the remnant is still recognizable.

`/mg isolate thrusters,power,tanks` combines categories; legacy `gas` settings/commands migrate to `tanks`, and `mechanical` migrates to `other`. `none` hides all. Systems offers hidden/wireframe/faint/highlighted hull styles. Damage markers retain state colors; station hulls use the same category visibility and brightness styles.

Focusing on selected systems shows the others as faint outlines. **Ship > Systems and filters > Other systems opacity** adjusts their visibility. Set it to zero to hide them.

**Arrange HUD** supports dragging, corner resizing, and typed percentages for position, size, background dimensions, and opacity. Enable **Scale background with ship** to link background size to **Ship scale**. **Preferences** contains the missing-block and fresh-damage pulse toggles. In **Save & share**, use **Save as my default**, **Save for this ship**, named layouts, or clipboard import/export. Loading a layout preserves language, automatic matching, and station settings. In Arrange HUD, **Apply layout** saves custom bars with the ship profile. Layout names support up to 40 letters, digits, spaces, hyphens, or underscores.

In **Ship display > State colors**, **Choose color visually...** opens a hue strip and a saturation/brightness square for the selected state. The swatches show the draft and original colors. **Set RGB** updates the picker from an exact six-digit value; **Use color** applies the selection, while Back/Cancel discards it. **Save display** keeps an applied color. The same picker is available under **Health bars > Bar colors**; applying an independent bar color disables outline matching.

**Baselines → Saved versions** holds independent named copies and shows their source and date. Capture, import, or loading a baseline archives the previous reference when needed. Restoring a version with a different anchor opens a recovery preview before changing the active baseline. Each ship's version library supports up to 16 revisions and 250,000 blocks in total.


## Health bars

**Health bars** offers independent bars for Overall, Armor, Power, Thrusters, Utility, Cargo, Offensive, and Tools. They are off by default. Turn them on one at a time, choose which labels to show, and use **Arrange HUD** to position or resize them. Default, per-ship, and named layouts include system bar layouts. Older layouts keep their existing bars and leave newer bars off.

Bars count baseline block states: healthy blue, damaged yellow, non-functional orange, missing/replaced expected blocks red, and unreadable gray. This is not summed integrity. New blocks are excluded; replacements retain the original system category. Tools includes welders, grinders, drills and ToolCore blocks; Utility covers categories without a dedicated bar. Filters/pagination and docked-grid context do not affect these controlled-grid totals. Preview baselines are labeled. No baseline or an empty system is shown as unavailable, never 100% healthy. Global Show/hide and normal HUD suppression also hide the bars.

**Health bars → Bar colors** changes the healthy segment color or links it to the healthy schematic outline. Other segments use the state colors. Select **Power** to give reactors, batteries, or solar panels their own sections. Unchecked sources stay in **Other**, so every expected power block still counts. Save the display to keep these settings.

**Health bars > Custom bars** creates up to 16 bars for a named terminal block or an existing main-grid terminal group. Find selections by name; only currently present blocks matching the saved baseline become members. Damage and repairs update automatically: destroyed/replaced blocks stay red, and repaired blocks return healthy even if the terminal group disappears. Use **Update tracked blocks** only after adding/removing blocks in a terminal group and wanting this bar to follow the changed list. It requires confirmation and replaces the tracked list with the group's current matching baseline blocks; former members absent from that list stop counting.

Custom-bar edits preview immediately and use **Save bar**, independently from Save display. Leaving the editor discards unsaved edits. Members, names and layouts belong to the ship and its saved baseline versions, not shared display presets/clipboard layouts. Recovery and changing the main cockpit preserve the coordinate alignment. A new capture/import starts new custom-bar selections, while the previous ones remain in the archived baseline.


## Version controls and remembered matching

Baselines > Saved versions: **Save new version** creates a named copy of the active baseline. **Rename version** uses the name input to rename the selected copy without changing its blocks. **Replace version** copies the active baseline into the selected slot and preserves the overwritten copy in another slot. Use/rename/replace/delete require a second click. A full library refuses replacement before overwriting anything; remove an unwanted version first.

Remembered recovery can accept partial repairs when its remnant and expected-layout checks meet the 75% automatic threshold, with eight exact matches in each comparison. A changed cockpit type can match on surrounding structure. Ambiguity, auto-off, manual-detach and stable-observation safeguards remain. The original baseline and missing-block denominator never shrink to fit the remnant.

Coarse wireframe detail may merge small gaps. Outer extents still follow the actual hull. Docked grid outlines have a 768-line hull budget per grid, and their damage markers retain the state colors.


## Data location

Profiles, display preferences, shortcuts, presets, revisions, recovery data and backups all use `<game user data>/Storage/Marigold`. The Windows default is `%APPDATA%/SpaceEngineers/Storage/Marigold`. A custom `-appdata` location changes the game data root. Local blueprint search uses the active game's `Blueprints/local` folder. Profiles are stored in `profiles.bin` with a previous-version backup.

There is no migration or fallback to another data location. Marigold starts fresh when its active folder is absent. Existing files elsewhere are ignored.

`/mg stats` posts timings in chat and writes them to the game log. Failures are also logged. See [the performance test](PERFORMANCE-TEST.md) for how to read the numbers.
