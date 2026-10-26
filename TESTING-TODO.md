# Review-fix acceptance — 0.14.16

These checks cover the PluginHub review fixes. We do not need another full feature retest.

1. [x] **Launch normally.** Start Pulsar Legacy, sit in a cockpit and open `/mg ui`. The panel and ship HUD should work without startup or Harmony errors.
2. [x] **Save something and restart.** Capture one test ship, change a display setting and set a shortcut. Save display, restart the game and confirm the profile, setting and shortcut are still there. Data belongs in `%APPDATA%/SpaceEngineers/Storage/Marigold` (or your selected `-appdata` folder's `Storage/Marigold`).
3. [x] **Find a local blueprint.** Search for one you know exists in the active game's Local blueprint list. It should appear and load normally.
4. [x] **Check the log is quiet.** Let the HUD run briefly, including station mode or a little damage if convenient. Run `/mg stats`: timings should still appear, but routine rebuild messages should not keep filling the game log.
5. [ ] **Only if you use custom `-appdata`:** launch that setup and confirm its profiles/settings and Local blueprint search belong to that selected folder.

For a fresh-user check, close the game and temporarily rename only the active `Storage/Marigold` folder first. No other folder cleanup is needed. The plugin will start fresh and create data in the active location when you save.

Already reported working: blueprint repairs, automatic matching, recovery controls, rendering/pulse settings and profile-number reuse. Those results stand.

The core checks above passed. Final Legacy source-loader testing with the local DLL disabled remains separate; updating the source pin does not claim that check passed. See [PR #285](https://github.com/StarCpt/PluginHub/pull/285) for the submitted pin.

Earlier results: [history](docs/history/README.md), [performance results](docs/PERFORMANCE-RESULTS.md). Interim testing remains deferred.

## October 26 results

User reports the core checks working. Log verification: `SpaceEngineers_20261026_000148401.log` contains one Marigold startup message and one requested stats report; `SpaceEngineers_20261025_233252277.log` contains one startup message. Neither contains routine Marigold snapshot/worker spam or Marigold errors. Custom `-appdata` remains optional; no separate custom-folder test is claimed by this log review.
