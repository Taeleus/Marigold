# Marigold: initial research and proposal review

Reviewed October 20, 2026. Scope: research and feedback on the supplied proposal, not execution of its embedded implementation assignment.

Follow-up: development has since started. See ROADMAP.md and README.md for current status; the initial review below is preserved as a historical baseline. Plugin/game-code preparation and the first compiled observer prototype are now complete, with in-game acceptance still pending.

## Assessment

The client plugin approach is credible. The proposal correctly separates live observations from immutable references, treats subgrids as separate coordinate systems, and anticipates multiplayer uncertainty. A full interactive, high-performance 3D HUD is still an engineering hypothesis, not a verified capability of this prototype. No prototype was built or run during this review.

## Environment and setup

- Created this Marigold directory.
- Installed all eight public CometWorks skills in the user's Codex skills directory: se-dev, se-dev-plugin, se-dev-mod, se-dev-game-code, se-dev-plugin-review, se-dev-script, se-dev-server-code, se-dev-torch. Verified all eight SKILL.md files exist. Reviewed the overview, plugin, mod, game-code, and client-plugin guidance.
- Installation does not mean the optional source indexes/decompilation tooling is prepared. No full decompilation or index preparation was performed for this proposal review.
- Inspected both authorized Space Engineers folders read-only. No deployment, game launch, settings edits, or writes to either folder.
- Latest inspected game log: SpaceEngineers_20261020_181524111.log. It reports version 01_210_014, Pulsar Legacy.exe, and .NET Framework 4.8.9363.0. This supports net48 as the first target.
- Local Blueprints has local, cloud, and workshop directories. Their presence does not establish every blueprint format or availability.

## Platform and rules

Pulsar loads plugins and client mods. Legacy runs SE1 on .NET Framework; Interim runs SE1 on .NET 10 through compatibility tooling; Modern is for SE2. A client mod remains subject to the mod environment and whitelist; installing through Pulsar should not be assumed to grant plugin-level access.

PluginHub has separate plugin and client-mod registrations and reviews both. The rules fetched directly from its wiki are preserved in PluginHub-Plugins.md. Follow all of them: .NET Framework 4.8; no implicit usings; no whitelist or DLC availability changes; no Harmony reverse patches; explicit assembly selection if PatchAll is needed; no dependence on assembly name/location; no built-in updater; no changes to Pulsar behavior; no executable assets; avoid excessive multiplayer advantage. Control source inclusion because Pulsar does not read project files.

The modern template supports additional runtimes, while the wiki still specifies Framework 4.8. Start with the locally verified Legacy target; verify current submission requirements before advertising Interim support. Keep test sources outside the production descriptor's source directories. A separate Core project alone does not establish how Pulsar will include its code or dependencies.

## Verified API evidence and limits

The installed Bin64 XML documentation contains:

- VRage.Game.ModAPI.IMyCubeGrid.GetBlocks, OnBlockAdded and OnBlockRemoved (VRage.Game.xml lines 2904, 2917, 2922).
- IMyEntityController.ControlledEntity and ControlledEntityChanged (lines 3427 and 3438).
- IMyGridGroups.GetGroup with GridLinkTypeEnum (lines 4472 and 4480).
- Ingame.IMySlimBlock.BuildIntegrity (line 8976).
- VRageRender.MyRenderProxy.DrawSprite (VRage.Render.xml line 1384).

Keen's online IMySlimBlock documentation exposes position, definition, orientation and integrity-related data. These are documented API surfaces, not proof of complete replication on a multiplayer client. DrawSprite is evidence for sprite rendering, not proof of a separate 3D viewport, depth buffer, or offscreen render target. Assembly implementation and thread constraints need verification before calling internals.

## Changes recommended before implementation

