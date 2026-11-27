# Marigold

Marigold adds a ship schematic and damage HUD to **Space Engineers 1**. Compare your ship with a saved layout, spot damaged or missing blocks, and track individual systems.

**Install from PluginHub.** In Pulsar’s plugin list, enable **Marigold** and restart if prompted.

## Get started

1. Sit in the cockpit you want to use as the ship's orientation reference.
2. Type **`/mg capture`** in chat to save a **baseline** for missing-block detection. From **`/mg ui`**, you can also use **Baselines > Capture this ship** and click again to confirm.
3. Use **`/mg ui`** to configure the schematic and health bars.

A baseline is the layout your ship should have. Capture records the blocks currently present, including damaged ones. If sections are already missing, use a saved baseline or import a blueprint of the complete ship.

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

Fresh orange and red faults can pulse together briefly. Yellow stays steady. Use **Pulse missing blocks** and **Emphasize fresh damage** in **Preferences** to control the two effects.

## Make it yours

Use **`/mg ui`** to open the control panel.

- **Ship** lets you show or hide the HUD, choose views, pause rotation, focus on systems or damage states, and read a damage report.
- **Baselines** covers capture, blueprint import, saved ships and versions, and recovering alignment after cockpit loss.
- **Ship display** controls the rendering mode, compact or expanded view, and state colors.
- **Health bars** lets you enable system bars or create bars for particular blocks and groups.
- **Preferences** covers rendering performance, pulses, automatic matching, language, and shortcuts.
- **Save & share** lets you save a default or a layout for this ship, reuse a named layout, or share a layout through the clipboard.

Views follow the cockpit you're using. Marigold uses the saved cockpit to align the baseline. Under **Ship > Systems and filters**, choose a system or a quick preset and hide or dim the rest of the ship. Open **Advanced** to combine systems, set an exact opacity for other systems, or style them individually.

Under **Ship display > State colors**, choose a state and open **Choose color visually...**. The color square and hue strip let you pick by eye, with original and selected swatches to compare. Exact RGB entry is available too. **Use color** applies your choice. **Cancel** or **Back** discards the selection.

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

Use **Save bar** to keep custom-bar edits before leaving the editor. Capturing or importing a new baseline starts a new set of custom bars. The previous set stays with the archived baseline version.

## Return to your ship

Saved ships survive restarts and reconnects. Automatic matching tries to match your ship to its saved baseline, including after server transfers. Identical ships or major refits may need manual selection under **Baselines > Saved ships**.

**Baselines > Saved versions** keeps layouts such as Original or Before refit. You can save, rename, restore or replace versions. Capture and blueprint import preserve the previous baseline automatically.

If the original cockpit is gone, open **Recovery tools**. Drag the preview to look around and scroll to zoom; these affect only your inspection view. The X/Y/Z controls adjust the baseline alignment. Inspect it before accepting. Back, Close or Escape ends an unaccepted preview.

## Import a blueprint

Open **Baselines > Import blueprint > Find blueprint**. Search any combination of **Local, Workshop and Cloud**, load a result, choose its grid and cockpit, then preview the alignment before accepting it. Press **Escape** or choose **Close** to discard the draft and return to your previous baseline.

Workshop search uses blueprints already cached on your computer. Plain or gzip-compressed XML `bp.sbc` files are supported; protobuf-only and archived formats are not. You can also provide an XML file path. See [blueprint formats](docs/BLUEPRINT-FORMATS.md) for details.

## Performance and connected ships

Filled displays can cost more FPS on large, heavily damaged ships. Choose **Outlines only** under **Ship display > Rendering mode** for lighter rendering. Health-bar counts stay the same. Damage pulse settings are under **Preferences**.

**Monochrome Ship** uses the same models in your healthy block/wireframe color, with depth shading and separate damage colors. Choose it in the rendering menu and adjust the healthy block color in the color settings.

**Textured Ship** uses **GPU (experimental)** when you have no saved renderer preference. Choose **CPU** under **Preferences > Performance > Textured Ship renderer** to save a different preference. Use `/mg stats` to check the active renderer, preview resolution and update rate.

**Show connector-docked ships** in Preferences is off by default. Docked ships can show damage against their own saved baselines. The station's baseline and health bars cover the station itself. In **0.17.16**, the controlled grid must be static and each grid in the connector chain must pass a [personal or faction ownership check](docs/COMMANDS.md#station-mode-and-automatic-association).

Wireframe modes draw from block bounds. **Textured Ship** uses available models, paint and textures, with simpler shapes where needed.

## Install a local package

Use the package for your launcher. [Build instructions](docs/RELEASE.md#build-and-check)

1. Close Space Engineers.
2. Copy `plugin.dll`, `plugin.pdb` and `plugin.xml` from the package into the matching folder:
   - Legacy: `%APPDATA%/Pulsar/Legacy/Local/Marigold`
   - Interim: If `%APPDATA%/Pulsar/Interim` exists, use its `Local/Marigold` subfolder. Otherwise use `%APPDATA%/Pulsar/Legacy/Local/Marigold`. Creating a separate `Interim` directory switches the launcher to that data directory.

   An existing `MarigoldDev` folder also works. Keep one Marigold package in the scanned folder. For a portable Pulsar install, use the equivalent folder under the launcher directory.
3. Start Pulsar, enable Marigold and restart if prompted. Sit in a cockpit and open **`/mg ui`**.

On Bazzite, a player using Pulsar Legacy from the game's Steam folder confirmed this local plugin path. Put the three files in `~/.local/share/Steam/steamapps/common/SpaceEngineers/Pulsar/Legacy/Local/Marigold/`. On Bazzite, `~` can resolve to `/var/home/<username>`. If Steam or Pulsar is installed elsewhere, use the `Legacy/Local` folder under that Pulsar install.

Both editions use the same Marigold profiles and settings when Space Engineers uses the same game data folder.

If Windows blocks a trusted downloaded package with error **0x80131515**, unblock the ZIP in Properties before extracting it, or unblock the installed DLL.

### Your saved data

Profiles and settings live in **`<game user data>/Storage/Marigold`**. Normally this is **`%APPDATA%/SpaceEngineers/Storage/Marigold`**. A custom `-appdata` location has its own data. Back up this folder before upgrading or moving computers.

## Help and feedback

Start with **`/mg ui`**, or see the [command reference](docs/COMMANDS.md). `/marigold` is an alias; `/m` belongs to the game's whisper command.

[Report a problem](https://github.com/Taeleus/Marigold/issues) with your version, what happened and a screenshot if useful.

[Changelog](CHANGELOG.md) · [Documentation](docs/INDEX.md) · [Roadmap](ROADMAP.md) · [Performance testing](docs/PERFORMANCE-TEST.md) · [Building from source](docs/RELEASE.md)

Marigold is available under the [MIT License](LICENSE).
