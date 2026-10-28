# Marigold

**See your ship. Find the damage. Know what's missing.**

Marigold is a ship schematic and damage HUD for **Space Engineers 1**, running through **Pulsar Legacy**. Compare your ship with a saved layout, spot damaged or missing blocks, and keep an eye on the systems that matter to you.

Current version: **0.16.1 for Pulsar Legacy**. See the [changelog](CHANGELOG.md) for what's new.

## Get started

1. Sit in the cockpit you want to use as the ship's orientation reference.
2. Type **`/mg ui`**.
3. Open **Baselines**, choose **Capture this ship**, then click again to confirm.
4. Close the panel to see your ship schematic.

A **baseline** is the layout your ship should have. Capturing records the blocks currently present; damage to those blocks still shows. For sections already missing, use a saved baseline or import a blueprint instead.

The baseline stays fixed while you take damage and make repairs. Capture again when you want a changed ship layout to become the new reference.

## Read your ship's condition

| Default color | Meaning |
| --- | --- |
| Faint yellow | Damaged |
| Orange | Too damaged or unfinished to function |
| Pulsing red | Missing from the baseline |
| Green | Added since the baseline |
| Purple | Replaced by a different block or orientation |
| Gray | Health information is unavailable |

Detached blocks count as missing, even when they are floating nearby. Switching equipment off or losing power does not make it orange.

Fresh orange/red faults can pulse together briefly to draw your attention. Yellow stays steady. Both the fresh alert and the gentler missing-block pulse can be turned off.

## Make it yours

Everything starts in **`/mg ui`**:

- **Ship:** show or hide the HUD, choose views, pause rotation, focus on systems or damage states, and read a damage report.
- **Baselines:** capture a layout, import a blueprint, manage saved ships and versions, or recover alignment after cockpit loss.
- **Ship display:** arrange the schematic and choose its colors.
- **Health bars:** enable system bars or create bars for particular blocks and groups.
- **Preferences:** choose performance options, pulses, automatic matching, language and shortcuts.
- **Save & share:** keep a default, ship setup or preset, or share your display settings through the clipboard.

Views follow your saved cockpit orientation. Under **Ship > Systems and filters**, focus on one or several systems, such as Power, Thrusters, Tools or **Offensive**, while keeping the rest of the ship faintly visible. Adjust **Other systems opacity** to choose how much context remains.

Under **Ship display > State colors**, choose a state and open **Choose color visually...**. The color square and hue strip let you pick by eye, with original and selected swatches to compare. Exact RGB entry is available too. **Use color** applies your choice; Cancel or Back discards the selection.

Forms label each value and scroll when needed. Apply a layout change, then use **Preview HUD** to inspect it at its actual position and size. **Return to editor** keeps your place and any custom-bar draft.

Use **Save display** to keep display changes as your default, for this ship, or as a named preset. Closing the panel alone does not save them. **Save & share > Share through clipboard** copies display settings, including standard health-bar layouts, colors and filters. Custom bars stay with their ship; baselines and shortcuts are not included.

Shortcuts are optional and start unbound. English and Spanish are available, with English fallback for some messages.

## Watch your systems

Enable health bars under **Health bars** for **Overall, Armor, Power, Thrusters, Utility, Cargo, Offensive and Tools**. They start hidden, and each can be moved, resized or faded independently. You can hide the ship schematic and keep only the bars.

Bars show the share of baseline blocks that are healthy, damaged, non-functional, missing or unreadable. They count blocks rather than hit points. Choose a healthy bar color under **Bar colors**, or match it to the schematic outline. Power can also be split into **Reactors**, **Batteries** and **Solar** sections.

### Custom health bars

Want to watch one coilgun, a group of drives or a bank of PDCs? Open **Health bars > Custom bars > Add custom bar**, then find a named block or terminal group. Custom bars track blocks from the main ship's baseline and save with that ship and its baseline versions.

Damage and repairs update automatically. Destroyed members stay counted as missing until repaired. After adding or removing members in a terminal group, use **Update tracked blocks** if you want the bar to follow the changed list.

Custom-bar edits have their own **Save bar** button. Leaving the editor discards unsaved bar edits. Capturing or importing a new baseline starts a new set of custom bars; the previous set stays with the archived baseline version.

## Save layouts and return to your ship

Saved ships survive restarts and reconnects. Automatic matching tries to recognize your saved layout, including after server transfers. Identical ships or heavily changed layouts may need manual selection under **Baselines > Saved ships**.

**Baselines > Saved versions** keeps layouts such as Original or Before refit. You can save, rename, restore or replace versions. Capture and blueprint import preserve the previous baseline automatically.

If the original cockpit is gone, open **Recovery tools**. Drag the preview to look around and scroll to zoom; these affect only your inspection view. The X/Y/Z controls adjust the baseline alignment. Inspect it before accepting. Back, Close or Escape ends an unaccepted preview.

## Import a blueprint

Open **Baselines > Import blueprint > Find blueprint**. Search any combination of **Local, Workshop and Cloud**, load a result, choose its grid and cockpit, then preview the alignment before accepting it.

Workshop search uses blueprints already cached on your computer. Plain or gzip-compressed XML `bp.sbc` files are supported; protobuf-only and archived formats are not. You can also provide an XML file path. See [blueprint formats](docs/BLUEPRINT-FORMATS.md) for details.

## Performance and connected ships

Filled displays can cost more FPS on large, heavily damaged ships. **Preferences > Performance options** offers outline and fault-only modes for lighter rendering. Health-bar counts stay the same.

Optional **Show connector-docked ships** adds context for ships attached through connectors. It starts off. Connected ships with saved profiles can show their own damage, but do not become part of your main ship's baseline or bars. Rotor, hinge and piston subgrids are excluded.

The schematic uses block bounds, so it will not reproduce every slope or small detail of the ship's model.

## Install a local package

Use **Pulsar Legacy**. Interim builds remain experimental and untested in-game.

1. Close Space Engineers.
2. Copy `plugin.dll`, `plugin.pdb` and `plugin.xml` from the Legacy package into `%APPDATA%/Pulsar/Legacy/Local/Marigold`. An existing `MarigoldDev` folder also works. Enable only one copy.
3. Start Pulsar, enable Marigold and restart if prompted. Sit in a cockpit and open **`/mg ui`**.

If Windows blocks a trusted downloaded package with error **0x80131515**, unblock the ZIP in Properties before extracting it, or unblock the installed DLL.

### Your saved data

Profiles and settings live in **`<game user data>/Storage/Marigold`**. Normally this is **`%APPDATA%/SpaceEngineers/Storage/Marigold`**. A custom `-appdata` location has its own data. Back up this folder before upgrading or moving computers.

## Help and feedback

Start with **`/mg ui`**, or see the [command reference](docs/COMMANDS.md). `/marigold` is an alias; `/m` belongs to the game's whisper command.

[Report a problem](https://github.com/Taeleus/Marigold/issues) with your version, what happened and a screenshot if useful.

More information: [changelog](CHANGELOG.md), [documentation](docs/INDEX.md), [roadmap](ROADMAP.md), [performance testing](docs/PERFORMANCE-TEST.md) and [building from source](docs/RELEASE.md).

Marigold is available under the [MIT License](LICENSE).
