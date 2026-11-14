# Marigold

**See your ship. Find the damage. Know what's missing.**

Marigold is a ship schematic and damage HUD for **Space Engineers 1**, running through **Pulsar Legacy**. Compare your ship with a saved layout, spot damaged or missing blocks, and keep an eye on the systems that matter to you.

PluginHub offers **0.17.15 for Pulsar Legacy**. The source on `master` is **0.17.15**. [Changelog](CHANGELOG.md)

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
- **Ship display:** choose a rendering mode, compact or expanded view, and state colors.
- **Health bars:** enable system bars or create bars for particular blocks and groups.
- **Preferences:** choose performance options, pulses, automatic matching, language and shortcuts.
- **Save & share** lets you save a default or a layout for this ship, reuse a named layout, or share a layout through the clipboard.

Views follow your saved cockpit orientation. Under **Ship > Systems and filters**, choose a system or a quick preset and hide or dim the rest of the ship. Open **Advanced** to combine systems, set an exact opacity for other systems, or style them individually.

Under **Ship display > State colors**, choose a state and open **Choose color visually...**. The color square and hue strip let you pick by eye, with original and selected swatches to compare. Exact RGB entry is available too. **Use color** applies your choice; Cancel or Back discards the selection.

Open **Advanced** on Ship, Ship display, Systems and filters, or Health bars for detailed controls. Use **Arrange HUD** to drag elements, resize bars, or type exact percentages. Set the schematic's background width and height, then enable **Scale background with ship** if you want both dimensions to follow **Ship scale**. Choose **Preview HUD** to inspect the layout at its actual size and **Return to editor** to keep editing.

In **Arrange HUD**, **Apply layout** saves custom bar changes with their ship. To keep the schematic and system bar layout, open **Save & share** or choose **Save display**. **Save as my default** sets your layout for other ships. **Save for this ship** keeps a separate layout for the ship you're in.

When a ship has a saved layout, **Use on reconnect** lets you choose **My default** or **This ship's layout**. The choice applies immediately and is remembered for that ship. **Reload saved layout** reapplies the selected layout after unsaved edits. Use **Load my default now** when you want to try the default temporarily.

Under **Named layouts**, enter a **Layout name** to save a reusable copy. Select an existing name from the dropdown to load it. **Copy or paste layout** shares display settings such as schematic and standard health-bar layouts, colors and filters. Custom bars belong to their ship. After pasting, save the result as your default or for this ship if you want it to load automatically.

Shortcuts are optional and start unbound. English and Spanish are available, with English fallback for some messages.

## Watch your systems

Enable health bars under **Health bars** for **Overall, Armor, Power, Thrusters, Utility, Cargo, Offensive and Tools**. They start hidden, and each can be moved, resized or faded independently. You can hide the ship schematic and keep only the bars.

In **Arrange HUD**, **Show label** controls whether a bar shows text. With it on, enable **Simple label** to show just the title. The Power bar still shows captions for its enabled sections.

Bars show the share of baseline blocks that are healthy, damaged, non-functional, missing or unreadable. They count blocks rather than hit points. Choose a healthy bar color under **Bar colors**, or match it to the schematic outline. Power can also be split into **Reactors**, **Batteries** and **Solar** sections.

### Custom health bars

Want to watch one coilgun, a group of drives or a bank of PDCs? Open **Health bars > Custom bars > Add custom bar**, then find a named block or terminal group. Custom bars track blocks from the main ship's baseline and save with that ship and its baseline versions.

Damage and repairs update automatically. Destroyed members stay counted as missing until repaired. After adding or removing members in a terminal group, use **Update tracked blocks** if you want the bar to follow the changed list.

Custom-bar edits have their own **Save bar** button. Leaving the editor discards unsaved bar edits. Capturing or importing a new baseline starts a new set of custom bars; the previous set stays with the archived baseline version.

## Return to your ship

Saved ships survive restarts and reconnects. Automatic matching tries to match your ship to its saved baseline, including after server transfers. Identical ships or major refits may need manual selection under **Baselines > Saved ships**.

**Baselines > Saved versions** keeps layouts such as Original or Before refit. You can save, rename, restore or replace versions. Capture and blueprint import preserve the previous baseline automatically.

If the original cockpit is gone, open **Recovery tools**. Drag the preview to look around and scroll to zoom; these affect only your inspection view. The X/Y/Z controls adjust the baseline alignment. Inspect it before accepting. Back, Close or Escape ends an unaccepted preview.

