using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI.Ingame;
using VRage.Game;
using VRage.Game.Entity.EntityComponents;
using VRage.Game.ModAPI;
using VRageMath;

namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private const string StationModeRequirement = "Station mode requires a static grid you own or that is shared with your faction.";
        private string stationStatus = "";
        private bool stationModeActive, stationGridLimit;
        private List<MyCubeGrid> stationGrids = new List<MyCubeGrid>();
        private List<MyCubeGrid> nextStationGrids = new List<MyCubeGrid>();
        private readonly HashSet<MyCubeGrid> examinedStationGrids = new HashSet<MyCubeGrid>();
        private readonly Queue<MyCubeGrid> stationQueue = new Queue<MyCubeGrid>();
        private readonly Dictionary<long, StationGrid> stationBaselines = new Dictionary<long, StationGrid>();
        private readonly Dictionary<MyCubeGrid, GridObservation> stationObservers = new Dictionary<MyCubeGrid, GridObservation>();

        private void ClearStationObservers()
        {
            foreach (var value in stationObservers.Values)
                value.Dispose();
            stationObservers.Clear();
            stationGrids.Clear();
            nextStationGrids.Clear();
            examinedStationGrids.Clear();
            stationQueue.Clear();
            stationModeActive = stationGridLimit = false;
        }

        private bool CanUseStationMode()
        {
            var session = MySession.Static;
            long player = session?.LocalPlayerId ?? 0;
            return player != 0 && grid != null && grid.IsStatic
                && HasStationAccess(grid, player, session.Factions.TryGetPlayerFaction(player));
        }

        private static bool HasStationAccess(MyCubeGrid candidate, long player, IMyFaction faction)
        {
            if (candidate.Closed || candidate.MarkedForClose)
                return false;

            int total = 0, owned = 0, shared = 0;
            foreach (var block in candidate.GetFatBlocks())
            {
                var ownership = block.Components.Get<MyEntityOwnershipComponent>();
                var module = block.IDModule;
                if (ownership == null && module == null)
                    continue;

                long owner = ownership?.OwnerId ?? module.Owner;
                var mode = ownership?.ShareMode ?? module.ShareMode;
                total++;
                if (owner == player)
                    owned++;
                if (owner != 0 && mode == MyOwnershipShareModeEnum.Faction && faction?.IsMember(owner) == true)
                    shared++;
            }

            return StationGeometry.HasAccess(total, owned, shared);
        }

        private void UpdateStationConnections()
        {
            if (!controls.Station && !stationModeActive)
                return;

            var session = MySession.Static;
            long player = session?.LocalPlayerId ?? 0;
            var faction = player != 0 ? session.Factions.TryGetPlayerFaction(player) : null;
            bool allowed = controls.Station && player != 0 && grid != null && grid.IsStatic
                && HasStationAccess(grid, player, faction);
            stationGridLimit = false;

            if (allowed)
            {
                examinedStationGrids.Add(grid);
                stationQueue.Enqueue(grid);
                while (stationQueue.Count > 0)
                {
                    foreach (var block in stationQueue.Dequeue().GetFatBlocks())
                    {
                        if (!(block is Sandbox.ModAPI.IMyShipConnector connector)
                            || connector.Status != MyShipConnectorStatus.Connected)
                            continue;

                        var next = connector.OtherConnector?.CubeGrid as MyCubeGrid;
                        if (next == null || !examinedStationGrids.Add(next) || !HasStationAccess(next, player, faction))
                            continue;

                        if (nextStationGrids.Count >= 8)
                        {
                            stationGridLimit = true;
                            continue;
                        }

                        nextStationGrids.Add(next);
                        stationQueue.Enqueue(next);
                    }
                }
                examinedStationGrids.Clear();
            }

            bool removed = stationModeActive && !allowed;
            foreach (var previous in stationGrids)
            {
                if (nextStationGrids.Contains(previous))
                    continue;

                removed = true;
                stationBaselines.Remove(previous.EntityId);
                if (stationObservers.TryGetValue(previous, out var observer))
                {
                    observer.Dispose();
                    stationObservers.Remove(previous);
                }
            }

            // Docked grids affect the merged geometry, its bounds, and the damage report.
            if (removed)
                ClearDisplayedStation();
            else if (stationModeActive != allowed || stationGrids.Count != nextStationGrids.Count)
            {
                dirty = true;
                cooldown = 0;
            }

            stationModeActive = allowed;
            var old = stationGrids;
            stationGrids = nextStationGrids;
            nextStationGrids = old;
            nextStationGrids.Clear();
        }

        private void ClearDisplayedStation()
        {
            RefreshReference(references.ContainsKey(grid) ? "Comparison pending." : "No reference captured.");
            stationStatus = "";
            points = Array.Empty<Point3>();
            shadedMarkers = Array.Empty<int>();
            fillBands.Clear();
            schematicBounds = null;
            mainHullLines = 0;
            ReleaseSchematicTexture();
            ReleaseTexturedShip();
        }

        private void UpdateStationObservers(bool updateHud)
        {
            if (!updateHud)
                return;
            int budget = Math.Max(32, 512 / Math.Max(1, stationObservers.Count));
            foreach (var observer in stationObservers.Values)
                observer.Step(budget);
        }

        private StationGrid[] SampleStation()
        {
            if (!stationModeActive)
                return Array.Empty<StationGrid>();

            var result = new List<StationGrid>();
            int blocks = 0;
            bool limited = stationGridLimit;
            foreach (var current in stationGrids)
            {
                GridObservation observer;
                if (!stationObservers.TryGetValue(current, out observer))
                {
                    observer = new GridObservation(current);
                    stationObservers.Add(current, observer);
                }

                if (!observer.Ready)
                {
                    limited = true;
                    continue;
                }

                var snapshot = observer.Snapshot();
                if (blocks + snapshot.Length > 100000)
                {
                    limited = true;
                    continue;
                }

                blocks += snapshot.Length;
                MatrixD transform = current.WorldMatrix * grid.PositionComp.WorldMatrixNormalizedInv;
                double scale = current.GridSize / grid.GridSize;
                var origin = transform.Translation / grid.GridSize;
                var x = transform.Right * scale;
                var y = transform.Up * scale;
                var z = transform.Backward * scale;
                Reference binding;
                references.TryGetValue(current, out binding);
                StationGrid cached;
                stationBaselines.TryGetValue(current.EntityId, out cached);
                if (manualDetached.Contains(current))
                    cached = null;
                if (cached != null)
                {
                    try
                    {
                        if (profileStore.Find(cached.MatchedProfile).Blocks == null)
                            cached = null;
                    }
                    catch
                    {
                        cached = null;
                    }
                }

                var anchors = new List<AnchorFrame>();
                foreach (var block in current.GetFatBlocks())
                    if (block is MyCockpit cockpit)
                    {
                        anchors.Add(CockpitAnchor(cockpit));
                        if (anchors.Count > 16)
                            break;
                    }

                result.Add(new StationGrid
                {
                    EntityId = current.EntityId,
                    Blocks = snapshot,
                    Baseline = binding == null ? (cached == null ? null : cached.Baseline) : binding.Blocks,
                    Anchors = anchors.ToArray(),
                    Profiles = controls.AutoAssociate && !manualDetached.Contains(current)
                        ? new List<ShipProfile>(profileStore.Profiles).ToArray()
                        : new ShipProfile[0],
                    GridSize = current.GridSize,
                    Name = current.DisplayName,
                    Origin = new Point3(origin.X, origin.Y, origin.Z),
                    X = new Point3(x.X, x.Y, x.Z),
                    Y = new Point3(y.X, y.Y, y.Z),
                    Z = new Point3(z.X, z.Y, z.Z)
                });
            }

            stationStatus = "Station context: " + result.Count + " connector-linked grids"
                + (limited ? " (loading or display limit reached)" : "")
                + ". Docked grids use their own compatible saved baselines; unmatched grids remain context only.";
            return result.ToArray();
        }
    }
}
