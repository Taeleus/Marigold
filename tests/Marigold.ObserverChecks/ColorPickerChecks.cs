using System;
using Marigold;

internal static class ColorPickerChecks
{
    private static int checks;

    private static void Check(bool value, string message)
    {
        if (!value)
            throw new Exception(message);
        checks++;
    }

    private static bool Close(double actual, double expected)
    {
        return Math.Abs(actual - expected) < 1e-10;
    }

    private static void Reject(Action action, string message)
    {
        bool rejected = false;
        try
        {
            action();
        }
        catch (ArgumentOutOfRangeException)
        {
            rejected = true;
        }
        Check(rejected, message);
    }

    public static void Run()
    {
        var black = new ColorPickerState(0);
        Check(black.Hex == "000000" && black.Value == 0, "Black initializes without division by zero");
        var white = new ColorPickerState(0xFFFFFF);
        Check(white.Hex == "FFFFFF" && white.Saturation == 0 && white.Value == 1, "White initializes correctly");

        uint[] spectrum = { 0xFF0000, 0xFFFF00, 0x00FF00, 0x00FFFF, 0x0000FF, 0xFF00FF };
        for (int i = 0; i < spectrum.Length; i++)
        {
            double hue = i / 6.0;
            Check(ColorPickerState.ToRgb(hue, 1, 1) == spectrum[i], "Hue strip contains the six primary/secondary colors");
            var selected = new ColorPickerState(spectrum[i]);
            Check(Close(selected.Hue, hue) && selected.Saturation == 1 && selected.Value == 1, "RGB initializes HSV selection");
            selected.SelectSquare(0, 0);
            Check(selected.Rgb == 0xFFFFFF, "Square's upper-left corner is white");
            selected.SelectSquare(1, 0);
            Check(selected.Rgb == spectrum[i], "Square's upper-right corner is the selected hue");
            selected.SelectSquare(1, 1);
            Check(selected.Rgb == 0, "Square's lower-right corner is black");
            selected.SelectSquare(0, 1);
            Check(selected.Rgb == 0, "Square's lower-left corner is black");
        }

        var state = new ColorPickerState(0x0000FF);
        state.SelectSquare(.5, .5);
        Check(state.Rgb == 0x404080, "Square interpolates both saturation and brightness");
        Check(state.TrySetHex("aB12eF") && state.Hex == "AB12EF", "Hex editing accepts mixed case and displays uppercase");
        Check(state.TrySetHex("  FF8214  ") && state.Hex == "FF8214", "Hex editing permits surrounding whitespace");
        foreach (var invalid in new[] { null, "", " ", "12345", "1234567", "#123456", "ZZ1234", "-00001", "12 456", "0x1234" })
        {
            string before = state.Hex;
            double hue = state.Hue, saturation = state.Saturation, value = state.Value;
            Check(!state.TrySetHex(invalid), "Malformed RGB text is rejected");
            Check(state.Hex == before && state.Hue == hue && state.Saturation == saturation && state.Value == value,
                "Invalid text leaves the color and selection unchanged");
        }

        state.SelectHue(2.0 / 3);
        Check(state.TrySetHex("808080") && Close(state.Hue, 2.0 / 3) && state.Saturation == 0,
            "Gray preserves the selected hue while removing saturation");
        state.SelectSquare(.8, .2);
        double retainedHue = state.Hue, retainedSaturation = state.Saturation;
        Check(state.TrySetHex("000000") && state.Value == 0 && state.Hue == retainedHue
            && state.Saturation == retainedSaturation, "Black preserves hue and saturation for later brightness adjustment");
        state.SelectSquare(state.Saturation, 0);
        Check((state.Rgb & 255) == 255, "Lifting brightness restores the selected blue hue");

        state.SelectHue(-1);
        state.SelectSquare(5, -5);
        Check(state.Hue == 0 && state.Saturation == 1 && state.Value == 1 && state.Rgb == 0xFF0000,
            "Dragging beyond the square/strip clamps to their edges");
        state.SelectHue(2);
        Check(state.Hue == 1 && state.Rgb == 0xFF0000, "Both ends of the hue strip are red");
        state.SelectSquare(-5, 5);
        Check(state.Saturation == 0 && state.Value == 0, "Opposite square drag edges clamp safely");
        Check(ColorPickerState.ToRgb(1, 1, 1) == ColorPickerState.ToRgb(0, 1, 1), "Hue wraps after a full turn");
        Check(ColorPickerState.ToRgb(-.25, 1, 1) == ColorPickerState.ToRgb(.75, 1, 1), "Color sampling wraps negative hue");
        Check(ColorPickerState.ToRgb(3.25, 1, 1) == ColorPickerState.ToRgb(.25, 1, 1), "Color sampling wraps multiple hue turns");
        Check(ColorPickerState.ToRgb(0, 5, 5) == 0xFF0000, "Color sampling clamps saturation and brightness");

        foreach (double invalid in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            string before = state.Hex;
            double hue = state.Hue, saturation = state.Saturation, value = state.Value;
            Reject(() => state.SelectSquare(.5, invalid), "Nonfinite square Y is rejected");
            Reject(() => state.SelectSquare(invalid, .5), "Nonfinite square X is rejected");
            Reject(() => state.SelectHue(invalid), "Nonfinite hue dragging is rejected");
            Check(state.Hex == before && state.Hue == hue && state.Saturation == saturation && state.Value == value,
                "Invalid drag coordinates leave selection untouched");
            Reject(() => ColorPickerState.ToRgb(invalid, 1, 1), "Nonfinite sampled hue is rejected");
            Reject(() => ColorPickerState.ToRgb(0, invalid, 1), "Nonfinite sampled saturation is rejected");
            Reject(() => ColorPickerState.ToRgb(0, 1, invalid), "Nonfinite sampled brightness is rejected");
        }
        Reject(() => new ColorPickerState(0xFF000000), "An alpha channel is rejected by the RGB-only model");

        // A deterministic sweep checks arbitrary colors as well as the named spectrum above.
        uint seed = 0x12345678;
        for (int i = 0; i < 1024; i++)
        {
            seed = unchecked(seed * 1664525 + 1013904223);
            uint color = seed & 0xFFFFFF;
            uint result = new ColorPickerState(color).Rgb;
            Check(Math.Abs((int)(color >> 16) - (int)(result >> 16)) <= 1
                && Math.Abs((int)((color >> 8) & 255) - (int)((result >> 8) & 255)) <= 1
                && Math.Abs((int)(color & 255) - (int)(result & 255)) <= 1,
                "RGB/HSV conversion preserves each 8-bit channel");
        }
        Console.WriteLine("PASS: " + checks + " visual color-picker state checks.");
    }
}
