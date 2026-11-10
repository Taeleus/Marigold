# PluginHub submission plan — Legacy

PluginHub lists **0.16.1 for Pulsar Legacy**. The initial listing merged on November 4, 2026. [PluginHub PR #285](https://github.com/StarCpt/PluginHub/pull/285)

For a future **0.17.8** update, prepare a new PR against `StarCpt/PluginHub:main` to update `Plugins/Marigold.xml`. The descriptor should reference the exact tested `master` commit in `Taeleus/Marigold`. Show the proposed PR text and descriptor to the user for review before submitting it.

Use the existing ID `7db0d53e-7572-4cbc-911e-25fa26ef182c`, author Taeleus and runtime `CLR`. Use `src/Marigold.Plugin` as the sole production `SourceDirectories` entry. Pulsar supplies Harmony at runtime and the project uses its package reference for local builds. Submit the descriptor by itself.

Run the Legacy build, regressions and source checks before generating the descriptor with `prepare-release.ps1`. Verify one element each for `RepoId`, `Commit` and `Runtimes`, a description of at most 1,000 characters and the existing GUID. Check the upstream validator and rules before opening the PR. [Release procedure](RELEASE.md)

Use **Update Marigold to 0.17.8 for Pulsar Legacy** as the proposed PR title. Summarize Arrange HUD, layout saving, rendering improvements, blueprint preview cleanup and linked background sizing fixes. Include the final build, source compilation and in-game validation results. [Changelog](../CHANGELOG.md)

The four focused smoke checks passed on the deployed 0.17.7 Legacy package. The 0.17.8 build, regression and source compilation checks passed. Its in-game check for linked background size limits and the custom-bar editor hint remains pending, along with loading the exact pinned source through Pulsar Legacy with the local binary disabled and testing a fresh install. [Testing checklist](../TESTING-TODO.md)

Check the upstream repository and descriptor example when preparing the update. [PluginHub](https://github.com/StarCpt/PluginHub) · [Sample descriptor](https://github.com/StarCpt/PluginHub/blob/main/SamplePlugin.xml)

Reference submission: [Taeleus TetherSE PR #260](https://github.com/StarCpt/PluginHub/pull/260), merged. Follow its GUID + RepoId + full source Commit + CLR structure and concise behavior/validation description. Current [client template](https://github.com/CometWorks/client-plugin-template) confirms CLR runtime spelling and explicit source directories. Existing Marigold project layout need not be renamed to match template folders; the manifest is authoritative.

The 0.14.16 review fixes remain in 0.16.1. Marigold uses the selected game-data root, limits rebuild logging, relies on Pulsar's Harmony dependency and includes the MIT license. Core runtime checks, the recorded custom-`-appdata` check and prior log inspection passed. The later removal-notification and short Spanish-name findings are fixed in 0.16.1 with regression tests.
