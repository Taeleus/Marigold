# Snapshot diagnostics 0.4

## Use

Control the ship and run `/mg capture` to take or replace its reference. Capture is explicit; there is no automatic baseline replacement. `/mg report` shows counts and marker coverage. `/mg clear` removes the current ship's reference. These commands are local and do not send chat to other players.

References use the actual grid object, not its display name. Renaming and switching cockpits do not change the reference. Leaving the cockpit and returning preserves it; world unload or plugin disposal clears all references. Nothing is written to the game folders or saved to disk. Up to 16 grids / 250,000 total reference blocks are retained. Closed references are pruned on capture.

## Colors and meaning

- Cyan: current occupied hull outline.
- Gently pulsing red outline and fill: reference block position absent from the observed grid, including after splits and on multiplayer clients. This does not establish destruction; detachment or intentional removal can cause absence.
- Yellow outline with translucent yellow interior: integrity/build integrity fell below the captured value, or a present block has current damage. Welding back to the captured state removes degradation markers; capturing an already damaged ship still exposes its existing damage.
- Orange outline and fill: a matching functional block has IsFunctional=false (structural damage or incomplete construction). Overrides yellow; being switched off or unpowered alone does not trigger it.
- Purple: definition, orientation or occupied bounds changed at the same block position.
- Green: observed block at a position absent from the reference.
- Gray: invalid or unreadable integrity values on an observed block.

Matching uses grid-local block Position, definition ID, orientation and occupied bounds. Moving a block to another position produces absence plus addition. The same definition rebuilt at the same position/orientation is equivalent. Multi-cell blocks are represented once by their complete envelope. Small integrity changes below 0.01% of maximum integrity are ignored as numerical noise. Modded definitions are compared by their complete IDs.

## Refresh and limits

While visible and controlled, comparison refreshes every 60 updates (about one simulation second). Add/remove events can trigger earlier refreshes with a 30-update throttle. Re-enter the cockpit to see changes made on foot. Hidden views pause sampling. Reports flag paused/pending results.

Numeric snapshots are copied on the game thread and compared on a worker. Workers never access game objects or mutable reference dictionaries. One worker runs at a time; grid changes, captures, clears, splits and detail changes invalidate older results.

The renderer draws exact block-envelope markers over the current outline using a shared reference/current coordinate frame. Up to 128 markers are drawn, in absent/damaged/replaced/added/unknown order, then coordinate order. Reports count all changes and disclose how many markers are shown. Marker boxes add up to 1,536 lines beyond the existing hull detail budget. There is no hidden-line removal or fine block-mesh rendering.

A reference subscribes to its original grid's split event even while another ship is controlled. After a split, absent positions remain red: they are missing from the original ship even if they survive as detached pieces. The detached grid does not inherit the reference. Merges, coordinate remapping, streaming freshness and multiplayer damage synchronization require further validation; this version never labels absence as confirmed destruction. On multiplayer clients, absence is also assumed missing; markers reflect the latest locally observed layout.

## In-game acceptance test

1. Capture the miner in a cockpit; run `/mg top` for a stable view.
2. Exit and partially grind an accessible armor block, then return. It should be yellow within approximately one simulation second.
3. Remove that block completely without splitting the ship. Return and inspect the red missing-position box (also red on multiplayer clients).
4. Rebuild and weld the same block at its original position. Its marker should clear.
5. Add a new block: green. Replace a reference block with a different definition or orientation at the same anchor: purple.
6. Rename the grid, leave/re-enter, and switch ships; confirm references stay separate.
7. Test a deliberate split in a disposable test world: absent positions on the original grid should remain red; the new grid should have no inherited reference.
8. Clear the reference: colored markers disappear after rebuilding. Reload the world: references are gone.

Pure comparison, normalization and command checks pass. Compilation verifies the installed API signatures. In-game colors, refresh, split event delivery and multiplayer behavior remain pending user validation.

0.4.4: shaded yellow, orange and red markers share the fill budget. Missing markers pulse smoothly every two seconds. Added/replaced states retain precedence over structural classification.
