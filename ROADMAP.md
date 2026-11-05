# Marigold roadmap

Marigold **0.16.1** is available for **Pulsar Legacy**. The first PluginHub listing went live on November 4, 2026. [PluginHub entry](https://github.com/StarCpt/PluginHub/blob/main/Plugins/Marigold.xml) · [Release review](https://github.com/StarCpt/PluginHub/pull/285)

Beta **0.17.2** is on `dev` and targets Legacy, with an experimental Interim artifact. [Changelog](CHANGELOG.md#0172--november-5-2026)

## Suggestions

| Idea | Status | What it would do |
| --- | --- | --- |
| Simplify the control panel | Planned | Review the full `/mg ui` flow. Combine related pages, shorten common paths and use plain labels. Explore a **Standard** view for everyday actions and an **Advanced** view for detailed settings. Keep positioning and resizing in **Arrange HUD** wherever it already supports those adjustments. |
| Resize the ship display background | Planned | Adjust background width and height separately in **Arrange HUD** so the panel can fit long or narrow ships with less empty space. Keep the ship model's scale independent and save the background dimensions with the HUD layout. |
| Title-only health bar labels | Planned | In **Arrange HUD**, add a **Simple label** checkbox for system and custom health bars. With **Show label** on, checking it shows just the bar title and unchecking it shows the full status text. Turn **Show label** off to hide the text entirely. |
| Exact values in Arrange HUD | Planned | Let players type a value for every numeric setting, including position, size, scale, opacity and the planned background dimensions. Keep the sliders, drag controls and typed values in sync with the live preview so matching elements is easy. |
| Simplify rendering mode choices | Planned | Remove **Faults with fills** and **Fault outlines only** from the rendering mode controls. List **Textured Ship** above **HD Wireframe** and keep **HD Wireframe** selected by default. |
| Show ship label | Confirmed in game | Turn off **Show ship label** under **Ship display** to hide the text below the schematic. The ship's **Show label** checkbox in **Arrange HUD** controls the same setting. The label is visible by default. **Save display** keeps the choice. [Test checklist](TESTING-TODO.md#display-follow-up) |
| Drag and drop HUD layout | Confirmed in game | Move and resize the ship display and health bars in **Arrange HUD**. Use **Apply layout** to use the draft and **Save display** to keep it. Custom-bar changes use **Save for this ship**. [Follow-up checks](TESTING-TODO.md#display-follow-up) |
| Sharper, steadier ship display | Confirmed in game | **HD Wireframe** is the default. A performance comparison with **SD Wireframe** on the damaged station is still open. [Follow-up checks](TESTING-TODO.md#display-follow-up) |
| Textured ship display | Confirmed in game | The textured renderer works in game, including the Expanse drive. The Legacy station retest showed smoother rotation and textured shapes. The live performance and appearance check on grids near **50,000 blocks** is still open. [Follow-up checks](TESTING-TODO.md#display-follow-up) |

As feedback comes in, add a row with the request, its status and a link to the discussion when available. Use **Suggested**, **Planned**, **In progress**, **Ready to test** or **Confirmed in game**. Once a change ships, record it in the [changelog](CHANGELOG.md). Put in-game checks in the [testing checklist](TESTING-TODO.md).

## After release

- [ ] **Try the PluginHub install.** Disable the local copy, enable Marigold from PluginHub in Pulsar Legacy and open `/mg ui` in a cockpit. Check a fresh setup and confirm an existing profile still loads. Source compilation and local package tests have passed. The live PluginHub install check remains unrecorded.

## Ideas for later

- **Pulsar Interim support.** The Interim build compiles. Verify it in-game before adding Interim to the PluginHub listing.
- **Schematic pan.** Consider letting players pan the ship model within its display.
- **Spanish translation.** Translate the remaining diagnostic messages as gaps are reported.
- **Performance.** Revisit filled rendering if an improvement can preserve its appearance and damage coverage. Outline modes are available for large grids. [Performance results](docs/PERFORMANCE-RESULTS.md)

Blueprint imports currently require XML, and the schematic uses block bounds. Ambiguous ship matches may need manual recovery. Station mode supports connector-docked ships. Mechanical subgrids on rotors, hinges or pistons are outside scope.

## Share feedback

[Open an issue](https://github.com/Taeleus/Marigold/issues) with what you were trying to do and what would make it easier. For a bug, include your Marigold version, steps to reproduce and a screenshot if it helps.

For shipped features and completed checks, see the [changelog](CHANGELOG.md) and [testing checklist](TESTING-TODO.md). Earlier development milestones are in the [roadmap history](docs/history/ROADMAP-HISTORY-through-0.14.14.md).
