# Build and release Marigold

See the [roadmap](../ROADMAP.md) for the published version and development status.

## Build and check

Build from the repository root on Windows with the .NET 10 SDK, .NET Framework 4.8 reference assemblies, and Space Engineers installed. Pass `-Bin64 '<path to Space Engineers Bin64>'` if the game is installed outside the default path in the scripts.

```powershell
./build.ps1 -Runtime Legacy
./build.ps1 -Runtime Interim
./verify-source.ps1 -Runtime Both
```

| Package | Framework | Output |
| --- | --- | --- |
| Legacy | .NET Framework 4.8 | `artifacts/Marigold` |
| Interim | .NET 10 | `artifacts/Marigold-Interim` |

Each package contains `plugin.dll`, `plugin.pdb`, and `plugin.xml` for its launcher. The build runs the regression suite and refreshes the package in `artifacts`. Install it separately using the [local package instructions](../README.md#install-a-local-package). The source check compiles the files selected by the manifest without launching Space Engineers.

## Test the pinned source

1. Push the candidate commit so Pulsar can fetch it. From a checkout without pending production changes, pass the full commit SHA to `./prepare-release.ps1 -Commit`. The script writes `artifacts/PluginHub/Marigold.xml` with the requested commit and the `CLR;CoreCLR` runtime list.
2. Close Space Engineers. Move any installed Marigold package with the production plugin ID outside Pulsar's scanned `Local` folders, or use an isolated Pulsar installation. Remove any same-ID development folder source and older test Local Hub entries. Pulsar can discover a competing local copy even if it is disabled.
3. Copy the candidate XML into a dedicated folder. Launch Pulsar with `-sources`, open **Plugins → Sources → Add Local Hub**, and select that folder. Apply the changes, enable the candidate, and restart. Verify the commit in the descriptor and the Local Hub origin in Pulsar.
4. Load a world, sit in a cockpit and open `/mg ui`. Check the version, the HUD, and the Pulsar log for compilation or loading errors. Record the launcher, platform, commit, and results.

For a quick source smoke test alongside an installed DLL, use a temporary plugin ID and distinct name. Enable only the candidate. Use the production ID for the final check after clearing competing sources. Back up Marigold's [game data](../README.md#your-saved-data) before a fresh-install or existing-profile test.

## Submit the update

1. Merge the tested source to `master`. Generate the descriptor for the exact commit you intend to publish. If the merge creates a different commit, confirm the source and descriptor against that final pin.
2. Review the candidate with the [PluginHub submission plan](PLUGINHUB-PLAN.md). Submit the manifest change with a short player-facing summary and the checks actually run.
3. After it lands, remove the candidate Local Hub from **Plugins → Sources** and keep same-ID local packages out of the scan path. Enable Marigold from the public listing, verify its version in `/mg ui`, then restore your usual development sources as needed.

[Documentation index](INDEX.md) · [Release history](history/RELEASE-HISTORY.md)
