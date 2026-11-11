# PluginHub submission plan — Legacy

PluginHub lists **0.16.1 for Pulsar Legacy**. The initial listing merged on November 4, 2026. [PluginHub PR #285](https://github.com/StarCpt/PluginHub/pull/285)

The proposed **0.17.14** update uses the existing draft PR against `StarCpt/PluginHub:main`. Prepare the descriptor to reference the tested `master` commit in `Taeleus/Marigold`. Review the proposed pin and PR text before applying the update. [PluginHub PR #344](https://github.com/StarCpt/PluginHub/pull/344)

Use the existing ID `7db0d53e-7572-4cbc-911e-25fa26ef182c`, author Taeleus and runtime `CLR`. Use `src/Marigold.Plugin` as the sole production `SourceDirectories` entry. Pulsar supplies Harmony at runtime and the project uses its package reference for local builds. Submit the descriptor by itself.

Run the Legacy build, regressions and source checks before generating the descriptor with `prepare-release.ps1`. Verify one element each for `RepoId`, `Commit` and `Runtimes`, a description of at most 1,000 characters and the existing GUID. Check the upstream validator and rules before opening the PR. [Release procedure](RELEASE.md)

Use **Update Marigold to 0.17.14 for Pulsar Legacy** as the proposed PR title. Summarize the improved modded meshes, CPU and GPU renderer choices, and the layout and blueprint changes since 0.16.1. [Changelog](../CHANGELOG.md)

The 0.17.14 Legacy build, regressions, and stock Legacy source compilation passed. The local package passed GPU seat exit and recovery after losing the main seat. Loading the exact pinned source through Pulsar Legacy and checking a fresh install are separate checks. [Testing checklist](../TESTING-TODO.md)

Check the upstream repository and descriptor example when preparing the update. [PluginHub](https://github.com/StarCpt/PluginHub) · [Sample descriptor](https://github.com/StarCpt/PluginHub/blob/main/SamplePlugin.xml)

Reference submission: [Taeleus TetherSE PR #260](https://github.com/StarCpt/PluginHub/pull/260), merged. Follow its GUID + RepoId + full source Commit + CLR structure and concise behavior/validation description. Current [client template](https://github.com/CometWorks/client-plugin-template) confirms CLR runtime spelling and explicit source directories. Existing Marigold project layout need not be renamed to match template folders; the manifest is authoritative.

The 0.14.16 review fixes remain in 0.16.1. Marigold uses the selected game-data root, limits rebuild logging, relies on Pulsar's Harmony dependency and includes the MIT license. Core runtime checks, the recorded custom-`-appdata` check and prior log inspection passed. The later removal-notification and short Spanish-name findings are fixed in 0.16.1 with regression tests.
