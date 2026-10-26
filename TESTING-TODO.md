# Release acceptance

**Submitted candidate: 0.14.14. Development review fixes: 0.14.15 — Pulsar Legacy. Updated October 25, 2026.**

## User-reported passes

- [x] Blueprint baselines recognize repairs block by block.
- [x] Automatic baseline matching works after the occupied-seat and stability fixes.
- [x] Recovery controls and inspection camera work correctly.
- [x] Rendering/performance modes, pulse settings, saved configuration and final regressions work.
- [x] Deleted profile numbers are reused for new baselines.
- [x] Performance comparisons completed; outline modes are effective. Further marginal rendering work is deferred.

## Final distribution checks

- [ ] Load the final committed source pin through Pulsar Legacy with the local DLL disabled.
- [ ] Fresh-install smoke test of the final package, preserving existing data/backups.
- [x] Legacy 0.14.14 descriptor validated and submitted in [PluginHub PR #285](https://github.com/StarCpt/PluginHub/pull/285). The review-fix source pin is not updated yet.

Interim runtime testing is deferred and outside this release. The 0.14.15 review corrections are being validated on dev.

Detailed prior results: [testing through 0.14.8](docs/history/TESTING-HISTORY-through-0.14.8.md), [testing through 0.14.14](docs/history/TESTING-HISTORY-through-0.14.14.md), [performance results](docs/PERFORMANCE-RESULTS.md).


## 0.14.15 review-fix acceptance — pending

Use copied test data and close the game before manipulating folders. Preserve the real profile folders and backups.

- [ ] Default-location upgrade: with old-format-location data present and the new destination absent, start the dev build. Verify profiles, global/per-ship displays, shortcuts, presets, versions, recovery bindings and backups are copied to `%APPDATA%/SpaceEngineers/Storage/Marigold` and the original tree is unchanged. Restart and confirm migration does not run again.
- [ ] Existing destination: place distinct data in both locations before startup. The destination must remain unchanged, with no merge or overwrite of the old source.
- [ ] Migration failure/retry: use disposable data to cause a copy failure. The original stays intact, a partial destination is not used, and a later retry succeeds after the failure is removed. Check the diagnostic message.
- [ ] Custom `-appdata`: use a separate test user-data folder. Saves and preferences must stay in its `Storage/Marigold`; the shared old `%APPDATA%/Marigold` tree must not be imported automatically. Confirm the normal installation's data remains untouched.
- [ ] Local blueprints: search using default and custom `-appdata` folders containing different test blueprint names. Each search must use the active game's `Blueprints/local` folder. Workshop and Cloud behavior should remain unchanged.
- [ ] Logging: observe idle station mode and sustained damage. No routine `Schematic snapshot:` or `Schematic worker:` entries should accumulate. `/mg stats` must still return timings; real errors must still be logged.
- [ ] Legacy build/source compilation and regression checks pass after removing only the manifest's Harmony dependency. Load through Pulsar and confirm the HUD patch still initializes using the host-provided Harmony.

No migration has been run against the real game data by the development tests, and these new in-game checks are not marked passed by older regression results.
