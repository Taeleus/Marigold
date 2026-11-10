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

            appearance.X = appearance.ExpandedX = 1;
            appearance.BackgroundWidth = .05;
            var rightPanel = HudLayoutGeometry.ShipLayout(screen, appearance, expanded).Background;
            float captionLeft = HudLayoutGeometry.CaptionLeft(screen.X, rightPanel.X, 320);
            Check(captionLeft < rightPanel.X && Near(captionLeft + 320 + 2, screen.X),
                "Long captions move left at the right edge");
            Check(Near(HudLayoutGeometry.CaptionLeft(screen.X, rightPanel.X, 20), rightPanel.X),
                "Short captions retain their background alignment");
        }
        var linked = new HudAppearance { Size = .3, BackgroundWidth = .4, BackgroundHeight = .2 };
        linked.ScaleBackgroundWithShip = false;
        linked.ResizeShip(.4);
        Check(linked.BackgroundWidth == .4 && linked.BackgroundHeight == .2, "Unlinked resizing changes only ship scale");
        linked.ScaleBackgroundWithShip = true;
        Check(linked.BackgroundWidth == .4 && linked.BackgroundHeight == .2, "Enabling the link keeps the current dimensions");
        linked.ResizeShip(.6);
        Check(Near(linked.BackgroundWidth, .6) && Near(linked.BackgroundHeight, .3), "Linked resizing keeps custom proportions");
        linked.BackgroundWidth = .8;
        linked.BackgroundHeight = .2;
        linked.Set("size", "0.7");
        Check(Near(linked.BackgroundWidth, .8 * .7 / .6) && Near(linked.BackgroundHeight, .2 * .7 / .6),
            "Manually edited dimensions are the basis for the next linked resize");
        linked.ResizeShip(.5);
        linked.BackgroundWidth = .85;
        linked.BackgroundHeight = .34;
        linked.ResizeShip(.7);
        Check(linked.Size == .7 && linked.BackgroundWidth <= HudAppearance.MaxBackgroundSize
            && Near(linked.BackgroundWidth / linked.BackgroundHeight, 2.5),
            "The background respects its size limit and aspect ratio while the ship can keep growing");
        Console.WriteLine("PASS: 37 HUD layout checks for background scaling, anchoring, drag and captions.");
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
