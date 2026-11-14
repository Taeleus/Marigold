# Testing checklist

## Display follow-up

Main 0.17.2 Legacy beta checks passed in game on November 5, 2026.

- [x] **Upgrade from 0.16.1.** Saved baseline, custom bars, colors and shortcuts carry over. The schematic and bars update after damage and repair.
- [x] **Arrange HUD.** Drag the display and both types of bars, resize the bars with the corner handle and sliders, and check Cancel and Apply layout. Hide the ship label and move the schematic to the bottom edge.
- [x] **Save and reconnect.** Save different Power bar positions as the default and the ship layout. Both choices apply immediately and are remembered on reconnect. **Load my default now** temporarily previews the default with the ship layout selected.
- [x] **Reuse and share layouts.** Restore a standard bar from a named layout and through the clipboard.
- [x] **Rendering modes.** Compare HD, SD and Textured Ship while rotating. Lower textured opacity and check that damage behind the hull stays visible. The textured Expanse drive also passed an earlier in-game check.
- [x] **Textured filters.** Cycle Armor, Thrusters and All systems with Other systems opacity at zero and above zero. Try Damaged and Missing with a baseline.
- [x] **Retest textured station rotation and shapes.** The 2,257-block station looked much better in Legacy. At 768 px, `/mg stats` reported 16.5 preview FPS, 48.47 ms for the last raster and 181.77 ms for the last mesh job. It used 18 reduced meshes and reported zero painted box replacements. The earlier 441 px preview reported a 73.81 ms worker job and 126 simplified parts.

## 0.17.3 beta results

- [x] **Arrange HUD values.** Typed values and sliders match.
- [x] **Simple labels.** Title-only labels work. Power section captions need their own follow-up.
- [x] **Custom bar persistence.** The custom bar stays put after applying the layout and returning to the ship.
- [x] **Rendering choices.** The revised list is organized correctly.

## 0.17.4 beta results

- [x] **Standard and Advanced.** The Advanced tab works in game.
- [x] **Place the ship display at the bottom.** A shorter background lets the display move lower at the same scale.
- [x] **Keep the ship label by its display.** The caption sits just below the resized background.
- [x] **Keep Power section captions.** **Simple label** keeps the enabled section captions visible.

## 0.17.7 Legacy smoke test

The user confirmed all four checks in Pulsar Legacy on November 10, 2026.

- [x] **Blueprint preview.** **Escape** and **Close** clear the draft and restore the previous HUD. An accepted baseline stays active after closing.
- [x] **Background scaling.** Existing layouts start with **Scale background with ship** off. The slider and typed values resize the linked background, and the dimensions and toggle persist after applying, saving and reloading.
- [x] **HUD placement.** **Arrange HUD** builds its preview after changing seats with the HUD hidden. The caption stays readable under a narrow background at the right edge.
- [x] **Rendering.** Paused **HD Wireframe** updates after view or damage changes. **Textured Ship** still renders familiar modded parts.

## 0.17.10 beta result

- [x] **Drive shapes.** The drive test grid shows all the nozzles correctly, and the reported ship shows rounded Canterbury drives. The compact cargo textures still show a little speckling.

## 0.17.13 GPU test result

- [x] **Leave the seat with GPU rendering.** The preview worked in compact and expanded views, and leaving the seat no longer crashed in the user's retest.

## 0.17.14 GPU stress test

These results are from the local Pulsar Legacy package built from the source pinned in PluginHub PR #344 (`2af48f3`).

- [x] **Canterbury drive shapes.** The Canterbury drive retest passed on 0.17.14.
- [x] **Renderer and HUD transitions.** Entered and exited the seat, showed and hid the display, switched between CPU and GPU and back, toggled compact and expanded at both slow and rapid speeds, and resized the ship display. All passed.
- [x] **Recover after losing the main seat.** Capybarra lost its main seat and much of its hull in a collision. The GPU preview worked after repair and applying recovery. At 768 px, `/mg stats` showed 25.0 preview FPS before the collision and 23.4 after recovery, with 566 damage fills in the second view.

## 0.17.15 GPU preview retest

- [x] **First GPU frame.** The wireframe briefly appeared at startup, then GPU Textured Ship took over as expected.
- [x] **Display changes.** Changing resolution and switching between fullscreen and borderless worked in Legacy.

## Pending beta checks

- [ ] **Check the 0.17.14 renderer default.** With no saved renderer preference, choose **Textured Ship** and check `/mg stats` for GPU. Choose CPU, restart, and confirm Textured Ship still uses CPU.
- [ ] **Developer check for unavailable GPU output.** Verify that the wireframe and damage markers remain visible if GPU Textured Ship cannot produce a frame, and that the GPU preference stays selected.
- [ ] **Developer check for graphics-device loss.** If the game recreates its Direct3D device while the preview is paused, verify that the GPU preview recovers. A window or HUD resize exercises a separate path.
- [ ] **Check GPU filters and ship switching.** Change system and state filters, pause and spin, and switch ships. Check that the image and damage markers update together.
- [ ] **Compare textured preview performance.** Use `/mg stats` and the game FPS counter to compare CPU and GPU in compact and expanded views. Check that damage behind the hull stays visible at lower intensity. Check the 7×11×3 cargo faces and large-grid RCS variants on the same pass.
- [ ] **Check 0.17.8 layout fixes.** Link an 85% × 34% background at 50% ship scale. Raise ship scale to 70%, save and reload, then return to 50%. The background should return to 85% × 34%. In the custom-bar editor, check that the hint explains saving and going back before using **Arrange HUD**.
- [ ] **Check Textured Ship on large grids.** Try a dense grid and a thin or hollow grid near 50,000 blocks. Confirm both show recognizable geometry and real textures. Record FPS and `/mg stats` during initial capture and rotation, then switch ships to check for stale images.
- [ ] **Compare performance during sustained damage.** Compare HD, SD and Textured Ship at the same view and detail. Check that markers agree with the health bars, and record FPS with the display shown and hidden plus `/mg stats`. [Performance test procedure](docs/PERFORMANCE-TEST.md)
- [ ] **Smoke-test Pulsar Interim.** Launch with the separate Interim package and open `/mg ui`. Check HD Wireframe, CPU and GPU Textured Ship, seat exit, filters and saving a layout. Then disable the local package and verify loading the exact proposed source pin on Interim.

---

## Completed Pulsar Interim checks — 0.16.1

All five checks below are user-reported passed on November 8, 2026, on `Interim.exe` (Pulsar 2.4.3, runtime `CoreCLR`) with a local 0.16.1 `net10.0` Release build. The build was installed in the shared `%APPDATA%/Pulsar/Legacy/Local/Marigold` folder with `plugin.xml` tagged `<Runtimes>CoreCLR</Runtimes>`. Interim uses `%APPDATA%/Pulsar/Interim` only when that folder exists; otherwise it shares the Legacy folder, and this test ran on that shared setup. Loading from PluginHub source and the 0.17 features are not covered here. See **Smoke-test Pulsar Interim** above.

- [x] **Loads on Interim.** Marigold initializes without dependency or Harmony errors.
- [x] **Overlay and UI.** Sit in a cockpit, open `/mg ui`, capture a baseline and preview the HUD. The schematic draws over the HUD as it does on Legacy.
- [x] **Shared data.** Profiles and settings saved on Legacy load on Interim, and the reverse.
- [x] **Clipboard.** Export the display config to the clipboard and import it again.
- [x] **Unload.** Leave to the main menu and load another world: no duplicate overlay and no errors. This check used a heavily modded world.

---

## Completed review-fix checks — 0.16.1

Both focused checks below are user-reported passed on October 28, 2026, using the deployed Legacy package. The build, regression suite (including the new removal/translation checks) and source compilation also passed. Earlier completed results remain below.

1. [x] **Undock without false damage.** With a saved baseline, dock and undock through a connector a few times. Live blocks should not briefly turn red or drop from the healthy count. On a test ship, remove a real baseline block afterward: it should still show missing, then return healthy when repaired.
2. [x] **Open Spanish controls with a bracketed name.** Temporarily name a test ship `[ ]`, choose Spanish under Preferences and reopen `/mg ui`. The panel should open normally. If you use custom bars, try a group named `[ ]` in its selection list too. Restore your preferred name and language afterward.

---

## Completed control panel checks — 0.16.0

All six redesigned-UI checks below are user-reported passed as of October 28, 2026, using the deployed Pulsar Legacy build. Automated build, regression and source-compilation checks also passed. Earlier completed results remain below.

1. [x] **Find your way around.** Open `/mg ui` and visit all six sidebar sections. At your usual resolution/UI scale, labels, fields and footer buttons should stay readable. Scroll a long page, open a dropdown near its bottom and come back: it should stay usable without jumping to the top. Try Tab and Escape too.
2. [x] **Arrange the display and inspect it.** In **Ship display**, change position/size or transparency and apply it. Use **Preview HUD**, then **Return to editor** (or Escape). Your edits and place in the form should remain. The editor's small ship preview should stay out of the controls; the full preview should use your actual HUD positions.
3. [x] **Check filters and colors.** In **Ship > Systems and filters**, check multiple systems and change a style/background opacity. In **Ship display > State colors**, use the picker and exact RGB entry, then try canceling a selection. Existing damage colors and filters should behave as before.
4. [x] **Edit bars without losing a draft.** In **Health bars**, try a system bar and Power sections. Edit a custom bar's name/layout, choose **Preview layout**, return to the editor and **Save bar**. Try leaving a second unsaved edit: it should be discarded. Damage/repair tracking and **Update tracked blocks** still have their separate meanings.
5. [x] **Walk through baseline tools on a test ship.** Try capture/select, versions and a blueprint search/import. Check that confirmations name the intended action and that empty lists offer a useful next step. Open recovery, drag/zoom the preview, adjust alignment and cancel or accept; exiting should clear preview geometry as before.
6. [x] **Keep and share your setup.** Use **Save & share** for a default/ship setup or preset, and try the clipboard page. Open **Preferences** for performance/pulses, language and shortcuts. Save a setup and restart when convenient: your existing profiles, bars, colors and bindings should remain intact. If you use Spanish, check the new labels and longer help text too.

The menu redesign is accepted in-game. A final exact-pin source-loader check with the local DLL disabled remains separate from this local-package testing.

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

Earlier results: [history](docs/history/README.md), [performance results](docs/PERFORMANCE-RESULTS.md).

## October 26 results

User reports the core checks working. Log verification: `SpaceEngineers_20261026_000148401.log` contains one Marigold startup message and one requested stats report; `SpaceEngineers_20261025_233252277.log` contains one startup message. Neither contains routine Marigold snapshot/worker spam or Marigold errors. Custom `-appdata` remains optional; no separate custom-folder test is claimed by this log review.