1. **Separate observation from damage inference.** Store observation time and confidence. Missing from the currently observed construct does not prove destroyed. Detached grids, replication gaps, session transitions and grid splits need explicit unknown/detached states. Do not produce destruction telemetry without evidence.
2. **Start references with live snapshots.** They preserve the original local coordinates and avoid arbitrary blueprint registration. Add imported blueprint alignment after snapshot comparison works.
3. **Match blocks, not just occupied cells.** Record definition ID, grid size, anchor position, orientation and footprint. A multi-cell block is one block; an orientation change or replacement is meaningful. Separate unfinished construction, damage, deformation, and functional status.
4. **Use discrete grid alignment.** For same-sized rigid grids, try the 24 proper cube orientations plus integer translations using distinctive anchors. Score matches and reject ambiguity. Do not use world position or a floating-point nearest-neighbor match as primary identity. Subgrids require independent matching and a mechanical relationship graph.
5. **Separate profile identity from design similarity.** Two identical ships may share a reference but need distinct profiles. Include a world/server scope and user confirmation for ambiguous reassociation. Ship names and dimensions are hints, not identifiers.
6. **Prove a schematic HUD first.** Prototype a rotating wireframe or simplified block silhouette, projected into a HUD rectangle. Measure clipping, occlusion, draw calls and input focus. An offscreen full-mesh renderer is a later candidate requiring source verification. Never spawn a gameplay grid solely for display.
7. **Keep collection and drawing independent.** Read game objects on the supported game thread; send immutable plain data to workers. Update topology incrementally, sample integrity within a time budget, and reconcile periodically. Add/remove events do not alone prove complete integrity tracking. Bound queues and cancel stale work after ship switches.
8. **Make performance measurable.** Benchmark 1k, 10k and 50k blocks with armor-heavy and functional-block-heavy constructs. Record CPU collection/comparison time, draw cost, allocations, memory, and frame-time percentiles. Thirty visual updates per second alone is not a sufficient budget. Surface extraction and simplified geometry are candidates, not measured wins yet.
9. **Handle lifecycle explicitly.** Initialize without assuming an active session; attach when a local controller exists; detach on ship change, entity close and session unload; clear stale HUD state; dispose event handlers and rendering resources. Expanded UI should release input on close, focus loss and exceptions.
10. **Bound blueprint import.** Start with an explicitly chosen local bp.sbc, parse without constructing game entities, reject excessive size/counts and external XML entities, preserve unknown modded definition IDs, and retain placeholders when definitions are unavailable. Add compressed/cloud/Workshop formats only after examining their actual representation.

## Recommended sequence

1. Minimal Legacy plugin: controlled ship name and total slim-block count; cockpit/remote switching and clean session teardown.
2. Renderer feasibility spike: synthetic geometry, then a cached main-grid silhouette; rotate/zoom and benchmark.
3. Immutable snapshot and comparison tests, including replacements, multi-cell blocks, ambiguity and grid splits.
4. Category filtering and reliable multiplayer observation states.
5. Local blueprint import and alignment, then persistent profile reassociation.
6. Mechanical subgrid matching, moving transforms, richer rendering and expanded UI.

The proposal's overall order is sound. Bringing snapshot comparison ahead of broad blueprint support gives useful diagnostics earlier and isolates alignment failures. Keep comparison data structures grid-aware from day one without implementing every subgrid feature immediately.

## Open questions

Independent HUD rendering path and resource lifecycle; multiplayer integrity freshness; split/merge behavior; classification of modded systems; local Workshop blueprint representations; exact production source selection; SDX2-specific compatibility. None has been validated in a running game. Projector Preview's Workshop description is useful inspiration, but its source and license were not audited and no reuse is recommended yet.

## Sources

- https://github.com/SpaceGT/Pulsar
- https://github.com/StarCpt/PluginHub
- https://github.com/StarCpt/PluginHub/wiki/Plugins#rules
- https://raw.githubusercontent.com/wiki/StarCpt/PluginHub/Plugins.md
- https://github.com/CometWorks/skills
- https://github.com/CometWorks/client-plugin-template
- https://keensoftwarehouse.github.io/SpaceEngineersModAPI/api/VRage.Game.ModAPI.IMyCubeGrid.html
- https://keensoftwarehouse.github.io/SpaceEngineersModAPI/api/VRage.Game.ModAPI.IMySlimBlock.html
- https://keensoftwarehouse.github.io/SpaceEngineersModAPI/api/VRage.Game.ModAPI.IMyPlayer.html
- https://steamcommunity.com/sharedfiles/filedetails/?id=517675282

Local evidence: installed Bin64 XML documentation and the October 20 game log specified above. Recommendations are design judgments; API documentation presence is not runtime validation.
