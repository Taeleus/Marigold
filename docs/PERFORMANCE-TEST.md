# Repeatable in-game performance test

Use a disposable local copy of a saved world and reload the same starting state before each trial. Measure Legacy and Interim separately so launcher overhead does not change between paired runs.

## Fix the conditions

Record game/Pulsar/Marigold versions, CPU/GPU/RAM, resolution, UI scale, graphics settings, FPS cap, enabled mods/plugins and simulation speed. Use the same camera position, cockpit, ship blueprint, baseline and saved HUD preset. Disable other overlays if possible. Avoid multiplayer for the initial comparison: server load and replication add noise.

Prepare named saves for approximately 1k, 10k and 50k baseline blocks. For each size keep (a) intact, (b) a fixed heavily damaged state with yellow/orange/red, and optionally (c) the same connector dock. Create damage once and save it rather than improvising destruction for every run. For sustained-fire testing use the same fixed target, weapon count, range and duration, reloading between trials.

## Run paired trials

1. Warm up for 60 seconds after the world and display finish loading. Wait for stable simulation speed. Measure first-frame startup separately if comparing load time.
2. Measure 60 seconds with the plugin disabled (restart as required). Record frame-time median/p95/p99 using the same frame-time tool each time, or clearly label FPS-only measurements if that is all you have.
3. Reload the same save with Marigold enabled and the same preset and filters. Warm up, record `/mg stats`, then measure another 60 seconds with chat closed. Record `/mg stats` again immediately afterward. Use separate runs if you need more diagnostics during a specific part of the trial.
4. Repeat the disabled/enabled pair three times, alternating which goes first to reduce order bias. Compare medians across trials rather than selecting the fastest run.
5. With Marigold enabled, compare schematic plus bars, bars only, and all HUD elements hidden. For bars only, turn off **Ship display → Show ship schematic**. To hide all Marigold HUD elements use `/mg hide`. Keep the fully disabled run as a separate control.
6. Repeat the sustained-fire trial with the same setup and duration. Record whether updates lag or any damage category disappears. Check after fire stops that the final report, bars and display settle to the expected state.

## Compare display settings

- Keep view and scale fixed while comparing **HD Wireframe**, **SD Wireframe**, and **Outlines only**. For the 0.17.16 redraw change, stop rotation on a grid with missing blocks and run the same test with **Pulse missing blocks** on and off.
- Compare **Textured Ship** with **CPU** and **GPU (experimental)** under **Preferences → Performance → Textured Ship renderer**. Match preview dimensions and compact or expanded layout. Check the renderer reported by `/mg stats` and record if the display stays on wireframe while a textured frame loads or cannot draw. Compare the first frame and rotation separately.
- Compare station mode on and off in the same static station with eligible connector-docked grids. Repeat with the HUD hidden if measuring the cost of tracking connections. Record the number of docked grids and whether they have active baselines.

## Read `/mg stats`

Use the last snapshot and worker times to describe rebuild work. Worker time is elapsed background work. HUD CPU mean and maximum cover up to 600 draws and reset on a rebuild. When a rebuild is pending, geometry and draw statistics refer to the model still on screen. Use game frame-time samples for percentiles and total cost.

Textured Ship adds preview dimensions, preview update rate and last mesh time. CPU mode reports raster time. GPU mode reports CPU time spent submitting graphics work. For GPU rendering time use a GPU profiler. Copy the `[Marigold]` log lines for your explicit `/mg stats` requests if the chat output is hard to capture.

## Record results

Copy this table into your own results file; one row per trial/configuration:

| Runtime/version | Save/block count | Damage/dock | Preset/view | Trial | Frame ms median/p95/p99 | Sim speed | Snapshot ms | Worker ms | HUD mean/max ms | Notes |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| | | | | | | | | | | |

Keep saves, presets, raw frame-time data and relevant logs together. Flag repeatable regressions larger than trial-to-trial noise; investigate spikes and visible lag even if averages look good. Set an acceptable frame budget for your hardware before judging results (16.7 ms total frame time at 60 FPS is a useful reference, not a plugin budget). These tests exercise real rendering; synthetic geometry benchmarks alone do not.
