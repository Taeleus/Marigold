using System;
using System.Collections.Generic;

namespace Marigold
{
    // Members are expected baseline blocks, not the current contents of a terminal group.
    // Keeping that snapshot makes a destroyed member stay missing until it is repaired.
    internal sealed class CustomHealthBar
    {
        public string Id = Guid.NewGuid().ToString("N");
        public string Name = "Custom bar";
        public string GroupName = "";
        public HealthBarLayout Layout = new HealthBarLayout
        {
            Enabled = true,
            Y = .80
        };
        public BlockSample[] Members = new BlockSample[0];

        public CustomHealthBar Copy()
        {
            return new CustomHealthBar
            {
                Id = Id,
                Name = Name,
                GroupName = GroupName,
                Layout = new HealthBarLayout
                {
                    Enabled = Layout.Enabled,
                    Label = Layout.Label,
                    X = Layout.X,
                    Y = Layout.Y,
                    Width = Layout.Width,
                    Height = Layout.Height,
                    Opacity = Layout.Opacity,
                    PowerMask = Layout.PowerMask
                },
                Members = (BlockSample[])Members.Clone()
            };
        }
    }

    internal static class CustomHealthBars
    {
        public const int MaximumBars = 16;
        public const int MaximumMembers = 250000;

        public static CustomHealthBar Create(string name, string groupName, IEnumerable<BlockSample> members)
        {
            var bar = new CustomHealthBar
            {
                Name = (name ?? "").Trim(),
                GroupName = groupName ?? "",
                Members = CopyMembers(members)
            };
            ValidateBar(bar);
            return bar;
        }

        // Deliberate refresh changes membership, while preserving the bar's identity and layout.
        public static CustomHealthBar Refresh(CustomHealthBar bar, IEnumerable<BlockSample> members)
        {
            if (bar == null)
                throw new ArgumentNullException("bar");
            var updated = bar.Copy();
            updated.Members = CopyMembers(members);
            ValidateBar(updated);
            return updated;
        }

        private static BlockSample[] CopyMembers(IEnumerable<BlockSample> members)
        {
            if (members == null)
                throw new ArgumentNullException("members");
            var result = new List<BlockSample>();
            var positions = new HashSet<Point3>();
            foreach (var member in members)
            {
                if (!positions.Add(member.Position))
                    throw new InvalidOperationException("A block can belong to a custom bar only once.");
                if (result.Count >= MaximumMembers)
                    throw new InvalidOperationException("Custom bars support at most 250,000 saved members.");
                result.Add(member);
            }
            if (result.Count == 0)
                throw new InvalidOperationException("Select at least one block from the saved baseline.");
            return result.ToArray();
        }

        // The supplied observations use the same baseline-relative frame as Members.
        // State order is shared with the standard bars: healthy, damaged, non-functional, missing, unknown.
        public static int[] Build(CustomHealthBar bar, BlockSample[] current)
        {
            var live = new Dictionary<Point3, BlockSample>();
            if (current != null)
                foreach (var block in current)
                    live[block.Position] = block;
            return Build(bar, live);
        }

        // Sharing one observation lookup across bars avoids rebuilding it for every selection.
        public static int[] Build(CustomHealthBar bar, IDictionary<Point3, BlockSample> current)
        {
            if (bar == null)
                throw new ArgumentNullException("bar");
            if (current == null)
                throw new ArgumentNullException("current");
            var result = new int[5];
            foreach (var expected in bar.Members)
            {
                BlockSample block;
                var kind = SnapshotComparison.Classify(
                    expected,
                    current.TryGetValue(expected.Position, out block) ? (BlockSample?)block : null
                );
                int state = kind == ChangeKind.Hull ? 0
                    : kind == ChangeKind.Damaged ? 1
                    : kind == ChangeKind.NonFunctional ? 2
                    : kind == ChangeKind.Missing || kind == ChangeKind.Replaced ? 3
                    : 4;
                result[state]++;
            }
            return result;
        }

        public static int Validate(ShipProfile profile)
        {
            if (profile.CustomBars == null || profile.CustomBars.Count > MaximumBars)
                throw new InvalidOperationException("Limit: 16 custom bars per ship.");
            var baseline = new Dictionary<Point3, BlockSample>();
            if (profile.Blocks != null)
                foreach (var block in profile.Blocks)
                    baseline[block.Position] = block;
            var ids = new HashSet<Guid>();
            int total = 0;
            foreach (var bar in profile.CustomBars)
            {
                ValidateBar(bar);
                if (!ids.Add(Guid.ParseExact(bar.Id, "N")))
                    throw new InvalidOperationException("Custom bar identities must be unique.");
                total += bar.Members.Length;
                if (total > MaximumMembers)
                    throw new InvalidOperationException("Limit: 250,000 custom-bar members per ship.");
                var members = new HashSet<Point3>();
                foreach (var member in bar.Members)
                {
                    BlockSample expected;
                    if (!members.Add(member.Position)
                        || !baseline.TryGetValue(member.Position, out expected)
                        || !SameMember(member, expected))
                        throw new InvalidOperationException("Custom bar members must belong to this saved baseline.");
                }
            }
            return total;
        }

        private static void ValidateBar(CustomHealthBar bar)
        {
            Guid id;
            if (bar == null || !Guid.TryParseExact(bar.Id, "N", out id)
                || string.IsNullOrWhiteSpace(bar.Name) || bar.Name.Length > 128
                || bar.GroupName == null || bar.GroupName.Length > 256
                || bar.Members == null || bar.Members.Length == 0 || bar.Members.Length > MaximumMembers
                || bar.Layout == null || bar.Layout.PowerMask != 0)
                throw new InvalidOperationException("Invalid custom health bar.");
            HealthBarLayout.Parse(bar.Layout.Encode());
        }

        internal static bool SameMember(BlockSample a, BlockSample b)
        {
            return a.Position.Equals(b.Position)
                && a.Definition == b.Definition
                && a.Forward == b.Forward
                && a.Up == b.Up
                && a.Bounds.Min.Equals(b.Bounds.Min)
                && a.Bounds.Max.Equals(b.Bounds.Max);
        }

        public static bool Same(IList<CustomHealthBar> a, IList<CustomHealthBar> b)
        {
            if (a == null || b == null || a.Count != b.Count)
                return a == b;
            for (int i = 0; i < a.Count; i++)
            {
                var first = a[i];
                var second = b[i];
                if (first.Id != second.Id || first.Name != second.Name || first.GroupName != second.GroupName
                    || first.Layout.Encode() != second.Layout.Encode() || first.Members.Length != second.Members.Length)
                    return false;
                for (int member = 0; member < first.Members.Length; member++)
                    if (!SameMember(first.Members[member], second.Members[member]))
                        return false;
            }
            return true;
        }
    }
}
