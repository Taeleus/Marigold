# Legacy release procedure

The source version is **0.17.15**. The Legacy build and regression suite passed. The local Legacy package showed the wireframe while the first GPU frame loaded and passed resolution and display-mode changes. PluginHub currently lists **0.16.1** for **Legacy/CLR**. [Test checklist](../TESTING-TODO.md#01715-gpu-preview-retest)

## Merge the source

Building locally requires a .NET 10 SDK, .NET Framework 4.8 reference assemblies and the installed Space Engineers Bin64 folder. The build scripts accept `-Bin64 'D:\SteamLibrary\steamapps\common\SpaceEngineers\Bin64'` if you need to specify the game location.

1. Run `./build.ps1 -Runtime Legacy` and `./verify-source.ps1 -Runtime Legacy` against the tested code. Record any build or dependency audit warnings.
2. Record the in-game results in the testing checklist. The local package consists of `plugin.dll`, `plugin.pdb` and `plugin.xml`.
3. Commit and push `dev`, then merge and push `master`.

## Publish a PluginHub update

1. From the tested `master` commit, run `./prepare-release.ps1 -Commit HEAD` to generate a Legacy manifest with the full source SHA and exactly one runtime entry (`CLR`).
2. Disable the local binary and test loading the exact source pin through Pulsar Legacy. Check a fresh install and existing profiles. Back up the profiles before testing.
3. Check the current upstream requirements and prepare the proposed PR text and descriptor for review. After approval, update the existing `Plugins/Marigold.xml` descriptor through the open PR, or open a PR if there is none. [PluginHub plan](PLUGINHUB-PLAN.md)

See [test checklist](../TESTING-TODO.md), [historical notes](history/RELEASE-HISTORY.md), [documentation index](INDEX.md).
