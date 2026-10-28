# PluginHub submission plan — Legacy

Target: StarCpt/PluginHub, base `main`. Descriptor file: `Plugins/Marigold.xml`. Plugin source remains in public Taeleus/Marigold. Open submission: [PluginHub PR #285](https://github.com/StarCpt/PluginHub/pull/285).

Use the existing ID `7db0d53e-7572-4cbc-911e-25fa26ef182c`, author Taeleus, repository Taeleus/Marigold and exact tested master commit. Runtime restriction: `CLR` (Pulsar Legacy). Use `src/Marigold.Plugin` as the sole production SourceDirectories entry. Version 0.14.16 removes the redundant manifest Harmony declaration because Pulsar provides it; the local project keeps its build-time package reference. No game DLLs, local skill files or build outputs belong in the PR.

Preparation: run Legacy build/regressions and source checks; generate descriptor with `prepare-release.ps1`; verify one RepoId/Commit/Runtimes element, description <=1,000 characters and existing GUID. Test the exact pinned source in Legacy with the local binary disabled, plus fresh install. Check the current upstream validator/rules before creating the PR.

Proposed title: Add Marigold ship damage HUD for Pulsar Legacy

Proposed description: Marigold adds a cockpit-relative ship schematic and optional system health bars. It compares observed blocks with a captured or XML-blueprint baseline, showing damaged, non-functional and missing regions. Includes persistent profiles, recovery alignment, baseline versions and configurable rendering/performance settings. Targets Legacy/CLR only; Interim is not included in this submission. Validation: attach final build/source-check results and actual pinned-source/fresh-install outcomes, without claiming pending checks as passed.

Current status: the 0.16.0 UI checklist and both 0.16.1 review-fix checks passed in local-package user testing. Build/regression and source-compilation checks passed. The open PR can be updated to the tested master source; a live exact-pin source-loader and fresh-install smoke test remain separate validation, and are not implied by local DLL testing.

References: https://github.com/StarCpt/PluginHub and https://github.com/StarCpt/PluginHub/blob/main/SamplePlugin.xml . The current README directs authors to add a descriptor under Plugins and submit a PR; detailed guidance is temporarily on Pulsar Discord.

Reference submission: [Taeleus TetherSE PR #260](https://github.com/StarCpt/PluginHub/pull/260), merged. Follow its GUID + RepoId + full source Commit + CLR structure and concise behavior/validation description. Current [client template](https://github.com/CometWorks/client-plugin-template) confirms CLR runtime spelling and explicit source directories. Existing Marigold project layout need not be renamed to match template folders; the manifest is authoritative.

The 0.14.16 review fixes remain in 0.16.1: use only the selected game-data root, remove routine rebuild logging and the redundant descriptor Harmony dependency, and include the MIT license. Core runtime checks, the recorded custom-`-appdata` check and prior log inspection passed. The later removal-notification and short Spanish-name findings are fixed in 0.16.1 with regression tests. Final exact-pin source loading is not claimed as completed.
