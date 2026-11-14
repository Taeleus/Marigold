# PluginHub submission plan

PluginHub currently lists **0.17.15 for Pulsar Legacy**. The next update targets **Legacy and Interim** once the current build passes in-game testing on both launchers. [Current listing](https://github.com/StarCpt/PluginHub/blob/main/Plugins/Marigold.xml) · [Previous release](https://github.com/StarCpt/PluginHub/pull/344)

Run both local builds, regressions and source compilation. Verify the exact candidate pin with the local DLL disabled on each launcher. The contributor's Interim test covered 0.16.1. The current Interim checklist also covers CPU and GPU Textured Ship. [Release procedure](RELEASE.md) · [Testing checklist](../TESTING-TODO.md#pending-beta-checks)

Generate the candidate with `prepare-release.ps1`. Verify the full source commit, `RepoId` of `Taeleus/Marigold`, existing ID `7db0d53e-7572-4cbc-911e-25fa26ef182c`, `src/Marigold.Plugin` as the source directory, and `CLR;CoreCLR` as the runtime list. Keep the description within 1,000 characters and run the PluginHub validator. [PluginHub rules](https://github.com/StarCpt/PluginHub/wiki/Plugins#rules)

Submit the descriptor by itself and summarize the player-facing changes and validation in the pull request. [Changelog](../CHANGELOG.md)
