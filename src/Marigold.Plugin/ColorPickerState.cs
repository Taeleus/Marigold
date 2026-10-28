using System;
using System.Globalization;

namespace Marigold
{
    // The square uses saturation left-to-right and brightness top-to-bottom.
    // Hue is separate so gray/black selections do not lose the user's chosen color family.
    internal sealed class ColorPickerState
    {
        public double Hue { get; private set; }
        public double Saturation { get; private set; }
        public double Value { get; private set; }

        public uint Rgb
        {
            get { return ToRgb(Hue, Saturation, Value); }
        }

        public string Hex
        {
            get { return Rgb.ToString("X6", CultureInfo.InvariantCulture); }
        }

        public ColorPickerState(uint rgb)
        {
            SetRgb(rgb);
        }

        public void SelectSquare(double x, double y)
        {
            RequireFinite(x, "x");
            RequireFinite(y, "y");
            Saturation = Clamp(x);
            Value = 1 - Clamp(y);
        }

        public void SelectHue(double hue)
        {
            RequireFinite(hue, "hue");
            Hue = Clamp(hue);
        }

        public bool TrySetHex(string text)
        {
            if (text == null)
                return false;
            text = text.Trim();
            uint rgb;
            if (text.Length != 6
                || !uint.TryParse(text, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out rgb))
                return false;
            SetRgb(rgb);
            return true;
        }

        private void SetRgb(uint rgb)
        {
            if (rgb > 0xFFFFFF)
                throw new ArgumentOutOfRangeException("rgb", "Use a 24-bit RGB color.");
            double red = ((rgb >> 16) & 255) / 255.0;
            double green = ((rgb >> 8) & 255) / 255.0;
            double blue = (rgb & 255) / 255.0;
            double maximum = Math.Max(red, Math.Max(green, blue));
            double minimum = Math.Min(red, Math.Min(green, blue));
            double difference = maximum - minimum;
            Value = maximum;
            if (maximum == 0)
                return;
            Saturation = difference / maximum;
            if (difference == 0)
                return;
            if (maximum == red)
                Hue = ((green - blue) / difference + (green < blue ? 6 : 0)) / 6;
            else if (maximum == green)
                Hue = ((blue - red) / difference + 2) / 6;
            else
                Hue = ((red - green) / difference + 4) / 6;
        }

        public static uint ToRgb(double hue, double saturation, double value)
        {
            RequireFinite(hue, "hue");
            RequireFinite(saturation, "saturation");
            RequireFinite(value, "value");
            hue -= Math.Floor(hue);
            saturation = Clamp(saturation);
            value = Clamp(value);
            double chroma = value * saturation;
            double sector = hue * 6;
            double second = chroma * (1 - Math.Abs(sector % 2 - 1));
            double red, green, blue;
            if (sector < 1)
            {
                red = chroma;
                green = second;
                blue = 0;
            }
            else if (sector < 2)
            {
                red = second;
                green = chroma;
                blue = 0;
            }
            else if (sector < 3)
            {
                red = 0;
                green = chroma;
                blue = second;
            }
            else if (sector < 4)
            {
                red = 0;
                green = second;
                blue = chroma;
            }
            else if (sector < 5)
            {
                red = second;
                green = 0;
                blue = chroma;
            }
            else
            {
                red = chroma;
                green = 0;
                blue = second;
            }
            double offset = value - chroma;
            return (Channel(red + offset) << 16) | (Channel(green + offset) << 8) | Channel(blue + offset);
        }

        private static uint Channel(double value)
        {
            return (uint)Math.Round(Clamp(value) * 255, MidpointRounding.AwayFromZero);
        }

        private static double Clamp(double value)
        {
            return Math.Max(0, Math.Min(1, value));
        }

        private static void RequireFinite(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
                throw new ArgumentOutOfRangeException(name, "Color coordinates must be finite.");
        }
    }
}
