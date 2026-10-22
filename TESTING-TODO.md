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

- [ ] Restart into 0.10.0 and open `/mg ui`. Check the middle-left miniature and its caption at your resolution/UI scale; neither should obscure controls.
- [ ] Bind apostrophe (`'`), another punctuation key and a digit. Test modifiers, duplicates, blank-to-unbind and restart persistence. Existing three shortcuts must migrate.
- [ ] Use Press a key in Views. Modifiers alone must not complete capture; Escape cancels. No shortcuts should activate in chat or while editing the panel. Test new Show/hide and Open panel actions too.
- [ ] Capture a ship, destroy the original cockpit and substantial hull, then sit in a different seat. Select its profile and Force apply. Inspect the candidate preview, then accept. Lost blocks must remain missing; the saved baseline must not change.
- [ ] Repeat with a differently oriented/different-type seat and a split remnant. Match counts must describe surviving structure rather than treating the new seat as the old cockpit.
- [ ] Try symmetric wreckage and a wrong profile. Inspect Next alignment and the match counts. Fewer than three exact matches must be refused. Do not accept visibly wrong candidates.
- [ ] In Alignment tools, shift by X/Y/Z grid cells, watch the match count change, and return the preview to the correct position. These axes are grid-relative, not screen-relative.
- [ ] Cancel, switch ships/profiles, split the ship or disconnect during recovery. No stale result may attach to the next ship. Prior saved baselines must remain intact.
- [ ] After accepting recovery, switch seats and repair blocks. After reconnect, Force apply may be needed again when the original anchor is gone; automatic persistence of recovery is a remaining feature.

### Appearance, systems and saved displays

- [ ] Move/resize the HUD to the screen edges. Check caption visibility, compact/expanded layout and Reset HUD. Check normal and larger/wider resolutions if available.
- [ ] Change background/outline/fill opacity, disable/re-enable pulse, and edit the seven colors in their listed order. Invalid numbers/colors and out-of-range values must leave the previous settings intact.
- [ ] Use Turn left/right and Tilt up/down; rotation should stop and orientation remain correct. Reset restores the familiar spinning view.
- [ ] Combine categories, add/remove systems, choose All/No systems and return to damage overview. Missing blocks must follow their original category.
- [ ] Set armor faint, another category highlighted and another hidden. Hull presentation should change while damage markers retain state colors. Station context follows visibility but not category brightness yet.
- [ ] Save/load a named preset with custom appearance, filters and styles. Empty/invalid names must be rejected. Loading must not change auto-match, language or station choices.
- [ ] Save different displays for two profiles using Save ship. Switch ships and restart/reconnect. Each should restore its own display; a new unbound ship should use global defaults. Unsaved edits may be lost on switches.
- [ ] Upgrade from the previous settings file. Verify language, visibility, zoom and filters survive, and existing profile numbers/baselines are intact.

### Named baselines and diagnostics

- [ ] Rename a profile in Library. Its numeric ID and baseline must remain unchanged even if the grid name differs.
- [ ] Save Original and Retrofit revisions around a layout change. Load each and verify comparison, restart persistence and that capture/import never change saved named copies.
- [ ] Load a revision and verify the previous active baseline is archived when needed. Verify the second-click prompt and deletion of only the selected revision.
- [ ] Change the saved anchor and try an older revision. Cross-anchor loading must be refused clearly; this remains a follow-up feature.
- [ ] On disposable data, reach a revision limit (16 entries or 250,000 total blocks). It must report the limit without silently deleting history or changing the active baseline.
- [ ] Compare expected/observed/matching counts on a small known layout with damaged, missing, replaced and added blocks. Structural completeness counts present matching structure, not health. Docked reports remain separate.

### Blueprint, performance and release checks

- [ ] Use Workshop cache with an already-downloaded XML blueprint. It must remain read-only and initiate no download. Missing cache/unsupported archived or PB-only items must not crash the panel.
- [ ] Import missing mod definitions and open Omitted blocks. Check names/counts before accepting the partial baseline.
- [ ] Recheck cloud/local preview, cancellation and acceptance, including switching grids during preview.
- [ ] Measure recovery time and frame smoothness on a large ship. Compare default and mixed-style rendering with station mode on/off. Record `/mg stats`; matching can still be expensive on complicated wrecks.
- [ ] Load the 0.10 source package through Pulsar with the binary copy disabled. Verify dependencies, controls and unload. Compilation alone does not complete this check.

Notes:
