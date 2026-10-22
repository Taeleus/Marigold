# Marigold testing to-do

Use this file as your testing log. Check boxes only after testing in-game; add notes below each section. Automated checks are recorded separately and do not mark these complete. Future updates will append new sections rather than overwrite your notes.

## 0.5.0 â€” Persistent profiles and cockpit anchors (not yet tested)

- [ ] Sit in the intended cockpit, run `/mg anchor`, then `/mg capture`; note the profile ID.
- [ ] Change one block, leave/reload the world, sit in the same cockpit, run `/mg profiles` and `/mg profile select ID`. The original damage/missing state returns.
- [ ] Switch seats and rename the ship. The saved baseline remains aligned; viewing direction follows the active seat.
- [ ] Create profiles for two similarly named ships. Selecting one never silently attaches it to another live ship.
- [ ] `/mg clear` clears only the baseline; anchor/profile remain. `/mg profile detach` leaves saved data intact.
- [ ] In a disposable test profile, `/mg profile delete ID` removes only that profile.
- [ ] After reconnect or Nexus transfer, explicitly select the profile from the corresponding cockpit and check alignment. Automatic Nexus matching is not validated.

Notes:

## Regression checks for every installed build

- [ ] Default/reset, top/bottom and side views place known asymmetric blocks on the expected side.
- [ ] Yellow damage is faint; orange non-functional blocks stand out; missing positions pulse red.
- [ ] Weld a damaged block back to the baseline and check the marker clears.
- [ ] `/mg hide`, show, spin, zoom and ship switching still work.
- [ ] Compare `/mg stats` on a small ship and a large ship. Record snapshot, worker and HUD CPU times; check for visible stutter.

Notes:

## 0.6 â€” Blueprint baseline import (pending in-game testing)

- [ ] With a profile selected, run `/mg blueprint load "C:\full path\bp.sbc"`. The file must remain unchanged.
- [ ] Run `/mg blueprint grids`, then `/mg blueprint grid N`. Run `/mg blueprint anchors` and select the matching cockpit with `/mg blueprint preview N`.
- [ ] Inspect top, bottom and default views. The caption says blueprint preview; `/mg report` describes differences. Preview must not replace the saved baseline.
- [ ] Run `/mg blueprint cancel`; the previous baseline returns. Repeat preview and use `/mg blueprint accept`; the blueprint baseline survives reload/profile selection.
- [ ] Try a rotated, multi-cell cockpit and a blueprint with several grids/cockpits; selection must be explicit and alignment correct.
- [ ] Try a blueprint whose mods are missing: import must fail with the missing definition, not save a partial reference.
- [ ] Switch ships or change profiles during a preview: no preview may leak onto the new ship/profile.
- [ ] Confirm blueprint scripts, inventories and ownership are not executed or applied to the world. This importer reads only layout metadata.

Notes:

## 0.6 — Filters, view modes and marker pages (pending)

- [ ] Compare `/mg mode live`, `reference`, and `compare` after removing and adding blocks. Live shows the current hull; reference shows the saved layout; compare shows change markers.
- [ ] Use `/mg isolate tools`, `thrusters`, `power`, `control`, and `all`. Missing blocks must still appear in their original category. Modded types may fall back to `other`.
- [ ] Use `/mg filter missing`, `damaged`, `broken`, `faults`, then `all`. Orange is structurally non-functional, not merely switched off or unpowered.
- [ ] On a disposable ship with more than 128 changed blocks, use `/mg page next`, `prev`, and a large page number. `/mg report` shows the actual clamped page and full comparison totals.
- [ ] Change filters, detail and modes while an update is running. Old geometry must not replace the new choice. Empty categories should show an empty panel and ship name.
- [ ] Try `/mg layout expanded` and `compact` at your display resolution. Compact retains only the ship name; expanded adds view/filter context.

Notes:

## 0.6 — Preferences, suggestions and observation reuse (pending)

