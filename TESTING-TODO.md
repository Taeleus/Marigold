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
