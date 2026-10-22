# Release preparation

This is a testable prototype, not a validated public release. No PluginHub submission has been made.

## Repeatable checks

1. Run `./build.ps1` to compile the net48 plugin and run the automated suites.
2. Run `./verify-source.ps1` to compile exactly the current descriptor's production source files without `LOCAL_BUILD` or SDK-generated assembly metadata. This catches differences between the binary and source paths. It is not execution inside Pulsar's compiler/loader.
3. Commit the production source and metadata, then run `./prepare-release.ps1`. The resulting `artifacts/PluginHub/Marigold.xml` identifies `Taeleus/Marigold`, pins the source commit and restricts runtime to `NETFramework`.
4. Complete the deferred runtime checks in TESTING-TODO.md before hub submission. Do not mark GUI appearance, Nexus behavior or live performance as validated from automated results.

The local binary package contains only plugin.dll, plugin.pdb and plugin.xml. It does not include game assemblies or a second Harmony DLL. Build and verification scripts write only inside the workspace. Installation into Pulsar is a separate step.

## Rules review

Reviewed the current [PluginHub rules](https://github.com/StarCpt/PluginHub/wiki/Plugins#rules) on October 22, 2026, using the upstream wiki source. The implementation targets .NET Framework 4.8, disables implicit usings, does not modify the mod API whitelist or DLC, has no reverse patches or built-in updater, and does not alter Pulsar. Its single Harmony HUD postfix is explicitly owned and unpatched on disposal. Production code does not use assembly location/name for behavior. No executable assets are distributed.

The manifest explicitly selects `src/Marigold.Plugin`; tests and research are excluded. The short plugin description is checked against the sample descriptor's 1,000-character limit. Hub reviewers still determine whether the display is acceptable for multiplayer use; this local review is not hub approval.

## Validation recorded

The plugin and source-only compilation target the installed game API. Automated suites cover lifecycle doubles, geometry, comparison, profiles, parsing, transforms, commands, preference migration, catalog discovery and budgeted observation. GUI drawing and actual game-thread timing are not simulated by these checks.

Read scheduling allows up to 512 controlled-grid block reads per update plus a shared budget of 512 for up to eight station-context grids. At most half the main scheduler budget is used for queued event work, with the rest available for sweeping. The dirty queue is capped at 4,096; periodic sweeps/reconciliation cover overflow and missed events. Initial membership enumeration, periodic membership reconciliation, explicit capture, immutable-array copies and matching still have costs that need live measurement.

Localization covers built-in control labels/options in English and Spanish. Diagnostic and command-response text remains English. Additional languages and translated diagnostics are future polish, not claimed complete.


October 22, 2026 validation: release binary and 23-file source-only builds passed with zero warnings/errors. Automated assertions passed: 12 lifecycle, 16 blueprint, 45 display/settings, 50 association/station, 216 interface/observation, 119 profile, 5 fill, 29 comparison, 6,973 geometry and 403 command/orientation checks. These are development checks; the testing checklist remains unchecked for user validation.


## 0.10 validation

Binary build, automated suites and the 30-file manifest-selected source compilation pass. The new feedback suite adds 68 checks covering recovery rotations, missing structure, ambiguity, cancellation, key aliases, category styles and settings migration. A sandboxed repeat emitted NU1900 when the NuGet audit service was unreachable; the network-enabled dependency restore/build completed without warnings. Actual Pulsar source loading remains a user acceptance check.

The 0.9 runtime passes are recorded in TESTING-TODO.md. The new 0.10 section covers recovery, customization, reference libraries, imports and performance; none are marked passed by compilation. See ROADMAP.md for outstanding recovery/revision/import limitations. No PluginHub submission has been made.


## 0.11 validation scope

Health bars add pure classification and settings checks, with existing comparison tests retained after sharing the classifier. Runtime appearance, positioning, persistence and large-grid refresh behavior remain in the 0.11 user checklist. No PluginHub submission is made by this update.
