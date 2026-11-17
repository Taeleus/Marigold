# PluginHub submission plan

The public listing pins **0.17.15 for Legacy**. The next candidate uses `CLR;CoreCLR` for **Legacy and Interim** and `Windows;Proton` for platform eligibility. This includes Bazzite when running through Proton. Native Linux Interim needs model and texture path fixes and in-game checks of both Textured Ship renderers before it can be listed. [Public entry](https://github.com/StarCpt/PluginHub/blob/main/Plugins/Marigold.xml) · [Pulsar runtime and platform checks](https://github.com/SpaceGT/Pulsar/blob/v2.4.3/Shared/Tools.cs) · [Platform review](https://github.com/StarCpt/PluginHub/pull/362#pullrequestreview-5928295452)

## Manifest

- Submit the change to `Plugins/Marigold.xml` by itself. Use ID `7db0d53e-7572-4cbc-911e-25fa26ef182c`, `RepoId` of `Taeleus/Marigold`, and the full tested source commit. Preserve `Marigold` as the display name.
- Verify the pinned source and PluginHub descriptors both specify `Windows;Proton` and `CLR;CoreCLR`.
- Select `src/Marigold.Plugin` as the source directory. The loader supplies Harmony. The project's Harmony reference is for local compilation.
- Use the player-facing description from the source manifest. It starts with `/mg capture` and the equivalent **Baselines > Capture this ship** path. Keep the description within 1,000 characters.
- Run PluginHub's `python test.py Plugins/` with the candidate descriptor applied. Review the exact diff from the published source pin to the proposed pin. [PluginHub rules](https://github.com/StarCpt/PluginHub/wiki/Plugins#rules)

## Review and submission

Use the [release guide](RELEASE.md#test-the-pinned-source) to verify source loading. Keep local binary runs, source compilation and in-game source loading distinct in the review. Link the [changelog](../CHANGELOG.md) and summarize the changes that matter to players.

[Interim contribution](https://github.com/Taeleus/Marigold/pull/3) · [Initial submission](https://github.com/StarCpt/PluginHub/pull/285)
