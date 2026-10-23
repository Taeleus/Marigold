# Marigold

**See your ship. Find the damage. Know what's missing.**

Marigold is a ship schematic and damage HUD for **Space Engineers 1**, running through **Pulsar Legacy**. It compares the ship you're piloting with a saved layout, so you can spot damage, find missing sections and check individual systems without inspecting every block yourself.

This page describes **0.12.6**, the version on `master`. Marigold is still a prototype; newer features are being developed on [`dev`](https://github.com/Taeleus/Marigold/tree/dev).

## Your first ship

With Marigold installed and enabled:

1. Sit in the cockpit you want to use as the ship's orientation reference.
2. Type **`/mg ui`** to open the controls.
3. Open **Baseline**, choose **Capture this ship**, then click again to confirm.
4. Close the panel. The schematic now compares your ship against that saved layout.

A **baseline** is the layout your ship should have. Capture records the blocks currently present and treats them as fully repaired, so existing damage still shows. Blocks already missing when you capture cannot be included; use an older saved baseline or a blueprint to account for those.

The baseline stays fixed as your ship takes damage or gets repaired. Capture again only when you want to make the current layout the new reference. Before a refit, use **Baseline > Saved versions** to keep a named copy of the old layout.

## Read the damage

| Color | Meaning |
| --- | --- |
| Faint yellow | Damaged |
| Orange | Too damaged or unfinished to function |
| Pulsing red | Missing from the saved layout |
| Green | Added since the baseline |
| Purple | Replaced by a different block or orientation |
| Gray | Health information is unavailable |

A block detached from your ship counts as missing even if it is floating nearby. Switching a block off or losing power does not make it orange.

The display is a schematic built from block bounds, rather than a detailed model. Missing sections remain outlined even when individual damage markers span multiple pages.

## Make the HUD yours

Open **`/mg ui`** for three main sections:

- **Ship:** show or hide the HUD, pause rotation, choose systems and damage states, change views or zoom, and read the damage report.
- **Baseline:** capture your ship, import a blueprint, select saved ships, manage saved versions and recover alignment after cockpit loss.
- **Customize:** adjust the display's position, size, opacity and colors; configure health bars, system styles, shortcuts and preferences.

Views follow your saved cockpit orientation. Combine system filters to focus on propulsion, cargo, tools or **Offensive** blocks. WeaponCore and ToolCore registrations help recognize supported modded weapons and tools.

Use **Save display** to save your setup as the default, for this ship, or as a named preset. Closing the panel alone does not save display edits. **Customize > Share config** lets you copy and paste layouts, health bars, colors and filters through the clipboard; ship baselines and shortcuts are not included.

Shortcuts are optional and start unbound. The interface offers English and Spanish, with English fallback for some controls and diagnostic messages.

### Optional health bars

Under **Customize > Health bars**, enable any of these: **Overall, Armor, Power, Thrusters, Utility, Cargo, Offensive and Tools**. All start hidden. Each has its own position, size, opacity and label settings.

Bars run from **blue → yellow → orange → red → gray**, showing healthy, damaged, non-functional, missing and unreadable blocks. Segments count blocks equally, not hit points. Added blocks do not improve the baseline score, and replacing an expected block counts as missing that original block.

Use **Customize > HUD layout > Hide ship display** to keep the bars without the schematic. **Hide HUD** hides everything. Bars describe the controlled ship's baseline regardless of schematic filters or docked ships.

## Use a blueprint as your baseline

Open **Baseline > Import blueprint > Find blueprint**. Choose any combination of **Local, Workshop and Cloud**, enter part of the blueprint's title, and search. Workshop results come from files already cached on your computer.

Load a result, choose its grid and matching cockpit, then **Preview** the alignment before selecting **Use baseline**. You can also enter the path to an XML blueprint file. **Omitted blocks** lists definitions unavailable in your current game.

Marigold supports plain or gzip-compressed XML `bp.sbc` files. Protobuf-only and archived blueprints are not supported. Blueprint scripts, inventories and ownership are never applied, and source files are never modified.

## Come back to your ship

Saved profiles survive restarts and reconnects. Automatic matching tries to recognize a unique saved layout after the ship loads, including after server transfers. It does not capture a fresh baseline or erase missing sections.

Identical ships and heavily changed layouts may need manual selection in **Baseline > Saved ships**. Automatic matching can be disabled under **Customize > Preferences**.

If the original cockpit is gone, use **Baseline > Recovery tools** to preview alignment from surviving blocks. Inspect the result, adjust it if needed, and accept or cancel. In 0.12.6, this recovery alignment may need to be applied again after reconnecting. Saved versions can only be restored when their saved anchors match.

### Connector-docked ships

Enable **Show connector-docked ships** under **Customize > Preferences** to turn on station mode for the current ship. It is **off by default**.

Connected grids with a matching saved profile show their own damage outlines and report counts. Others appear as context. They do not become part of your ship's baseline or health bars. Rotor, hinge and piston subgrids are excluded.

## Install

Marigold targets **Pulsar Legacy / .NET Framework 4.8**.

1. Close Space Engineers.
2. Copy the built package's `plugin.dll`, `plugin.pdb` and `plugin.xml` into `%APPDATA%/Pulsar/Legacy/Local/Marigold` (an existing `MarigoldDev` folder also works).
3. Start the game through Pulsar Legacy and enable Marigold. Restart if prompted.

Enable only one Marigold installation at a time. Profiles and preferences live in **`%APPDATA%/Marigold`**; keep a copy of that folder before upgrading or moving computers.

<details>
<summary>Build a package from source</summary>

Clone this repository and open PowerShell in its folder. Building requires a .NET SDK capable of running the .NET 10 checks, .NET Framework 4.8 reference assemblies and a local Space Engineers installation.

```powershell
.\build.ps1
# Or specify your game's Bin64 folder:
.\build.ps1 -Bin64 'D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64'
```

The package is written to `artifacts/Marigold`. Building does not install the plugin or launch the game. The plugin itself targets .NET Framework 4.8; the automated checks use .NET 10.

</details>

## Help and feedback

Most actions are available through **`/mg ui`**. For chat commands, start with `/mg` or read the [command reference](docs/COMMANDS.md). `/marigold` is an alias; `/m` belongs to the game's whisper command.

Found a problem? [Open an issue](https://github.com/Taeleus/Marigold/issues) with your Marigold version, steps to reproduce it, relevant mods and a screenshot if helpful.

For project progress, see the [roadmap](ROADMAP.md) and [testing checklist](TESTING-TODO.md).
