> Historical document. Current instructions: [user guide](../../README.md), [active testing](../../TESTING-TODO.md).

# Schematic prototype 0.2

Enter a ship controller to see an automatically rotating cyan outline in the upper-right HUD. The previous name/count notification remains. This is a main-grid spatial schematic, not a full mesh hologram. Large ships are grouped into at most 256 bounding cells; every block contributes to an envelope, but empty spaces can be filled by coarse grouping. Small/modded blocks use their grid-aligned Min/Max envelope. No occlusion or category filtering yet.

## Controls (updated in 0.2.1)

The former Ctrl+Alt keybinds have been removed. Use local chat commands instead; see [Control correction 0.2.1](CONTROLS-0.2.1.md).

## Architecture and limits

- One explicitly targeted Harmony 2.4.2 postfix on `MyGuiScreenHudSpace.Draw`. No original behavior or result is modified. It draws only after a successful HUD draw and removes only its own postfix on disposal.
- Sprite lines use the game's Blank.dds texture, projected orthographically in pixel space; zoomed segments are clipped to the panel. The installed sprite implementation confirms rotation about the rectangle center.
- Block add/remove events mark the snapshot dirty. Rebuild requests are rate-limited to every 30 updates with a 600-update reconciliation interval. There is no per-frame block scan.
- Snapshot collection reads game objects on the game update thread. Coarsening and normalization run on one worker using only copied numeric boxes. A generation number rejects results from a previously controlled grid. Drawing reads completed point arrays; it never waits for the worker.
- At most 256 cells / 3,072 line sprites per rendered frame. This cap bounds cost; it does not prove acceptable GPU or render-thread performance. Detailed mesh rendering, deduplicated edges, stronger batching and better shape preservation remain future work.
- Synthetic CPU geometry results on this machine: approximately 4 ms / 25 ms / 88 ms for 1k / 10k / 50k input blocks. These measure worker computations only, not snapshot collection, draw time or game frame time. The 50k result is why processing runs off-thread.

## Required in-game checks

- [ ] Restart Legacy with 0.2 and enter both test ships. Confirm the panel contains a recognizable rotating outline.
- [ ] Verify /mg show, hide, spin, reset, rotation and both zoom directions. Confirm no drawing escapes the panel.
- [ ] Exit, switch ships quickly, rename, add/remove a block and reload a world. No stale geometry or retained grid handlers.
- [ ] Open terminal/chat, hide HUD and pause. No control surprises or stale overlay on menus.
- [ ] Inspect `[Marigold] Schematic snapshot` and `Schematic worker` log timings. Check for drawing/initialization failures.
- [ ] Compare actual frame times with schematic visible/hidden; assess large ships before increasing detail.

Automated checks: release compile against installed assemblies, 13 observer checks using API doubles, and 24 pure geometry checks. The observer double does not exercise the actual Harmony hook or asynchronous renderer integration. In-game rendering remains unverified until tested.

