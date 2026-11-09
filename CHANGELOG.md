# Changelog

Changes to Marigold, newest first. Early entries were reconstructed from development notes and testing records. Some changes appeared in local builds before publication.

The **0.17.4** beta is on `dev`. The published PluginHub release is **0.16.1 for Pulsar Legacy**.

## 0.17.5 — Unreleased

- Added Pulsar Interim (.NET 10) support. The PluginHub manifest and `prepare-release.ps1` now declare `CLR;CoreCLR`, so one hub entry serves Legacy and Interim. No code changes were needed: Marigold's Harmony patch (`MyGuiScreenHudSpace.Draw` postfix) targets a large virtual override that the .NET 10 JIT cannot inline, the text clipboard uses the game's STA-thread service, which Interim's Windows Desktop runtime supports, and profile and settings files are shared between editions. A local 0.16.1 `net10.0` build passed the [Interim checks](TESTING-TODO.md#completed-pulsar-interim-checks--0161) in game. The 0.17 Interim smoke test under Pending beta checks is still open.
- **HD Wireframe** reuses the last image while the view and damage indicators are still. Rotating, changing the layout or animating damage still refreshes the image.
- **Textured Ship** transforms only the vertices used by the preview. It also checks model data before allocating preview meshes and falls back to painted shapes when a model cannot be read safely.
- **Arrange HUD** updates its preview even when the regular HUD is hidden. Long ship labels shift left as needed to fit along the right edge.

## 0.17.4 — November 6, 2026

- Arrange HUD uses the ship background to set the selection box and how far the display can move. A shorter background can sit lower at the same ship scale.
- The ship label follows the background's lower edge, including when the background is shorter than the model viewport.
- **Simple label** shortens the Power bar's heading and keeps the enabled section captions.
- **Standard** offers quick system selection and Hide or Dim controls. **Advanced** lets you combine systems, set exact background opacity and style individual systems. Exact RGB entry is in the color picker.

## 0.17.3 — November 6, 2026

- The HUD menu groups related controls. **Standard** shows everyday actions, and **Advanced** reveals fine rotation, rendering detail, system styles and diagnostics.
- **Arrange HUD** accepts typed percentages alongside sliders. Adjust the schematic background width and height independently of ship scale.
- Health bars can show full status text, just the title, or no label. **Apply layout** saves custom bar changes directly with the ship profile so they can carry across reseating and server transfers.
- **Ship display** lists **Textured Ship** above **HD Wireframe**, which is still the default. The two fault-only rendering modes are removed from the menu. Saved fault modes load as HD Wireframe or Outlines only.

## 0.17.2 — November 5, 2026

- **Save & share** separates **Save as my default** from **Save for this ship**. Ship layouts keep custom bar positions and sizes.
- **Use on reconnect** lets you choose which layout the ship loads. It applies the choice immediately and remembers it for that ship. **Reload saved layout** restores the selected layout, and **Load my default now** lets you try the default temporarily.
- **Named layouts** lists your saved presets in a dropdown. **Copy or paste layout** has its own clipboard page.
- Changing language or automatic matching preserves your saved default layout.
- **Textured Ship** now follows system and damage-state filters. **Other systems opacity** dims or hides unselected systems. Missing blocks still show red markers.

