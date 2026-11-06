using System;
using VRageMath;

namespace Marigold
{
    // Use the same pixel bounds for drawing, hit testing, and dragging.
    internal static class HudLayoutGeometry
    {
        internal readonly struct ShipHudLayout
        {
            public readonly RectangleF Model, Background, Bounds;

            public ShipHudLayout(RectangleF model, RectangleF background, RectangleF bounds)
            {
                Model = model;
                Background = background;
                Bounds = bounds;
            }
        }

        private const float Inset = 8;
        private const float CompactHorizontalInsets = 24;
        private const float ExpandedHorizontalInsets = 16;
        private const float ShipCaptionInsets = 65;
        public const float BarLabelHeight = 24;
        private const float PowerLabelHeight = 18;

        public static RectangleF Ship(Vector2 screen, HudAppearance appearance, bool expanded)
        {
            return ShipLayout(screen, appearance, expanded).Bounds;
        }

        public static ShipHudLayout ShipLayout(Vector2 screen, HudAppearance appearance, bool expanded)
        {
            float verticalInsets = ShipVerticalInsets(appearance);
            float horizontalInsets = expanded ? ExpandedHorizontalInsets : CompactHorizontalInsets;
            float limit = Math.Min(screen.X * .6f, Math.Max(1, screen.Y - verticalInsets));
            float scale = expanded ? screen.Y * .7f / .32f : screen.Y;
            float side = Math.Min(limit, scale * (float)appearance.Size);
            float backgroundWidth = Math.Min(limit, scale * (float)appearance.BackgroundWidth);
            float backgroundHeight = Math.Min(limit, scale * (float)appearance.BackgroundHeight);
            double x = expanded ? appearance.ExpandedX : appearance.X;
            // Anchor to the visible panel so unused space in the square projection can overhang the screen.
            float left = Inset + Travel(screen.X, backgroundWidth, horizontalInsets) * (float)x;
            float top = Inset + Travel(screen.Y, backgroundHeight, verticalInsets) * (float)appearance.Y;
            float centerX = left + backgroundWidth / 2, centerY = top + backgroundHeight / 2;
            var model = new RectangleF(centerX - side / 2, centerY - side / 2, side, side);
            float backgroundTop = top - Inset;
            var background = new RectangleF(left - Inset, backgroundTop,
                backgroundWidth + 2 * Inset, Math.Min(backgroundHeight + 2 * Inset, screen.Y - backgroundTop));
            return new ShipHudLayout(model, background, new RectangleF(left, top, backgroundWidth, backgroundHeight));
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

        public static float CaptionLeft(float screenWidth, float preferredLeft, float textWidth)
        {
            return Math.Max(0, Math.Min(preferredLeft, screenWidth - textWidth - 2));
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
