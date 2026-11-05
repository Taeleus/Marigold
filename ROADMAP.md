# Marigold roadmap

Marigold **0.16.1** is available for **Pulsar Legacy**. The first PluginHub listing went live on November 4, 2026. [PluginHub entry](https://github.com/StarCpt/PluginHub/blob/main/Plugins/Marigold.xml) · [Release review](https://github.com/StarCpt/PluginHub/pull/285)

Development build **0.17.0** is on `dev` and targets Legacy, with an experimental Interim artifact. [Changelog](CHANGELOG.md#0170--november-5-2026)

## Suggestions

| Idea | Status | What it would do |
| --- | --- | --- |
| Show ship label | Ready to test | Turn off **Show ship label** under **Ship display** to hide the text below the schematic, including the second line in expanded view. The ship's **Show label** checkbox in **Arrange HUD** controls the same setting. The label is visible by default. **Save display** keeps the choice. [Test checklist](TESTING-TODO.md#ship-label--pending) |
| Drag and drop HUD layout | Confirmed in game | Move and resize the ship display and health bars in **Arrange HUD**. Use **Apply layout** to use the draft and **Save display** to keep it. Custom-bar changes use **Save for this ship**. [Follow-up checks](TESTING-TODO.md#display-follow-up) |
| Sharper, steadier ship display | Confirmed in game | **HD Wireframe** is the default. A performance comparison with **SD Wireframe** on the damaged station is still open. [Follow-up checks](TESTING-TODO.md#display-follow-up) |
| Textured ship display | Confirmed in game | The textured renderer works in game, including the Expanse drive. The live performance and appearance check on grids near **50,000 blocks** is still open. [Follow-up checks](TESTING-TODO.md#display-follow-up) |

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
