# Quick test — 0.14.16 dev

These checks cover the PluginHub review fixes. We do not need another full feature retest.

1. [ ] **Launch normally.** Start Pulsar Legacy, sit in a cockpit and open `/mg ui`. The panel and ship HUD should work without startup or Harmony errors.
2. [ ] **Save something and restart.** Capture one test ship, change a display setting and set a shortcut. Save display, restart the game and confirm the profile, setting and shortcut are still there. Data belongs in `%APPDATA%/SpaceEngineers/Storage/Marigold` (or your selected `-appdata` folder's `Storage/Marigold`).
3. [ ] **Find a local blueprint.** Search for one you know exists in the active game's Local blueprint list. It should appear and load normally.
4. [ ] **Check the log is quiet.** Let the HUD run briefly, including station mode or a little damage if convenient. Run `/mg stats`: timings should still appear, but routine rebuild messages should not keep filling the game log.
5. [ ] **Only if you use custom `-appdata`:** launch that setup and confirm its profiles/settings and Local blueprint search belong to that selected folder.

For a fresh-user check, close the game and temporarily rename only the active `Storage/Marigold` folder first. No other folder cleanup is needed. The plugin will start fresh and create data in the active location when you save.

Already reported working: blueprint repairs, automatic matching, recovery controls, rendering/pulse settings and profile-number reuse. Those results stand.

Before updating the PluginHub source pin, we still need a final Legacy source-loader check with the local DLL disabled. That is separate from this short dev-build test. Master and [PR #285](https://github.com/StarCpt/PluginHub/pull/285) remain on 0.14.14 for now.

Earlier results: [history](docs/history/README.md), [performance results](docs/PERFORMANCE-RESULTS.md). Interim testing remains deferred.
