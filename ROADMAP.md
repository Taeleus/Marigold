# Marigold roadmap

**Published** · 0.17.16 for Pulsar Legacy and Interim on Windows and Proton. [PluginHub entry](https://github.com/StarCpt/PluginHub/blob/main/Plugins/Marigold.xml) · [Changelog](CHANGELOG.md)

## 0.17.16 release status

| Work | Status |
| --- | --- |
| Station mode access and lifecycle | Earlier local station-mode checks passed. The station-visibility fixes passed 48 offline checks. Performance at a busy multiplayer dock remains unmeasured on this build. |
| HUD performance fixes | Local checks passed for the HD Wireframe pulse and CPU/GPU renderer transitions. |
| Pulsar Interim | The local Windows package passed in-game testing before the station-visibility fixes. |
| PluginHub publication | Published November 18, 2026. The XML check and offline source compilation for both runtimes passed. Installation and in-game loading from the live listing remain unverified. [Merged PR](https://github.com/StarCpt/PluginHub/pull/362) |

The [release guide](docs/RELEASE.md) covers local source testing and publication. [Performance results](docs/PERFORMANCE-RESULTS.md) records in-game measurements from earlier builds.

## Ideas for later

- **Native Linux Interim.** Resolve model and texture paths using Linux separators and on-disk casing. Test CPU and GPU Textured Ship in game before widening the listing.
- **Pan the schematic.** Let players pan the ship model within the HUD display.
- **Spanish translation.** Translate remaining diagnostics as gaps are reported.
- **Large-grid rendering.** Use [measured results](docs/PERFORMANCE-RESULTS.md) to guide further work on filled rendering, textured detail, and sustained damage.

## Share feedback

[Open an issue](https://github.com/Taeleus/Marigold/issues) with what you were trying to do and what would make it easier. For a bug, include your Marigold version, launcher, and steps to reproduce.

Completed features are in the [changelog](CHANGELOG.md). [Historical roadmap](docs/history/ROADMAP-HISTORY-through-0.14.14.md)
