using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;

namespace Marigold
{
    internal sealed class GridObservation : IDisposable
    {
        private readonly MyCubeGrid grid;
        private readonly ObservationSchedule<MySlimBlock> schedule = new ObservationSchedule<MySlimBlock>();
        private readonly Dictionary<Point3, BlockSample> values = new Dictionary<Point3, BlockSample>();
        private int reconcile;
        private bool initialized;

        public bool Ready
        {
            get
            {
                return initialized;
            }
        }

        public int Revision { get; private set; }

        public GridObservation(MyCubeGrid grid)
        {
            this.grid = grid;
            grid.OnBlockAdded += Changed;
            grid.OnBlockRemoved += Removed;
            grid.OnBlockIntegrityChanged += Changed;
            Reconcile();
        }

        private void Changed(MySlimBlock block)
        {
            schedule.Mark(block);
        }

        private void Removed(MySlimBlock block)
        {
            // Connector detach also reports the other grid's still-existing connector.
            // Only evict a local sample once its cell is actually empty.
            if (block == null || block.CubeGrid != grid || grid.GetCubeBlock(block.Position) != null)
                return;
            if (values.Remove(new Point3(block.Position.X, block.Position.Y, block.Position.Z)))
                Revision++;
        }

        private void Reconcile()
        {
            var blocks = new List<MySlimBlock>(grid.GetBlocks());
            schedule.Reset(blocks);
            var live = new HashSet<Point3>();
            foreach (var b in blocks)
                live.Add(new Point3(b.Position.X, b.Position.Y, b.Position.Z));
            var removed = new List<Point3>();
            foreach (var p in values.Keys)
                if (!live.Contains(p))
                    removed.Add(p);
            foreach (var p in removed)
            {
                values.Remove(p);
                Revision++;
            }

            reconcile = 0;
        }

        public void Step(int budget)
        {
            if (grid.Closed || grid.MarkedForClose)
                return;
            if (++reconcile >= 600 && schedule.Ready)
                Reconcile();
            foreach (var b in schedule.Take(budget))
            {
                if (grid.GetCubeBlock(b.Position) != b)
                    continue;
                var sample = Read(b);
                BlockSample old;
                if (!values.TryGetValue(sample.Position, out old) || !ObservationCache.Equal(old, sample))
                {
                    values[sample.Position] = sample;
                    Revision++;
                }
            }

            if (schedule.Ready)
                initialized = true;
        }

        public BlockSample[] Snapshot()
        {
            var result = new BlockSample[values.Count];
            values.Values.CopyTo(result, 0);
            return result;
        }

        internal static BlockSample Read(MySlimBlock block)
        {
            return new BlockSample
            {
                Position = new Point3(block.Position.X, block.Position.Y, block.Position.Z),
                Bounds = new Box3(
                    new Point3(block.Min.X - .5, block.Min.Y - .5, block.Min.Z - .5),
                    new Point3(block.Max.X + .5, block.Max.Y + .5, block.Max.Z + .5)
                ),
                Definition = block.BlockDefinition.Id.ToString(),
                Forward = (int)block.Orientation.Forward,
                Up = (int)block.Orientation.Up,
                HasFunctionalState = block.FatBlock is Sandbox.ModAPI.IMyFunctionalBlock,
                IsFunctional = block.FatBlock == null || block.FatBlock.IsFunctional,
                Integrity = block.Integrity,
                BuildIntegrity = block.BuildIntegrity,
                MaxIntegrity = block.MaxIntegrity
            };
        }

        public void Dispose()
        {
            grid.OnBlockAdded -= Changed;
            grid.OnBlockRemoved -= Removed;
            grid.OnBlockIntegrityChanged -= Changed;
        }
    }
}