- [ ] Set layout, zoom, mode, category, state filter and detail; run `/mg settings save`. Restart the game and verify they return. Orientation/spin and marker page deliberately start fresh.
- [ ] Change those settings without saving, run `/mg settings load` and confirm the saved values return. `/mg settings reset` restores defaults; save again only if wanted.
- [ ] If saved visibility was hidden, `/mg show` must reveal the display after restart.
- [ ] From the original anchor cockpit, run `/mg profile suggest`. Check that damaged copies still score well; a different layout scores lower; identical ships can tie. No profile is selected automatically.
- [ ] Run suggestions after reconnect/Nexus transfer, explicitly select a profile, and check alignment before capturing. This remains a manual workflow, not verified automatic transfer support.
- [ ] Watch `/mg stats` on an idle ship: unchanged samples should reuse the worker geometry. Damage, repair, block replacement/removal and functional threshold changes must still refresh it.
- [ ] Hide the display, alter the ship, then show it. Comparison must catch up. Switch ships and exit/reload to check stale results are discarded.

Notes:

## 0.7 — Connector station view (pending)

- [ ] With fresh settings, confirm station mode is OFF. Dock a second grid using locked connectors; it stays hidden until `/mg station on`.
- [ ] With station mode ON, check the docked outline's position, orientation and scale. Include small-grid/large-grid connections and a connector mounted at an angle.
- [ ] Connect a chain of ships and a cycle of connectors; each grid should appear once. Merely yellow/connectable connectors must not include a ship.
- [ ] Unlock a connector: the detached branch disappears within roughly one second of active observation. A ship still linked through a second locked connector remains visible.
- [ ] Capture or import a baseline while docked, then undock. Docked context must not become missing-red baseline blocks; report counts remain controlled-grid-only.
- [ ] Rotor/hinge/piston-only attachments remain excluded. A connector on such an excluded grid does not extend station traversal.
- [ ] `/mg station off` restores the single-grid framing. Restart and confirm the chosen station setting persists. Run `/mg station status` and `/mg report` for context/limit information.
- [ ] Test a large dock: up to 8 additional grids/100,000 context blocks are displayed; the report flags truncation. Record `/mg stats` and any stutter.

Notes:

## 0.7 — Automatic reconnect/Nexus reassociation (pending)

- [ ] Anchor and capture a distinctive ship with at least 8 blocks. Quit/rejoin the server, control that ship, wait several seconds: Marigold should announce the profile it automatically selected without a new capture.
- [ ] Repeat after fully restarting the game and after a Nexus sector transfer. Verify the saved damage/missing state and cockpit orientation remain correct. This is the actual Nexus acceptance test, not yet established by compilation.
- [ ] Rename the ship and use another cockpit. Matching searches cockpit candidates and must retain the correct saved anchor alignment; active-seat view direction may change normally.
- [ ] Remove a small number of blocks before reconnecting. Matching should tolerate modest loss and retain the missing-red baseline positions. Large layout changes should leave it unbound for manual selection.
- [ ] Make two saved profiles of identical/near-identical ships: automatic matching must refuse the tie. `/mg auto status` explains it; `/mg profile select ID` resolves it.
- [ ] Test delayed streaming after reconnect: the matcher waits/retries without taking a new baseline. Inspect `/mg auto status` if unbound.
- [ ] `/mg profile detach` must remain detached even after waiting or switching seats/ships in that session. `/mg auto retry` allows matching again. Detach suppression ends on session unload; use auto off for a persistent opt-out.
- [ ] `/mg auto off`, restart/rejoin, and confirm nothing auto-binds. `/mg auto on` persists and resumes matching. Turning it off leaves an already selected profile attached; detach separately if desired.
- [ ] Switch ships or select/delete a profile while matching is running; an old worker result must not override that action. A profile already attached to another live ship must not be automatically reused.
- [ ] An unsaved identical copy can structurally match the same profile; this is layout recognition, not proof of identity. Check manual override behavior for your fleet and use auto off if copies need distinct identities.

Notes:
