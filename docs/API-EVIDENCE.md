# Space Engineers and Pulsar API evidence

The game API observations below come from a read-only inspection of Space Engineers 1.210.014. Paths refer to the game-code skill's `Data/Decompiled` tree. The generated game sources stay outside this repository. See the [roadmap](../ROADMAP.md) for current release status and [performance results](PERFORMANCE-RESULTS.md) for measured behavior.

## Game lifecycle and observations

| API | Observed behavior and use |
| --- | --- |
| `IPlugin.Init`, `Update`, `Dispose` | `VRage/VRage/Plugins/IPlugin.cs` has no drawing callback. `Sandbox.Game/Sandbox/MySandboxGame.cs` calls plugin `Update` in its update loop under exclusive Havok access. |
| `MySession.Static`, `ControlledEntity`, `OnUnloading` | In `Sandbox.Game/Sandbox/Game/World/MySession.cs`, control comes from the local human player's controller. It can be absent before the player controls a grid. Marigold unsubscribes from the unload event on dispose and clears the prior session's display and observers. |
| `IMyShipController`, `MyCockpit`, `MyBlockOrientation.GetMatrix` | Marigold reads the current ship controller's grid and orientation for the live view. It uses a seated `MyCockpit` for manual anchoring and a cockpit's position, orientation, and definition for alignment. Verified `Base6Directions.Direction` order is Forward, Backward, Left, Right, Up, Down. |
| `MyCubeGrid.GetBlocks`, `BlocksCount` and block events | In `Sandbox.Game/Sandbox/Game/Entities/MyCubeGrid.cs`, these refer to slim block records. `BlocksCount` counts block records. Marigold updates observations from add, remove, and integrity events and reconciles with periodic scans. |
| `MySlimBlock.Integrity`, `BuildIntegrity`, `MaxIntegrity` | `Sandbox.Game/Sandbox/Game/Entities/Cube/MySlimBlock.cs` exposes these values through the component stack. `CurrentDamage` is build integrity minus current integrity. Marigold copies geometry, definition, orientation, and integrity into a worker sample. |
| `MyCubeBlock.IsFunctional` | In `Sandbox.Game/Sandbox/Game/Entities/MyCubeBlock.cs`, this comes from the component stack. Marigold tracks that state for fat blocks implementing `IMyFunctionalBlock`. `IsWorking`, power availability, and the enabled switch are separate from structural integrity. |
| `MyCubeGrid.OnGridSplit` | The game invokes `Action<MyCubeGrid, MyCubeGrid>` with the original and detached grids. Marigold tracks this event while a reference is bound and clears subscriptions on detach or session teardown. |

Marigold's game update code reads live grid and block state. Background matching and outline building work on copied values. The textured mesh path snapshots the loaded geometry and model references it needs before starting worker processing. Old worker results are discarded when the relevant display or reference changes. Automatic matching binds an existing profile in memory and leaves its saved baseline fixed.

## Rendering

- **HUD draw.** An owner-scoped Harmony postfix runs after the game's `MyGuiScreenHudSpace.Draw` in `Sandbox.Game/Sandbox/Game/GUI`. The method's return value tells Marigold whether the HUD drew. `MyRenderProxy.DrawSprite` queues sprite messages and the game renderer rotates a sprite around the destination rectangle center. Marigold uses the game's regular HUD drawing path for the schematic and generates a texture for the rasterized outline display. `Sandbox.Graphics/Sandbox/Graphics/MyGuiManager.cs` uses `Textures/GUI/Blank.dds` for solid UI rectangles.
- **GPU Textured Ship.** An additional Harmony prefix targets `VRageRender.MyRender11.RenderMainSprites` in `VRage.Render11/VRageRender/MyRender11.cs`. The prefix submits this plugin's preview to the game's Direct3D 11 device using the installed SharpDX assemblies. The renderer manages its preview resources on this path and uses the existing wireframe while it has no matching textured frame. The CPU renderer is available in Preferences.
- **Input.** `IMyGui` exposes cursor and chat entry visibility. `IMyInput` exposes modifiers and new key presses. The game still receives keys assigned to Marigold shortcuts.

In the first prototype, `IMyUtilities.CreateNotification` used `MyHudNotification` and its custom text. `MyHudNotifications` caches draw text on insertion, so the prototype hid and re-showed notifications to update it. The later HUD hook replaced that approach for schematic rendering. `DebugDrawLine2D`, HUD Hooks and HudLcd 3 were investigated as alternatives. The installed Pulsar Legacy Harmony assembly inspected during development reported 2.4.2.0.

## Station context and reconnects

`Sandbox.ModAPI.IMyShipConnector.OtherConnector` and `MyShipConnectorStatus.Connected` identify locked connector links. `MyCubeGrid.IsStatic` identifies whether the controlled grid can act as a station. Each docked grid's cell positions are transformed using its `WorldMatrix`, the station's `WorldMatrixNormalizedInv`, and their grid-size ratio. Damage is calculated in each grid's own coordinates first.

Station access in the 0.17.16 development build counts each ownership-capable fat block. It reads `MyEntityOwnershipComponent.OwnerId` and `ShareMode`, falling back to the block's `IDModule`. The game takes the same component-first order in `MyCubeBlock.ChangeOwner`. `MyCubeGrid.BigOwners` can return ties and the largest plurality of functional blocks. `MyIDModule.GetUserRelationToOwner` also returns `FactionShare` for public sharing. Marigold checks explicit `Faction` sharing and current membership from `MySession.Static.Factions`. Ownership, fat-block, merge, connector and faction notifications mark station access for refresh. The update thread performs the rescan.

Reconnect matching uses local block and cockpit observations. Earlier restart and sector-transfer checks cover the tested server setup. [Nexus Seamless Client](https://github.com/SE-Nexus/SeamlessClient)

## Pulsar loader and release evidence

The inspected Pulsar commit `013a108ee1aa6f8912aa97a4832397a781a8051e` is the v2.4.3 release. The client template revision `2d530601755b36afb45cee2045fa252153edd077` was an earlier packaging reference. [Pulsar v2.4.3](https://github.com/SpaceGT/Pulsar/releases/tag/v2.4.3) · [Client template revision](https://github.com/CometWorks/client-plugin-template/tree/2d530601755b36afb45cee2045fa252153edd077)

Pulsar reads an adjacent `plugin.xml` for a precompiled local DLL. The descriptor can override its inferred runtime. The local packages carry explicit `CLR` (Legacy) or `CoreCLR` (Interim) tags. For a GitHub source descriptor, Pulsar downloads the `RepoId` and `Commit` archive and compiles the C# files selected by `SourceDirectories`. A local development folder also honors those directories and excludes build output in its fallback scan. These loader paths read the descriptor and source files directly. [Local DLL loader](https://github.com/SpaceGT/Pulsar/blob/v2.4.3/Shared/Data/LocalPlugin.cs) · [Local development folder](https://github.com/SpaceGT/Pulsar/blob/v2.4.3/Shared/Data/LocalFolderPlugin.cs) · [GitHub source loader](https://github.com/SpaceGT/Pulsar/blob/v2.4.3/Shared/Data/GitHubPlugin.cs)

PluginHub publishes 0.17.15 for Legacy. The 0.17.16 candidate descriptor lists both runtimes. The candidate passed in-game testing from local DLL packages on Windows in Legacy and Interim. Pinned source loading and native Linux Interim remain unverified. `CoreCLR` can also select native Linux Interim when `Platforms` is unrestricted. [Live descriptor](https://github.com/StarCpt/PluginHub/blob/main/Plugins/Marigold.xml) · [Pulsar environment filters](https://github.com/SpaceGT/Pulsar/blob/v2.4.3/Shared/Tools.cs) · [Pinned source test](RELEASE.md#test-the-pinned-source)
