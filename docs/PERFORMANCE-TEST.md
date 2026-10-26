# Repeatable in-game performance test

Use a disposable local copy of one saved world. Keep its original intact and reload that same copy before each trial. Repeat on Legacy and Interim separately; do not compare different launchers as if only Marigold changed.

## Fix the conditions

Record game/Pulsar/Marigold versions, CPU/GPU/RAM, resolution, UI scale, graphics settings, FPS cap, enabled mods/plugins and simulation speed. Use the same camera position, cockpit, ship blueprint, baseline and saved HUD preset. Disable other overlays if possible. Avoid multiplayer for the initial comparison: server load and replication add noise.

Prepare named saves for approximately 1k, 10k and 50k baseline blocks. For each size keep (a) intact, (b) a fixed heavily damaged state with yellow/orange/red, and optionally (c) the same connector dock. Create damage once and save it rather than improvising destruction for every run. For sustained-fire testing use the same fixed target, weapon count, range and duration, reloading between trials.

## Run paired trials

1. Warm up for 60 seconds after the world and display finish loading. Wait for stable simulation speed.
2. Measure 60 seconds with the plugin disabled (restart as required). Record frame-time median/p95/p99 using the same frame-time tool each time, or clearly label FPS-only measurements if that is all you have.
3. Reload the same save with Marigold enabled, same preset and filters. Warm up, then measure another 60 seconds. Run `/mg stats` at 15, 30, 45 and 60 seconds; copy the matching `[Marigold]` log entries after exiting the game. Do not type during frame-time sampling if possible; take stats immediately adjacent to each interval instead.
4. Repeat the disabled/enabled pair three times, alternating which goes first to reduce order bias. Compare medians across trials rather than selecting the fastest run.
5. On the enabled build compare schematic+bars, bars-only and hidden HUD. Hidden HUD is not equivalent to disabling the plugin. Test static view and spinning separately, then station mode off/on using the fixed dock save.
6. Repeat the sustained-fire trial with the same setup and duration. Record whether updates lag or any damage category disappears. Check after fire stops that the final report, bars and display settle to the expected state.

`/mg stats` reports last snapshot/worker time and displayed-model HUD CPU mean/max, with up to 600 draws per statistics window; rebuilds reset draw samples. It does not report GPU time, total plugin overhead or a true percentile. Capture several samples; do not present its maximum as p99. Rebuild-pending messages mean timings describe the previous model. Routine rebuild timings are no longer logged starting in 0.14.15. Explicit `/mg stats` requests still log the displayed timings; error logs remain. These are not a complete profiler.

## Record results

Copy this table into your own results file; one row per trial/configuration:

| Runtime/version | Save/block count | Damage/dock | Preset/view | Trial | Frame ms median/p95/p99 | Sim speed | Snapshot ms | Worker ms | HUD mean/max ms | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| | | | | | | | | | | |

Keep saves, presets, raw frame-time data and relevant logs together. Flag repeatable regressions larger than trial-to-trial noise; investigate spikes and visible lag even if averages look good. Set an acceptable frame budget for your hardware before judging results (16.7 ms total frame time at 60 FPS is a useful reference, not a plugin budget). These tests exercise real rendering; synthetic geometry benchmarks alone do not.
