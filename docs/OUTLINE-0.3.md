# Outline 0.3

The renderer now extracts exposed faces from occupied grid cells, cancels shared coplanar edges, and merges contiguous collinear segments. It no longer draws overlapping spatial-group envelopes. At cell size 1, grid-cell gaps, recesses and openings are preserved. Sealed cavity boundaries can still appear because this is a transparent wireframe, without hidden-line removal.

This is an outline of occupied block envelopes, not individual meshes: slopes, windows, interior spaces within a block model, and decorative shapes still use the block's rectangular footprint. Mechanical subgrids remain deferred.

## Controls

Existing controls are unchanged. Added local chat commands:

- `/mg detail low`: maximum 768 lines.
- `/mg detail medium`: maximum 2,048 lines (default).
- `/mg detail high`: maximum 4,096 lines.
- `/mg stats`: last snapshot and worker build times, line count, cell size, and mean/maximum HUD CPU submission time. Also writes these measurements to the normal game log.

Each level starts at one grid cell and doubles cell size only when needed to meet the line or 100,000 occupied-cell budget. Simple ships can therefore look identical at all three levels. Larger cell sizes fill small openings; `/mg stats` reports the actual cell size. Every envelope contributes occupancy; no random block sampling. View reset preserves detail. Settings currently last only for the plugin session.

Geometry builds run on an immutable numeric snapshot in one background task. Rapid grid/detail changes reject obsolete results. Snapshots still read game objects on the game update thread. Hidden outlines defer rebuilding. Event updates are throttled, with a periodic reconciliation scan.

## Verification

Automated checks cover cubes, joined blocks, L-shaped recesses, a ring opening, separated blocks, multi-cell envelopes, deterministic ordering, clipping, normalization, duplicate edges, detail limits, large envelopes and existing cockpit controls. Synthetic 1k/10k/50k solid rectangular hulls retain cell size 1 and reduce to 12 lines. Tests run on .NET 10; production compiles for .NET Framework 4.8 against installed game assemblies. These measurements are not a game frame-rate benchmark.

## In-game comparison

On both Lemon Drop and OPS Miner, inspect left/right/top/bottom views. Compare low, medium and high, noting the line count and cell size from `/mg stats`. After several seconds with chat closed, run `/mg stats`. HUD CPU timing covers transformation, clipping and sprite submission, not GPU rendering or unrelated game work. Samples reset after a rebuild or 600 drawn frames; grid changes clear them. Compare the game's frame rate with `/mg hide` and `/mg show` while keeping the camera and scene unchanged. Confirm adding/removing a block refreshes the outline, and changing grids during a rebuild never shows the previous ship.

In-game 0.3 appearance and performance remain pending user testing.
