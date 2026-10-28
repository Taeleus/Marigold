using System;
using System.Collections.Generic;
using System.IO;
using Marigold;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;

internal static class InterfaceObservationChecks
{
    private static int count;

    private static void Check(bool value, string message)
    {
        if (!value)
            throw new Exception(message);
        count++;
    }

    public static void Run()
    {
        var scheduler = new ObservationSchedule<int>();
        var items = new List<int>();
        for (int i = 0; i < 50000; i++)
            items.Add(i);
        scheduler.Reset(items);
        var seen = new HashSet<int>();
        int reads = 0;
        for (int i = 0; i < 200; i++)
        {
            scheduler.Mark(7);
            scheduler.Mark(7);
            var batch = scheduler.Take(512);
            Check(batch.Count <= 512, "Read budget bounded");
            foreach (int item in batch)
                seen.Add(item);
            reads += batch.Count;
        }

        Check(seen.Count == 50000 && scheduler.Ready, "Event traffic cannot starve reconciliation");
        scheduler.Reset(new int[0]);
        Check(scheduler.Ready && scheduler.Take(512).Count == 0, "Empty grid ready");
        var grid = new MyCubeGrid();
        for (int i = 0; i < 1100; i++)
            grid.Add(new MySlimBlock
            {
                Position = new Point3(i, 0, 0),
                Min = new Point3(i, 0, 0),
                Max = new Point3(i, 0, 0)
            });
        var observer = new GridObservation(grid);
        observer.Step(512);
        Check(!observer.Ready, "Partial initial scan never published");
        observer.Step(512);
        observer.Step(512);
        Check(observer.Ready && observer.Snapshot().Length == 1100, "Initial scan complete");
        var block = grid.GetCubeBlock(new Point3(4, 0, 0));
        block.Integrity = 1;
        grid.Damage(block);
        observer.Step(512);
        Check(
            Array.Find(observer.Snapshot(), b => b.Position.Equals(block.Position)).Integrity == 1,
            "Integrity event prioritized"
        );
        grid.Remove(block);
        Check(observer.Snapshot().Length == 1099, "Removal reflected immediately");
        var replacement = new MySlimBlock
        {
            Position = block.Position,
            Min = block.Min,
            Max = block.Max,
            Integrity = 75
        };
        grid.Add(replacement);
        observer.Step(512);
        Check(
            observer.Snapshot().Length == 1100
                && Array.Find(observer.Snapshot(), b => b.Position.Equals(block.Position)).Integrity == 75,
            "Replacement does not use old block object"
        );
        grid.Blocks.Remove(replacement.Position);
        for (int i = 0; i < 610; i++)
            observer.Step(512);
        Check(observer.Snapshot().Length == 1099, "Reconciliation catches missed removal events");
        observer.Dispose();
        Check(grid.HandlerCount == 0, "Dispose releases all observation events");
        CheckRemovalNotifications();
        var c = new SchematicControls();
        c.Execute("/mg ui");
        Check(c.ReferenceAction == "ui", "UI command local routing");
        c.Execute("/mg language es");
        Check(c.ReferenceAction == "language es", "Language command routing");
        Check(
            UiText.Get("es", "Profiles") == "Perfiles" && UiText.Get("es", "Untranslated") == "Untranslated",
            "Translations and fallback"
        );
        foreach (var name in new[] { "", "[", "]", "[]", "[ ", " ]", "[ ]", "[  ]", "[ [ ] ]", "Ship [ ]" })
        {
            Check(UiText.Get("es", name) == name, "Spanish preserves short or empty bracket names: " + name);
            Check(UiText.Get("en", name) == name, "English preserves bracket names: " + name);
        }
        Check(UiText.Get("es", null) == null, "Spanish preserves a missing label");
        Check(UiText.Get("es", "[ Profiles ]") == "[ Perfiles ]", "Generated selection labels still translate");
        Check(UiText.Get("es", "[ [ Profiles ] ]") == "[ [ Perfiles ] ]", "Nested selection labels still translate");
        Check(UiText.Get("es", "[ ] / Ship") == "[ ] / Nave", "Short bracket name works in a translated path");
        string root = Path.Combine(Directory.GetCurrentDirectory(), ".cache", "InterfaceTests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            string sub = Path.Combine(root, "Ship Name");
            Directory.CreateDirectory(sub);
            File.WriteAllText(Path.Combine(sub, "bp.sbc"), "unchanged");
            Directory.CreateDirectory(Path.Combine(root, "Empty"));
            var files = BlueprintCatalog.Find(root);
            Check(
                files.Length == 1 && files[0].EndsWith("bp.sbc") && File.ReadAllText(files[0]) == "unchanged",
                "Catalog reads only local blueprint paths"
            );
            Check(BlueprintCatalog.Find(Path.Combine(root, "Missing")).Length == 0, "Absent catalog root supported");
            c.Language = "es";
            string settings = Path.Combine(root, "display.txt");
            DisplaySettings.Save(settings, c);
            var loaded = new SchematicControls();
            DisplaySettings.Load(settings, loaded);
            Check(loaded.Language == "es", "Language persists");
            var lines = File.ReadAllLines(settings);
            lines[10] = "unsupported";
            File.WriteAllLines(settings, lines);
            bool failed = false;
            try
            {
                DisplaySettings.Load(settings, loaded);
            }
            catch (InvalidDataException)
            {
                failed = true;
            }

            Check(failed && loaded.Language == "es", "Invalid language preserves settings");
        }
        finally
        {
            Directory.Delete(root, true);
        }

        Console.WriteLine("PASS: " + count + " interface/observation checks (GUI rendering remains in-game).");
    }

