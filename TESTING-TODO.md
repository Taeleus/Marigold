# Try the redesigned control panel — 0.16.0

Development build on `dev` for Pulsar Legacy. The UI has been rebuilt; these checks are still pending in-game. Your completed 0.15.1 results remain below. Back up your active `Storage/Marigold` folder before trying baseline changes on a test ship.

1. [ ] **Find your way around.** Open `/mg ui` and visit all six sidebar sections. At your usual resolution/UI scale, labels, fields and footer buttons should stay readable. Scroll a long page, open a dropdown near its bottom and come back: it should stay usable without jumping to the top. Try Tab and Escape too.
2. [ ] **Arrange the display and inspect it.** In **Ship display**, change position/size or transparency and apply it. Use **Preview HUD**, then **Return to editor** (or Escape). Your edits and place in the form should remain. The editor's small ship preview should stay out of the controls; the full preview should use your actual HUD positions.
3. [ ] **Check filters and colors.** In **Ship > Systems and filters**, check multiple systems and change a style/background opacity. In **Ship display > State colors**, use the picker and exact RGB entry, then try canceling a selection. Existing damage colors and filters should behave as before.
4. [ ] **Edit bars without losing a draft.** In **Health bars**, try a system bar and Power sections. Edit a custom bar's name/layout, choose **Preview layout**, return to the editor and **Save bar**. Try leaving a second unsaved edit: it should be discarded. Damage/repair tracking and **Update tracked blocks** still have their separate meanings.
5. [ ] **Walk through baseline tools on a test ship.** Try capture/select, versions and a blueprint search/import. Check that confirmations name the intended action and that empty lists offer a useful next step. Open recovery, drag/zoom the preview, adjust alignment and cancel or accept; exiting should clear preview geometry as before.
6. [ ] **Keep and share your setup.** Use **Save & share** for a default/ship setup or preset, and try the clipboard page. Open **Preferences** for performance/pulses, language and shortcuts. Save a setup and restart when convenient: your existing profiles, bars, colors and bindings should remain intact. If you use Spanish, check the new labels and longer help text too.

No new combat or FPS benchmark is required just for this menu redesign. Automated backend checks and source compilation remain separate from visual acceptance.

---

## Completed usability checks — 0.15.1

All 0.15.1 follow-up checks and the remaining 0.15.0 menu check are user-reported passed as of October 28, 2026. Automated build, regression and source-compilation checks also passed. Earlier results are preserved below.

1. [x] **Try picking colors visually.** Open **Customize > Colors**, select a state and choose **Choose color visually...**. Drag the hue strip and color square: the selected swatch should follow your mouse and agree with the RGB value. **Use color** should change only that state. Try the picker under **Health bars > Bar colors** too; choosing a bar color should turn off outline matching.
2. [x] **Check cancel and save.** Change a picker selection, then Cancel or Back: your original color should remain. Reopen, apply a color, use **Save display** and restart when convenient to check it stays saved. The square, strip, swatches and buttons should be readable and clickable without overlap at your normal UI scale.
3. [x] **Check the custom-bar page.** Under **Health bars > Custom bars > Add custom bar**, search by name and click **Find**; the textbox and button should no longer overlap. On the custom-bar list, **Update tracked blocks** should clearly refer to adding/removing group members. Damage and repairs should keep updating without pressing it.

The menu-label and **Power sections** check below is also complete. No need to repeat the completed checklist.

---

## Usability update — 0.15.0 results

User testing passed all items below, including the menu-label and Power sections layout check.

1. [x] **Focus without losing the ship.** Select Offensive, Power or another system. The rest of the ship should remain as faint outlines. Try **System filters > Other systems opacity**, including zero. Selected faults should stay visible; fault-only modes should still hide the hull.
2. [x] **Check the pulse in a small fight or test.** Newly broken/missing blocks should breathe together in orange/red, more strongly than the normal missing pulse. Yellow stays steady. Try an outline-only mode too; both pulse toggles should still work.
3. [x] **Pick your healthy color.** In **Health bars > Bar colors**, choose a color, then try matching the schematic outline. Standard and custom bars should agree. Use **Save display** to keep it.
4. [x] **Look over the menus.** Dropdowns and value boxes should have clear labels. Open the separate **Power sections** page; controls and the status text should not overlap at your usual resolution/UI scale.
5. [x] **Make two custom bars.** In **Health bars > Custom bars**, create one for a named weapon and one for a terminal group. Edit their names/visibility/size/position, then **Save bar**. Damage/remove a member and repair it: missing stays red and repaired returns healthy. New group members require a confirmed manual update (now named **Update tracked blocks**).
6. [x] **Come back to it.** Restart/reconnect and confirm saved bars, layouts and colors return. Leaving the custom-bar editor without **Save bar** should discard its preview edits. If convenient, also try recovery or a saved baseline version; its custom bars should follow the correct ship alignment.

Existing 0.14.16 results below remain valid. No need to repeat that whole checklist just for this update.

---

## Review-fix acceptance — 0.14.16

These checks cover the PluginHub review fixes. We do not need another full feature retest.

1. [x] **Launch normally.** Start Pulsar Legacy, sit in a cockpit and open `/mg ui`. The panel and ship HUD should work without startup or Harmony errors.
2. [x] **Save something and restart.** Capture one test ship, change a display setting and set a shortcut. Save display, restart the game and confirm the profile, setting and shortcut are still there. Data belongs in `%APPDATA%/SpaceEngineers/Storage/Marigold` (or your selected `-appdata` folder's `Storage/Marigold`).
3. [x] **Find a local blueprint.** Search for one you know exists in the active game's Local blueprint list. It should appear and load normally.
4. [x] **Check the log is quiet.** Let the HUD run briefly, including station mode or a little damage if convenient. Run `/mg stats`: timings should still appear, but routine rebuild messages should not keep filling the game log.
5. [x] **Only if you use custom `-appdata`:** launch that setup and confirm its profiles/settings and Local blueprint search belong to that selected folder.

For a fresh-user check, close the game and temporarily rename only the active `Storage/Marigold` folder first. No other folder cleanup is needed. The plugin will start fresh and create data in the active location when you save.

Already reported working: blueprint repairs, automatic matching, recovery controls, rendering/pulse settings and profile-number reuse. Those results stand.

The core checks above passed. Final Legacy source-loader testing with the local DLL disabled remains separate; updating the source pin does not claim that check passed. See [PR #285](https://github.com/StarCpt/PluginHub/pull/285) for the submitted pin.

Earlier results: [history](docs/history/README.md), [performance results](docs/PERFORMANCE-RESULTS.md). Interim testing remains deferred.

## October 26 results

User reports the core checks working. Log verification: `SpaceEngineers_20261026_000148401.log` contains one Marigold startup message and one requested stats report; `SpaceEngineers_20261025_233252277.log` contains one startup message. Neither contains routine Marigold snapshot/worker spam or Marigold errors. Custom `-appdata` remains optional; no separate custom-folder test is claimed by this log review.
