using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using VRageMath;

namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private sealed partial class Panel
        {
            private ColorPickerState colorSelection;
            private uint originalPickerColor;
            private int colorStateIndex;
            private bool pickingBarColor;
            private MyGuiControlTextbox pickerHex;
            private ColorPickerControl saturationPicker, huePicker;
            private static readonly string[] ColorStateNames =
            {
                "Healthy outline", "Missing", "Damaged", "Replaced", "Added", "Unknown", "Non-functional"
            };

            private void OpenColorPicker(int state, bool healthBar = false)
            {
                colorStateIndex = state;
                pickingBarColor = healthBar;
                originalPickerColor = healthBar
                    ? owner.controls.Appearance.HealthBarColor(0)
                    : owner.controls.Appearance.Color(state);
                colorSelection = new ColorPickerState(originalPickerColor);
                Switch(26);
            }

            private void ColorPicker()
            {
                string target = pickingBarColor ? "Healthy health bars" : ColorStateNames[colorStateIndex];
                Label(-.39f, -.26f, T(target) + " — " + T("Drag the color square and hue strip."));
                var screen = MyGuiManager.GetScreenSizeFromNormalizedSize(Vector2.One);
                float height = .30f;
                float width = height * screen.Y / screen.X;
                float left = -.18f - width / 2;
                FieldLabel(left, -.215f, "Saturation →", width);
                FieldLabel(.005f, -.215f, "Hue", .07f);
                FieldLabel(-.40f, -.175f, "Bright", .08f);
                FieldLabel(-.40f, .105f, "Dark", .08f);
                saturationPicker = new ColorPickerControl(
                    new Vector2(-.18f, -.03f), new Vector2(width, height), colorSelection, false, UpdatePickerHex
                );
                saturationPicker.SetToolTip(T("Left is gray; right is vivid. Top is bright; bottom is dark."));
                huePicker = new ColorPickerControl(
                    new Vector2(.035f, -.03f), new Vector2(.025f, height), colorSelection, true, UpdatePickerHex
                );
                huePicker.SetToolTip(T("Drag to choose a color family."));
                Controls.Add(saturationPicker);
                Controls.Add(huePicker);
                FieldLabel(.13f, -.205f, "Selected color", .25f);
                Controls.Add(new ColorSwatchControl(
                    new Vector2(.25f, -.15f), new Vector2(.24f, .05f), () => colorSelection.Rgb
                ));
                FieldLabel(.13f, -.10f, "RGB (hex)", .25f);
                pickerHex = new MyGuiControlTextbox(
                    new Vector2(.25f, -.06f), colorSelection.Hex, maxLength: 6, textScale: .65f
                )
                {
                    Size = new Vector2(.24f, .04f)
                };
                pickerHex.EnterPressed += box => ReadPickerHex();
                Controls.Add(pickerHex);
                Button(.25f, .015f, "Set RGB", ReadPickerHex, .24f);
                FieldLabel(.13f, .065f, "Original color", .25f);
                Controls.Add(new ColorSwatchControl(
                    new Vector2(.25f, .10f), new Vector2(.24f, .03f), () => originalPickerColor
                ));
                Label(-.39f, .145f, pickingBarColor && owner.controls.Appearance.MatchHealthyBarColor
                    ? "Choosing a bar color turns off outline matching."
                    : "Use color applies now. Save display keeps your choice.");
                Button(-.21f, .19f, "Use color", () =>
                {
                    if (!colorSelection.TrySetHex(pickerHex.Text))
                    {
                        Status("Use six hexadecimal RGB digits, for example 3296FF.");
                        return;
                    }
                    if (pickingBarColor)
                    {
                        owner.controls.Appearance.Set("healthy", colorSelection.Hex);
                        owner.controls.Appearance.MatchHealthyBarColor = false;
                    }
                    else
                    {
                        var colors = owner.controls.Appearance.Colors.Split(',');
                        colors[colorStateIndex] = colorSelection.Hex;
                        owner.controls.Appearance.Set("colors", string.Join(",", colors));
                    }
                    Back();
                    Status("Color updated. Save display to keep it.");
                }, .25f);
                Button(.21f, .19f, "Cancel", Back, .25f);
            }

            private void ReadPickerHex()
            {
                if (!colorSelection.TrySetHex(pickerHex.Text))
                {
                    Status("Use six hexadecimal RGB digits, for example 3296FF.");
                    return;
                }
                UpdatePickerHex();
            }

            private void UpdatePickerHex()
            {
                if (pickerHex != null)
                    pickerHex.Text = colorSelection.Hex;
            }
        }
    }
}
