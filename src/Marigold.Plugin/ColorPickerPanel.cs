using System;
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
                Intro(T(target) + ". " + T("Choose a color family with the hue strip, then adjust brightness and intensity in the square."));
                var host = new MyGuiControlParent(size: new Vector2(.60f, .34f));
                var screen = MyGuiManager.GetScreenSizeFromNormalizedSize(Vector2.One);
                float height = .28f;
                float width = Math.Min(.28f, height * screen.Y / Math.Max(1, screen.X));
                float squareLeft = -.285f;
                float hueX = squareLeft + width + .04f;
                PickerLabel(host, squareLeft, -.155f, "Color intensity", width);
                PickerLabel(host, hueX - .018f, -.155f, "Hue", .06f);
                saturationPicker = new ColorPickerControl(
                    new Vector2(squareLeft + width / 2, .015f), new Vector2(width, height),
                    colorSelection, false, UpdatePickerHex
                );
                saturationPicker.SetToolTip(T("Left is gray; right is vivid. Top is bright; bottom is dark."));
                huePicker = new ColorPickerControl(
                    new Vector2(hueX, .015f), new Vector2(.025f, height),
                    colorSelection, true, UpdatePickerHex
                );
                huePicker.SetToolTip(T("Drag to choose a color family."));
                host.Controls.Add(saturationPicker);
                host.Controls.Add(huePicker);
                PickerLabel(host, .065f, -.13f, "Selected color", .22f);
                host.Controls.Add(new ColorSwatchControl(
                    new Vector2(.175f, -.075f), new Vector2(.22f, .06f), () => colorSelection.Rgb
                ));
                PickerLabel(host, .065f, .015f, "Original color", .22f);
                host.Controls.Add(new ColorSwatchControl(
                    new Vector2(.175f, .075f), new Vector2(.22f, .06f), () => originalPickerColor
                ));
                AddContent(host, .34f);
                ActionRow("Use color", ApplyPickerColor, "Cancel", Back);
                Heading("Exact RGB");
                pickerHex = TextField("RGB color (six hexadecimal digits)", colorSelection.Hex, 6);
                pickerHex.EnterPressed += box => ReadPickerHex();
                ActionControl("Set RGB", ReadPickerHex);
                Note(pickingBarColor && owner.controls.Appearance.MatchHealthyBarColor
                    ? "Choosing a bar color turns off outline matching."
                    : "Use color applies now. Save display keeps your choice.");
            }

            private void PickerLabel(MyGuiControlParent host, float x, float y, string text, float width)
            {
                host.Controls.Add(new MyGuiControlLabel(
                    new Vector2(x, y), text: T(text), textScale: .62f, font: "White",
                    maxWidth: width, isAutoEllipsisEnabled: true
                ));
            }

            private void ApplyPickerColor()
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
