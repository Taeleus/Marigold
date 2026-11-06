namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private sealed partial class Panel
        {
            private void HealthBars()
            {
                Intro("Choose which bars to show. Use Arrange HUD to move or resize them.");
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
                    PowerSections(layout);

                if (!advancedControls)
                    return;
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
            }

            private void PowerSections(HealthBarLayout layout)
            {
                Heading("Power bar sections");
                Note("Give selected power sources their own labeled section within the Power bar.");
                for (int i = 0; i < 3; i++)
                {
                    int bit = 1 << i;
                    CheckField(ShipHealth.PowerNames[i], (layout.PowerMask & bit) != 0, value =>
                    {
                        layout.PowerMask = value ? layout.PowerMask | bit : layout.PowerMask & ~bit;
                    });
                }
                Note("Unchecked sources stay in Other. Every baseline power block is still counted.");
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
                Heading("Related settings");
                ActionRow("Health bar colors", () => Switch(25), "Damage pulse settings", () => Switch(12));
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
                CheckField("Match healthy schematic outline", appearance.MatchHealthyBarColor, value =>
                {
                    appearance.MatchHealthyBarColor = value;
                    RecreateControls(false);
                });
                PrimaryAction("Choose color visually...", () => OpenColorPicker(0, true));
                Note("Choosing an independent bar color turns off outline matching.");
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
