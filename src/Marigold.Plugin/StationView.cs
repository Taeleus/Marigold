using System;
using System.Collections.Generic;
using System.Threading;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.World;
using Sandbox.ModAPI.Ingame;
using VRage.Game;
using VRage.Game.Entity.EntityComponents;
using VRage.Game.ModAPI;
using VRageMath;
using ShipConnector = Sandbox.ModAPI.IMyShipConnector;

namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private const string StationModeRequirement = "Station mode requires a static grid you own or that is shared with your faction.";
        private const int StationGridLimit = 8;
        private const int StationRefreshInterval = 60;

        private string stationStatus = "";
        private bool stationModeActive, stationGridLimit, stationDiscoveryPending, stationRefreshDeferred;
        private long stationPlayer, stationFactionId;
        private int stationRefreshIn, stationGraphDirty, stationFactionDirty;
        private MyFactionCollection stationFactions;
        private List<MyCubeGrid> stationGrids = new List<MyCubeGrid>();
        private List<MyCubeGrid> nextStationGrids = new List<MyCubeGrid>();
        private readonly HashSet<MyCubeGrid> examinedStationGrids = new HashSet<MyCubeGrid>();
        private readonly HashSet<MyCubeGrid> deniedStationGrids = new HashSet<MyCubeGrid>();
        private readonly Queue<StationGridAccess> stationQueue = new Queue<StationGridAccess>();
        private readonly Dictionary<MyCubeGrid, StationGridAccess> stationAccess = new Dictionary<MyCubeGrid, StationGridAccess>();
        private readonly Dictionary<MyCubeGrid, StationGrid> stationBaselines = new Dictionary<MyCubeGrid, StationGrid>();
        private readonly Dictionary<MyCubeGrid, GridObservation> stationObservers = new Dictionary<MyCubeGrid, GridObservation>();

        private sealed class StationGridAccess : IDisposable
        {
            private readonly Action markGraphChanged;
            private int ownershipDirty = 1, connectorsDirty = 1;
            public readonly MyCubeGrid Grid;
            public readonly List<ShipConnector> Connectors = new List<ShipConnector>();
            public bool Allowed { get; private set; }

            public StationGridAccess(MyCubeGrid grid, Action markGraphChanged)
            {
                Grid = grid;
                this.markGraphChanged = markGraphChanged;
                Grid.OnBlockOwnershipChanged += OwnershipChanged;
                Grid.OnFatBlockAdded += BlockChanged;
                Grid.OnFatBlockRemoved += BlockChanged;
                Grid.OnGridMerge += GridMerged;
            }

            public void Refresh(long player, IMyFaction faction, bool reconcile)
            {
                bool recount = Interlocked.Exchange(ref ownershipDirty, 0) != 0 || reconcile;
                bool refreshConnectors = Interlocked.Exchange(ref connectorsDirty, 0) != 0 || reconcile;
                if (!recount && !refreshConnectors)
                    return;

                if (refreshConnectors)
                    ClearConnectors();
                Allowed = HasStationAccess(Grid, player, faction, refreshConnectors ? Connectors : null);
                if (refreshConnectors)
                    foreach (var connector in Connectors)
                        connector.IsConnectedChanged += ConnectionChanged;
            }

            private void OwnershipChanged(MyCubeGrid source)
            {
                Interlocked.Exchange(ref ownershipDirty, 1);
                markGraphChanged();
            }

            private void BlockChanged(MyCubeBlock block)
            {
                if (block.CubeGrid != Grid)
                    return;

                if (block is ShipConnector)
                    Interlocked.Exchange(ref connectorsDirty, 1);
                OwnershipChanged(Grid);
            }

            private void GridMerged(MyCubeGrid destination, MyCubeGrid source)
            {
                // A merged-away grid can lose its connectors without block-removal events.
                Interlocked.Exchange(ref connectorsDirty, 1);
                OwnershipChanged(Grid);
            }

            private void ConnectionChanged(ShipConnector connector)
            {
                markGraphChanged();
            }

            private void ClearConnectors()
            {
                foreach (var connector in Connectors)
                    connector.IsConnectedChanged -= ConnectionChanged;
                Connectors.Clear();
            }

            public void Dispose()
            {
                Grid.OnBlockOwnershipChanged -= OwnershipChanged;
                Grid.OnFatBlockAdded -= BlockChanged;
                Grid.OnFatBlockRemoved -= BlockChanged;
                Grid.OnGridMerge -= GridMerged;
                ClearConnectors();
            }
        }

        private void ClearStationAccess()
        {
            foreach (var access in stationAccess.Values)
                access.Dispose();
            stationAccess.Clear();
            deniedStationGrids.Clear();
            if (stationFactions != null)
                stationFactions.FactionStateChanged -= StationFactionChanged;
            stationFactions = null;
            stationPlayer = stationFactionId = 0;
            stationRefreshIn = 0;
            stationDiscoveryPending = false;
            stationRefreshDeferred = false;
        }

        private void ClearStationObservers()
        {
            ClearStationAccess();
            foreach (var value in stationObservers.Values)
                value.Dispose();
            stationObservers.Clear();
            stationGrids.Clear();
            nextStationGrids.Clear();
            examinedStationGrids.Clear();
            stationQueue.Clear();
            stationModeActive = stationGridLimit = false;
        }

        private void MarkStationGraphChanged()
        {
            // Grid ownership notifications can arrive outside the game update thread.
            Interlocked.Exchange(ref stationGraphDirty, 1);
        }

        private void StationFactionChanged(MyFactionStateChange action, long from, long to, long member, long sender)
        {
            if (action != MyFactionStateChange.RemoveFaction && action != MyFactionStateChange.FactionMemberAcceptJoin
                && action != MyFactionStateChange.FactionMemberKick && action != MyFactionStateChange.FactionMemberLeave)
                return;

            long faction = Interlocked.Read(ref stationFactionId);
            if (member == Interlocked.Read(ref stationPlayer) || (faction != 0 && (from == faction || to == faction)))
                Interlocked.Exchange(ref stationFactionDirty, 1);
        }

        private bool CanUseStationMode()
        {
            var session = MySession.Static;
            long player = session?.LocalPlayerId ?? 0;
            return player != 0 && grid != null && grid.IsStatic
                && HasStationAccess(grid, player, session.Factions.TryGetPlayerFaction(player));
        }

        private static bool HasStationAccess(MyCubeGrid candidate, long player, IMyFaction faction,
            List<ShipConnector> connectors = null)
        {
            if (candidate.Closed || candidate.MarkedForClose)
                return false;

            int total = 0, owned = 0, shared = 0;
            foreach (var block in candidate.GetFatBlocks())
            {
                if (connectors != null && block is ShipConnector connector)
                    connectors.Add(connector);

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

        private StationGridAccess GetStationAccess(MyCubeGrid candidate, long player, IMyFaction faction, bool reconcile)
        {
            if (!stationAccess.TryGetValue(candidate, out var access))
                stationAccess.Add(candidate, access = new StationGridAccess(candidate, MarkStationGraphChanged));
            access.Refresh(player, faction, reconcile);
            return access;
        }

        private void UpdateStationConnections(bool reconcileNow = false, bool refreshGeometry = false)
        {
            var session = MySession.Static;
            long player = session?.LocalPlayerId ?? 0;
            bool enabled = controls.Station && player != 0 && grid != null && grid.IsStatic
                && !grid.Closed && !grid.MarkedForClose;
            if (!enabled && !stationModeActive && stationAccess.Count == 0)
                return;

            var factions = enabled ? session.Factions : null;
            var faction = factions?.TryGetPlayerFaction(player);
            long factionId = faction?.FactionId ?? 0;
            bool factionChanged = Interlocked.Exchange(ref stationFactionDirty, 0) != 0
                || stationPlayer != player || stationFactionId != factionId || stationFactions != factions;
            bool graphChanged = Interlocked.Exchange(ref stationGraphDirty, 0) != 0;
            bool reconcile = reconcileNow || stationRefreshDeferred || (stationRefreshIn <= 0 && !stationDiscoveryPending);
            if (stationRefreshIn > 0)
                stationRefreshIn--;
            bool gridClosed = stationGrids.Exists(candidate => candidate.Closed || candidate.MarkedForClose);
            if (enabled && !factionChanged && !graphChanged && !reconcile && !stationDiscoveryPending && !gridClosed)
                return;

            Interlocked.Exchange(ref stationPlayer, player);
            Interlocked.Exchange(ref stationFactionId, factionId);
            if (stationFactions != factions)
            {
                if (stationFactions != null)
                    stationFactions.FactionStateChanged -= StationFactionChanged;
                stationFactions = factions;
                if (stationFactions != null)
                    stationFactions.FactionStateChanged += StationFactionChanged;
            }

            if (reconcile || factionChanged)
            {
                stationRefreshIn = StationRefreshInterval;
                deniedStationGrids.Clear();
                ForgetClosedStationMatches();
            }

            bool refreshAccess = reconcile || factionChanged;
            var root = enabled ? GetStationAccess(grid, player, faction, refreshAccess) : null;
            bool allowed = root?.Allowed == true;
            stationGridLimit = stationDiscoveryPending = false;
            if (allowed)
                DiscoverStationGrids(root, player, faction, refreshAccess);

            bool removed = stationModeActive && !allowed;
            foreach (var previous in stationGrids)
            {
                if (nextStationGrids.Contains(previous))
                    continue;

                removed = true;
                // An ordinary disconnect keeps the match. Readmission still requires a fresh access check.
                if (previous.Closed || previous.MarkedForClose || deniedStationGrids.Contains(previous))
                    stationBaselines.Remove(previous);
                if (stationObservers.TryGetValue(previous, out var observer))
                {
                    observer.Dispose();
                    stationObservers.Remove(previous);
                }
                if (stationAccess.TryGetValue(previous, out var access))
                {
                    access.Dispose();
                    stationAccess.Remove(previous);
                }
            }

            if (!enabled)
                ClearStationAccess();

            // A ship can redock at a different berth while hidden without changing membership.
            if (removed || (refreshGeometry && stationGrids.Count > 0))
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

        private void DiscoverStationGrids(StationGridAccess root, long player, IMyFaction faction, bool refreshAccess)
        {
            int newGridChecks = StationGridLimit;
            examinedStationGrids.Add(grid);
            stationQueue.Enqueue(root);
            while (stationQueue.Count > 0)
            {
                var accessFrom = stationQueue.Dequeue();
                foreach (var connector in accessFrom.Connectors)
                {
                    if (connector.CubeGrid != accessFrom.Grid || connector.Status != MyShipConnectorStatus.Connected)
                        continue;

                    var next = connector.OtherConnector?.CubeGrid as MyCubeGrid;
                    if (next == null || !examinedStationGrids.Add(next) || deniedStationGrids.Contains(next))
                        continue;

                    if (nextStationGrids.Count >= StationGridLimit)
                    {
                        stationGridLimit = true;
                        continue;
                    }

                    if (next.Closed || next.MarkedForClose)
                        continue;

                    // Existing branches are revalidated first when reached. Spread checks for
                    // newly discovered grids across updates so a crowded dock cannot queue them all at once.
                    if (!stationAccess.ContainsKey(next))
                    {
                        if (newGridChecks == 0)
                        {
                            stationDiscoveryPending = true;
                            continue;
                        }
                        newGridChecks--;
                    }

                    var access = GetStationAccess(next, player, faction, refreshAccess);
                    if (!access.Allowed)
                    {
                        deniedStationGrids.Add(next);
                        stationBaselines.Remove(next);
                        access.Dispose();
                        stationAccess.Remove(next);
                        continue;
                    }

                    nextStationGrids.Add(next);
                    stationQueue.Enqueue(access);
                }
            }
            examinedStationGrids.Clear();
        }

        private void ForgetClosedStationMatches()
        {
            if (stationBaselines.Count == 0)
                return;

            List<MyCubeGrid> closed = null;
            foreach (var candidate in stationBaselines.Keys)
                if (candidate.Closed || candidate.MarkedForClose)
                    (closed ?? (closed = new List<MyCubeGrid>())).Add(candidate);
            if (closed != null)
                foreach (var candidate in closed)
                    stationBaselines.Remove(candidate);
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

        private void UpdateStationView(bool updateHud)
        {
            if (!updateHud)
            {
                // Keep event invalidation armed and reconcile access before using the cached station view again.
                stationRefreshDeferred = true;
                return;
            }

            UpdateStationConnections(refreshGeometry: stationRefreshDeferred);
            stationRefreshDeferred = false;
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
            bool limited = stationGridLimit || stationDiscoveryPending;
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
                stationBaselines.TryGetValue(current, out cached);
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
