# Performance results — October 25, 2026

User-reported Legacy 0.14.9 test. Screenshots supplied in chat; exact hardware, FPS limit, measurement duration, repetitions and damage state were not recorded. Results are an initial observation, not final scale acceptance.

| Grid / mode | FPS reported | Geometry | Snapshot ms | Worker ms | HUD CPU mean ms | HUD CPU max ms |
| --- | --- | --- | --- | --- | --- | --- |
| 769 blocks, full display | 165 throughout | High, 739 lines, cell 1 | .21–.25 | 10.15 | .33–.36 | .43, 5.76, 5.69 |
| Same ship, bars only | 165 throughout | Same cached geometry | .21–.25 | 10.15 | .01 | .02 |
| Marigold disabled | 165 throughout | — | — | — | — | — |
| Approximately 15k blocks, display | Not separately reported | High, 1,938 lines, cell 2 | .26–.35 | 122.66 | .86–.87 | 3.20–3.90 |

Small-grid info screenshot: 769 blocks, 346 non-armor, 5,056 PCU, 153 conveyors and 29 thrusters. Recovery screenshot is a different comparison with 4,032 observed/expected blocks; do not treat it as confirmation of the 15k grid count.

Worker values repeated across screenshots describe the last geometry build, not three separate measured builds. HUD maxima are individual observed peaks, not percentiles. Worker generation runs on a background task and its wall time is not a game-thread stall measurement. Snapshot/HUD timings exclude other main-thread work, GPU and total game cost.

Interpretation: no user-observed FPS loss in the tested small-grid conditions. Equal 165 FPS does not establish equal frame-time headroom; a frame cap or other bottleneck can conceal the difference. Bars-only drawing costs much less than full display in these samples. The larger grid merits repeated rebuild and sustained-damage testing. No regression threshold or whole-plugin overhead claim is established.

Next: confirm whether 165 is capped, record hardware/settings, repeat paired trials and test the saved heavily damaged large grid. Capture build/update responsiveness as well as HUD timings. See [procedure](PERFORMANCE-TEST.md).

Recovery controls: user reports the new spacing/style is much better and controls actually work. Marked passed at the tested resolution; other scales remain optional coverage.

## Large grid under active heavy damage

October 25 user screenshot, same ongoing test session; user describes a large grid taking active damage. Exact remaining block count and FPS not supplied.

| Time | Lines / cell | Snapshot ms | Last worker ms | HUD CPU mean/max ms | Draw samples | Pending rebuild |
| --- | --- | --- | --- | --- | --- | --- |
| 02:55:27 | 9,977 / 2 | .29 | 129.78 | 4.30 / 7.60 | 113 | No pending label |
| 02:55:35 | 10,007 / 2 | .25 | 143.19 | 4.75 / 7.80 | 12 | Yes |
| 02:55:45 | 10,240 / 2 | .41 | 158.04 | 4.44 / 4.89 | 20 | Yes |

This is a significant drawing-cost increase relative to the earlier ~15k-grid sample (.86–.87 ms mean). Short draw windows reset during frequent updates; maxima are not percentiles. Pending labels describe the displayed previous model, not proof of lost updates or an indefinitely growing queue. The active-damage HUD submission alone is comparable to much of the 6.06 ms total frame budget at 165 FPS; no actual FPS loss is established without an FPS/frame-time measurement.

Prioritize profiling projection/line submission and fill generation at ~10k lines, plus pulse-region matching during result adoption. Preserve complete fault coverage. Compare damage stopped versus active, then bars-only under the same saved damage state to distinguish rendering from rebuild-related costs.

Follow-up: user reports ~85 FPS with ship display shown versus ~125 hidden during the heavy-damage case. Bars visibility has little effect; detail settings show similar cost. 0.14.10 removes redundant primary outlines, deduplicates identical same-state edges, precomputes projection axes and shared pulse phase. Actual improvement is unmeasured pending repeat testing.

0.14.10 user follow-up: ~97 FPS shown / ~135 hidden, improved from previous ~85/~125. Conditions are not a controlled paired benchmark; schematic cost remains material. 0.14.11 adds four rendering modes for a direct same-save comparison.

## 0.14.13 rendering modes — user results

October 25, same reported show/hide comparison; exact duration, repetitions and hardware not supplied.

| Mode | Shown FPS | Hidden FPS | Approx. shown frame ms | Approx. hidden frame ms | Difference ms |
| --- | --- | --- | --- | --- | --- |
| Full display | 85 | 142 | 11.76 | 7.04 | 4.72 |
| Outlines only | 133 | 142 | 7.52 | 7.04 | .48 |
| Faults with fills | 90 | 142 | 11.11 | 7.04 | 4.07 |
| Fault outlines only | 139 | 142 | 7.19 | 7.04 | .15 |

Frame-time values are 1000 divided by reported FPS, not measured frame-time averages or percentiles. Differences compare the reported conditions and are not isolated profiler measurements. The large difference between modes with and without fills points strongly toward shading/submission as the remaining rendering bottleneck. Hiding healthy hull context alone offers much less improvement. Preserve all-state coverage when optimizing fills; outline modes already provide practical lower-cost choices.

User also confirms profile numbers are reused correctly for new baselines after deletion.

## 0.14.14 user follow-up

User reports performance about the same after fill union; no new numeric measurements supplied. Do not claim a proven FPS improvement from the synthetic draw-call reduction. User prefers preserving current appearance and behavior over further marginal optimization. No additional rendering change made in response. Further renderer work is deferred unless profiling identifies a substantial benefit without visual or coverage compromises. Existing outline modes remain the tested lower-cost alternatives.
