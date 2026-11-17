# Marigold roadmap

**Published** · 0.17.15 for Pulsar Legacy. **Next release** · 0.17.16 for Legacy and Interim on Windows and Proton. [PluginHub entry](https://github.com/StarCpt/PluginHub/blob/main/Plugins/Marigold.xml) · [Changelog](CHANGELOG.md)

## Next PluginHub update

| Work | Status |
| --- | --- |
| Station mode access and lifecycle | Local station-mode checks passed. Performance at a busy multiplayer dock remains unmeasured on this build. |
| HUD performance fixes | Local checks passed for the HD Wireframe pulse and CPU/GPU renderer transitions. |
| Pulsar Interim | The Windows package loaded and passed in-game testing. The candidate listing allows Windows and Proton. |
| PluginHub submission | Verify that Pulsar compiles and loads the proposed source pin, then submit the descriptor and confirm the public listing after publication. |

The [release guide](docs/RELEASE.md) covers local source testing and publication. [Performance results](docs/PERFORMANCE-RESULTS.md) records in-game measurements from earlier builds.

## Ideas for later

- **Native Linux Interim.** Resolve model and texture paths using Linux separators and on-disk casing. Test CPU and GPU Textured Ship in game before widening the listing.
- **Pan the schematic.** Let players pan the ship model within the HUD display.
- **Spanish translation.** Translate remaining diagnostics as gaps are reported.
- **Large-grid rendering.** Use [measured results](docs/PERFORMANCE-RESULTS.md) to guide further work on filled rendering, textured detail, and sustained damage.

## Share feedback

[Open an issue](https://github.com/Taeleus/Marigold/issues) with what you were trying to do and what would make it easier. For a bug, include your Marigold version, launcher, and steps to reproduce.

Completed features are in the [changelog](CHANGELOG.md). [Historical roadmap](docs/history/ROADMAP-HISTORY-through-0.14.14.md)
