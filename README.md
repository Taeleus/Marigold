# Marigold

A ship schematic and damage display for **Space Engineers 1**, built for **Pulsar Legacy**.

Marigold shows the grid you are piloting and compares it with a saved reference. Spot damaged or missing blocks, inspect your ship from cockpit-relative views, and use a blueprint as the layout your ship should have.

**Current version: 0.8.0 prototype.** Builds and automated checks pass; the new interface, performance changes and Nexus behavior still need in-game testing. Track those checks in [TESTING-TODO.md](TESTING-TODO.md).

## Get started

1. Enable **Marigold (prototype)** in Pulsar Legacy and launch the game.
2. Sit in the cockpit you want to use as your ship's reference point.
3. Type **`/mg ui`** to open Marigold's controls.
4. In **Profiles**, choose **Set anchor**, then **Capture** to save the current ship as your reference. Click Capture again when prompted to confirm.
5. Close the panel to see the schematic. Damage and missing blocks update as the ship changes.

Prefer chat commands? Use `/mg anchor`, then `/mg capture`. `/mg` shows help, and `/marigold` works as an alias. `/m` is reserved by the game.

## What the colors mean

| Color | Meaning |
| --- | --- |
| Faint yellow | Damaged or degraded |
| Orange | Too damaged or unfinished to function |
| Pulsing red | Missing from the saved reference |
| Green | Added since the reference |
| Purple | Replaced with a different block or orientation |
| Gray | Block health could not be read |

A detached block counts as missing from the observed grid, even if it still exists nearby. A block being switched off or unpowered does not make it structurally non-functional.

## Use the control panel

- **Display:** choose systems and damage states to show, adjust detail and layout, or toggle station mode and automatic profile matching.
- **Profiles:** create or select a reference, capture changes, detach a profile, or view a report. Clearing, deleting and replacing saved data requires a second click in the panel.
- **Blueprints:** find local blueprints or enter a full `bp.sbc` path, load the file, refresh the lists, prepare the intended grid, then choose its matching cockpit. Preview the alignment before accepting it as your baseline.
- **Views:** snap to a side, zoom, rotate, browse marker pages, or save/load your display settings.

The interface has English and Spanish labels. Diagnostic messages remain in English. Use **Save settings** to keep ordinary display changes; station, automatic matching and language choices save immediately.

## Returning to your ship

Profiles survive game restarts and server reconnects. Automatic matching is on by default and tries to select a unique saved layout once the ship has loaded. It preserves the saved baseline rather than capturing a new one.

If matching is uncertain, select a profile yourself from the corresponding cockpit. `/mg auto status` explains the current state; `/mg auto off` disables automatic matching persistently. `/mg profile detach` undoes a binding and prevents it from being selected again for that grid during the session, unless you use `/mg auto retry`.

Identical copies cannot reliably be distinguished by layout alone. Nexus transfer handling is implemented as best-effort client-side matching and remains unverified in-game.

## Station mode

Station mode is **off by default**. Enable it in Display or with `/mg station on` to show grids attached through locked connectors.

Docked grids are visual context only: they do not become part of your ship's captured baseline or damage counts. Rotor, hinge and piston subgrids are outside the current scope. Large docked groups have display limits; `/mg report` tells you when context is loading or limited.

## Files and useful links

Profiles and preferences are stored locally in `%APPDATA%/Marigold`, with backups. Blueprint files are read-only; Marigold does not spawn blocks, run blueprint scripts or edit the world.

- [All commands and detailed behavior](docs/COMMANDS.md)
- [Testing to-do](TESTING-TODO.md)
- [Roadmap and release status](ROADMAP.md)
- [Release preparation](docs/RELEASE.md)
- [Earlier development notes](docs/DEVELOPMENT-HISTORY.md)

## Build and install

Development needs a .NET SDK, .NET Framework 4.8 reference assemblies, and a local Space Engineers installation. Automated checks currently target .NET 10; the plugin itself targets .NET Framework 4.8.

```powershell
.\build.ps1
# If your game is installed elsewhere:
.\build.ps1 -Bin64 'D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64'
```

The build creates `artifacts/Marigold` with `plugin.dll`, `plugin.pdb` and `plugin.xml`. With the game closed, copy these into a folder such as `%APPDATA%/Pulsar/Legacy/Local/MarigoldDev`, enable the plugin and restart. Avoid enabling duplicate source and binary installations. Building alone never deploys or launches the game.

`verify-source.ps1` also compiles the production source selection without local-build defines. `prepare-release.ps1` prepares a commit-pinned PluginHub manifest after source changes are committed. These checks do not replace loading the source package in Pulsar or completing the in-game checklist.
