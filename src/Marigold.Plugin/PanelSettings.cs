using System;
using System.Collections.Generic;
using System.Globalization;
using Sandbox.Graphics.GUI;

namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private sealed partial class Panel
        {
            private void Appearance()
            {
                Intro("Arrange the ship schematic. Health bars have their own layout settings.");
                ActionRow("State colors", () => Switch(16), "Systems and filters", () => Switch(5));
                CheckField("Show ship schematic", owner.controls.SchematicVisible, visible =>
                {
                    owner.controls.SchematicVisible = visible;
                    Status("Updated. Save display to keep this setting.");
                });

                Heading("Position and size");
                var appearance = owner.controls.Appearance;
                var x = TextField("Horizontal position (0 = left, 1 = right)", Number(appearance.X), 24);
                var y = TextField("Vertical position (0 = top, 1 = bottom)", Number(appearance.Y), 24);
                var size = TextField("Size (0.12 to 0.7 of screen height)", Number(appearance.Size), 24);
                PrimaryAction("Apply position and size", () =>
                {
                    var updated = HudAppearance.Parse(owner.controls.Appearance.Encode());
                    updated.Set("x", x.Text);
                    updated.Set("y", y.Text);
                    updated.Set("size", size.Text);
                    owner.controls.Appearance = updated;
                    Status("Updated. Save display to keep this layout.");
                });

                Heading("Transparency");
                Note("Use 0 for invisible and 1 for fully visible.");
                var background = TextField("Background opacity (0 to 1)", Number(appearance.Background), 24);
                var outline = TextField("Outline opacity (0 to 1)", Number(appearance.Outline), 24);
                var fill = TextField("Damage fill opacity (0 to 1)", Number(appearance.Fill), 24);
                PrimaryAction("Apply transparency", () =>
                {
                    var updated = HudAppearance.Parse(owner.controls.Appearance.Encode());
                    updated.Set("background", background.Text);
                    updated.Set("outline", outline.Text);
                    updated.Set("fill", fill.Text);
                    owner.controls.Appearance = updated;
                    Status("Updated. Save display to keep this layout.");
                });

                Heading("View style");
                ChoiceField("Schematic layout", new[] { "Compact", "Expanded" }, owner.controls.Expanded ? 1 : 0,
                    i => Run("layout " + (i == 1 ? "expanded" : "compact")));
                var details = new[] { "low", "medium", "high" };
                ChoiceField("Outline detail", new[] { "Low", "Medium", "High" },
                    Math.Max(0, Array.IndexOf(details, owner.controls.DetailName)), i => Run("detail " + details[i]));
                Note("Use Save display to keep your layout after restarting.");
                PrimaryAction("Reset HUD layout", () =>
                {
                    var previous = owner.controls.Appearance;
                    owner.controls.Appearance = new HudAppearance
                    {
                        Colors = previous.Colors,
                        Pulse = previous.Pulse,
                        NewDamagePulse = previous.NewDamagePulse,
                        HealthyBarColor = previous.HealthyBarColor,
                        MatchHealthyBarColor = previous.MatchHealthyBarColor,
                        ContextOpacity = previous.ContextOpacity,
                        Rendering = previous.Rendering
                    };
                    RecreateControls(false);
                    Status("Position, size and opacity reset. Colors unchanged.");
                });
                Note("Reset changes position, size and transparency. Your colors and pulse settings stay as they are.");
            }

            private static string Number(double value)
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }

            private void Performance()
            {
                Intro("Choose how much the schematic draws on large or heavily damaged ships.");
                ChoiceField("Rendering mode", new[]
                {
                    "Full display", "Outlines only", "Faults with fills", "Fault outlines only"
                }, owner.controls.Appearance.Rendering, i =>
                {
                    owner.controls.Appearance.Rendering = i;
                    owner.RefreshReference("Rendering mode changed.");
                    Status("Updated. Save display to keep this setting.");
                });
                Note("Full display includes the hull and translucent damage shading. Outlines only removes the shading.");
                Note("Fault modes hide healthy hull context. Every filtered fault remains represented, and health-bar counts stay the same.");

                Heading("Damage pulses");
                CheckField("Pulse missing blocks", owner.controls.Appearance.Pulse > 0, enabled =>
                {
                    owner.controls.Appearance.Pulse = enabled ? 1 : 0;
                    Status("Updated. Save display to keep this setting.");
                });
                CheckField("Emphasize fresh damage", owner.controls.Appearance.NewDamagePulse, enabled =>
                {
                    owner.controls.Appearance.NewDamagePulse = enabled;
                    Status("Updated. Save display to keep this setting.");
                });
                Note("Fresh orange/red faults pulse together for four seconds. Yellow stays steady. Turn both options off for a still display.");
                Note("Use Save display to keep these choices. Recovery previews always use full rendering.");
            }

            private void Shortcuts()
            {
                Intro("Choose an action, then record a shortcut or type one with Ctrl, Alt or Shift.");
                owner.LoadHotkeys();
                var action = ChoiceField("Action", new[]
                {
                    "Cycle system", "Cycle block states", "Toggle expanded HUD", "Show / hide HUD", "Open this panel"
                }, 0);
                var binding = TextField("Shortcut", owner.hotkeys[0] ?? "", 80);
                shortcutBox = binding;
                action.ItemSelected += () =>
                {
                    captureShortcut = -1;
                    binding.Text = owner.hotkeys[(int)action.GetSelectedKey()] ?? "";
                };
                ActionRow("Press a key", () =>
                {
                    captureShortcut = (int)action.GetSelectedKey();
                    Status("Press your shortcut with any modifiers. Escape cancels. The binding saves automatically.");
                }, "Save typed shortcut", () =>
                    Status(owner.SaveHotkey((int)action.GetSelectedKey(), binding.Text)));
                PrimaryAction("Remove shortcut", () =>
                {
                    captureShortcut = -1;
                    Status(owner.SaveHotkey((int)action.GetSelectedKey(), ""));
                    binding.Text = "";
                });
                Note("Shortcuts save immediately. Escape cancels key recording. The game still receives bound keys, so choose combinations that suit your controls.");
            }

            private void Preferences()
            {
                Intro("Choose how Marigold recognizes ships and which connected grids it shows.");
                Heading("Ships and connections");
                CheckField("Automatically match saved ships", owner.controls.AutoAssociate,
                    enabled => Run("auto " + (enabled ? "on" : "off")));
                Note("After reconnecting, Marigold looks for a unique matching baseline. Ambiguous layouts still need manual selection.");
                CheckField("Show connector-docked ships", owner.controls.Station,
                    enabled => Run("station " + (enabled ? "on" : "off")));
                Note("This ship's station mode adds connector-docked context. Rotor, hinge and piston subgrids are excluded.");

                Heading("Language");
                ChoiceField("Interface language", new[] { "English", "Español" },
                    owner.controls.Language == "es" ? 1 : 0, i =>
                    {
                        Run("language " + (i == 1 ? "es" : "en"));
                        RecreateControls(false);
                    });
                Note("Language and automatic matching save when changed. Station mode saves with this ship's profile once it has one.");

                Heading("Drawing and feedback");
                ActionRow("Performance options", () => Switch(19), "Keyboard shortcuts", () => Switch(11));
                Note("Choose lighter rendering and turn missing-block or fresh-damage pulses on or off.");
                PrimaryAction("Save preferences", () => Run("settings save"));
                Note("Save preferences also keeps the current display as your default for future launches.");
            }

            private void SavedDisplays()
            {
                Intro("Save how the HUD looks. Ship baselines and custom-bar membership stay separate.");
                ActionRow("Share through clipboard", () => Switch(17));
                Heading("Default display");
                Note("The default is loaded on future game launches.");
                ActionRow("Save as default", () => Run("settings save"), "Restore default", () =>
                {
                    Run("settings load");
                    RecreateControls(false);
                });

                Heading("This ship");
                Note("Keep a separate display setup for the current saved ship.");
                ActionRow("Save for this ship", () => Status(owner.DisplayProfileAction("ship-save", null)),
                    "Restore ship setup", () =>
                    {
                        string message = owner.DisplayProfileAction("ship-load", null);
                        RecreateControls(false);
                        Status(message);
                    });

                Heading("Named presets");
                Note("Save a reusable display setup under a name, or enter an existing name to load it.");
                var name = TextField("Preset name", "My preset", 40);
                ActionRow("Save preset", () => Status(owner.DisplayProfileAction("preset-save", name.Text)),
                    "Load preset", () =>
                    {
                        string message = owner.DisplayProfileAction("preset-load", name.Text);
                        RecreateControls(false);
                        Status(message);
                    });
            }

            private void ClipboardConfig()
            {
                Intro("Copy your display setup to share it or use it on another computer.");
                Note("Includes schematic and standard health-bar layouts, colors, filters and display preferences. Custom bars stay with their ship; baselines and shortcuts are not included.");
                ActionRow("Copy config", () =>
                {
                    new Sandbox.Graphics.MyClipboardService().SetText(DisplaySettings.Export(owner.controls));
                    Status("Display config copied. Ship baselines and shortcuts are not included.");
                }, "Paste config", () =>
                {
                    DisplaySettings.Import(new Sandbox.Graphics.MyClipboardService().GetText(), owner.controls);
                    owner.RefreshReference("Display config imported.");
                    Status("Imported. Use Save display to keep it after restarting.");
                });
                Note("Pasting applies immediately. Invalid configurations leave your settings unchanged.");
                Note("After pasting, use Save display to keep the result as your default, this ship's setup or a named preset.");
            }

            private void Systems()
            {
                Intro("Focus on the systems you need while keeping the rest of the ship in context.");
                Heading("Quick selections");
                ActionRow("All systems", () => SelectSystems("all"), "No systems", () => SelectSystems("none"));
                ActionRow("Damage overview", () =>
                {
                    Run("isolate all");
                    Run("filter faults");
                    RecreateControls(false);
                }, "Propulsion", () => SelectSystems("thrusters,power,gas,control"));
                ActionRow("Conveyors and cargo", () => SelectSystems("conveyors,cargo"),
                    "Show all states", () => Run("filter all"));

                Heading("Systems in focus");
                Note("Check any combination. Unselected systems use the background opacity below.");
                var items = DisplayFilter.Categories.Split(' ');
                for (int i = 1; i < items.Length; i++)
                {
                    string category = items[i];
                    bool selected = owner.controls.Category == "all"
                        || Array.IndexOf(owner.controls.Category.Split(','), category) >= 0;
                    CheckField(SystemName(category), selected, enabled =>
                    {
                        var current = new List<string>(owner.controls.Category == "all"
                            ? items : owner.controls.Category.Split(','));
                        current.Remove("all");
                        current.Remove("none");
                        if (enabled && !current.Contains(category))
                            current.Add(category);
                        if (!enabled)
                            current.Remove(category);
                        Run("isolate " + (current.Count == 0 ? "none" : string.Join(",", current)));
                    });
                }

                Heading("Outline presentation");
                Note("Set a style for one system or all systems. Damage markers keep their state colors.");
                MyGuiControlCombobox style = null;
                var system = ChoiceField("Apply style to", Array.ConvertAll(items, SystemName), 1, i =>
                {
                    if (style != null)
                        style.SelectItemByKey(i == 0 ? 1 : owner.controls.SystemStyles[i] - '0');
                });
                style = ChoiceField("Presentation style", new[] { "Hidden", "Wireframe", "Faint outline", "Highlighted" },
                    owner.controls.SystemStyles[1] - '0');
                PrimaryAction("Apply style", () =>
                {
                    int index = (int)system.GetSelectedKey();
                    var values = owner.controls.SystemStyles.ToCharArray();
                    if (index == 0)
                    {
                        for (int i = 1; i < values.Length; i++)
                            values[i] = (char)('0' + style.GetSelectedKey());
                    }
                    else
                        values[index] = (char)('0' + style.GetSelectedKey());
                    owner.controls.SystemStyles = new string(values);
                    owner.RefreshReference("System presentation updated; damage markers retain their state colors.");
                    Status(owner.comparisonReport);
                });
                PrimaryAction("Other systems opacity...", () => Switch(24));
            }

            private void SelectSystems(string categories)
            {
                Run("isolate " + categories);
                RecreateControls(false);
            }

            private string SystemName(string category)
            {
                string name = T(category == "weapons" ? "Offensive" : category == "other" ? "Utility" : category);
                return name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name.Substring(1);
            }

            private void ContextSettings()
            {
                Intro("Keep the rest of your ship visible while focusing on selected systems.");
                var value = TextField("Other systems opacity (0 to 1)", Number(owner.controls.Appearance.ContextOpacity), 24);
                PrimaryAction("Apply opacity", () =>
                {
                    double opacity;
                    if (!double.TryParse(value.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out opacity)
                        || double.IsNaN(opacity) || double.IsInfinity(opacity) || opacity < 0 || opacity > 1)
                        throw new InvalidOperationException("Other systems opacity must be between 0 and 1.");
                    SetContextOpacity(opacity);
                });
                ActionRow("Hide other systems", () =>
                {
                    SetContextOpacity(0);
                    value.Text = "0";
                }, "Use faint outlines", () =>
                {
                    SetContextOpacity(.15);
                    value.Text = "0.15";
                });
                Note("0 hides other systems; 1 uses full outline opacity. Background systems use outlines only, and fault-only rendering modes hide them.");
                Note("Selected systems keep their damage colors. Use Save display to keep these choices.");
            }

            private void SetContextOpacity(double opacity)
            {
                owner.controls.Appearance.ContextOpacity = opacity;
                owner.RefreshReference("Other systems presentation updated.");
                Status("Updated. Save display to keep this setting.");
            }
        }
    }
}
