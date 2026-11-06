using System;
using Marigold;
using VRageMath;

internal static class HudLayoutChecks
{
    private static void Check(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static bool Near(double a, double b)
    {
        return Math.Abs(a - b) < .001;
    }

    public static void Run()
    {
        foreach (var screen in new[] { new Vector2(1920, 1080), new Vector2(3440, 1440) })
        foreach (bool expanded in new[] { false, true })
        {
            var appearance = new HudAppearance { Y = 1, ShowShipLabel = false };
            var square = HudLayoutGeometry.ShipLayout(screen, appearance, expanded);
            Check(Near(square.Bounds.Y, screen.Y - square.Model.Height), "Square layouts retain their bottom position");

            appearance.BackgroundWidth = .17;
            appearance.BackgroundHeight = .10;
            var shortPanel = HudLayoutGeometry.ShipLayout(screen, appearance, expanded);
            Check(shortPanel.Bounds.Width < square.Bounds.Width && shortPanel.Bounds.Height < square.Bounds.Height,
                "Selection bounds follow both background dimensions");
            Check(Near(shortPanel.Background.Bottom, screen.Y) && shortPanel.Model.Y > square.Model.Y,
                "A short background reaches the bottom and moves the ship down");
            Check(Near(shortPanel.Model.Width, square.Model.Width), "The ship retains its projection scale");

            var position = new Vector2(120, 560);
            HudLayoutGeometry.MoveShip(screen, appearance, expanded, position);
            var moved = HudLayoutGeometry.Ship(screen, appearance, expanded);
            Check(Near(moved.X, position.X) && Near(moved.Y, position.Y), "Dragging uses the same bounds as selection");

            appearance.Y = 1;
            appearance.ShowShipLabel = true;
            var labeled = HudLayoutGeometry.ShipLayout(screen, appearance, expanded);
            Check(Near(labeled.Background.Bottom, screen.Y - 49) && labeled.Model.Bottom > labeled.Background.Bottom,
                "The background reserves space for its caption even when the model viewport extends below it");
        }
        Console.WriteLine("PASS: 24 HUD layout checks for background anchoring, drag and captions.");
    }
}

namespace VRageMath
{
    internal readonly struct Vector2
    {
        public readonly float X, Y;

        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }
    }

    internal readonly struct RectangleF
    {
        public readonly float X, Y, Width, Height;
        public float Bottom => Y + Height;

        public RectangleF(float x, float y, float width, float height)
        {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }
    }
}
