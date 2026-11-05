using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
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
                CheckField("Show ship label", owner.controls.Appearance.ShowShipLabel, visible =>
                {
                    owner.controls.Appearance.ShowShipLabel = visible;
                    Status("Updated. Save display to keep this setting.");
                });

                Heading("Position and size");
                var appearance = owner.controls.Appearance;
                bool expanded = owner.controls.Expanded;
                var x = TextField("Horizontal position (0 = left, 1 = right)", Number(expanded ? appearance.ExpandedX : appearance.X), 24);
                var y = TextField("Vertical position (0 = top, 1 = bottom)", Number(appearance.Y), 24);
                var size = TextField("Size (0.12 to 0.7 of screen height)", Number(appearance.Size), 24);
                PrimaryAction("Apply position and size", () =>
                {
                    var updated = HudAppearance.Parse(owner.controls.Appearance.Encode());
                    updated.Set(expanded ? "expandedx" : "x", x.Text);
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
                TextRow("Textured ship opacity");
                var shipOpacity = new MyGuiControlSlider(
                    minValue: 0, maxValue: 100, width: ContentWidth, defaultValue: (float)(appearance.ShipOpacity * 100),
                    labelText: "{0}%", labelDecimalPlaces: 0, labelScale: .68f, labelSpaceWidth: .075f,
                    intValue: true, showLabel: true
                );
                AddContent(shipOpacity, shipOpacity.Size.Y);
                Note("The slider fades the textured ship and damage fills together. Damage outlines stay visible.");
                PrimaryAction("Apply transparency", () =>
                {
                    var updated = HudAppearance.Parse(owner.controls.Appearance.Encode());
                    updated.Set("background", background.Text);
                    updated.Set("outline", outline.Text);
                    updated.Set("fill", fill.Text);
                    updated.ShipOpacity = shipOpacity.Value / 100d;
                    owner.controls.Appearance = updated;
                    Status("Updated. Save display to keep this layout.");
                });

                Heading("View style");
                ChoiceField("Schematic layout", new[] { "Compact", "Expanded" }, owner.controls.Expanded ? 1 : 0,
                    i =>
                    {
                        Run("layout " + (i == 1 ? "expanded" : "compact"));
                        RecreateControls(false);
                    });
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
                        Rendering = previous.Rendering,
                        ShowShipLabel = previous.ShowShipLabel
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
                var modes = new[]
                {
                    HudAppearance.HdWireframe, HudAppearance.SdWireframe, HudAppearance.Textured,
                    HudAppearance.Outlines, HudAppearance.FaultsWithFills, HudAppearance.FaultOutlines
                };
                ChoiceField("Rendering mode", new[]
                {
                    "HD Wireframe", "SD Wireframe", "Textured Ship",
                    "Outlines only", "Faults with fills", "Fault outlines only"
                }, Math.Max(0, Array.IndexOf(modes, owner.controls.Appearance.Rendering)), i =>
                {
                    owner.controls.Appearance.Rendering = modes[i];
                    owner.ReleaseTexturedShip();
                    owner.ReleaseSchematicTexture();
                    owner.drawSamples = 0;
                    owner.drawTotalMs = owner.drawMaxMs = 0;
                    owner.RefreshReference("Rendering mode changed.");
                    Status("Updated. Save display to keep this setting.");
                });
                Note("HD Wireframe has sharper lines and fill edges. SD Wireframe uses the original sprite display.");
                Note("Textured Ship uses the live ship's models, paint, and available textures. Adjust its opacity in Ship display.");
                Note("Outlines only removes the translucent damage shading.");
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
                Note("Use Save display to keep these choices. Recovery previews use wireframe with damage shading.");
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
            }

            private void SavedDisplays()
            {
                bool hasDefault = File.Exists(owner.settingsPath);
                Heading("Save the current layout");
                if (owner.ActiveShipSettingsPath != null)
                {
                    ActionRow("Save as my default", () => ChangeDefault("settings save"),
                        "Save for this ship", () => ChangeShipDisplay("ship-save"));
                    Note("Ships use your default unless you save a layout for them. Custom bar positions and sizes are saved per ship.");
                }
                else
                {
                    PrimaryAction("Save as my default", () => ChangeDefault("settings save"));
                    Note("To save a layout for one ship, capture or load its baseline first.");
                }

                Heading("When you return");
                bool useShipLayout = owner.UsesSavedShipLayout;
                if (owner.HasSavedShipLayout)
                {
                    ChoiceField("Use on reconnect", new[] { "My default", "This ship's layout" },
                        useShipLayout ? 1 : 0, i => ChangeShipDisplay(i == 0 ? "ship-default" : "ship-load"));
                    Note("Changing this choice loads that layout now and on reconnect.");
                }
                else
                    Note("Your default loads when you return.");

                if (useShipLayout)
                    ActionRow("Reload saved layout", () => ChangeShipDisplay("ship-load"),
                        hasDefault ? "Load my default now" : null, () => ChangeDefault("settings load"));
                else if (hasDefault)
                    ActionRow("Reload saved layout", () => ChangeDefault("settings load"));

                Heading("Reuse or share");
                ActionRow("Named layouts", () => Switch(27), "Copy or paste layout", () => Switch(17));

                void ChangeDefault(string command)
                {
                    Run(command);
                    RecreateControls(false);
                }

                void ChangeShipDisplay(string action)
                {
                    string message;
                    try
                    {
                        message = owner.DisplayProfileAction(action, null);
                    }
                    catch (Exception error)
                    {
                        message = error.Message;
                    }
                    RecreateControls(false);
                    Status(message);
                }
            }

            private void NamedLayouts()
            {
                Intro("Keep layouts you can load on any ship. Custom bar positions stay with their ship.");
                Heading("Save a named copy");
                var name = TextField("Layout name", "", 40);
                ActionRow("Save named layout", () =>
                {
                    string message = owner.DisplayProfileAction("preset-save", name.Text);
                    selectedNamedLayout = name.Text.Trim();
                    RecreateControls(false);
                    Status(message);
                });
                Note("Saving with an existing name replaces that copy.");

                Heading("Load a named layout");
                string[] layouts;
                try
                {
                    layouts = owner.SavedLayoutNames();
                }
                catch (Exception error) when (error is IOException || error is UnauthorizedAccessException)
                {
                    Note("Couldn't read your named layouts.");
                    return;
                }
                if (layouts.Length == 0)
                {
                    Note("Your named layouts will appear here after you save one.");
                    return;
                }

                int selected = Array.FindIndex(layouts, layout =>
                    string.Equals(layout, selectedNamedLayout, StringComparison.OrdinalIgnoreCase));
                var list = ChoiceField("Saved layout", layouts, selected,
                    i => selectedNamedLayout = layouts[i], translateItems: false);
                ActionRow("Load selected layout", () =>
                {
                    string message = owner.DisplayProfileAction("preset-load", layouts[(int)list.GetSelectedKey()]);
                    RecreateControls(false);
                    Status(message);
                });
                Note("Loading applies the layout now. Use Save display to keep it as your default or for this ship.");
            }

            private void ClipboardConfig()
            {
                Intro("Copy your HUD layout to share it or use it on another computer.");
                Note("Includes the schematic, standard health bars, colors, filters and preferences. Custom bars, baselines and shortcuts stay on this computer.");
                ActionRow("Copy layout", () =>
                {
                    new Sandbox.Graphics.MyClipboardService().SetText(DisplaySettings.Export(owner.controls));
                    Status("HUD layout copied to the clipboard.");
                }, "Paste layout", () =>
                {
                    DisplaySettings.Import(new Sandbox.Graphics.MyClipboardService().GetText(), owner.controls);
                    owner.RefreshReference("Display config imported.");
                    Status("Imported. Use Save display to keep it after restarting.");
                });
                Note("Pasting applies immediately. Invalid configurations leave your settings unchanged.");
                Note("Use Save display to keep the imported layout as your default, for this ship, or as a named copy.");
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
