# Marigold

**See your ship. Find the damage. Know what's missing.**

Marigold is a ship schematic and damage HUD for **Space Engineers 1**, running through **Pulsar**. It compares the ship you're piloting with a saved layout, so you can spot damage, find missing sections and check individual systems without inspecting every block yourself.

Current release candidate: **0.14.14 for Pulsar Legacy**. Interim support is experimental and untested in-game.

See the [changelog](CHANGELOG.md) for changes in each version.

## Your first ship

With Marigold installed and enabled:

1. Sit in the cockpit you want to use as the ship's orientation reference.
2. Type **`/mg ui`** to open the controls.
3. Open **Baseline**, choose **Capture this ship**, then click again to confirm.
4. Close the panel. The schematic now compares your ship against that saved layout.

A **baseline** is the layout your ship should have. Capture records the blocks currently present and treats them as fully repaired, so existing damage still shows. Blocks already missing when you capture cannot be included; use an older saved baseline or a blueprint to account for those.

The baseline stays fixed as your ship takes damage or gets repaired. Capture again only when you want to make the current layout the new reference. Capture and blueprint import preserve the previous baseline in **Baseline > Saved versions**. You can also save named versions such as Original or Before refit.

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

Yellow stays steady and faint so serious damage is easier to spot. Newly non-functional or missing blocks can pulse more vividly for four seconds, using one shared rhythm across the display. Toggle **Fresh pulse** under **Customize > Preferences > Performance options**; the gentler ongoing missing-block pulse has its own setting. Save display to keep your choices.

Damage fills remain visible across marker pages. On extremely damaged ships, nearby same-color regions may merge and shade gaps between blocks. The display is a schematic built from block bounds, rather than a detailed model.

## Make the HUD yours

Open **`/mg ui`** for three main sections:

- **Ship:** show or hide the HUD, pause rotation, choose systems and damage states, change views or zoom, and read the damage report.
- **Baseline:** capture your ship, import a blueprint, select saved ships, manage saved versions and recover alignment after cockpit loss.
- **Customize:** adjust the display's position, size, opacity and colors; configure health bars, system styles, shortcuts and preferences.

Views follow your saved cockpit orientation. Combine system filters to focus on propulsion, cargo, tools or **Offensive** blocks. WeaponCore and ToolCore registrations help recognize supported modded weapons and tools.

Use **Save display** to save your setup as the default, for this ship, or as a named preset. Closing the panel alone does not save display edits. **Customize > Share config** lets you copy and paste layouts, health bars, colors and filters through the clipboard; ship baselines and shortcuts are not included.

Shortcuts are optional and start unbound. The interface offers English and Spanish, with English fallback for some controls and diagnostic messages.

**Customize > Preferences > Performance options** offers Full display, Outlines only, Faults with fills and Fault outlines only. Lighter modes skip translucent fills and/or healthy hull context while retaining filtered fault coverage and unchanged health counts. Save display keeps the choice. Automatic matching now requires at least 75% overlap, a clear lead and stable observations; ambiguous ships still need manual selection.

### Optional health bars

Under **Customize > Health bars**, enable any of these: **Overall, Armor, Power, Thrusters, Utility, Cargo, Offensive and Tools**. All start hidden. Each has its own position, size, opacity and label settings.

Bars run from **blue → yellow → orange → red → gray**, showing healthy, damaged, non-functional, missing and unreadable blocks. Segments count blocks equally, not hit points. Added blocks do not improve the baseline score, and replacing an expected block counts as missing that original block.

For **Power**, check **Reactors**, **Batteries** or **Solar** to give those types separate labeled sections, in that order. Unselected types and other power sources stay in **Other**. Section widths reflect block counts, and each section uses the same health colors. These options start off and save with your layout.

Use **Customize > HUD layout > Hide ship display** to keep the bars without the schematic. **Hide HUD** hides everything. Bars describe the controlled ship's baseline regardless of schematic filters or docked ships.

## Use a blueprint as your baseline

Open **Baseline > Import blueprint > Find blueprint**. Choose any combination of **Local, Workshop and Cloud**, enter part of the blueprint's title, and search. Workshop results come from files already cached on your computer.

Load a result, choose its grid and matching cockpit, then **Preview** the alignment before selecting **Use baseline**. You can also enter the path to an XML blueprint file. **Omitted blocks** lists definitions unavailable in your current game.

