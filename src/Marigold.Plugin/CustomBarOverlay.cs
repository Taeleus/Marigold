using System;
using System.Collections.Generic;
using Sandbox.Graphics;
using Sandbox.ModAPI;
using VRageMath;
using VRageRender;

namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private sealed class CustomBarSource
        {
            public string Name;
            public string GroupName = "";
            public IMyTerminalBlock[] Blocks;
        }

        private sealed class CustomBarView
        {
            public CustomHealthBar Bar;
            public CustomHealthBar Aligned;
            public int[] Counts;
        }

        private CustomBarView[] customBarViews = new CustomBarView[0];
        private ShipProfile customAlignedProfile;
        private AnchorFrame customAlignedAnchor;
        private readonly Dictionary<string, CustomHealthBar> customAlignedMembers = new Dictionary<string, CustomHealthBar>();

        private void ResetCustomBarHealth()
        {
            customBarViews = new CustomBarView[0];
            customAlignedProfile = null;
            customAlignedMembers.Clear();
        }

        // Prepare immutable expected members on the update thread, only when profile/alignment changes.
        private CustomBarView[] CustomBarsForBuild(Reference binding, bool preview)
        {
            if (preview || binding == null)
                return new CustomBarView[0];

            var profile = profileStore.Find(binding.ProfileId);
            if (profile.CustomBars.Count == 0)
                return new CustomBarView[0];
            if (!ReferenceEquals(profile, customAlignedProfile) || !ReferenceHistory.SameAnchor(binding.Anchor, customAlignedAnchor))
            {
                customAlignedMembers.Clear();
                foreach (var bar in profile.CustomBars)
                    customAlignedMembers.Add(bar.Id, new CustomHealthBar
                    {
                        Id = bar.Id,
                        Name = bar.Name,
                        GroupName = bar.GroupName,
                        Layout = bar.Layout,
                        Members = ProfileAlignment.Transform(bar.Members, binding.Anchor, false)
                    });
                customAlignedProfile = profile;
                customAlignedAnchor = binding.Anchor;
            }
            var views = new List<CustomBarView>();
            foreach (var bar in profile.CustomBars)
                views.Add(new CustomBarView
                {
                    Bar = bar,
                    Aligned = customAlignedMembers[bar.Id]
                });
            return views.ToArray();
        }

        // Condition counting shares the geometry worker and its observation, never the draw/update loop.
        private static void BuildCustomBarHealth(CustomBarView[] views, BlockSample[] snapshot)
        {
            if (views.Length == 0)
                return;
            var live = new Dictionary<Point3, BlockSample>();
            foreach (var block in snapshot)
                live[block.Position] = block;
            foreach (var view in views)
                view.Counts = CustomHealthBars.Build(view.Aligned, live);
        }

        private List<CustomBarSource> CustomBarSources(bool groups, string search)
        {
            if (grid == null)
                throw new InvalidOperationException("Sit in the ship you want to customize first.");
            var terminal = MyAPIGateway.TerminalActionsHelper?.GetTerminalSystemForGrid(grid);
            if (terminal == null)
                throw new InvalidOperationException("The ship terminal is still loading. Try again shortly.");
            var result = new List<CustomBarSource>();
            search = (search ?? "").Trim();
            if (groups)
            {
                var available = new List<IMyBlockGroup>();
                terminal.GetBlockGroups(available);
                foreach (var group in available)
                {
                    if (group.Name.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    var members = new List<IMyTerminalBlock>();
                    group.GetBlocks(members, block => block.CubeGrid == grid);
                    if (members.Count > 0)
                        result.Add(new CustomBarSource
                        {
                            Name = group.Name,
                            GroupName = group.Name,
                            Blocks = members.ToArray()
                        });
                }
            }
            else
            {
                var available = new List<IMyTerminalBlock>();
                terminal.GetBlocks(available);
                foreach (var block in available)
                {
                    if (block.CubeGrid != grid || block.CustomName.IndexOf(search, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    result.Add(new CustomBarSource
                    {
                        Name = block.CustomName + " (" + block.Position.X + ", " + block.Position.Y + ", " + block.Position.Z + ")",
                        Blocks = new[] { block }
                    });
                }
            }
            result.Sort((a, b) => StringComparer.OrdinalIgnoreCase.Compare(a.Name, b.Name));
            return result;
        }

        private BlockSample[] CustomBarMembers(CustomBarSource source)
        {
            var profile = SelectedProfile();
            Reference binding;
            if (profile.Blocks == null || !references.TryGetValue(grid, out binding))
                throw new InvalidOperationException("Capture or load a baseline before adding a custom bar.");
            if (recoveryPreview != null || previewBlocks != null)
                throw new InvalidOperationException("Finish or cancel the baseline preview first.");
            if (!string.IsNullOrEmpty(source.GroupName))
            {
                source = CustomBarSources(true, source.GroupName).Find(item => item.GroupName == source.GroupName);
                if (source == null)
                    throw new InvalidOperationException("Terminal group not found. This bar keeps its saved block list and continues updating damage and repairs.");
            }

            var selected = new List<BlockSample>();
            foreach (var block in source.Blocks)
            {
                if (block.CubeGrid != grid)
                    continue;
                var slim = grid.GetCubeBlock(block.Position);
                if (slim != null)
                    selected.Add(GridObservation.Read(slim));
            }
            var local = ProfileAlignment.Transform(selected.ToArray(), binding.Anchor, true);
            var baseline = new Dictionary<Point3, BlockSample>();
            foreach (var expected in profile.Blocks)
                baseline[expected.Position] = expected;
            var members = new List<BlockSample>();
            foreach (var block in local)
            {
                BlockSample expected;
                if (baseline.TryGetValue(block.Position, out expected) && CustomHealthBars.SameMember(expected, block))
                    members.Add(expected);
            }
            if (members.Count == 0)
                throw new InvalidOperationException("No matching baseline blocks in this selection. Choose another block or update the baseline.");
            return members.ToArray();
        }

        private string SaveCustomBar(CustomHealthBar bar)
        {
            if (string.IsNullOrWhiteSpace(bar.Name))
                throw new InvalidOperationException("Enter a name for this bar.");
            var updated = SelectedProfile().Copy();
            int index = updated.CustomBars.FindIndex(item => item.Id == bar.Id);
            if (index < 0)
                updated.CustomBars.Add(bar.Copy());
            else
                updated.CustomBars[index] = bar.Copy();
            profileStore.Put(updated);
            RefreshReference("Custom bar saved; updating its health.");
            return "Custom bar saved for this ship and baseline version.";
        }

        private string DeleteCustomBar(string id)
        {
            var updated = SelectedProfile().Copy();
            updated.CustomBars.RemoveAll(bar => bar.Id == id);
            profileStore.Put(updated);
            customBarViews = Array.FindAll(customBarViews, view => view.Bar.Id != id);
            RefreshReference("Custom bar deleted.");
            return "Custom bar deleted. The ship baseline is unchanged.";
        }

        private string RefreshCustomGroup(CustomHealthBar bar)
        {
            if (string.IsNullOrEmpty(bar.GroupName))
                return "Single-block bars update damage and repairs automatically. Create another bar to track a different block.";
            var source = CustomBarSources(true, bar.GroupName).Find(item => item.GroupName == bar.GroupName);
            if (source == null)
                return "Terminal group not found. This bar keeps its saved block list and continues updating damage and repairs.";
            var members = CustomBarMembers(source);
            SaveCustomBar(CustomHealthBars.Refresh(bar, members));
            return "Tracked blocks updated from the terminal group. Damage and repairs continue to update automatically.";
        }

        private void DrawCustomHealthBars(Vector2 screen)
        {
            foreach (var view in customBarViews)
            {
                var preview = panel?.CustomBarPreview;
                var bar = preview != null && preview.Id == view.Bar.Id ? preview : view.Bar;
                var layout = bar.Layout;
                if (!layout.Enabled)
                    continue;
                float width = (float)(layout.Width * screen.X);
                float height = (float)(layout.Height * screen.Y);
                float labelHeight = layout.Label ? 24f : 0;
                float x = 8 + (screen.X - width - 16) * (float)layout.X;
                float y = 8 + (screen.Y - height - labelHeight - 16) * (float)layout.Y;
                int total = view.Bar.Members.Length;
                if (layout.Label)
                    MyGuiManager.DrawString(
                        "White",
                        bar.Name + ": " + view.Counts[0] + "/" + total + " " + UiText.Get(controls.Language, "healthy blocks"),
                        new Vector2(x / screen.X, y / screen.Y),
                        .45f,
                        new Color(255, 255, 255, (byte)(255 * layout.Opacity)),
                        useFullClientArea: true,
                        maxTextWidth: width / screen.X
                    );
                y += labelHeight;
                var background = new RectangleF(x - 2, y - 2, width + 4, height + 4);
                MyRenderProxy.DrawSprite(Texture, ref background, null, new Color(12, 22, 32, (byte)(210 * layout.Opacity)), 0, false, false);
                float offset = 0;
                for (int state = 0; state < view.Counts.Length; state++)
                {
                    if (view.Counts[state] == 0)
                        continue;
                    float portion = (float)view.Counts[state] / total;
                    var segment = new RectangleF(x + width * offset, y, width * portion, height);
                    uint rgb = controls.Appearance.HealthBarColor(state);
                    var color = new Color((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, (byte)(255 * layout.Opacity));
                    MyRenderProxy.DrawSprite(Texture, ref segment, null, color, 0, false, false);
                    offset += portion;
                }
            }
        }
    }
}
