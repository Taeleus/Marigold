using System;
using System.Globalization;
using System.IO;

namespace Marigold
{
    internal sealed class HudAppearance
    {
        public const int SdWireframe = 0;
        public const int Outlines = 1;
        public const int FaultsWithFills = 2;
        public const int FaultOutlines = 3;
        public const int HdWireframe = 4;
        public const int Textured = 5;
        public const double MinBackgroundSize = .05, MaxBackgroundSize = .95;

        private const double DefaultShipOpacity = .7;
        private const double DefaultExpandedX = .5;
        private const double DefaultSize = .32;
        public double X = .98, Y = .12, Size = DefaultSize, Background = .75, Outline = 1, Fill = 1, Pulse = 1;
        public double BackgroundWidth = DefaultSize, BackgroundHeight = DefaultSize;
        public double ExpandedX = DefaultExpandedX;
        public bool NewDamagePulse = true;
        public int Rendering = HdWireframe;
        public double ContextOpacity = .15;
        public string HealthyBarColor = "3296FF";
        public bool MatchHealthyBarColor;
        public double ShipOpacity = DefaultShipOpacity;
        public bool ShowShipLabel = true;

        public bool DrawFills
        {
            get
            {
                return Rendering != Outlines && Rendering != FaultOutlines;
            }
        }

        public bool DrawHull
        {
            get
            {
                return Rendering != FaultsWithFills && Rendering != FaultOutlines;
            }
        }

        public string Colors = "50DCDC,FF3737,DCCD41,DC64FF,5AFF78,BEBEBE,FF8214";

        private static string N(double x)
        {
            return x.ToString("R", CultureInfo.InvariantCulture);
        }

        public string Encode()
        {
            return string.Join(
                ";",
                new[]
                {
                    N(X),
                    N(Y),
                    N(Size),
                    N(Background),
                    N(Outline),
                    N(Fill),
                    N(Pulse),
                    Colors,
                    NewDamagePulse.ToString(),
                    Rendering.ToString(CultureInfo.InvariantCulture),
                    N(ContextOpacity),
                    HealthyBarColor,
                    MatchHealthyBarColor.ToString(),
                    N(ShipOpacity),
                    N(ExpandedX),
                    ShowShipLabel.ToString(),
                    N(BackgroundWidth),
                    N(BackgroundHeight)
                }
            );
        }

        public static HudAppearance Parse(string text)
        {
            var a = text.Split(';');
            if (a.Length != 8 && a.Length != 9 && a.Length != 10 && a.Length != 13 && a.Length != 14 && a.Length != 15
                && a.Length != 16 && a.Length != 18)
                throw new InvalidDataException("Invalid HUD appearance.");
            var n = new double[7];
            for (int i = 0; i < 7; i++)
                if (!double.TryParse(a[i], NumberStyles.Float, CultureInfo.InvariantCulture, out n[i])
                    || double.IsNaN(n[i])
                    || double.IsInfinity(n[i])
                    || n[i] < 0
                    || n[i] > (i == 2 ? .7 : 1))
                    throw new InvalidDataException("HUD values outside range.");
            if (n[2] < .12)
                throw new InvalidDataException("HUD size must be 0.12 to 0.7.");
            var colors = a[7].Split(',');
            if (colors.Length != 7)
                throw new InvalidDataException("Expected seven RGB colors.");
            foreach (var c in colors)
            {
                uint color;
                if (c.Length != 6 || !uint.TryParse(c, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out color))
                    throw new InvalidDataException("Use six hex digits for colors.");
            }

            bool fresh = true;
            if (a.Length >= 9 && !bool.TryParse(a[8], out fresh))
                throw new InvalidDataException("Invalid damage pulse toggle.");
            int rendering = HdWireframe;
            // Earlier dev layouts stored a separate textured toggle before model opacity.
            bool texturedShip = false;
            bool legacyStyle = a.Length == 15 && bool.TryParse(a[13], out texturedShip);
            bool unifiedRendering = a.Length >= 14 && !legacyStyle;
            if (a.Length >= 10 && (!int.TryParse(a[9], NumberStyles.Integer, CultureInfo.InvariantCulture, out rendering)
                || rendering < SdWireframe || rendering > (unifiedRendering ? Textured : FaultOutlines)))
                throw new InvalidDataException("Invalid rendering mode.");
            if (!unifiedRendering && rendering == SdWireframe)
                rendering = HdWireframe; // The earlier default Full display moves to HD Wireframe.
            if (rendering == FaultsWithFills)
                rendering = HdWireframe;
            else if (rendering == FaultOutlines)
                rendering = Outlines;
            double context = .15;
            string healthy = "3296FF";
            bool matchHealthy = false;
            if (a.Length >= 13)
            {
                uint color;
                if (!double.TryParse(a[10], NumberStyles.Float, CultureInfo.InvariantCulture, out context)
                    || double.IsNaN(context) || double.IsInfinity(context) || context < 0 || context > 1)
                    throw new InvalidDataException("Other systems opacity must be between 0 and 1.");
                healthy = a[11];
                if (healthy.Length != 6 || !uint.TryParse(healthy, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out color))
                    throw new InvalidDataException("Use six hex digits for the healthy bar color.");
                if (!bool.TryParse(a[12], out matchHealthy))
                    throw new InvalidDataException("Invalid healthy bar color link.");
            }
            double shipOpacity = DefaultShipOpacity;
            if (texturedShip)
                rendering = Textured;
            if (a.Length >= 14 && (!double.TryParse(a[legacyStyle ? 14 : 13], NumberStyles.Float, CultureInfo.InvariantCulture, out shipOpacity)
                || double.IsNaN(shipOpacity) || double.IsInfinity(shipOpacity) || shipOpacity < 0 || shipOpacity > 1))
                throw new InvalidDataException("Textured ship opacity must be between 0 and 1.");
            double expandedX = DefaultExpandedX;
            if (a.Length >= 15 && !legacyStyle
                && (!double.TryParse(a[14], NumberStyles.Float, CultureInfo.InvariantCulture, out expandedX)
                    || double.IsNaN(expandedX) || double.IsInfinity(expandedX) || expandedX < 0 || expandedX > 1))
                throw new InvalidDataException("Expanded horizontal position must be between 0 and 1.");
            bool showShipLabel = true;
            if (a.Length >= 16 && !bool.TryParse(a[15], out showShipLabel))
                throw new InvalidDataException("Invalid ship label visibility.");
            return new HudAppearance
            {
                ContextOpacity = context,
                HealthyBarColor = healthy,
                MatchHealthyBarColor = matchHealthy,
                ShipOpacity = shipOpacity,
                Rendering = rendering,
                NewDamagePulse = fresh,
                X = n[0],
                ExpandedX = expandedX,
                ShowShipLabel = showShipLabel,
                Y = n[1],
                Size = n[2],
                BackgroundWidth = a.Length == 18 ? ParseBackgroundSize(a[16]) : n[2],
                BackgroundHeight = a.Length == 18 ? ParseBackgroundSize(a[17]) : n[2],
                Background = n[3],
                Outline = n[4],
                Fill = n[5],
                Pulse = n[6],
                Colors = a[7]
            };
        }

