# Marigold testing to-do

Use this file as your testing log. Check boxes only after testing in-game; add notes below each section. Automated checks are recorded separately and do not mark these complete. Future updates will append new sections rather than overwrite your notes.

## 0.5.0 — Persistent profiles and cockpit anchors (not yet tested)

- [x do we need two commands? cant we just use /mg capture to do both?] Sit in the intended cockpit, run `/mg anchor`, then `/mg capture`; note the profile ID.
- [didnt have to run profiles. automatically selected the previous profile and showed the missing/damaged blocks. profile select should be profile load.] Change one block, leave/reload the world, sit in the same cockpit, run `/mg profiles` and `/mg profile select ID`. The original damage/missing state returns.
- [x] Switch seats and rename the ship. The saved baseline remains aligned; viewing direction follows the active seat.
- [x] Create profiles for two similarly named ships. Selecting one never silently attaches it to another live ship.
- [did mg clear and the base profile was gone. ] `/mg clear` clears only the baseline; anchor/profile remain. `/mg profile detach` leaves saved data intact.
- [we need to make profile id's start at 1 and incriment up. 8 is too much to type.] In a disposable test profile, `/mg profile delete ID` removes only that profile.
- [ran anchor then capture, disconnected, reconnected, got in the seat. after a second it auto loaded the corect profile. alignment was good.] After reconnect or Nexus transfer, explicitly select the profile from the corresponding cockpit and check alignment. Automatic Nexus matching is not validated.

Notes:

## Regression checks for every installed build

- [x] Default/reset, top/bottom and side views place known asymmetric blocks on the expected side.
- [x] Yellow damage is faint; orange non-functional blocks stand out; missing positions pulse red.
- [x] Weld a damaged block back to the baseline and check the marker clears.
- [x works. Cant we adjust the /mg spin to default to an isometric static view when spin is off? something like 45 degrees off to the side top left viewpoint of the model or something? still centered of course.] `/mg hide`, show, spin, zoom and ship switching still work.
- [worked good. snapshot .21 - .24 ms. worker .42 on smaller grid. 7.12ms on larger grid. ] Compare `/mg stats` on a small ship and a large ship. Record snapshot, worker and HUD CPU times; check for visible stutter.

Notes:

## 0.6 — Blueprint baseline import (pending in-game testing)

- [I dont want to only load local blueprints. I want to be able to select cloud blueprints as well from a UI, not from the chat.] With a profile selected, run `/mg blueprint load "C:\full path\bp.sbc"`. The file must remain unchanged.
- [ ] Run `/mg blueprint grids`, then `/mg blueprint grid N`. Run `/mg blueprint anchors` and select the matching cockpit with `/mg blueprint preview N`.
- [ ] Inspect top, bottom and default views. The caption says blueprint preview; `/mg report` describes differences. Preview must not replace the saved baseline.
- [ ] Run `/mg blueprint cancel`; the previous baseline returns. Repeat preview and use `/mg blueprint accept`; the blueprint baseline survives reload/profile selection.
- [ ] Try a rotated, multi-cell cockpit and a blueprint with several grids/cockpits; selection must be explicit and alignment correct.
- [ ] Try a blueprint whose mods are missing: import must fail with the missing definition, not save a partial reference.
- [ ] Switch ships or change profiles during a preview: no preview may leak onto the new ship/profile.
- [ ] Confirm blueprint scripts, inventories and ownership are not executed or applied to the world. This importer reads only layout metadata.

Notes:

## 0.6 � Filters, view modes and marker pages (pending)

- [live doesnt show any damage. Compare is the only view we need I think. this things entire purpose is for showing damage compared to a valid good state.] Compare `/mg mode live`, `reference`, and `compare` after removing and adding blocks. Live shows the current hull; reference shows the saved layout; compare shows change markers.
- [these all work. the chat commands are long to type tho. maybe addressed with a UI and manual keybinds to flip thru the isolations?] Use `/mg isolate tools`, `thrusters`, `power`, `control`, and `all`. Missing blocks must still appear in their original category. Modded types may fall back to `other`.
- [these all work. same issue as above tho. maybe better in a UI that has some keybinds to flip thru filters.] Use `/mg filter missing`, `damaged`, `broken`, `faults`, then `all`. Orange is structurally non-functional, not merely switched off or unpowered.
- [x] On a disposable ship with more than 128 changed blocks, use `/mg page next`, `prev`, and a large page number. `/mg report` shows the actual clamped page and full comparison totals.
- [x] Change filters, detail and modes while an update is running. Old geometry must not replace the new choice. Empty categories should show an empty panel and ship name.
- [works great. needs a keybind.] Try `/mg layout expanded` and `compact` at your display resolution. Compact retains only the ship name; expanded adds view/filter context.

Notes:

## 0.6 � Preferences, suggestions and observation reuse (pending)

- [x not sure if related but after setting save the settings did save, but the auto profile select didnt kick in. had to manually select a profile for the compare to start working] Set layout, zoom, mode, category, state filter and detail; run `/mg settings save`. Restart the game and verify they return. Orientation/spin and marker page deliberately start fresh.
- [x] Change those settings without saving, run `/mg settings load` and confirm the saved values return. `/mg settings reset` restores defaults; save again only if wanted.
- [x] If saved visibility was hidden, `/mg show` must reveal the display after restart.
- [this worked a TREAT.] From the original anchor cockpit, run `/mg profile suggest`. Check that damaged copies still score well; a different layout scores lower; identical ships can tie. No profile is selected automatically.
- [auto select seemed to work fine. suggest helped fill in the gaps.] Run suggestions after reconnect/Nexus transfer, explicitly select a profile, and check alignment before capturing. This remains a manual workflow, not verified automatic transfer support.
- [x] Watch `/mg stats` on an idle ship: unchanged samples should reuse the worker geometry. Damage, repair, block replacement/removal and functional threshold changes must still refresh it.
- [x] Hide the display, alter the ship, then show it. Comparison must catch up. Switch ships and exit/reload to check stale results are discarded.

Notes:

## 0.7 � Connector station view (pending)

- [worked like a treat!] With fresh settings, confirm station mode is OFF. Dock a second grid using locked connectors; it stays hidden until `/mg station on`.
- [worked but station couldnt tell that the docked ship was damaged even though I had a profile for it. also station mode should stay on for the grid I turned it on, and not persist when I switch cockpits from a station to a ship] With station mode ON, check the docked outline's position, orientation and scale. Include small-grid/large-grid connections and a connector mounted at an angle.
- [x] Connect a chain of ships and a cycle of connectors; each grid should appear once. Merely yellow/connectable connectors must not include a ship.
- [x] Unlock a connector: the detached branch disappears within roughly one second of active observation. A ship still linked through a second locked connector remains visible.
- [x] Capture or import a baseline while docked, then undock. Docked context must not become missing-red baseline blocks; report counts remain controlled-grid-only.
- [ it shows the rotor but not the subgrid. perfect.] Rotor/hinge/piston-only attachments remain excluded. A connector on such an excluded grid does not extend station traversal.
- [ ] `/mg station off` restores the single-grid framing. Restart and confirm the chosen station setting persists. Run `/mg station status` and `/mg report` for context/limit information.
- [ ] Test a large dock: up to 8 additional grids/100,000 context blocks are displayed; the report flags truncation. Record `/mg stats` and any stutter.

Notes: realizing. a profile capture might have an issue. if a block is damaged but present when a capture happens, the display shows no damage now. damaged blocks should still show up as damaged, capture should assume 100% block health as a baseline.

## 0.7 � Automatic reconnect/Nexus reassociation (pending)

- [x] Anchor and capture a distinctive ship with at least 8 blocks. Quit/rejoin the server, control that ship, wait several seconds: Marigold should announce the profile it automatically selected without a new capture.
- [x] Repeat after fully restarting the game and after a Nexus sector transfer. Verify the saved damage/missing state and cockpit orientation remain correct. This is the actual Nexus acceptance test, not yet established by compilation.
- [x] Rename the ship and use another cockpit. Matching searches cockpit candidates and must retain the correct saved anchor alignment; active-seat view direction may change normally.
- [x] Remove a small number of blocks before reconnecting. Matching should tolerate modest loss and retain the missing-red baseline positions. Large layout changes should leave it unbound for manual selection.
- [ ] Make two saved profiles of identical/near-identical ships: automatic matching must refuse the tie. `/mg auto status` explains it; `/mg profile select ID` resolves it.
- [x] Test delayed streaming after reconnect: the matcher waits/retries without taking a new baseline. Inspect `/mg auto status` if unbound.
- [x this feature is dope. the auto retry is really nice.] `/mg profile detach` must remain detached even after waiting or switching seats/ships in that session. `/mg auto retry` allows matching again. Detach suppression ends on session unload; use auto off for a persistent opt-out.
- [x] `/mg auto off`, restart/rejoin, and confirm nothing auto-binds. `/mg auto on` persists and resumes matching. Turning it off leaves an already selected profile attached; detach separately if desired.
- [x] Switch ships or select/delete a profile while matching is running; an old worker result must not override that action. A profile already attached to another live ship must not be automatically reused.
- [x] An unsaved identical copy can structurally match the same profile; this is layout recognition, not proof of identity. Check manual override behavior for your fleet and use auto off if copies need distinct identities.

Notes:

## 0.8 — Native controls and local blueprint picker (pending)

- [the render should be a small picture in picture on the edge of this screen so you can see what it does in real time] Open `/mg ui` from a cockpit. Visit Display, Profiles, Blueprints and Views. Escape/Close must restore normal game input; reopening must not duplicate screens.
- [x] Test 1080p, your usual resolution and UI scale. Check dropdown scrolling, long profile/blueprint names, status text wrapping and Spanish labels for clipping.
- [x] Display controls must match their chat equivalents: modes, categories, marker filters, detail, compact/expanded, station and auto. Save, change, then load settings.
- [ ] Switch English/Spanish and restart. Labels should persist; command responses/diagnostics intentionally remain English. Existing 0.5–0.7 preferences must load normally.
- [x] In Profiles, anchor/capture/select/detach a test ship. Capture, clear, delete, recovery and blueprint acceptance require a second identical click before changing saved data. Switching profiles/actions must not confirm the previous action.
- [able to load local blueprints but no folder navigation. I have nested local blueprints. also, I couldnt figure out how to apply it. it kept saying Select the blueprint agrid again to prepare it. Preview also did nothing. matching cockpip remained an empty drop down. report keeps saying no profile selected. how am I supposed to apply a blueprint as a profile? it also doent handle it well if the blueprint has blocks that the game doesnt have (like if the blueprint has modded blocks not in the current world) it should cut out the modded blocks and let you save a baseline or something. not very intuitive at all.] In Blueprints, Find local, select a blueprint or paste a path, Load, Refresh lists, choose/Prepare grid, Refresh lists, then select a cockpit and Preview. Close/reopen the panel to inspect and accept/cancel. Source blueprints remain unchanged.
- [marigold doesnt crash but it does give me errors in the chat when thigns go wrong. missing mods, etc.] Try an empty blueprint directory, invalid path, missing mods, spaces/non-English names and a blueprint still loading. Errors should stay in the panel/chat without disabling Marigold.
- [cant interact with the game with the panel open. have to close ti first.] Leave the cockpit, switch ships, disconnect or unload with the panel open. It should close cleanly; stale actions/previews must not affect another ship.
- [x] Use Views for left/right/top/bottom, zoom, rotation, reset and marker pages. Existing asymmetric orientation checks still pass.

Notes:

## 0.8 — Incremental observation and performance (pending)

- [x] Load a large ship. No partial initial scan should produce a flood of false missing markers; the model appears once the initial scan completes.
- [ crashed 2 ships into each other and alot of blocks just disappeared from the screen. theres alot fo data missing from my damage report here.] Damage, weld, remove and replace blocks rapidly. Event updates should refresh nearby changes; periodic sweeps must eventually catch changes not reported by events.
- [x] Repeat while hidden, then show again. Turn auto on/off too; check observation resumes and the reference remains intact.
- [x] Dock multiple large ships in station mode. Newly loaded context may take time to appear; undocking removes it and does not leave old subscriptions or outlines.
- [ ] Record `/mg stats` on approximately 1k, 10k and 50k-block ships, with station mode on/off. Watch actual frame smoothness during initial enumeration, captures, imports and automatic matching. Read budgets are not a guaranteed millisecond frame budget.
- [x] Repeat ship switches, world reloads and reconnects several times. Check logs for errors and watch for steadily increasing memory or CPU use.

Notes: when alot of damage happens at once, marigold seems to lose track of things. like if whole chunks get blown out of the ship they are just gone. marigold should remember the baseline and render it fully that things are missing.

## 0.8 — Release acceptance (pending; no PluginHub submission yet)

- [ ] Load the source version through Pulsar Legacy with the binary version disabled. Confirm version 0.8.0, Harmony dependency resolution and `/mg ui` work.
- [ ] Test the prepared pinned manifest against the intended commit. Keep source and binary installations mutually exclusive.
- [ ] Run the earlier remote-control, multiplayer, damaged/unfinished block, blueprint, persistence and Nexus checklists. Record actual pass/fail rather than assuming build success verifies runtime behavior.
- [ ] Review the new README as a first-time user: install, open controls, capture a baseline, import a blueprint, reconnect and disable automatic matching.
- [ ] Resolve runtime failures before submitting the prepared manifest to PluginHub. Hub acceptance and in-game validation remain release gates.

Notes:


## 0.9 — Follow-up to your October 22 testing (not yet tested in-game)

Your notes above are preserved. These checks cover the changes made in response.

- [x] With no profile selected, sit in a cockpit and `/mg capture`. It should create the anchor and baseline together. Capture a damaged/unfinished ship: damage should remain visible because the baseline assumes full health.
- [x] `/mg profiles` shows short numbers; `/mg profile load 1` loads the chosen ship. Existing profiles migrate without losing their layouts. Delete a profile, restart, and create another: its number should not reuse the deleted number.
- [x] `/mg clear` keeps the profile, cockpit anchor and number but removes its baseline. Capture again to restore comparison. Do not confuse Clear with Delete.
- [x] Disable spin from several angles. It should snap to the same centered isometric view. Reset should restore the familiar rotating orientation; verify left/right remains correct.
- [x] Save settings, restart and reconnect. Comparison should remain enabled and automatic association should still work. Old live/reference settings must not hide damage.
- [x worked like a charm this time] Crash/remove more than 128 blocks, including whole sections. Their entire missing footprint should stay red on every marker page (subject to filters and outline detail). Individual shaded markers remain paginated. Check totals with `/mg report`.
- [x] Split a ship so the piloted cockpit ends up on the smaller/new grid. Its saved baseline should follow, showing the other section as missing. Repeat with the cockpit remaining on the original grid.
- [x] Enable station mode for a saved station, switch to another ship, and restart. The station should remember its setting; other ships should default off.
- [x] Dock ships with uniquely matching saved profiles. They should show their own damage outlines and separate report counts. Damage them heavily after matching: their baseline should stay. Ambiguous/unmatched ships should remain context only. Rotor/piston/hinge-only subgrids remain excluded.
- [ worked but wouldnt let me set it to odd things like "'"] In Views, assign shortcuts for cycle-system, cycle-marker and expanded layout. Try Ctrl+Shift+F6/F7/F8 only if those combinations are free in your setup. Confirm save/restart, blank-to-unbind, duplicate rejection, and no activation while typing chat or using menus. The game still receives keys.
- [x] Browse nested local blueprint folders. Load a single-grid blueprint and verify the cockpit list fills automatically; multi-grid blueprints should prepare when selected.
- [x] Use Cloud with Steam cloud blueprints available. Select an XML blueprint, Load, pick its cockpit and Preview. Test a failed/offline load too. Protobuf-only and Workshop downloads are not supported by this browser yet.
- [x] On a ship without a profile, Preview a blueprint and Save profile. No capture or anchor command should be required first. Cancel and preview again without reloading.
- [x] Import a blueprint with unavailable mod blocks. It should report how many were omitted and allow the remaining layout, provided a matching cockpit exists. Review carefully before saving: omitted blocks are not part of the baseline.
- [x] Keep the panel open and change views/filters/zoom or preview a blueprint. The small schematic at the bottom-left should update without obstructing controls. Check at your normal resolution and UI scale.
- [x] Repeat earlier large-grid/station performance checks. Docked damage outlines and full missing footprints add rendering work; record `/mg stats` and frame smoothness.

Notes: in the blueprint menu, the preview is on the correct side of the screen, however it should be in the middle left to make it not occlude other things on the HUD. also, if a grid gets blown up so bad the main cockpit is gone, it cant seem to let me load the profile from another seat. There should be some kinda force apply profile option and it does its best to match up whats left with what was.


## 0.10 - Recovery, customization and reference library (pending in-game)

Your previous results are unchanged. These are new checks, not automated passes.

### Recovery and the latest fixes

- [x] Restart into 0.10.0 and open `/mg ui`. Check the middle-left miniature and its caption at your resolution/UI scale; neither should obscure controls.
- [x All custom keybinds work now. when switching between views (cycle systems) I Feel it should say under the display what system your looking at next to the ship name. same with faults.] Bind apostrophe (`'`), another punctuation key and a digit. Test modifiers, duplicates, blank-to-unbind and restart persistence. Existing three shortcuts must migrate.
- [Press a key works. The text "Live preview appears at the middle-left edge" partially covers the button. I dont think this text is needed at all] Use Press a key in Views. Modifiers alone must not complete capture; Escape cancels. No shortcuts should activate in chat or while editing the panel. Test new Show/hide and Open panel actions too.
- [captured a ship, profile loaded, destroyed a TON of the ship and the cockpit. hopped in a side cockpit that was 90 degrees rotated out, and the profile still works. built a new cockpit and hopped in that, profile still works] Capture a ship, destroy the original cockpit and substantial hull, then sit in a different seat. Select its profile and Force apply. Inspect the candidate preview, then accept. Lost blocks must remain missing; the saved baseline must not change.
- [split ship in half and built a new bridge. stil works.] Repeat with a differently oriented/different-type seat and a split remnant. Match counts must describe surviving structure rather than treating the new seat as the old cockpit.
- [ ] Try symmetric wreckage and a wrong profile. Inspect Next alignment and the match counts. Fewer than three exact matches must be refused. Do not accept visibly wrong candidates.
- [ ] In Alignment tools, shift by X/Y/Z grid cells, watch the match count change, and return the preview to the correct position. These axes are grid-relative, not screen-relative.
- [ ] Cancel, switch ships/profiles, split the ship or disconnect during recovery. No stale result may attach to the next ship. Prior saved baselines must remain intact.
- [ ] After accepting recovery, switch seats and repair blocks. After reconnect, Force apply may be needed again when the original anchor is gone; automatic persistence of recovery is a remaining feature.

### Appearance, systems and saved displays

- [x] Move/resize the HUD to the screen edges. Check caption visibility, compact/expanded layout and Reset HUD. Check normal and larger/wider resolutions if available.
- [x there were a few bars in the UI in general that were overlapping.] Change background/outline/fill opacity, disable/re-enable pulse, and edit the seven colors in their listed order. Invalid numbers/colors and out-of-range values must leave the previous settings intact.
- [x] Use Turn left/right and Tilt up/down; rotation should stop and orientation remain correct. Reset restores the familiar spinning view.
- [x] Combine categories, add/remove systems, choose All/No systems and return to damage overview. Missing blocks must follow their original category.
- [x] Set armor faint, another category highlighted and another hidden. Hull presentation should change while damage markers retain state colors. Station context follows visibility but not category brightness yet.
- [x] Save/load a named preset with custom appearance, filters and styles. Empty/invalid names must be rejected. Loading must not change auto-match, language or station choices.
- [x] Save different displays for two profiles using Save ship. Switch ships and restart/reconnect. Each should restore its own display; a new unbound ship should use global defaults. Unsaved edits may be lost on switches.
- [x] Upgrade from the previous settings file. Verify language, visibility, zoom and filters survive, and existing profile numbers/baselines are intact.

### Named baselines and diagnostics

- [x] Rename a profile in Library. Its numeric ID and baseline must remain unchanged even if the grid name differs.
- [x] Save Original and Retrofit revisions around a layout change. Load each and verify comparison, restart persistence and that capture/import never change saved named copies.
- [x] Load a revision and verify the previous active baseline is archived when needed. Verify the second-click prompt and deletion of only the selected revision.
- [x] Change the saved anchor and try an older revision. Cross-anchor loading must be refused clearly; this remains a follow-up feature.
- [ ] On disposable data, reach a revision limit (16 entries or 250,000 total blocks). It must report the limit without silently deleting history or changing the active baseline.
- [ ] Compare expected/observed/matching counts on a small known layout with damaged, missing, replaced and added blocks. Structural completeness counts present matching structure, not health. Docked reports remain separate.

### Blueprint, performance and release checks

- [the workshop cache shows a full path that is cut off so its not readable. "D:\SteamLibrary\steamapps\workshop\content\244..." it gets cut off there and they are all the same to my human eyes] Use Workshop cache with an already-downloaded XML blueprint. It must remain read-only and initiate no download. Missing cache/unsupported archived or PB-only items must not crash the panel.
- [x] Import missing mod definitions and open Omitted blocks. Check names/counts before accepting the partial baseline.
- [x] Recheck cloud/local preview, cancellation and acceptance, including switching grids during preview.
- [x 2606 lines, big grid. snapshot .28ms, worker 70.79ms. 328 draws] Measure recovery time and frame smoothness on a large ship. Compare default and mixed-style rendering with station mode on/off. Record `/mg stats`; matching can still be expensive on complicated wrecks.
- [x] Load the 0.10 source package through Pulsar with the binary copy disabled. Verify dependencies, controls and unload. Compilation alone does not complete this check.

Notes:


## 0.11 - Optional stacked health bars (pending in-game)

- [x] Restart into 0.11.0 with existing settings. All six bars should be hidden by default; previous HUD settings must survive.
- [x] Open `/mg ui` > Display > Health bars. Enable Overall, then Armor, Power, Thrusters, Utility and Cargo individually. Check that blue is leftmost, followed by yellow, orange, red and any gray unknown segment.
- [x] On a small captured ship, damage, disable structurally, remove and repair known blocks. Segment widths should match counts, with repairs returning to blue. Simply switching a block off/unpowering it must not make it orange.
- [x] Remove most of a system. Its red segment must grow using the original baseline denominator; the bar must not become healthier merely because blocks disappeared.
- [x] Add extra blocks and replace an expected block with another type/orientation. Additions should not improve the baseline score; replacements count red in the expected block's original system. The schematic replacement marker stays purple.
- [x] Confirm Utility covers conveyors, tanks, mechanical blocks, tools, weapons, production, control and other remaining categories. Armor/Power/Thrusters/Cargo should not be counted twice. Overall combines every baseline block.
- [x] Change schematic category/state filters, marker pages and station mode. Health totals should remain for the entire controlled-grid baseline; docked context must not inflate them.
- [x] Clear/detach the baseline, select a profile with no Cargo or Power blocks, and switch ships. Empty/unavailable bars should explain the state rather than show 100% healthy or another ship's data.
- [x] Preview a blueprint or forced recovery. Bar labels should say preview. Accept/cancel should return to the correct reference totals. Gray is reserved for unreadable health rather than treating it as blue.
- [x] Move each bar independently with X/Y or arrow buttons. Resize width/height, toggle labels and adjust opacity. Test screen edges and your UI scale. Invalid/out-of-range values must leave the previous layout intact.
- [x] Save global, Save ship and a named preset with different enabled bars/layouts. Restart and switch profiles; verify the intended layout returns. Hide all/Reset this bar should not silently erase other bars' layouts.
- [x] Use Show/hide and normal HUD modes: the whole Marigold HUD, including bars, should hide together. Check bar updates after showing again and monitor performance on a large damaged ship.

Notes: We are missing a healthbar for offensive weapons.


## 0.11.1 - Follow-up to your 0.11 results

Your 0.11 passes and original comments above are preserved.

- [ ] Upgrade from 0.11: all six existing bar settings remain unchanged; the new Offensive bar starts hidden. Enable Offensive, damage/remove/repair a weapon, and verify it is counted there and Overall, but no longer in Utility. Save/reload settings.
- [ ] Cycle systems and faults in compact and expanded views. The current filters should appear beside the ship name; check long names and multiple selected systems at your resolution.
- [ ] Check Views: the redundant preview hint is gone. Check Health bars value/Apply, Appearance preset controls, and Library name/save controls for overlaps.
- [ ] Workshop cache should show blueprint names and Workshop IDs instead of full paths. Select two entries and confirm Load opens the correct blueprint. Malformed or nameless XML should have a folder-name fallback.


## 0.11.2 - Offensive detection for modded weapons

- [ ] Restart with 0.11.2, keep your existing capture, enable Offensive, and allow up to five seconds for WeaponCore. Your sdx PDCs, railgun and torpedo launchers should count without recapturing.
- [ ] Damage/remove/repair one of these weapons. Offensive and Overall should change together; missing weapons must stay red and retain their original denominator. Check a weapon already missing before login too.
- [ ] Select the Offensive schematic filter: the same registered weapons should appear. Ordinary conveyor sorters, cargo weapon racks, railings, vanilla welders/grinders and decoys must not become offensive just because of their names or broad AI target group.
- [ ] Reconnect or change worlds, including a world without WeaponCore. No prior world registry should leak across sessions; vanilla weapons should still classify correctly.
- [ ] If a mod uses WeaponCore weapon registrations for utility devices, report any such devices counted as Offensive. This version follows the registry; it does not infer a modded device's purpose from its name.


## 0.12.0 - Streamlined control panel

Older checklist page names refer to the previous UI. Ship, Baseline and Customize are now the three main sections.

- [x] Open `/mg ui`. Check the active section, page title, Back, Save display and Close at your usual resolution/UI scale. Check that labels, dropdowns and the preview do not overlap. This layout has compiled but has not been visually verified in-game.
- [x] Ship: show/hide, rotation, system/state filters (including a saved multi-system selection), View & zoom, and Damage report. Confirm all four cockpit-relative views, fine rotation, reset and marker pages still work.
- [x] Baseline: capture with confirmation; select/load/detach/delete a saved ship. Saved versions now have separate ship-name and version-name inputs. Rename the active profile, save/use/delete a version, and check confirmations still protect changes.
- [ ] Import a Local, Workshop and Cloud blueprint. Grid/cockpit steps appear only after loading. Select a grid, prepare it, preview, accept or cancel. Check switching sources selects the correct file; custom XML paths remain available before a blueprint is loaded.
- [x] Recovery tools: preview recovery for a selected ship, review/adjust alignment, accept/cancel, set main cockpit, retry matching, clear baseline and restore backup. Destructive actions must still require a second click.
- [x] Customize: HUD layout/detail, individual state colors, missing-block pulse, health bars, system filters and styles. Color editing should change only the selected state; HUD layout reset must retain colors and pulse choice.
- [x] Shortcuts: press-to-bind, typed shortcuts, remove binding, Escape cancellation and page changes during capture. Preferences: language, auto matching and connector-docked ships.
- [x] Save display: save/restore defaults, save/restore this ship, and named preset save/load. Restart and confirm settings persist. Close alone does not save display edits.


User feedback: Local and Workshop entries show the player name instead of the blueprint title; Cloud shows its storage path. Add searchable results across any combination of all three sources. The file dropdown jumps back to the selected item when scrolling/clicking.

## 0.12.1 - Independent ship display and clipboard config

- [x] Customize > HUD layout: hide the ship display while enabled health bars remain visible and update with damage. Show the display again; test Schematic size in compact and expanded layouts. Global Hide HUD still hides everything.
- [x] Customize > Share config: Copy config, change bar positions/sizes and display layout, then Paste config. Confirm layouts, colors, filters and ship-display visibility return. Save display and restart to verify persistence. Baselines and shortcuts are excluded.
- [x] Paste invalid/unrelated clipboard text: existing config must remain unchanged. Check old saved configs keep the ship display visible by default.
- [x] Confirm the system selector, System filters list and HUD captions all display Offensive.


## 0.12.2 - Blueprint browser fixes

- [x] Baseline > Import blueprint > Find blueprint: Local and Workshop display blueprint titles (or grid-name fallback), not the author. Cloud displays just the blueprint name.
- [x] Toggle any combination of Local, Workshop and Cloud, then search a non-empty title or partial title. Matching is case-insensitive. No sources selected should explain what to do.
- [x] Submit an empty or whitespace-only search using Enter and Search. It must say the search can't be empty, preserve the text/caret and keep focus in the search box so another query can be entered.
- [x] Search from the text box using Enter, then repeat with the Search button. Both should return the same matching results.
- [x] Browse Previous/Next with Local-only results spanning at least three pages and mixed sources spanning at least six pages. Reach the last result, return to the first, and select a title near the end. Paging must not stop early, wrap, or load a different file.
- [x] Retest: Previous/Next now advances through all 20 pages; page navigation updates the existing result buttons and counter in place.
- [x] Load/prepare/preview/accept/cancel for each source. Recheck a blueprint with a blank title, malformed XML and duplicate names across sources. Original files must remain untouched.

- [x] In Baseline > Saved ships, confirm deletion of a profile and verify the dropdown refreshes immediately without navigating away. The deleted profile must no longer be selectable.

## 0.12.3 - System groups and labels

- [x] Verify the system filter list uses consistent title capitalization and labels Offensive, not Weapons.
- [x] Verify Tanks includes gas tanks/generators, with no separate Gas filter. Existing saved `gas` filters should load as Tanks.
- [x] Verify there is no Mechanical filter. Rotor/piston blocks should be included by Utility, including old saved `mechanical` selections.
- [x] Load an existing saved display with custom system styles. Confirm Tanks, Production and later group styles retain their previous settings after removing Mechanical.
- [x] Check old `gas` and `mechanical` chat isolate values remain backward compatible and new controls/commands use `tanks`.

## 0.12.4 - Tools health bar and ToolCore recognition

- [x] In Customize > Health bars, enable Tools and verify its label, placement, persistence and visibility work independently of Utility and other bars.
- [drills work, welders and grinders do not show up] Capture a baseline with welders, grinders and drills; damage, disable, remove and repair them. Confirm Tools and Overall bars update with the correct blue/yellow/orange/red counts.
- [They show up in other view mode] Verify SDX2: Core Modpack ToolCore blocks appear when filtering the schematic to Tools and are counted in the Tools health bar, not Utility.
- [ ] Verify a ToolCore block stored as a `MyObjectBuilder_CubeBlock` subtype (for example, SDX2 Deep Welder) is recognized by the Tools filter and health bar.
- [x] Load existing six- and seven-bar display settings. Confirm all existing bar settings remain unchanged and the new Tools bar starts disabled.

## 0.12.6 - Review follow-up and ToolCore classification

Your 0.12.2-0.12.5 changes and existing results are preserved. Review found generic ConveyorSorter classification ran before modded-tool detection; source-build version attributes also remained at 0.12.2. Both are corrected. ToolCore GetTCDefs now supplies authoritative subtype membership, including tools with no recognizable name.

- [x] With the existing baseline, check SDX welders/grinders and Deep Welder under Tools (schematic and health bar). Allow up to five seconds for ToolCore registration; no recapture should be needed.
- [x] Damage, remove and repair a modded tool. Tools and Overall should agree; missing tools must remain red in Tools. Offensive and Utility must not also count them.
- [x] Check ordinary sorters remain Conveyors, PDCs remain Offensive, and cargo with tool-related names remains Cargo.
- [x] Reconnect/change worlds, including a world without ToolCore. Old ToolCore registrations must not leak into the next world.
- [x] Confirm binary and Pulsar source builds both identify as 0.12.6.


## 0.13.0 dev - Persistent recovery and baseline history

Not deployed yet. Binary/source compilation and automated migration/recovery checks passed; these runtime checks remain unverified. Preserve your existing results above.

Before upgrading: close the game and copy the entire `%APPDATA%/Marigold` folder to a safe backup. Format 3 is readable by 0.13.0, not 0.12.6; rolling back requires restoring that folder as well as the old plugin.

- [x] Upgrade existing profiles. Check profile numbers/names, station settings, original blocks and saved versions. Legacy references should show unknown creation date rather than an invented date. Save, restart and recheck.
- [the Next Alignment idea feels... weak. but the x, y and z adjustments work GREAT. ITs hard to tell from the UI if things are lining up. maybe when we do recovery preview the ship display gets expanded and in the middle top of the screen nice and big, and the buttons get small and at the bottom? so its easier to view?] Capture a baseline, remove the original cockpit and a substantial section, then sit in another cockpit. Baseline > Recovery: preview/adjust/accept. Missing sections must stay red and health bars must retain the original denominator.
- [x] Restart/reconnect with auto matching on and the same remnant (at least eight blocks). It should recover the accepted alignment without Force apply again. Repeat across a Nexus transfer if available; check a differently oriented replacement cockpit and non-origin placement.
- [x] Repair the ship fully and reconnect. It should still align against the original baseline. Try partial repairs too; if matching is uncertain, manual recovery must remain available without changing the baseline.
- [x] Try identical profiles or symmetric remnants, auto-off, manual detach, and a profile already used by another loaded grid. Uncertain recovery must not silently choose a conflicting profile. Tiny remnants may need manual recovery each session.
- [x] Capture a changed layout, import a blueprint and change main cockpit. Verify the previous baseline appears in Saved versions before replacement. Recovery accepted for an older baseline must not be reused for a different baseline identity.
- [x] Name/save a version, switch versions and restart. Check readable name/source/date, available blueprint identity and active indicator. Deleting a saved version must not clear the active baseline. Renaming the ship must retain metadata.
- [x] Restore a version with a different saved anchor. The Alignment page should open; inspect missing markers and health preview, then cancel (no change). Repeat and accept: the older baseline becomes active and the previous baseline remains in Saved versions.
- [x] Leave control, change grids or cancel during recovery/version alignment. No late worker result should bind another ship or replace its baseline. Capture a new baseline during a pending preview and ensure stale acceptance is rejected.
- [x] Fill a version library to its limit (16 revisions / 250,000 total blocks). Replacement must fail clearly without losing the active baseline or deleting history. Remove an unwanted revision and retry.
- [x] Check profile backup recovery with copied test data, then restart. Verify recovered references, metadata and conservative matching; preserve the original pre-upgrade backup for rollback.


## 0.14.0 dev - Recovery, versions, rendering and UI polish

0.13.0 results above are still pending. This section adds checks; it does not replace your previous notes. 0.14.0 builds into artifacts/Marigold and is not deployed by the build. It uses the same format 3 as 0.13.0. Retain the full pre-0.13 backup when testing or rolling back to 0.12.6.

- [x] Accept recovery of a damaged ship, repair about half of its missing blocks, then reconnect. It should reuse the accepted alignment without recapturing; original missing blocks remain red. Repeat after full repair. Automatic matching still needs enough exact surviving structure.
- [x] Replace the recovery cockpit with a different type at the same grid position/orientation, then reconnect. With enough surrounding matches it should associate correctly. Try a relocated/rotated seat too: uncertain matching should wait for manual recovery rather than guess.
- [x] Add unrelated blocks, remove more remembered structure or introduce an identical saved profile. Ambiguous/insufficient matches must not silently attach. Auto-off, manual detach and original health-bar denominator must remain intact.
- [x nice! this feels WAY better and makes sense.] Recovery > Review alignment: Rotate X/Y/Z each turns 90 degrees about the baseline origin using live-grid axes. Four clicks restore the original orientation. Combine rotations with translation; accept/cancel, reconnect and check markers at asymmetric locations. Acceptance must still reject fewer than three exact matches.
- [x] With a profile that produces no automatic alignment, check the clearly unverified starting point and manual controls. Cancel leaves the baseline/binding unchanged. Do not accept a wrong alignment merely because three repeated blocks happen to match.
- [x] Saved versions: Save new version, select it, enter a different name and Rename version twice to confirm. Check name, active indicator and metadata after restart; its block contents must not change.
- [x] Make a new active baseline, select an older version and Replace version. First click only asks for confirmation. Second copies the active baseline into that slot and keeps a '(previous)' copy with the old contents. Load both to confirm the difference. Replacement at the library limit must refuse without overwriting.
- [x] Station mode: set Armor faint, Thrusters highlighted and another system hidden. Check main and connector-docked ships, including mixed grid sizes and multiple docked damaged ships. Hull styles should agree; damage markers retain yellow/orange/red independently. Switch styles during rebuild and reconnect.
- [x] Inspect a thin/long or irregular ship at Low/Medium/High detail. Outer geometry must not swell beyond actual block bounds. Slopes remain block envelopes; coarse detail can merge small gaps. Read the report's explanation and watch docked-display performance.
- [x] Open a .sbcB5, ZIP or .bin path: the message must clearly explain XML bp.sbc support. Search remains XML-only. Load normal XML and gzip XML, including local/Workshop/Cloud, and confirm existing imports still work and source files stay untouched.
- [x] Switch to Spanish: check all section tabs, page titles, menus, health/style selectors, clipboard controls, version/rotation buttons and preferences. Check truncation/overlap at your normal UI scale; save language and restart. Ship/version names must remain yours. Dynamic diagnostics may fall back to English.
- [x] Smoke-test capture, cross-anchor version restore, UI navigation and config import/export after these changes. No unchecked 0.13 or older test is considered passed by this build.


## Readiness review - October 23, 2026

Your recorded results and comments above are preserved. This review supersedes the old introductory statements that 0.13/0.14 are untested or not deployed. All 12 of your 0.14 checks are reported passed (including the rotation check with an inline comment); 10 of 11 0.13 checks are marked passed, with the remaining entry containing recovery-UI feedback rather than an unqualified pass.

Verified locally: installed DLL is 0.14.0 and matches the built artifact by SHA-256. The package contains only plugin.dll, plugin.pdb and plugin.xml. The recorded 0.14 binary/regression and 35-file source-compilation checks passed. Descriptor description is 964 characters, within the previously documented 1,000-character limit. No new in-game checks have been marked passed by this review.

### Outstanding product feedback

- [x] Improve recovery inspection: large ship preview near the top center, compact alignment controls below. Preserve the successful X/Y/Z moves and 90-degree turns. Make alternative candidates secondary and clearly distinguish changing candidates from manually adjusting one. This is not implemented yet.
- [x] After that change, check preview readability, control overlap, accept/cancel and return to the normal saved HUD layout at your usual resolution/UI scale. The temporary preview layout must not overwrite your saved display settings.

### Final release checks

Later passes supersede overlapping historical failures: mass-loss rendering (0.9), blueprint browsing (0.12.2), Tools recognition (0.12.6), recovery persistence/version limits (0.13), and station styles/Spanish controls (0.14). Old unchecked entries are not all new blockers, and obsolete expectations such as rejecting every missing-mod blueprint no longer apply.

- [ ] Record scale-specific performance on approximately 1k/10k/50k-block ships and a dock at the context limit. Earlier timings and repeated-session checks passed; this fills the remaining size/budget coverage rather than discarding that evidence.
- [x] Close remaining unique edge-case coverage: rotated multi-cell blueprint alignment, multiple cockpit choices, remote-control transitions and known-layout report counts where not already covered by your later tests.
- [ ] Fresh-install smoke test for the final release package. Existing-profile migration passed in 0.13.
- [ ] Load the final pinned source commit through Pulsar Legacy with the binary disabled, then check dependencies/UI/unload. The earlier 0.10 source-loader test passed; 0.14 source compilation alone does not establish final-pin runtime acceptance.
- [ ] Finalize the release commit/package, recheck current PluginHub rules and generate its pinned manifest. Decide on publication separately; this review does not publish or merge dev into master.

Status: feature behavior is user-tested, with one recovery UI improvement outstanding. Release preparation/targeted validation remains; not yet a blanket public-release sign-off.


## 0.14.1 - Focused recovery preview

Implemented the recovery-layout feedback above. Earlier results and notes are preserved; this section supersedes the readiness review's statement that the layout change is not implemented. Built on dev; in-game visual acceptance remains pending.

- [x] Baseline > Recovery tools > Preview recovery opens the alignment page automatically. Check the large top-center ship view, compact controls underneath and readable status area at your usual resolution/UI scale. Test a wider aspect ratio if available.
- [x] Move X/Y/Z and rotate X/Y/Z. Inspect using Top, Left, Zoom in/out and Reset view. These view controls change only the inspection camera; rotation/movement controls still change the recovery alignment. Other candidate selects an alternative rather than moving the current one.
- [x] Accept still requires confirmation; changing alignment after the first click resets confirmation. Cancel and successful acceptance return to Recovery tools. Back/Close must restore normal HUD size/position, health bars, view/zoom and spin preference.
- [x] Hide the ordinary schematic but leave health bars enabled, then start recovery. The recovery preview should still appear; bars stay out of its way temporarily and return afterward. Check a cross-anchor saved-version restore uses the same focused page.
- [x] Restart and verify your saved HUD layout is unchanged. The recovery camera/layout must not be saved into defaults, ship settings or presets.


## 0.14.2 - Drag to inspect recovery

- [x] Hold the left mouse button inside the recovery image and drag horizontally/vertically. Only the camera should rotate; match count and baseline alignment must stay unchanged. Release, leave/reopen the page, and switch focus: no stuck dragging.
- [x] Start a click outside the image or on X/Y/Z adjustment buttons: it must not start camera dragging. Drag after Top/Left, then Reset view and zoom. Check sensitivity at your resolution.
- [x] Accept/cancel and close the panel. Ordinary HUD camera/layout and saved settings must remain unchanged. Baseline adjustments still use the buttons.


## 0.14.3 - Recovery preview cleanup and wheel zoom

- [x] Start recovery, adjust it, then close the panel with Close or Escape without accepting. Purple preview boxes must disappear; the prior baseline remains. Reopen: recovery must be started again. Repeat while alignment search/rebuild is pending.
- [x] Accept recovery, then close: the accepted comparison remains, without preview ghosts. Cross-anchor revision previews must also cancel safely when closing without acceptance.
- [x] Scroll over the recovery image to zoom in/out, with and without dragging. Scrolling over buttons/status must not zoom. Close/reopen and confirm normal HUD zoom and saved layout were not changed.
