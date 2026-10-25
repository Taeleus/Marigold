# Release acceptance

**Marigold 0.14.14 — Pulsar Legacy. Updated October 25, 2026.**

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
- [ ] Validate the Legacy descriptor against current PluginHub checks, then submit its PR when ready.

Interim runtime testing is deferred and outside this release. No additional feature work is currently planned for Legacy.

Detailed prior results: [testing through 0.14.8](docs/history/TESTING-HISTORY-through-0.14.8.md), [testing through 0.14.14](docs/history/TESTING-HISTORY-through-0.14.14.md), [performance results](docs/PERFORMANCE-RESULTS.md).
