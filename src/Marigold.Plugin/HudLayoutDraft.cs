using System;
using System.Collections.Generic;
using Sandbox.Game.Entities;

namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private sealed class HudLayoutDraft
        {
            public MyCubeGrid Grid;
            public Reference Binding;
            public HudAppearance Appearance;
            public HealthBarSettings Bars;
            public bool ShipVisible;
            public bool Expanded;
            public string ReferenceId;
            public List<CustomHealthBar> CustomBars;
        }

        private HudLayoutDraft hudLayoutPreview;
        private HudAppearance DrawingAppearance => hudLayoutPreview?.Appearance ?? controls.Appearance;
        private HealthBarSettings DrawingBars => hudLayoutPreview?.Bars ?? controls.HealthBars;

        private HudLayoutDraft CaptureHudLayout()
        {
            if (grid == null || grid.Closed || grid.MarkedForClose)
                throw new InvalidOperationException(UiText.Get(controls.Language, "Sit in a cockpit to arrange the HUD."));
            if (previewBlocks != null || recoveryPreview != null)
                throw new InvalidOperationException(UiText.Get(controls.Language, "Finish the baseline preview before arranging the HUD."));

            references.TryGetValue(grid, out var binding);
            var profile = binding == null ? null : profileStore.Find(binding.ProfileId);
            var customBars = new List<CustomHealthBar>();
            if (profile != null)
                foreach (var bar in profile.CustomBars)
                    customBars.Add(new CustomHealthBar
                    {
                        Id = bar.Id,
                        Name = bar.Name,
                        Layout = HealthBarLayout.Parse(bar.Layout.Encode())
                    });
            return new HudLayoutDraft
            {
                Grid = grid,
                Binding = binding,
                Appearance = HudAppearance.Parse(controls.Appearance.Encode()),
                Bars = HealthBarSettings.Parse(controls.HealthBars.Encode()),
                ShipVisible = controls.SchematicVisible,
                Expanded = controls.Expanded,
                ReferenceId = profile?.ReferenceId,
                CustomBars = customBars
            };
        }

        private bool ApplyHudLayout(HudLayoutDraft draft)
        {
            if (grid == null || !ReferenceEquals(grid, draft.Grid) || grid.Closed || grid.MarkedForClose)
                throw new InvalidOperationException(UiText.Get(controls.Language, "The controlled grid changed. Reopen Arrange HUD."));
            references.TryGetValue(grid, out var binding);
            if (!ReferenceEquals(binding, draft.Binding))
                throw new InvalidOperationException(UiText.Get(controls.Language, "The active baseline changed. Reopen Arrange HUD."));

            var appearance = HudAppearance.Parse(draft.Appearance.Encode());
            var bars = HealthBarSettings.Parse(draft.Bars.Encode());
            ShipProfile updated = null;
            if (draft.CustomBars.Count > 0)
            {
                var profile = profileStore.Find(binding.ProfileId);
                if (profile.ReferenceId != draft.ReferenceId)
                    throw new InvalidOperationException(UiText.Get(controls.Language, "The active baseline changed. Reopen Arrange HUD."));
                updated = CustomHealthBars.WithLayouts(profile, draft.CustomBars);
                if (updated != null)
                    profileStore.Put(updated);
            }
            controls.Appearance = appearance;
            controls.HealthBars = bars;
            controls.SchematicVisible = draft.ShipVisible;
            return updated != null;
        }

        private void DrawHudLayout(HudLayoutDraft draft)
        {
            if (!ReferenceEquals(grid, draft.Grid))
                return;
            // The live HUD draw reads the draft only for this pass. Saves and commands still see the active layout.
            hudLayoutPreview = draft;
            try
            {
                DrawSafely();
            }
            finally
            {
                hudLayoutPreview = null;
            }
        }
    }
}
