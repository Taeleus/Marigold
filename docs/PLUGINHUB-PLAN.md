# PluginHub submission plan

The public listing pins **0.17.15 for Legacy**. The next candidate targets **Legacy and Interim** using `CLR;CoreCLR`. Pulsar uses the runtime and platform fields independently. With no platform filter, the candidate also matches native Linux Interim. Record results for the launcher and platform combinations the submission will enable. [Public entry](https://github.com/StarCpt/PluginHub/blob/main/Plugins/Marigold.xml) · [Previous update](https://github.com/StarCpt/PluginHub/pull/344) · [Pulsar runtime and platform checks](https://github.com/SpaceGT/Pulsar/blob/v2.4.3/Shared/Tools.cs)

## Manifest

- Submit the change to `Plugins/Marigold.xml` by itself. Use ID `7db0d53e-7572-4cbc-911e-25fa26ef182c`, `RepoId` of `Taeleus/Marigold`, and the full tested source commit. Preserve `Marigold` as the display name.
- Select `src/Marigold.Plugin` as the source directory. The loader supplies Harmony. The project's Harmony reference is for local compilation.
- Use the player-facing description from the source manifest. It starts with `/mg capture` and the equivalent **Baselines > Capture this ship** path. Keep the description within 1,000 characters.
- Run PluginHub's `python test.py Plugins/` with the candidate descriptor applied. Review the exact diff from the published source pin to the proposed pin. [PluginHub rules](https://github.com/StarCpt/PluginHub/wiki/Plugins#rules)

## Review and submission

Use the [release guide](RELEASE.md#test-the-pinned-source) to verify source loading. Keep local binary runs, source compilation and in-game source loading distinct in the review. Link the [changelog](../CHANGELOG.md) and summarize the changes that matter to players.

[Interim contribution](https://github.com/Taeleus/Marigold/pull/3) · [Initial submission](https://github.com/StarCpt/PluginHub/pull/285)
