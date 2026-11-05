using System;
using VRageMath;

namespace Marigold
{
    // Use the same pixel bounds for drawing, hit testing, and dragging.
    internal static class HudLayoutGeometry
    {
        private const float Inset = 8;
        private const float CompactHorizontalInsets = 24;
        private const float ExpandedHorizontalInsets = 16;
        private const float ShipCaptionInsets = 65;
        public const float BarLabelHeight = 24;
        private const float PowerLabelHeight = 18;

        public static RectangleF Ship(Vector2 screen, HudAppearance appearance, bool expanded)
        {
            float side = expanded
                ? Math.Min(screen.X * .6f, screen.Y * .7f * (float)(appearance.Size / .32))
                : Math.Min(screen.X * .6f, screen.Y * (float)appearance.Size);
            float verticalInsets = ShipVerticalInsets(appearance);
            side = Math.Min(side, Math.Max(1, screen.Y - verticalInsets));
            float horizontalInsets = expanded ? ExpandedHorizontalInsets : CompactHorizontalInsets;
            double x = expanded ? appearance.ExpandedX : appearance.X;
            return new RectangleF(
                Inset + Travel(screen.X, side, horizontalInsets) * (float)x,
                Inset + Travel(screen.Y, side, verticalInsets) * (float)appearance.Y,
                side, side
            );
        }

        public static RectangleF Bar(Vector2 screen, HealthBarLayout layout, bool split = false)
        {
            float width = (float)(layout.Width * screen.X);
            float height = (float)(layout.Height * screen.Y) + LabelHeight(layout, split);
            return new RectangleF(
                Inset + Travel(screen.X, width, 2 * Inset) * (float)layout.X,
                Inset + Travel(screen.Y, height, 2 * Inset) * (float)layout.Y,
                width, height
            );
        }

        public static void MoveShip(Vector2 screen, HudAppearance appearance, bool expanded, Vector2 position)
        {
            var bounds = Ship(screen, appearance, expanded);
            if (expanded)
                appearance.ExpandedX = Anchor(position.X, Travel(screen.X, bounds.Width, ExpandedHorizontalInsets), appearance.ExpandedX);
            else
                appearance.X = Anchor(position.X, Travel(screen.X, bounds.Width, CompactHorizontalInsets), appearance.X);
            appearance.Y = Anchor(position.Y, Travel(screen.Y, bounds.Height, ShipVerticalInsets(appearance)), appearance.Y);
        }

        private static float ShipVerticalInsets(HudAppearance appearance)
        {
            return appearance.ShowShipLabel ? ShipCaptionInsets : Inset;
        }

        public static void MoveBar(Vector2 screen, HealthBarLayout layout, bool split, Vector2 position)
        {
            var bounds = Bar(screen, layout, split);
            layout.X = Anchor(position.X, Travel(screen.X, bounds.Width, 2 * Inset), layout.X);
            layout.Y = Anchor(position.Y, Travel(screen.Y, bounds.Height, 2 * Inset), layout.Y);
        }

        public static void ResizeBar(Vector2 screen, HealthBarLayout layout, bool split, Vector2 position, Vector2 bodySize)
        {
            if (screen.X <= 0 || screen.Y <= 0)
                return;

            double availableWidth = (screen.X - Math.Max(Inset, position.X) - Inset) / screen.X;
            double availableHeight = (screen.Y - Math.Max(Inset, position.Y) - Inset - LabelHeight(layout, split)) / screen.Y;
            layout.Width = Math.Max(HealthBarLayout.MinWidth, Math.Min(bodySize.X / (double)screen.X,
                Math.Min(HealthBarLayout.MaxWidth, availableWidth)));
            layout.Height = Math.Max(HealthBarLayout.MinHeight, Math.Min(bodySize.Y / (double)screen.Y,
                Math.Min(HealthBarLayout.MaxHeight, availableHeight)));
            MoveBar(screen, layout, split, position);
        }

        private static float LabelHeight(HealthBarLayout layout, bool split)
        {
            return layout.Label ? BarLabelHeight + (split ? PowerLabelHeight : 0) : 0;
        }

        private static float Travel(float extent, float size, float insets)
        {
            return Math.Max(0, extent - size - insets);
        }

        private static double Anchor(float position, float travel, double current)
        {
            return travel > 0 ? Math.Max(0d, Math.Min(1d, (position - Inset) / travel)) : current;
        }
    }
}
