using System;
using System.Globalization;
using Sandbox.Graphics.GUI;

namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private sealed partial class Panel
        {
            private void HealthBars()
            {
                Intro("Choose a system bar, then decide what it shows and where it sits. Damage and repairs update automatically.");
                ActionRow("Custom bars", () => Switch(20), "Bar colors", () => Switch(25));
                Heading("System bars");
                ChoiceField("Health bar", ShipHealth.Names, healthBarSelected, i =>
                {
                    healthBarSelected = i;
                    RecreateControls(false);
                });
                var layout = owner.controls.HealthBars.Bars[healthBarSelected];
                CheckField("Show this bar", layout.Enabled, value => layout.Enabled = value);
                CheckField("Show its label", layout.Label, value => layout.Label = value);
                if (healthBarSelected == 2)
                    ActionControl("Choose Power sections", () => Switch(23));

                var applyLayout = BarLayoutFields(layout);
                PrimaryAction("Apply bar layout", () =>
                {
                    applyLayout();
                    Status("Bar updated. Use Save display to keep it.");
                });
                Heading("Move in small steps");
                ActionRow("Left", () => MoveBar(layout, -.02, 0), "Right", () => MoveBar(layout, .02, 0));
                ActionRow("Up", () => MoveBar(layout, 0, -.02), "Down", () => MoveBar(layout, 0, .02));
                Heading("Reset and visibility");
                ActionRow("Reset this bar", () =>
                {
                    owner.controls.HealthBars.Bars[healthBarSelected] = new HealthBarSettings().Bars[healthBarSelected];
                    RecreateControls(false);
                }, "Hide system bars", () =>
                {
                    foreach (var bar in owner.controls.HealthBars.Bars)
                        bar.Enabled = false;
                    RecreateControls(false);
                });
                Note("Use Save display to keep system-bar settings. Custom bars use Save bar instead.");
            }

            private Action BarLayoutFields(HealthBarLayout layout)
            {
                Heading("Position and size");
                Note("Values are a share of the screen: 0 is the left or top edge, and 1 is the right or bottom edge.");
                var x = TextField("Horizontal position (0 to 1)", BarNumber(layout.X), 24);
                var y = TextField("Vertical position (0 to 1)", BarNumber(layout.Y), 24);
                var width = TextField("Width (0.05 to 0.8)", BarNumber(layout.Width), 24);
                var height = TextField("Height (0.005 to 0.08)", BarNumber(layout.Height), 24);
                var opacity = TextField("Opacity (0 transparent, 1 solid)", BarNumber(layout.Opacity), 24);
                return () =>
                {
                    // Validate the whole form before changing the live layout or draft.
                    var updated = HealthBarLayout.Parse(layout.Encode());
                    updated.Set("x", x.Text);
                    updated.Set("y", y.Text);
                    updated.Set("width", width.Text);
                    updated.Set("height", height.Text);
                    updated.Set("opacity", opacity.Text);
                    layout.X = updated.X;
                    layout.Y = updated.Y;
                    layout.Width = updated.Width;
                    layout.Height = updated.Height;
                    layout.Opacity = updated.Opacity;
                };
            }

            private static string BarNumber(double value)
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }

            private void MoveBar(HealthBarLayout layout, double x, double y)
            {
                layout.X = Math.Max(0, Math.Min(1, layout.X + x));
                layout.Y = Math.Max(0, Math.Min(1, layout.Y + y));
                RecreateControls(false);
            }

            private void Colors()
            {
                Intro("Choose which block state to edit. The visual picker lets you compare a new color with the current one before applying it.");
                var state = ChoiceField("Block state", ColorStateNames, colorStateIndex);
                state.ItemSelected += () =>
                {
                    colorStateIndex = (int)state.GetSelectedKey();
                    RecreateControls(false);
                };
                PrimaryAction("Choose color visually...", () => OpenColorPicker(colorStateIndex));
                Heading("Exact color");
                var value = TextField("RGB color (six hexadecimal digits)",
                    owner.controls.Appearance.Colors.Split(',')[colorStateIndex], 6);
                ActionControl("Apply RGB color", () =>
                {
                    var selection = new ColorPickerState(owner.controls.Appearance.Color(colorStateIndex));
                    if (!selection.TrySetHex(value.Text))
                        throw new InvalidOperationException("Use six hexadecimal RGB digits, for example 3296FF.");
                    var colors = owner.controls.Appearance.Colors.Split(',');
                    colors[colorStateIndex] = selection.Hex;
                    owner.controls.Appearance.Set("colors", string.Join(",", colors));
                    Status("Color updated. Save display to keep it.");
                });
                Heading("Related settings");
                ActionRow("Health bar colors", () => Switch(25), "Damage pulse settings", () => Switch(19));
                Note("Orange/red alerts pulse for four seconds. Yellow stays steady.");
                ActionControl("Restore default state colors", () =>
                {
                    owner.controls.Appearance.Colors = new HudAppearance().Colors;
                    RecreateControls(false);
                });
                Note("Use Save display to keep your colors.");
            }

            private void BarColors()
            {
                var appearance = owner.controls.Appearance;
                Intro("Choose the healthy color used by all system and custom health bars. Damage segments follow the block-state colors.");
                var match = CheckField("Match healthy schematic outline", appearance.MatchHealthyBarColor, value =>
                {
                    appearance.MatchHealthyBarColor = value;
                    RecreateControls(false);
                });
                PrimaryAction("Choose color visually...", () => OpenColorPicker(0, true));
                Note("Choosing an independent bar color turns off outline matching.");
                Heading("Exact color");
                var rgb = TextField("Healthy color (six hexadecimal RGB digits)", appearance.HealthyBarColor, 6);
                rgb.Enabled = !match.IsChecked;
                var apply = ActionControl("Apply RGB color", () =>
                {
                    var selection = new ColorPickerState(appearance.HealthBarColor(0));
                    if (!selection.TrySetHex(rgb.Text))
                        throw new InvalidOperationException("Use six hexadecimal RGB digits, for example 3296FF.");
                    appearance.HealthyBarColor = selection.Hex;
                    Status("Color updated. Save display to keep it.");
                });
                apply.Enabled = !match.IsChecked;
                Heading("Related settings");
                ActionRow("Block-state colors", () => Switch(16), "Restore healthy blue", () =>
                {
                    appearance.HealthyBarColor = "3296FF";
                    appearance.MatchHealthyBarColor = false;
                    RecreateControls(false);
                    Status("Color updated. Save display to keep it.");
                });
                Note("Use Save display to keep your colors.");
            }
        }
    }
}