        private static double ParseBackgroundSize(string text)
        {
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double size)
                || !(size >= MinBackgroundSize && size <= MaxBackgroundSize))
                throw new InvalidDataException("Background width and height must be 0.05 to 0.95.");
            return size;
        }

        private string cachedColors;
        private uint[] rgb;

        public uint Color(int kind)
        {
            if (cachedColors != Colors)
            {
                var values = Colors.Split(',');
                rgb = new uint[7];
                for (int i = 0; i < 7; i++)
                    rgb[i] = uint.Parse(values[i], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                cachedColors = Colors;
            }

            return rgb[kind];
        }

        public uint HealthBarColor(int state)
        {
            if (state == 0)
                return MatchHealthyBarColor
                    ? Color((int)ChangeKind.Hull)
                    : uint.Parse(HealthyBarColor, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            var kind = state == 1 ? ChangeKind.Damaged
                : state == 2 ? ChangeKind.NonFunctional
                : state == 3 ? ChangeKind.Missing : ChangeKind.Unknown;
            return Color((int)kind);
        }

        public void Set(string field, string value)
        {
            var a = Encode().Split(';');
            int index = Array.IndexOf(new[]
            {
                "x",
                "y",
                "size",
                "background",
                "outline",
                "fill",
                "pulse",
                "colors",
                "fresh",
                "rendering",
                "context",
                "healthy",
                "matchhealthy",
                "shipopacity",
                "expandedx"
            }, field);
            if (index < 0)
                throw new InvalidOperationException("Unknown appearance setting.");
            a[index] = value;
            var parsed = Parse(string.Join(";", a));
            X = parsed.X;
            ExpandedX = parsed.ExpandedX;
            Y = parsed.Y;
            Size = parsed.Size;
            BackgroundWidth = parsed.BackgroundWidth;
            BackgroundHeight = parsed.BackgroundHeight;
            Background = parsed.Background;
            Outline = parsed.Outline;
            Fill = parsed.Fill;
            Pulse = parsed.Pulse;
            Colors = parsed.Colors;
            NewDamagePulse = parsed.NewDamagePulse;
            Rendering = parsed.Rendering;
            ContextOpacity = parsed.ContextOpacity;
            HealthyBarColor = parsed.HealthyBarColor;
            MatchHealthyBarColor = parsed.MatchHealthyBarColor;
            ShipOpacity = parsed.ShipOpacity;
            ShowShipLabel = parsed.ShowShipLabel;
        }
    }
}
