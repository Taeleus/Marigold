using System;
using System.Globalization;
using System.IO;

namespace Marigold
{
    internal sealed class HudAppearance
    {
        public double X = .98, Y = .12, Size = .32, Background = .75, Outline = 1, Fill = 1, Pulse = 1;
        public bool NewDamagePulse = true;
        public int Rendering; // 0 full, 1 outlines, 2 faults with fills, 3 fault outlines

        public bool DrawFills
        {
            get
            {
                return (Rendering & 1) == 0;
            }
        }

        public bool DrawHull
        {
            get
            {
                return (Rendering & 2) == 0;
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
                    Rendering.ToString(CultureInfo.InvariantCulture)
                }
            );
        }

        public static HudAppearance Parse(string text)
        {
            var a = text.Split(';');
            if (a.Length != 8 && a.Length != 9 && a.Length != 10)
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
            int rendering = 0;
            if (a.Length == 10 && (!int.TryParse(a[9], out rendering) || rendering < 0 || rendering > 3))
                throw new InvalidDataException("Invalid rendering mode.");
            return new HudAppearance
            {
                Rendering = rendering,
                NewDamagePulse = fresh,
                X = n[0],
                Y = n[1],
                Size = n[2],
                Background = n[3],
                Outline = n[4],
                Fill = n[5],
                Pulse = n[6],
                Colors = a[7]
            };
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
                "colors"
            }, field);
            if (index < 0)
                throw new InvalidOperationException("Unknown appearance setting.");
            a[index] = value;
            var parsed = Parse(string.Join(";", a));
            X = parsed.X;
            Y = parsed.Y;
            Size = parsed.Size;
            Background = parsed.Background;
            Outline = parsed.Outline;
            Fill = parsed.Fill;
            Pulse = parsed.Pulse;
            Colors = parsed.Colors;
            NewDamagePulse = parsed.NewDamagePulse;
            Rendering = parsed.Rendering;
        }
    }
}