## Import a blueprint

Open **Baselines > Import blueprint > Find blueprint**. Search any combination of **Local, Workshop and Cloud**, load a result, choose its grid and cockpit, then preview the alignment before accepting it. Press **Escape** or choose **Close** to discard the draft and return to your previous baseline.

Workshop search uses blueprints already cached on your computer. Plain or gzip-compressed XML `bp.sbc` files are supported; protobuf-only and archived formats are not. You can also provide an XML file path. See [blueprint formats](docs/BLUEPRINT-FORMATS.md) for details.

## Performance and connected ships

Filled displays can cost more FPS on large, heavily damaged ships. Choose **Outlines only** under **Ship display > Rendering mode** for lighter rendering. Health-bar counts stay the same. Damage pulse settings are under **Preferences**.

In the GPU test build, **Textured Ship** uses **GPU (experimental)** by default. Choose **CPU** under **Preferences > Performance > Textured Ship renderer** to save a different preference. Use `/mg stats` to check the active renderer, preview resolution and update rate.

**Show connector-docked ships** is off by default and available from a static station you own or that is shared with your faction. The same access check applies to every docked grid. More than half of a grid's ownership-capable blocks must be yours, or more than half must be explicitly shared with your faction. Docked ships with saved profiles can show their own damage. The station's baseline and health bars cover the station itself. Rotor, hinge and piston subgrids are excluded.

Wireframe modes draw from block bounds. **Textured Ship** uses available models, paint and textures, with simpler shapes where needed.

## Install from PluginHub

Find **Marigold** in Pulsar Legacy's plugin list, enable it, and restart if prompted.

## Install a local package

Use the package for your launcher. Build **Pulsar Legacy** with `./build.ps1 -Runtime Legacy` and **Pulsar Interim** with `./build.ps1 -Runtime Interim`. Interim is available for local testing. [Interim test checklist](TESTING-TODO.md#pending-beta-checks)

1. Close Space Engineers.
2. Copy `plugin.dll`, `plugin.pdb` and `plugin.xml` from the package into the matching folder:
   - Legacy: `%APPDATA%/Pulsar/Legacy/Local/Marigold`
   - Interim: `%APPDATA%/Pulsar/Interim/Local/Marigold` if that folder exists. Otherwise use `%APPDATA%/Pulsar/Legacy/Local/Marigold`, because Interim then shares the Legacy folder. Don't create `Pulsar/Interim` just for Marigold: Interim would switch to that empty folder and stop seeing your existing profile and plugins. The Interim package's `plugin.xml` is tagged `CoreCLR`, so Legacy ignores it in a shared folder, but a shared folder can hold only one of the two packages.

   An existing `MarigoldDev` folder also works. Enable only one copy.
3. Start Pulsar, enable Marigold and restart if prompted. Sit in a cockpit and open **`/mg ui`**.

On Bazzite, a player using Pulsar Legacy from the game's Steam folder confirmed this local plugin path. Put the three files in `~/.local/share/Steam/steamapps/common/SpaceEngineers/Pulsar/Legacy/Local/Marigold/`. On Bazzite, `~` can resolve to `/var/home/<username>`. If Steam or Pulsar is installed elsewhere, use the `Legacy/Local` folder under that Pulsar install.

Both editions use the same Marigold profiles and settings when Space Engineers uses the same game data folder.

If Windows blocks a trusted downloaded package with error **0x80131515**, unblock the ZIP in Properties before extracting it, or unblock the installed DLL.

### Your saved data

Profiles and settings live in **`<game user data>/Storage/Marigold`**. Normally this is **`%APPDATA%/SpaceEngineers/Storage/Marigold`**. A custom `-appdata` location has its own data. Back up this folder before upgrading or moving computers.

## Help and feedback

Start with **`/mg ui`**, or see the [command reference](docs/COMMANDS.md). `/marigold` is an alias; `/m` belongs to the game's whisper command.

[Report a problem](https://github.com/Taeleus/Marigold/issues) with your version, what happened and a screenshot if useful.

More information: [changelog](CHANGELOG.md), [documentation](docs/INDEX.md), [roadmap](ROADMAP.md), [performance testing](docs/PERFORMANCE-TEST.md) and [building from source](docs/RELEASE.md).

Marigold is available under the [MIT License](LICENSE).
