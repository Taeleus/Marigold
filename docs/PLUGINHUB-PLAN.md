# PluginHub submission plan — Legacy

Target: StarCpt/PluginHub, base `main`. Proposed file: `Plugins/Marigold.xml`. Plugin source remains in public Taeleus/Marigold. No PR is created by this document.

Use the existing ID `7db0d53e-7572-4cbc-911e-25fa26ef182c`, author Taeleus, repository Taeleus/Marigold and exact tested master commit. Runtime restriction: `CLR` (Pulsar Legacy). Keep Harmony 2.4.2 in NuGetReferences and `src/Marigold.Plugin` as the sole production SourceDirectories entry. No game DLLs, local skill files or build outputs belong in the PR.

Preparation: run Legacy build/regressions and source checks; generate descriptor with `prepare-release.ps1`; verify one RepoId/Commit/Runtimes element, description <=1,000 characters and existing GUID. Test the exact pinned source in Legacy with the local binary disabled, plus fresh install. Check the current upstream validator/rules before creating the PR.

Proposed title: Add Marigold ship damage HUD for Pulsar Legacy

Proposed description: Marigold adds a cockpit-relative ship schematic and optional system health bars. It compares observed blocks with a captured or XML-blueprint baseline, showing damaged, non-functional and missing regions. Includes persistent profiles, recovery alignment, baseline versions and configurable rendering/performance settings. Targets Legacy/CLR only; Interim is not included in this submission. Validation: attach final build/source-check results and actual pinned-source/fresh-install outcomes, without claiming pending checks as passed.

User input: no additional details needed to plan/draft. Actual source-loader and fresh-install test results remain needed before declaring ready. Creating an upstream PR is a separate action from pushing this repository.

References: https://github.com/StarCpt/PluginHub and https://github.com/StarCpt/PluginHub/blob/main/SamplePlugin.xml . The current README directs authors to add a descriptor under Plugins and submit a PR; detailed guidance is temporarily on Pulsar Discord.

Reference submission: [Taeleus TetherSE PR #260](https://github.com/StarCpt/PluginHub/pull/260), merged. Follow its GUID + RepoId + full source Commit + CLR structure and concise behavior/validation description. Current [client template](https://github.com/CometWorks/client-plugin-template) confirms CLR runtime spelling and explicit source directories. Existing Marigold project layout need not be renamed to match template folders; the manifest is authoritative.
