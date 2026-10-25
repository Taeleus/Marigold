> Historical document. Current instructions: [user guide](../../README.md), [active testing](../../TESTING-TODO.md).

# Profiles and cockpit anchors — 0.5

## First setup

1. Sit in the cockpit that should define this ship's anchor.
2. Run `/mg anchor`. This creates a profile with a stable ID. It does not change the game's Main cockpit setting.
3. Run `/mg capture` to save the current ship layout as its baseline.
4. Use `/mg report` for differences and the active profile ID.

Switching seats changes the view only; it does not move the baseline. Renaming a ship does not change its profile ID. Capturing again explicitly replaces that profile's baseline. Running anchor again while a profile is selected changes the anchor while preserving the existing baseline's grid positions.

## After reload, reconnect, or transfer

Profiles are loaded from disk when the plugin starts. World unload clears live grid bindings, not saved profiles. Sit in the matching cockpit, run `/mg profiles`, and use `/mg profile select ID` (the displayed eight-character ID is enough unless ambiguous).

Selection explicitly pairs the current cockpit with the saved anchor. Cockpit definition and grid size must match. Choose the same corresponding cockpit when a ship has several identical seats, and inspect alignment before capturing again. Layout matching is not automatic in this version. A profile already bound to another live ship in the session cannot be selected until detached there. Closed grid bindings do not prevent reassociation.

This manual path is intended to support reconnects without relying on runtime grid IDs or names. It has not been validated on a live Nexus transfer. Automatic Nexus matching, surrounding-layout validation and blueprint imports remain future work.

## Commands

| Command | Effect |
| --- | --- |
| `/mg anchor` | Create a new profile from the seated cockpit, or re-anchor the selected profile while preserving its baseline |
| `/mg capture` | Persist the selected profile's current main-grid layout |
| `/mg profiles` | List saved profile IDs, names and baseline sizes; works without a cockpit |
| `/mg profile select ID` | Pair the selected profile with the currently seated matching cockpit |
| `/mg profile detach` | Disconnect the current grid from its profile; does not delete saved data |
| `/mg clear` | Persistently remove the selected baseline but retain profile and anchor |
| `/mg profile delete ID` | Delete the named profile and detach its live bindings; previous database remains in backup |
| `/mg report` | Show current comparison and selected profile ID |
| `/mg recover` | Explicitly restore a valid backup after a load error; preserves the unreadable original |

`/mg claim` aliases anchor. `/marigold` remains an alias for `/mg`. To give the same grid a separate new profile, detach first, then anchor. Each capture requires a selected profile. Remote-control blocks cannot be used to create or pair a cockpit anchor; an already bound grid can still display while remotely controlled.

## Storage and alignment

Files are in `%APPDATA%/Marigold/` (outside SpaceEngineers and the plugin installation). `profiles.bin` uses a versioned binary format. `profiles.bin.bak` holds the previous successful database. Updates use a flushed temporary file and atomic replacement. Profile writes commit to memory only after disk replacement succeeds. Invalid data or unsupported schemas disable writes; a valid backup may be loaded read-only until explicit recovery. Existing unreadable data is not silently replaced with an empty database.

Limits: 16 profiles, 250,000 total baseline blocks, 64 MB database, bounded strings and validated discrete grid coordinates/orientations. Profile filenames are fixed; names and IDs are never used as filesystem paths. Writes occur only on explicit profile commands; unload and rendering do not write to disk. Large captures save synchronously and may briefly pause the game.

Baselines store blocks relative to the anchor's right/up/backward axes and position. Selection transforms positions, all eight envelope corners and block forward/up directions into the new grid. Damage comparison still uses exact block definitions, orientations and envelopes. The live viewing basis follows the active cockpit separately from the saved baseline.

## Validation

119 profile persistence/alignment checks cover 24 discrete cockpit orientations, multi-cell bounds, disk roundtrips, same-name profiles, invalid saves, clear/delete, corruption, backups, recovery, unsupported schema and command parsing. The plugin compiles against the installed net48 game assemblies; file/geometry tests run on .NET 10. Runtime profile commands, framework file behavior and reload alignment still need in-game confirmation.

First test: anchor and capture the miner, modify one block, reload the world, select the profile from the same cockpit, and verify the saved difference reappears. Switch seats and return to ensure baseline alignment stays fixed. Repeat with a second similarly named ship to verify profiles remain distinct.
