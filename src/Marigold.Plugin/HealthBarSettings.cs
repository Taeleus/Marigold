using System;
using System.Globalization;
using System.IO;

namespace Marigold
{
    internal sealed class HealthBarLayout
    {
        public bool Enabled, Label = true;
        public int PowerMask;
        public double X = .75, Y = .48, Width = .22, Height = .014, Opacity = 1;

        public string Encode()
        {
            return string.Join(
                ",",
                new[]
                {
                    Enabled.ToString(),
                    Label.ToString(),
                    N(X),
                    N(Y),
                    N(Width),
                    N(Height),
                    N(Opacity),
                    PowerMask.ToString(CultureInfo.InvariantCulture)
                }
            );
        }

        private static string N(double n)
        {
            return n.ToString("R", CultureInfo.InvariantCulture);
        }

        public static HealthBarLayout Parse(string text)
        {
            var a = text.Split(',');
            bool enabled, label;
            if ((a.Length != 7 && a.Length != 8) || !bool.TryParse(a[0], out enabled) || !bool.TryParse(a[1], out label))
                throw new InvalidDataException("Invalid health bar settings.");
            var n = new double[5];
            for (int i = 0; i < 5; i++)
                if (!double.TryParse(a[i + 2], NumberStyles.Float, CultureInfo.InvariantCulture, out n[i])
                    || double.IsNaN(n[i])
                    || double.IsInfinity(n[i])
                    || n[i] < 0
                    || n[i] > 1)
                    throw new InvalidDataException("Health bar values must be between 0 and 1.");
            if (n[2] < .05 || n[2] > .8 || n[3] < .005 || n[3] > .08)
                throw new InvalidDataException("Width: 0.05 to 0.8; height: 0.005 to 0.08.");
            int mask = 0;
            if (a.Length == 8 && (!int.TryParse(a[7], out mask) || mask < 0 || mask > 7))
                throw new InvalidDataException("Invalid power sections.");
            return new HealthBarLayout
            {
                PowerMask = mask,
                Enabled = enabled,
                Label = label,
                X = n[0],
                Y = n[1],
                Width = n[2],
                Height = n[3],
                Opacity = n[4]
            };
        }

        public void Set(string field, string value)
        {
            int i = Array.IndexOf(new[]
            {
                "x",
                "y",
                "width",
                "height",
                "opacity"
            }, field);
            if (i < 0)
                throw new InvalidDataException("Unknown bar setting.");
            var a = Encode().Split(',');
            a[i + 2] = value;
            var updated = Parse(string.Join(",", a));
            X = updated.X;
            Y = updated.Y;
            Width = updated.Width;
            Height = updated.Height;
            Opacity = updated.Opacity;
        }
    }

    internal sealed class HealthBarSettings
    {
        public readonly HealthBarLayout[] Bars = new HealthBarLayout[ShipHealth.Names.Length];

        public HealthBarSettings()
        {
            for (int i = 0; i < Bars.Length; i++)
                Bars[i] = new HealthBarLayout
                {
                    Y = .48 + i * .055
                };
        }

        public string Encode()
        {
            var a = new string[ShipHealth.Names.Length];
            for (int i = 0; i < Bars.Length; i++)
                a[i] = Bars[i].Encode();
            return string.Join(";", a);
        }

        public static HealthBarSettings Parse(string text)
        {
            var a = text.Split(';');
            if (a.Length != 6 && (a.Length != 7 && a.Length != 8) && a.Length != ShipHealth.Names.Length)
                throw new InvalidDataException("Invalid health bar layout count.");
            var result = new HealthBarSettings();
            for (int i = 0; i < a.Length; i++)
                result.Bars[i] = HealthBarLayout.Parse(a[i]);
            return result;
        }
    }
}
