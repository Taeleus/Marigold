# API evidence for milestone 1

Target: installed SE 1.210.014. Paths below are relative to the game-code skill's `Data/Decompiled` directory. Generated sources remain outside this repository and must not be redistributed.

| API | Inspected source | Evidence/use |
| --- | --- | --- |
| IPlugin.Init/Update/Dispose | VRage/VRage/Plugins/IPlugin.cs; Sandbox.Game/Sandbox/MySandboxGame.cs | Game calls plugin.Update in its update loop under exclusive Havok access. No background game-object reads in Marigold. |
| MySession.Static / ControlledEntity | Sandbox.Game/Sandbox/Game/World/MySession.cs | ControlledEntity reads LocalHumanPlayer's controller. Null expected outside a session/control. |
| MySession.OnUnloading | Same | Static Action event invoked during unload; unsubscribe in Dispose. Keep unloading session suppressed until the current session changes. |
| IMyShipController | Sandbox.Common/Sandbox/ModAPI/IMyShipController.cs | Public mod interface for cockpit/remote/cryopod; inherits cube-block access. Actual control of these variants still needs runtime tests. |
| MyCubeGrid.BlocksCount | Sandbox.Game/Sandbox/Game/Entities/MyCubeGrid.cs | Returns m_cubeBlocks.Count, not occupied-cell volume or a terminal-block count. |
| IMyUtilities.CreateNotification | Sandbox.Game/Sandbox/ModAPI/MyAPIUtilities.cs | Creates a MyHudNotification using the CustomText format and supplied arguments. |
| IMyHudNotification.Text/Show/Hide | Sandbox.Game/Sandbox/Game/GUI/MyHudNotification.cs | Text sets format arguments; Show adds to notification collection; Hide removes it. |
| Notification draw caching | Sandbox.Game/Sandbox/Game/GUI/MyHudNotifications.cs | Add reuses an existing notification and resets lifetime; draw text is prepared on insertion. Hide before changing text and Show afterwards rebuild the cached drawing data. |

Read-only source access, successful net48 compilation and isolated behavior tests establish API compatibility and local logic only. They do not prove successful loading in Pulsar or in-game UI behavior.

## Loader and template reference revisions

- Pulsar: `013a108ee1aa6f8912aa97a4832397a781a8051e` from https://github.com/SpaceGT/Pulsar
- Client template: `2d530601755b36afb45cee2045fa252153edd077` from https://github.com/CometWorks/client-plugin-template

Pulsar `Shared/Data/LocalPlugin.cs` reads adjacent XML as GitHubPlugin metadata, loads a local DLL and infers its runtime from references. Missing repository/commit fields are not used in this local-binary path. `Shared/Data/LocalFolderPlugin.cs` filters source directories and excludes bin/obj in its fallback scan. `Shared/Data/GitHubPlugin.cs` restricts archive compilation by SourceDirectories.

The current loader's runtime identifiers differ from descriptions found in the template documentation. Consequently this prototype omits explicit runtime metadata and relies on local assembly inference. Recheck schema before publication or adding Interim support. The installed loader version has not been compared with this downloaded revision.

## Renderer follow-up

Implemented in 0.2: one owner-scoped Harmony postfix on `MyGuiScreenHudSpace.Draw` (returns bool); the method returns false while HUD is hidden/transitioning. Plugin IPlugin has no draw callback. MySession.RegisterComponent does not populate the prepared synchronous draw list on its own, so ad-hoc registration was not used. MyRenderProxy.DrawSprite queues a sprite message; `VRage.Render11/VRage/Render11/Sprites/MySpritesRenderer.cs` rotates it about DestinationRectangle.Center. `Sandbox.Graphics/Sandbox/Graphics/MyGuiManager.cs` uses `Textures/GUI/Blank.dds` for solid UI rectangles. MyCubeGrid.GetBlocks returns its slim-block set; add/remove events supply MySlimBlock. IMyGui exposes cursor/chat visibility and IMyInput exposes the modifier/new-key checks. All compile against local installed DLLs. The installed Pulsar Legacy Harmony assembly reports 2.4.2.0.

The installed `VRage.Render/VRageRender/MyRenderProxy.cs` contains `DebugDrawLine2D` and `DrawSprite`. These are candidates for a schematic feasibility experiment, not an established production rendering path. Verify the draw callback, viewport clipping, debug-render gating and UI lifetime before adopting either. The plugin registry search also returned HUD Hooks (`VanirDev/se-plugin-hudhooks`) and HudLcd 3 (`StarCpt/SE-HudLcdSprite`) as reference candidates; their implementations and licenses have not yet been reviewed.

## Snapshot diagnostics 0.4 evidence

Installed decompiled Sandbox.Game/Entities/Cube/MySlimBlock.cs exposes Integrity, BuildIntegrity and MaxIntegrity from m_componentStack. CurrentDamage is BuildIntegrity - Integrity (lines 957-969). Position/Min/Max, BlockDefinition.Id and Orientation are copied into numeric/string samples. No mutable game objects cross into the worker.

MyCubeGrid.OnGridSplit is Action<MyCubeGrid,MyCubeGrid> (line 3878); split creation invokes originalGrid.OnGridSplit(originalGrid,myCubeGrid) at line 4810. Captured references retain this subscription until clear/session teardown. MultiplayerActive/IsServer compile against the installed ModAPI; client absence is conservatively unknown. These facts verify signatures/implementation, not live multiplayer observation freshness.

0.4.4: installed Sandbox.Game/Entities/MyCubeBlock.cs line 321 defines IsFunctional as SlimBlock.ComponentStack.IsFunctional; IsWorking is a separate property at line 327. Only FatBlock instances implementing Sandbox.ModAPI.IMyFunctionalBlock are sampled for the orange state; Enabled and IsWorking are not used to infer structural failure.

## Profiles 0.5

MyCockpit in Sandbox.Game.Entities provides Position, Orientation, BlockDefinition and CustomName; these accesses compile against the installed assemblies. MyBlockOrientation.GetMatrix provides the anchor's discrete axes. Verified Base6Directions.Direction order in installed VRage.Math: Forward, Backward, Left, Right, Up, Down. Profile alignment transforms these directions and all envelope corners. No world IDs, grid names or runtime entity IDs are used as automatic persistent identity. Explicit profile selection pairs the cockpit after reload.


## 0.7 station and reconnect implementation

The installed `Sandbox.ModAPI.IMyShipConnector` exposes `OtherConnector`; its ingame base exposes `Status` with `MyShipConnectorStatus.Connected`. Station traversal reads only these locked links, never the logical/physical group that could include unintended mechanical attachments. A visited set prevents cycles. Local cell coordinates use each grid's `WorldMatrix`, the controlled grid's `WorldMatrixNormalizedInv`, and the ratio of grid sizes. Connected-grid geometry is normalized independently before conversion into the shared controlled-grid frame; no block positions are used as cross-grid comparison keys.

Reassociation uses existing `MyCockpit` local Position/Orientation/definition and immutable block samples. All game entities are read on the update thread; the worker matches copied values and receives no game objects. Candidate results carry the display generation and are discarded across grid/session changes or intervening profile operations. Automatic binding changes only the in-memory reference; `ProfileStore.Put` is never called by matching. The source-build assembly version and startup log were updated to 0.7.0 alongside the binary project version.

Nexus primary reference: https://github.com/SE-Nexus/SeamlessClient describes post-transfer entity synchronization delays. Marigold does not hook Nexus or claim a cluster identity token. Repeated stable snapshots and retries are a client-side compatibility strategy that still requires the queued Nexus test.
