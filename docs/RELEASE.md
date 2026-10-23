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

## 0.12.3 validation scope

The system list no longer exposes a separate Mechanical category; those blocks are grouped under Utility/Other. Gas is labeled Tanks. Legacy `gas`/`mechanical` filters and the previous system-style ordering migrate when saved display settings load. Pagination and the other 0.12.2 browser fixes are user-verified; the new group labels and settings migration still need the checks in TESTING-TODO.md.

## 0.12.4 validation scope

Adds a dedicated Tools health bar for welders, grinders, drills and ToolCore block definitions. ToolCore definitions also match the existing Tools schematic filter. Previous six- and seven-bar layouts retain their saved entries, with Tools disabled by default. Automated classification, health grouping and settings migration checks pass; in-game appearance and SDX2 modpack checks remain pending in TESTING-TODO.md.

## 0.12.5 validation scope

Corrects classification of modded tools registered as `MyObjectBuilder_CubeBlock` with tool names in their subtype IDs. Matching the complete definition ID ensures SDX2 Deep Welder ToolCore blocks are categorized into the Tools filter and health bar. An observer regression test covers the CubeBlock subtype form shown in the reported in-game screenshot.


## 0.12.6 review

Reviewed the 0.12.2 baseline through the committed blueprint browser fix and uncommitted 0.12.3-0.12.5 changes. In-place page updates, nonempty search validation, category/style migration and appended Tools layouts are retained. Fixed sorter precedence blocking SDX tools, added optional ToolCore GetTCDefs classification with session cleanup, and synchronized source assembly versions. Regression checks use actual SDX definition IDs and exercise registered subtype detection without WeaponCore, missing-tool accounting and cleanup. Game/mod directories remain read-only. Runtime checks are pending.


## 0.13.0 dev validation

Binary build and all automated suites pass, including 13 persistent-recovery/reference lifecycle checks and 129 profile persistence/alignment checks. All 35 manifest-selected sources compile without LOCAL_BUILD. NuGet vulnerability-data retrieval was unavailable (NU1900); no vulnerability scan is claimed. Runtime acceptance is pending in TESTING-TODO.md. Stable master remains 0.12.6; the dev package is not deployed. Profile format 3 requires a full pre-upgrade data-folder backup for rollback to stable.


## 0.14.0 development scope

Adds partial-repair/cockpit-type recovery matching, explicit manual quarter turns and fallback preview, version rename/replace with history preservation, station hull styles with a shared per-grid line budget, bounded coarse outer extents and current Spanish UI labels. XML-only blueprint support is an explicit release limitation; investigation is recorded in BLUEPRINT-FORMATS.md. ROADMAP.md now separates delivered features from release validation instead of repeating historical tasks.

Runtime acceptance remains pending in the appended 0.14 checklist, alongside the untested 0.13 checklist. The build does not deploy. Profile format remains 3; the full pre-0.13 data backup is still required for rollback to 0.12.6.

Validation: binary build and all regression suites passed, including 32 recovery/reference lifecycle checks and 22 blueprint checks. All 35 manifest-selected files compiled without LOCAL_BUILD. NU1900 reports unavailable NuGet vulnerability data; no vulnerability audit is claimed. Runtime testing is pending.