Marigold supports plain or gzip-compressed XML `bp.sbc` files. Protobuf-only and archived blueprints are not supported. Blueprint scripts, inventories and ownership are never applied, and source files are never modified. See [blueprint formats](docs/BLUEPRINT-FORMATS.md) for details.

## Come back to your ship

Saved profiles survive restarts and reconnects. Automatic matching tries to recognize a unique saved layout after the ship loads, including after server transfers. It does not capture a fresh baseline or erase missing sections.

Identical ships and heavily changed layouts may need manual selection in **Baseline > Saved ships**. Automatic matching can be disabled under **Customize > Preferences**.

If the original cockpit is gone, use **Baseline > Recovery tools** to preview alignment from surviving blocks. Inspect the result, adjust it if needed, and accept or cancel. The accepted alignment is remembered for later reconnects. Partial repairs and cockpit-type changes can still match when enough of the surrounding structure remains. Major changes or ambiguous layouts may need manual recovery again.

The recovery page puts a large preview above the controls. **Left-click and drag** the image to look around; **scroll over it** to zoom. These change only your inspection perspective. The **X/Y/Z buttons** move or rotate the baseline alignment. Inspect the result before accepting; Close, Escape or Back ends an unaccepted recovery preview without applying it. Your normal HUD camera and saved layout remain unchanged.

### Keep versions of your ship

Under **Baseline > Saved versions**, use **Save new version** to keep the current reference, **Rename version** to change its name, or **Use version** to compare against it. **Replace version** copies the active baseline into the selected slot while preserving the previous contents. Restoring a version with a different anchor opens a recovery preview for you to inspect and accept.

Libraries hold up to 16 versions and 250,000 blocks in total. Replacing a version needs a free slot to preserve its previous contents; a full library asks you to remove an unwanted version first.

### Connector-docked ships

Enable **Show connector-docked ships** under **Customize > Preferences** to turn on station mode for the current ship. It is **off by default**.

Connected grids with a matching saved profile show their own damage outlines and report counts. Others appear as context. They do not become part of your ship's baseline or health bars. Rotor, hinge and piston subgrids are excluded.

## Install

The supported release target is **Pulsar Legacy / .NET Framework 4.8**. Interim builds are experimental and are outside the initial PluginHub submission.

1. Close Space Engineers and back up `%APPDATA%/Marigold` (profiles and preferences).
2. Copy the Legacy package's plugin.dll, plugin.pdb and plugin.xml into `%APPDATA%/Pulsar/Legacy/Local/Marigold`. An existing MarigoldDev folder also works. Enable only one copy per launcher.
3. Start the matching launcher, enable Marigold and restart if prompted. Sit in a cockpit and open `/mg ui`.

For trusted downloaded packages blocked with error **0x80131515**, right-click the ZIP, open Properties, check Unblock, apply and extract again. Alternatively unblock the installed DLL in Properties. Check the log's Flavour and Runtime lines when reporting launch problems.

Profiles are shared between launchers. Upgrading from 0.12.6 changes the profile format; rollback requires the older plugin and the complete pre-upgrade data backup.

<details>
<summary>Build from source</summary>

Requires a .NET 10 SDK, .NET Framework 4.8 reference assemblies and a local Space Engineers Bin64 directory. In PowerShell:

```powershell
./build.ps1 -Runtime Legacy
./verify-source.ps1 -Runtime Legacy
# Optional game location:
./build.ps1 -Runtime Legacy -Bin64 'D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64'
```

The Legacy package is written to `artifacts/Marigold`. Builds do not deploy. Use `./verify-source.ps1 -Runtime Legacy` to check the source compilation path.

</details>

## Current limitations

Large damaged ships can still make filled rendering expensive; choose an outline mode under **Customize > Preferences > Performance options** for lower drawing cost.

Fresh-damage alerts apply to the main ship; connector-docked context does not pulse. See the [roadmap](ROADMAP.md) for remaining work.

## Help and feedback

Most actions are available through **`/mg ui`**. For chat commands, start with `/mg` or read the [command reference](docs/COMMANDS.md). `/marigold` is an alias; `/m` belongs to the game's whisper command.

Found a problem? [Open an issue](https://github.com/Taeleus/Marigold/issues) with your Marigold version, steps to reproduce it, relevant mods and a screenshot if helpful.

For project progress, see the [roadmap](ROADMAP.md).


See the [documentation index](docs/INDEX.md) and [performance testing procedure](docs/PERFORMANCE-TEST.md).
