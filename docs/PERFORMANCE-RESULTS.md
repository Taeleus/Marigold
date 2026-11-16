# Performance results

The current PluginHub release is 0.17.15 for Pulsar Legacy. The newer results below cover specific rendering checks on Legacy. The October results document the older 0.14 renderer. Neither set establishes current frame-time costs on Interim or a performance threshold across grid sizes.

## Textured Ship and GPU checks on Legacy

| Build and test | Recorded result |
| --- | --- |
| 0.17.2, textured station | The 2,257-block station showed improved shapes. At 768 px, `/mg stats` reported 16.5 preview FPS, a 48.47 ms last raster and a 181.77 ms last mesh job. |
| 0.17.14, GPU preview after a collision | At 768 px, `/mg stats` showed 25.0 preview FPS before the collision and 23.4 after repair and recovery, with 566 damage fills in the latter view. Seat transitions and switching between CPU and GPU also passed. |
| 0.17.15, GPU startup and display changes | The wireframe stayed visible until the first GPU frame. Resolution changes and switching between fullscreen and borderless worked. |

Preview FPS measures updates to the ship image. It is separate from game FPS. The samples used different builds and scenarios. Measuring a CPU versus GPU speedup requires paired tests. The 0.17.16 pulse redraw, GPU restart, and busy station changes still need in-game measurements. [Repeatable performance test](PERFORMANCE-TEST.md)

## October 25, 2026 — 0.14.9 Legacy

Screenshots supplied in chat. Hardware, FPS limit, sample duration, repetitions, and damage state were not recorded.

| Grid / mode | FPS reported | Geometry | Snapshot ms | Worker ms | HUD CPU mean ms | HUD CPU max ms |
| --- | --- | --- | --- | --- | --- | --- |
| 769 blocks, full display | 165 throughout | High, 739 lines, cell 1 | .21–.28 | 10.15 | .33–.36 | .43, 5.76, 5.69 |
| Same ship, bars only | 165 throughout | Same cached geometry | .21–.25 | 10.15 | .01 | .02 |
| Marigold disabled | 165 throughout | — | — | — | — | — |
| Approximately 15k blocks, display | Not separately reported | High, 1,938 lines, cell 2 | .26–.35 | 122.66 | .86–.87 | 3.20–3.90 |

Small-grid info screenshot: 769 blocks, 346 non-armor, 5,056 PCU, 153 conveyors and 29 thrusters. Recovery screenshot is a different comparison with 4,032 observed/expected blocks; do not treat it as confirmation of the 15k grid count.

Worker values repeated across screenshots describe the last geometry build, not three separate measured builds. HUD maxima are individual observed peaks, not percentiles. Worker generation runs on a background task and its wall time is not a game-thread stall measurement. Snapshot/HUD timings exclude other main-thread work, GPU and total game cost.

Interpretation: no user-observed FPS loss in the tested small-grid conditions. Equal 165 FPS does not establish equal frame-time headroom; a frame cap or other bottleneck can conceal the difference. Bars-only drawing costs much less than full display in these samples. The larger grid merits repeated rebuild and sustained-damage testing. No regression threshold or whole-plugin overhead claim is established.

The same user reported that the recovery controls worked at their tested resolution.

## October 25, 2026 — large grid under active heavy damage

October 25 user screenshot, same ongoing test session; user describes a large grid taking active damage. Exact remaining block count and FPS not supplied.

| Time | Lines / cell | Snapshot ms | Last worker ms | HUD CPU mean/max ms | Draw samples | Pending rebuild |
| --- | --- | --- | --- | --- | --- | --- |
| 02:55:27 | 9,977 / 2 | .29 | 129.78 | 4.30 / 7.60 | 113 | No pending label |
| 02:55:35 | 10,077 / 2 | .25 | 143.19 | 4.75 / 7.80 | 12 | Yes |
| 02:55:45 | 10,240 / 2 | .41 | 158.04 | 4.44 / 4.89 | 20 | Yes |

This is a significant drawing-cost increase relative to the earlier ~15k-grid sample (.86–.87 ms mean). Short draw windows reset during frequent updates; maxima are not percentiles. Pending labels describe the displayed previous model, not proof of lost updates or an indefinitely growing queue. The active-damage HUD submission alone is comparable to much of the 6.06 ms total frame budget at 165 FPS; no actual FPS loss is established without an FPS/frame-time measurement.

The user later reported about 85 game FPS with the ship display shown and 125 hidden during heavy damage. Toggling the bars or changing outline detail had little effect. Version 0.14.10 reduced outline duplication and reused projection axes and pulse phase.

0.14.10 user follow-up: ~97 FPS shown / ~135 hidden, improved from previous ~85/~125. Conditions are not a controlled paired benchmark; schematic cost remains material. 0.14.11 adds four rendering modes for a direct same-save comparison.

## 0.14.13 rendering modes — user results

October 25, same reported show/hide comparison; exact duration, repetitions and hardware not supplied.

| Mode | Shown FPS | Hidden FPS | Approx. shown frame ms | Approx. hidden frame ms | Difference ms |
| --- | --- | --- | --- | --- | --- |
| Full display | 85 | 142 | 11.76 | 7.04 | 4.72 |
| Outlines only | 133 | 142 | 7.52 | 7.04 | .48 |
| Faults with fills | 90 | 142 | 11.11 | 7.04 | 4.07 |
| Fault outlines only | 139 | 142 | 7.19 | 7.04 | .15 |

Frame-time values are 1,000 divided by reported FPS. The difference between modes with and without fills suggests shading or submission accounted for much of the cost in this build. Hiding only healthy hull context yielded a smaller change.

User also confirms profile numbers are reused correctly for new baselines after deletion.

## 0.14.14 user follow-up

The user reported similar performance after overlapping fills were merged. No new numbers were supplied. That retest favored preserving appearance and damage coverage. The outline-only modes were the lower-cost options in the 0.14 trials.