    private static void CheckRemovalNotifications()
    {
        var grid = new MyCubeGrid();
        for (int i = 0; i < 2048; i++)
            grid.Add(new MySlimBlock
            {
                Position = new Point3(i, 0, 0),
                Min = new Point3(i, 0, 0),
                Max = new Point3(i, 0, 0)
            });
        using (var observer = new GridObservation(grid))
        {
            observer.Step(2048);
            var baseline = observer.Snapshot();
            var local = grid.GetCubeBlock(new Point3(1900, 0, 0));
            var other = new MyCubeGrid();
            var foreign = new MySlimBlock { Position = local.Position, Min = local.Min, Max = local.Max };
            other.Add(foreign);
            int revision = observer.Revision;
            // Detaching connectors notifies each grid about the other grid's connector.
            grid.NotifyRemoved(foreign);
            Check(grid.Blocks.Count == 2048 && observer.Snapshot().Length == 2048,
                "Undocking notification cannot evict a live block at matching local coordinates");
            Check(observer.Revision == revision, "Foreign removal notification does not dirty observed geometry");
            Check(SnapshotComparison.Compare(baseline, observer.Snapshot()).Counts[(int)ChangeKind.Missing] == 0,
                "Undocking creates no false missing marker or health count");
            observer.Step(64);
            Check(observer.Snapshot().Length == 2048, "Undocking stays accurate before a full sweep");
            grid.NotifyRemoved(local);
            Check(observer.Snapshot().Length == 2048 && observer.Revision == revision,
                "Still-resident removal bookkeeping cannot evict its own live block");

            grid.Remove(local);
            Check(observer.Snapshot().Length == 2047 && observer.Revision == revision + 1,
                "A genuine removal is reflected immediately without waiting for a sweep");
            Check(SnapshotComparison.Compare(baseline, observer.Snapshot()).Counts[(int)ChangeKind.Missing] == 1,
                "A genuinely removed block remains classified as missing");
            var replacement = new MySlimBlock
            {
                Position = local.Position, Min = local.Min, Max = local.Max, Integrity = 75
            };
            grid.Add(replacement);
            observer.Step(64);
            revision = observer.Revision;
            grid.NotifyRemoved(local);
            var samples = observer.Snapshot();
            Check(samples.Length == 2048 && observer.Revision == revision,
                "A stale removal notification cannot erase a replacement block");
            Check(Array.Find(samples, item => item.Position.Equals(replacement.Position)).Integrity == 75,
                "Replacement integrity survives stale bookkeeping");
            grid.Remove(replacement);
            Check(observer.Snapshot().Length == 2047, "Removing the replacement still updates immediately");
        }
        Check(grid.HandlerCount == 0, "Removal probes release all event subscriptions");
    }
}

namespace Sandbox.ModAPI
{
    public interface IMyFunctionalBlock
    {
    }
}

namespace Sandbox.Game.Entities.Cube
{
    public sealed class TestDefinition
    {
        public string Id = "CubeBlock/Armor";
    }

    public sealed class TestOrientation
    {
        public int Forward = 0, Up = 4;
    }

    public class TestFatBlock
    {
        public bool IsFunctional = true;
    }

    public sealed class MySlimBlock
    {
        internal Point3 Position, Min, Max;
        public MyCubeGrid CubeGrid;
        public TestDefinition BlockDefinition = new TestDefinition();
        public TestOrientation Orientation = new TestOrientation();
        public TestFatBlock FatBlock;
        public double Integrity = 100, BuildIntegrity = 100, MaxIntegrity = 100;
    }
}

namespace Sandbox.Game.Entities
{
    public sealed partial class MyCubeGrid
    {
        internal readonly Dictionary<Point3, MySlimBlock> Blocks = new Dictionary<Point3, MySlimBlock>();

        public event Action<MySlimBlock> OnBlockAdded, OnBlockRemoved, OnBlockIntegrityChanged;

        public int HandlerCount
        {
            get
            {
                return (OnBlockAdded?.GetInvocationList().Length ?? 0) + (OnBlockRemoved?.GetInvocationList().Length ?? 0)
                    + (OnBlockIntegrityChanged?.GetInvocationList().Length ?? 0);
            }
        }

        public IEnumerable<MySlimBlock> GetBlocks()
        {
            return Blocks.Values;
        }

        internal MySlimBlock GetCubeBlock(Point3 point)
        {
            MySlimBlock value;
            Blocks.TryGetValue(point, out value);
            return value;
        }

        public void Add(MySlimBlock b)
        {
            b.CubeGrid = this;
            Blocks[b.Position] = b;
            OnBlockAdded?.Invoke(b);
        }

        public void Remove(MySlimBlock b)
        {
            Blocks.Remove(b.Position);
            NotifyRemoved(b);
        }

        public void NotifyRemoved(MySlimBlock b)
        {
            OnBlockRemoved?.Invoke(b);
        }

        public void Damage(MySlimBlock b)
        {
            OnBlockIntegrityChanged?.Invoke(b);
        }
    }
}
