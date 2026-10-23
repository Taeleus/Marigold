# Marigold

A ship schematic and damage display for **Space Engineers 1**, built for **Pulsar Legacy**.

Marigold shows the grid you are piloting and compares it with a saved reference. Spot damaged or missing blocks, inspect your ship from cockpit-relative views, and use a blueprint as the layout your ship should have.

**Stable: 0.12.6. Development: 0.14.1.** The 0.14.0 checklist has user-reported passes. The new 0.14.1 recovery layout and final release checks remain in [TESTING-TODO.md](TESTING-TODO.md).

## Get started

1. Enable **Marigold (prototype)** in Pulsar Legacy and launch the game.
2. Sit in the cockpit you want to use as your ship's reference point.
3. Type **`/mg ui`** to open Marigold's controls.
4. In **Baseline**, choose **Capture this ship** to save the current ship as your reference. Click Capture again when prompted to confirm.
5. Close the panel to see the schematic. Damage and missing blocks update as the ship changes.

Prefer chat commands? Use `/mg capture`; it saves the seated cockpit as the anchor automatically. Captured blocks are treated as fully repaired, so existing damage remains visible. `/mg` shows help, and `/marigold` works as an alias. `/m` is reserved by the game.

## What the colors mean

| Color | Meaning |
| --- | --- |
| Faint yellow | Damaged or degraded |
| Orange | Too damaged or unfinished to function |
| Pulsing red | Missing from the saved reference |
| Green | Added since the reference |
| Purple | Replaced with a different block or orientation |
| Gray | Block health could not be read |

Missing sections remain outlined across marker pages; individual shaded markers are paginated. Turning spin off snaps to a fixed isometric view.

A detached block counts as missing from the observed grid, even if it still exists nearby. A block being switched off or unpowered does not make it structurally non-functional.

## Use the control panel

- **Ship:** show/hide, rotation, system/state filters, views/zoom and damage reports.
- **Baseline:** capture/import, saved ships, saved versions and recovery tools.
- **Customize:** HUD layout, colors, health bars, system styles, shortcuts, preferences and clipboard config sharing.

**Save display** keeps your changes as defaults, a per-ship setup or a named preset. Closing the panel alone does not save layout edits. The ship schematic can be hidden while health bars stay visible; global Hide HUD hides everything. Shortcuts start unbound.

For a blueprint, use Baseline > Import blueprint > Find blueprint. Select any combination of Local, Workshop and Cloud and search part of its title. Load a result, select its grid and matching cockpit, preview, then **Use baseline**. Unsupported definitions are listed under Omitted blocks. Files remain read-only. See [supported formats](docs/BLUEPRINT-FORMATS.md).

The UI has English and Spanish controls and descriptions. Dynamic diagnostic responses may remain English.

## Optional health bars

Open **/mg ui > Customize > Health bars**. Overall, Armor, Power, Thrusters, Utility, Cargo, Offensive and Tools bars are all hidden by default. Enable the ones you want, then set each bar's position, width, height, opacity and label. Save global or Save ship to keep them; named display presets include them too.

Each bar runs **blue (healthy), yellow (damaged), orange (non-functional), red (missing expected blocks), then gray (unreadable)** from left to right. Segment widths count baseline blocks equally, not hit points. Replacements count red because the expected block is absent; additions do not inflate the denominator. Tools groups welders, grinders, drills and ToolCore blocks; Utility contains the remaining systems, including conveyors, tanks, mechanical blocks, production and controls.

Bars ignore schematic filters, marker pages and docked context. They use the controlled grid's saved reference; recovery/blueprint previews are labeled. No baseline or no blocks in a system shows an empty bar with an explanation. The normal Show/hide command hides the whole Marigold HUD, including bars. Damage colors follow Customize / Colors; healthy is blue.

## Returning to your ship

Profiles survive game restarts and server reconnects. Automatic matching is on by default and tries to select a unique saved layout once the ship has loaded. It preserves the saved baseline rather than capturing a new one.

Profiles have short numbers: use `/mg profiles` to list them and `/mg profile load 1` to load one. Clearing a baseline keeps its profile and number. If matching is uncertain, load a profile yourself from the corresponding cockpit. `/mg auto status` explains the current state; `/mg auto off` disables automatic matching persistently. `/mg profile detach` undoes a binding and prevents it from being selected again for that grid during the session, unless you use `/mg auto retry`.

Identical copies cannot reliably be distinguished by layout alone. Nexus transfer handling uses conservative client-side matching; it passed the reported transfer tests, but ambiguous or heavily altered ships may still need manual loading.

## Station mode

Station mode is **off by default**. Enable it in Customize > Preferences or with `/mg station on` to show grids attached through locked connectors.

Station mode belongs to the current grid/profile and defaults off on other ships. Docked grids with an unambiguous saved profile show their own damage outlines and report counts; unmatched grids remain context only. They never become part of the controlled ship's captured baseline. Rotor, hinge and piston subgrids are outside the current scope. Large docked groups have display limits; `/mg report` tells you when context is loading or limited.

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

Offensive recognizes loaded weapon definitions and weapon-targeting metadata, plus WeaponCore registrations when available. This includes weapons built on conveyor-sorter types. Existing captures need no conversion. A mod registering a utility device as a WeaponCore weapon may include that device in Offensive.

The control panel has three sections: **Ship** for everyday viewing and damage reports; **Baseline** for captures, blueprints, saved ships and recovery; **Customize** for layout, colors, health bars, filters, shortcuts and preferences. **Save display** opens the shared save/restore screen for defaults, this ship and named presets. Use **Back** to return to your previous page.

Find a blueprint under Baseline > Import blueprint > Find blueprint. Select any combination of Local, Workshop and Cloud, enter part of its title (required), and Search. Use Previous/Next and click a result to load it.

## Development recovery and versions

`master` contains the tested 0.12.6 release; `dev` contains 0.14.1 recovery, version-management and UI changes with user-reported runtime passes; recovery-preview visual acceptance and release checks remain.

After accepting recovery, Marigold remembers the surviving layout and its alignment to the original baseline. With automatic matching enabled, it can reuse that alignment after logout or transfer when the layout uniquely matches. Missing sections stay missing: recovery never captures the remnant as the new baseline. At least eight exact structural matches are needed for remembered recovery. Partial repairs and changed cockpit types are supported when the surrounding layout still matches. Identical ships, major changes or a relocated/reoriented cockpit may require recovery again. One accepted recovery is remembered per ship profile.

Use Alignment tools for grid-cell translation and 90-degree X/Y/Z turns about the baseline origin before accepting recovery. If no candidate is found, the preview starts unverified at your seat.

Capture/import automatically archive the previous baseline. Saved versions retain source, creation date and blueprint identity where available; old references show an unknown date. Restoring a version from a different anchor requires reviewing and accepting recovery alignment. A full version library blocks replacement until you remove an unwanted version, rather than discarding history.

In Saved versions, **Save new version** creates a named copy; **Rename version** uses the name field; **Replace version** copies the active baseline into the selected slot and preserves the overwritten version. Rename/use/replace/delete require confirmation. Replacement needs a free slot.

Before testing 0.13.0 or later, copy `%APPDATA%/Marigold` somewhere safe with the game closed. These builds read older profiles but save format 3, which 0.12.6 cannot read. To roll back, restore that complete pre-upgrade folder alongside the older plugin. Automated migration tests pass; the upgrade/reconnect checks have user-reported passes in TESTING-TODO.md.
