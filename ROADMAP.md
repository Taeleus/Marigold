# Marigold roadmap

Marigold **0.16.1** is available for **Pulsar Legacy**. The first PluginHub listing went live on November 4, 2026. [PluginHub entry](https://github.com/StarCpt/PluginHub/blob/main/Plugins/Marigold.xml) · [Release review](https://github.com/StarCpt/PluginHub/pull/285)

The **0.17.15** source is on `master`. The Legacy build keeps the wireframe visible until the first GPU frame is ready. Interim builds are experimental. [Changelog](CHANGELOG.md#01715--november-11-2026)

## Suggestions

| Idea | Status | What it would do |
| --- | --- | --- |
| Simplify the control panel | Confirmed in game | **Standard** shows everyday controls. **Advanced** reveals detailed settings and diagnostics. Related controls share pages, and positioning and resizing are handled in **Arrange HUD**. |
| Resize the ship display background | Confirmed in game | Set background width and height in **Arrange HUD**. Enable **Scale background with ship** to resize them with the ship. The caption stays below the background. |
| Title-only health bar labels | Confirmed in game | **Simple label** shows the title for system and custom bars. The Power bar keeps its enabled section captions. Turn **Show label** off to hide the text entirely. |
| Exact values in Arrange HUD | Confirmed in game | Every numeric slider has a percentage field. Typing values, dragging and moving sliders update the same live preview. |
| Simplify rendering mode choices | Confirmed in game | **Ship display** lists **Textured Ship** above **HD Wireframe**, followed by **SD Wireframe** and **Outlines only**. HD remains the default. |
| Show ship label | Confirmed in game | Turn off **Show ship label** under **Ship display** to hide the text below the schematic. The ship's **Show label** checkbox in **Arrange HUD** controls the same setting. The label is visible by default. **Save display** keeps the choice. [Test checklist](TESTING-TODO.md#display-follow-up) |
| Drag and drop HUD layout | Confirmed in game | Move and resize the ship display and health bars in **Arrange HUD**. **Apply layout** saves custom bar changes with the ship. Use **Save display** to keep the schematic and system bar layout. [Follow-up checks](TESTING-TODO.md#display-follow-up) |
| Sharper, steadier ship display | Confirmed in game | **HD Wireframe** is the default. A performance comparison with **SD Wireframe** on the damaged station is still open. [Follow-up checks](TESTING-TODO.md#display-follow-up) |
| Textured ship display | Confirmed in game | The textured renderer works in game, including the Expanse drive. The Legacy station retest showed smoother rotation and textured shapes. The live performance and appearance check on grids near **50,000 blocks** is still open. [Follow-up checks](TESTING-TODO.md#display-follow-up) |
| GPU textured rendering | Confirmed in game | The Legacy test build uses GPU by default for Textured Ship and offers CPU under **Preferences > Performance**. Seat entry and exit, renderer switching, HUD layout and scale changes, and recovery after losing the main seat passed on 0.17.14. On 0.17.15, the wireframe stayed visible while the first GPU frame loaded, and resolution and display-mode changes passed. [GPU preview retest](TESTING-TODO.md#01715-gpu-preview-retest) |

As feedback comes in, add a row with the request, its status and a link to the discussion when available. Use **Suggested**, **Planned**, **In progress**, **Ready to test** or **Confirmed in game**. Once a change ships, record it in the [changelog](CHANGELOG.md). Put in-game checks in the [testing checklist](TESTING-TODO.md).

## After release

- [ ] **Try the PluginHub install.** Disable the local copy, enable Marigold from PluginHub in Pulsar Legacy and open `/mg ui` in a cockpit. Check a fresh setup and confirm an existing profile still loads. Source compilation and local package tests have passed. The live PluginHub install check remains unrecorded.

## Ideas for later

- **Pulsar Interim support.** The Interim build compiles. Verify it in-game before adding Interim to the PluginHub listing.
- **Schematic pan.** Consider letting players pan the ship model within its display.
- **Spanish translation.** Translate the remaining diagnostic messages as gaps are reported.
- **Performance.** Revisit filled rendering if an improvement can preserve its appearance and damage coverage. Outline modes are available for large grids. [Performance results](docs/PERFORMANCE-RESULTS.md)

Blueprint imports require XML. Wireframe modes draw from block bounds, and **Textured Ship** uses available models and textures. Ambiguous ship matches may need manual recovery. Station mode supports connector-docked ships. Mechanical subgrids on rotors, hinges or pistons are outside scope.

## Share feedback

[Open an issue](https://github.com/Taeleus/Marigold/issues) with what you were trying to do and what would make it easier. For a bug, include your Marigold version, steps to reproduce and a screenshot if it helps.

For shipped features and completed checks, see the [changelog](CHANGELOG.md) and [testing checklist](TESTING-TODO.md). Earlier development milestones are in the [roadmap history](docs/history/ROADMAP-HISTORY-through-0.14.14.md).