The main Legacy beta checks passed in game. [Large-grid, sustained-damage and Interim checks](TESTING-TODO.md#pending-beta-checks) are still open.

## 0.17.1 — November 5, 2026

- **Textured Ship** shares its triangle budget across the grid and uses simpler textured meshes for dense parts.
- Rotating previews render faster by sharing vertices across faces. The preview reuses its mesh when the ship's render data is unchanged.
- `/mg stats` reports preview FPS separately from mesh build and raster times.

The Legacy station retest reached **768 px at 16.5 preview FPS** with zero reported painted box replacements. [Testing checklist](TESTING-TODO.md#display-follow-up)

## 0.17.0 — November 5, 2026

- Use **Arrange HUD** to drag the ship display, system bars, and custom bars into place. Select an element to adjust its size, opacity, or visibility in the live preview. Drag a bar's corner to resize it directly.
- Choose **Apply layout** to use your edits or **Cancel** to discard them. Use **Save display** to keep the layout and **Save for this ship** to keep custom bar edits.
- **HD Wireframe** is the new default, with sharper outlines and fill edges during rotation. **SD Wireframe** offers the original display.
- **Textured Ship** uses the ship's models, paint, and available textures, including modded Expanse drives. Damage behind the hull stays visible at lower intensity. The opacity slider fades the ship and damage fills together while keeping the outlines visible.
- Turn off **Show ship label** to hide the text below the schematic. With the label hidden, the display can sit flush with the bottom edge in both compact and expanded views.
- The Pulsar plugin list shows **Marigold**. The version appears inside the control panel.

## 0.16.1 — October 28, 2026

- Fixed connector undocking briefly marking an unrelated live block as missing. Genuine removals still update immediately, and stale notifications cannot erase a replacement block.
- Fixed the Spanish control panel failing to open for short bracketed names such as `[ ]`.

## 0.16.0 — October 28, 2026

- Rebuilt the control panel around six persistent sections: Ship, Baselines, Ship display, Health bars, Preferences and Save & share.
- Replaced dense setting/value menus with labeled forms, grouped actions and scrolling pages. Longer help text wraps, and empty lists explain the next step.
- Added a full HUD preview with **Return to editor**, preserving the editor's fields and custom-bar draft. Ordinary editing pages keep the ship preview away from form controls.
- Made save behavior clearer for display settings, custom bars, baselines and shortcuts. System selection now uses checkboxes, and baseline/version actions have clearer instructions.
- Expanded Spanish labels and help text. Existing profiles, commands, damage tracking and saved data formats are retained.

## 0.15.1

- Added a visual color picker for schematic states and healthy health bars. Choose a color with the hue strip and color square, compare it with the original, or enter an exact RGB value.
- Fixed the custom-bar search box overlapping the **Find** button.
- Renamed the group action to **Update tracked blocks**. Damage and repairs update automatically; this action only changes which group members the bar tracks.

## 0.15.0

- Keep other systems visible as faint outlines when focusing on a system. Their opacity is adjustable, including fully hidden; fault-only modes remain available.
- Make fresh orange/red alerts easier to distinguish using synchronized brightness changes within their own colors. Yellow stays steady, and the ongoing missing pulse remains gentler.
- Choose the healthy health-bar color or match it to the schematic's healthy outline.
- Add custom health bars for one named block or an existing terminal group. Saved membership keeps destroyed blocks counted until repair or an explicit group refresh.
- Save custom-bar names, visibility, layout and membership with ship profiles and baseline revisions, preserving alignment through recovery.
- Label ambiguous controls and give Power sections their own page, removing overlap with the status area.
- Reformat the source, tests and build files for readability without changing existing behavior.

## 0.14.16

- Uses only the game's selected user-data folder for all Marigold data. Removed automatic migration and old-location detection; leftover files elsewhere are ignored.
- Simplified the review-fix test checklist to ordinary launch, saving/restart, Local blueprint search and logging checks.

## 0.14.15

- Moved profiles, preferences, shortcuts, presets, versions and recovery data under the game's selected user-data folder, honoring custom `-appdata` locations.
- Local blueprint search now follows the game's own local blueprint folder.
- Initially included automatic import for existing testers; that development behavior was removed in 0.14.16.
- Removed routine snapshot/worker rebuild messages from the game log. Requested `/mg stats` output and error logging remain available.
- Removed the redundant Harmony dependency from the Pulsar descriptor; local builds retain their compile-time dependency.
- Added the MIT license.

## 0.14.14

- Combined overlapping translucent fills of the same damage state to reduce repeated drawing, while retaining separate yellow, orange and red coverage.
- Kept pulsing and steady regions distinct. Fills may look lighter and tiny gaps may merge; outlines are unchanged.
- Added before/after fill counts to `/mg stats`. User testing found no noticeable FPS improvement, so further fine-tuning is deferred.

## 0.14.13

- New ship profiles now reuse the lowest available number. Deleting all profiles makes the next one number 1.
- Existing profiles keep their numbers and identities; reusing a number does not reuse the deleted ship's saved data.

## 0.14.12

- Automatic matching now prefers the cockpit you occupy and no longer waits for block health to stop changing. Structural changes still require stable observations.
- Corrected outline modes to use merged fault contours without translucent marker fills.
- Moved Performance options into **Customize > Preferences**, alongside missing-block and fresh-damage pulse toggles.
- Added submitted fill counts to `/mg stats`.

## 0.14.11

- Lowered the automatic matching minimum to 75% layout overlap, retaining checks for ambiguous matches and stable observations.
- Added Full display, Outlines only, Faults with fills and Fault outlines only rendering modes, saved with display settings.

## 0.14.10

- Made High detail the default for new/reset displays, while preserving saved preferences.
- Removed redundant damage outlines and duplicate shared edges, and reduced repeated rotation/pulse calculations.
- Added separate geometry and rendered-line counts to `/mg stats`.

## 0.14.9

- Added experimental Interim builds alongside Legacy builds from the same source. Interim support is not included in the initial PluginHub release.
- Corrected launcher runtime labels so Legacy lists the plugin correctly.
- Improved recovery-button sizing and spacing, fixing overlapping controls and difficult clicks.
- Added installation guidance for Windows-blocked downloaded DLLs and a repeatable performance-test procedure.

## 0.14.8

- Stopped yellow damage from pulsing and made its outlines and fill fainter, keeping orange and red easier to notice.

## 0.14.7

- Added an optional four-second alert pulse for newly damaged blocks, with a shared rhythm across the display.
- Made fresh alerts more vivid than the normal missing-block pulse. Continuous hits finish a pulse window before another hit can retrigger it.
- Kept loading an already damaged ship from producing false fresh alerts. Yellow pulsing was removed in 0.14.8.

## 0.14.6

- Added optional Reactors, Batteries and Solar sections within the Power health bar.
- Unselected power types stay in Other; all baseline power blocks still count. Choices save with layouts, presets and clipboard configurations.

## 0.14.5

- Kept yellow, orange and red fills visible across every marker page during heavy damage.
- Dense same-state regions merge instead of dropping later blocks from the highlights.

## 0.14.4

- Fixed growing missing-block counts hiding damaged and non-functional outlines during sustained combat.
- Made whole-ship fault outlines independent of marker pages. Fill coverage was extended in 0.14.5.

## 0.14.3

- Closing, backing out of or escaping recovery now ends an unaccepted preview and removes its purple ghost boxes.
- Added mouse-wheel zoom over the recovery image, affecting only the inspection camera.

## 0.14.2

- Added click-and-drag rotation to the recovery preview, allowing inspection without changing baseline alignment.

## 0.14.1

- Enlarged and centered the recovery preview above compact controls.
- Kept recovery inspection separate from the normal HUD's camera, size and saved layout.

## 0.14.0

- Improved remembered recovery matching after partial repairs and cockpit-type changes.
- Added manual 90-degree baseline rotations around X/Y/Z and an unverified manual starting point when no alignment is found.
- Added direct version renaming and clearer Save new version / Replace version actions, preserving previous contents when replacing.
- Applied system brightness/styles consistently to connector-docked context and corrected oversized coarse schematic bounds.
- Improved Spanish labels and clarified unsupported blueprint formats and their XML alternative.

## 0.13.0

- Saved accepted recovery alignments for later reconnect matching.
- Added baseline revision history, automatic preservation before key baseline changes, and source/date information.
- Added previews for restoring versions with a different cockpit anchor.
- Introduced profile format 3. Rolling back to 0.12.6 requires restoring both the older plugin and a complete pre-upgrade data backup.

## 0.12.6

- Improved recognition of modded tools, including ToolCore registrations and tools using conveyor-sorter definitions.
- Fixed classification precedence and cleanup when changing worlds.

## 0.12.5

- Recognized modded welders and grinders using generic CubeBlock definitions, including SDX2 Deep Welder variants.

## 0.12.4

- Added a separate Tools health bar for drills, welders, grinders and supported ToolCore blocks.
- Kept existing health-bar layouts when adding the new hidden-by-default bar.

## 0.12.3

- Standardized visible system names, including Offensive and Tanks.
- Grouped mechanical blocks under Utility/Other and migrated older filters/styles.

## 0.12.2

- Reworked blueprint browsing around readable blueprint titles and searchable Local, Workshop-cache and Cloud sources.
- Fixed result pagination, incorrect selection behavior and empty-search handling.
- Refreshed the saved-ship list immediately after deletion.

## 0.12.1

- Added independent schematic show/hide and sizing, allowing health bars to remain visible.
- Added clipboard configuration export/import for layouts, bars, colors and filters.
- Corrected remaining visible Weapons labels to Offensive.

## 0.12.0

- Reorganized `/mg ui` into Ship, Baseline and Customize sections with clearer navigation and page titles.
- Separated ship names from version names and clarified explicit display saving.
- Streamlined blueprint, recovery, customization and shortcut controls.

## 0.11.2

- Added WeaponCore-based detection of supported modded Offensive blocks, including PDCs, railguns and launchers.
- Kept schematic filters and health-bar classification consistent across reconnects and world changes.

## 0.11.1

- Added the Offensive health bar, separate from Utility.
- Displayed selected system/state filters beside the ship name and improved blueprint labels and control spacing.

## 0.11.0

- Added optional Overall, Armor, Power, Thrusters, Utility and Cargo health bars, hidden by default.
- Added individual positioning, sizing, opacity and labels.
- Counted baseline blocks by state, retaining missing blocks in the total and allowing repairs to return them to healthy.

## 0.10.0

- Added wreck recovery from surviving structure, with alignment previews and manual translation.
- Added HUD appearance controls, fine rotation, combined system filters and outline styles.
- Added per-ship layouts, named display presets and named baseline versions.
- Improved shortcuts, blueprint omission reporting and damage diagnostics.

## 0.9.0

- Made capture create the cockpit anchor and baseline together, treating captured blocks as fully repaired for comparison.
- Added short profile numbers and improved clear/load behavior. Numbers originally increased without reuse; 0.14.13 changes that.
- Kept missing-section outlines visible beyond the individual marker page and followed the controlled cockpit after grid splits.
- Improved saved station settings and matched docked-grid comparisons.

## 0.8.0

- Added the native `/mg ui` control panel and local blueprint picker.
- Spread block observation over updates and prioritized changed blocks to reduce repeated full scans.
- Improved user documentation and optional shortcut controls.

## 0.7.0

- Added optional connector-docked station context, off by default.
- Added conservative automatic saved-profile matching across reconnects and world/server transfers.

## 0.6.0

- Added XML-blueprint baseline import with grid/cockpit selection and accept/cancel previews.
- Added system/state filters, marker pages, expanded display and saved preferences.
- Added profile suggestions and diagnostic display modes used by early builds.

## 0.5.0

- Added persistent ship profiles and saved cockpit anchors.
- Kept baselines across sessions and added profile selection, deletion and backup recovery.

## 0.4.10

- Fixed the remaining left/right mirroring in the default/reset rotating view without changing the working plan views.

## 0.4.9

- Adjusted the default/reset starting angle to address horizontal alignment. The complete rotating-view correction followed in 0.4.10.

## 0.4.8

- Corrected left/right orientation in top and bottom views while keeping the ship's nose up.

## 0.4.7

- Corrected inverted top/bottom view presets and their aliases.

## 0.4.6

- Made any block absent from the baseline comparison red and missing, including detached sections and multiplayer observations.
- Reserved gray for unavailable or unreadable health data.

## 0.4.5

- Muted yellow outlines and increased fill transparency to make orange and red stand out.
- Clarified missing/unknown reporting; missing-state rules were refined in 0.4.6.

## 0.4.4

- Established yellow for damage, orange for structurally non-functional blocks, and gently pulsing red for missing blocks.
- Distinguished structural failure from a block simply being switched off or unpowered.

## 0.4.3

- Standardized commands on `/mg` and `/marigold` after the short `/m` alias conflicted with built-in chat behavior.
- Rewrote the plugin description to be easier to understand.

## 0.4.2

- Added translucent shading inside damaged block envelopes, behind the outlines.

## 0.4.0–0.4.1

- Introduced baseline capture and live damage comparison, with capture/clear/report commands.
- Early baselines lasted only for the session; persistence arrived in 0.5.0.
- Surviving records do not identify a separate user-facing change for 0.4.1.

## 0.3.1

- Removed the old status notification and simplified the schematic caption to the ship/grid name.

## 0.3.0

- Replaced the initial wireframe with a hull-envelope outline that merges coplanar seams and connected edges.
- Added adaptive outline detail for larger ships.

## 0.2.2

- Added cockpit-relative snap views for left, right, top and bottom. Reset restored the rotating view.

## 0.2.1

- Replaced conflicting prototype keybinds with chat controls for visibility, spin, views and zoom.

## 0.2.0

- Added the first rotating ship schematic and seamless switching between controlled grids.

## 0.1.0

- Added the initial controlled-grid HUD showing the ship name and block count.
- Updated the name immediately when the grid was renamed.

---

Detailed development and testing records are preserved in [history](docs/history/README.md). See the [user guide](README.md) for current behavior and [roadmap](ROADMAP.md) for remaining release steps.
