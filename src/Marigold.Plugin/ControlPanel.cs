using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using VRage.Utils;
using VRageMath;

namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private Panel panel;

        private void OpenPanel()
        {
            if (panel != null)
                return;
            var opened = new Panel(this);
            panel = opened;
            opened.Closed += (screen, unloading) =>
            {
                if (ReferenceEquals(panel, opened))
                {
                    panel = null;
                    EndPanelRecovery();
                }
            };
            MyGuiSandbox.AddScreen(opened);
        }

        private void ClosePanel()
        {
            var previous = panel;
            panel = null;
            EndPanelRecovery();
            previous?.CloseScreen();
        }

        private void EndPanelRecovery()
        {
            if (recoveryProfile == null && recoveryPreview == null && recoveryTask == null)
                return;
            CancelRecovery();
            // Remove preview geometry immediately and reject any pending preview build.
            points = new Point3[0];
            shadedMarkers = new int[0];
            fillBands.Clear();
            health = new ShipHealth();
            healthPreview = false;
            RefreshReference("Recovery preview closed; saved baseline retained.");
        }

        private string PanelCommand(string command)
        {
            try
            {
                string before = controls.GeometryOptions;
                string response = controls.Execute("/mg " + command);
                if (controls.ReferenceAction == "ui")
                {
                    OpenPanel();
                    response = "Marigold controls opened.";
                }
                else if (controls.ReferenceAction != null)
                    response = ReferenceCommand(controls.ReferenceAction);
                if (controls.StatsRequested)
                    response = Statistics();
                if (before != controls.GeometryOptions)
                {
                    generation++;
                    dirty = true;
                    cooldown = 0;
                }

                return response;
            }
            catch (Exception e)
            {
                return "Could not complete action: " + e.Message;
            }
        }

        private sealed class Panel : MyGuiScreenBase
        {
            private readonly SchematicOverlay owner;
            private int blueprintUiRevision = -1;
            private bool refreshBlueprint;

            public void NotifyBlueprint(string message)
            {
                Status(message);
                refreshBlueprint = true;
            }

            public bool RecoveryFocus
            {
                get
                {
                    return tab == 7;
                }
            }

            public readonly SchematicControls RecoveryView = new SchematicControls();
            private bool draggingRecovery;
            private Vector2 recoveryMouse;
            private int healthBarSelected;
            private bool browseLocal = true, browseWorkshop, browseCloud;
            private string searchText = "";
            private int browsePage;
            private int captureShortcut = -1;
            private MyGuiControlTextbox shortcutBox;
            private int tab;
            private readonly Stack<int> history = new Stack<int>();
            private string status = "", path = "", confirm;
            private MyGuiControlMultilineText output;
            private MyGuiControlTextbox pathBox, blueprintSearchBox;
            private MyGuiControlButton[] blueprintResultButtons;
            private MyGuiControlLabel blueprintPageLabel;
            private string[] localFiles = new string[0], localNames = new string[0];
            private Task<BlueprintCatalog.Listing> catalog;

            public Panel(SchematicOverlay owner) : base(
                new Vector2(.5f, .5f),
                new Vector4(.03f, .06f, .08f, 1),
                new Vector2(.92f, .92f),
                backgroundTransition: 1f,
                guiTransition: 1f
            )
            {
                this.owner = owner;
                RecoveryView.Execute("/mg spin");
                CanHideOthers = false;
                RecreateControls(true);
            }

            public override string GetFriendlyName()
            {
                return "MarigoldControls";
            }

            private string T(string text)
            {
                return UiText.Get(owner.controls.Language, text);
            }

            private void Label(float x, float y, string text)
            {
                Controls.Add(new MyGuiControlLabel(new Vector2(x, y), text: T(text), textScale: .65f, maxWidth: .78f, isAutoEllipsisEnabled: true));
            }

            private void Button(float x, float y, string text, Action action, float width = .19f)
            {
                Controls.Add(new MyGuiControlButton(
                    new Vector2(x, y),
                    visualStyle: RecoveryFocus
                        ? VRage.Game.MyGuiControlButtonStyleEnum.Rectangular
                        : VRage.Game.MyGuiControlButtonStyleEnum.Default,
                    size: new Vector2(width, .042f),
                    text: new StringBuilder(T(text)),
                    textScale: .62f,
                    onButtonClick: b =>
                    {
                        try
                        {
                            action();
                        }
                        catch (Exception e)
                        {
                            Status(e.Message);
                        }
                    }
                ));
            }

            private MyGuiControlCombobox Combo(
                float x,
                float y,
                string[] items,
                int selected,
                Action<int> change = null
            )
            {
                var combo = new MyGuiControlCombobox(new Vector2(x, y), new Vector2(.34f, .04f), openAreaItemsCount: 6);
                for (int i = 0; i < items.Length; i++)
                    combo.AddItem(i, T(items[i]), sort: false);
                if (items.Length > 0)
                    combo.SelectItemByKey(Math.Max(0, Math.Min(selected, items.Length - 1)), false);
                if (change != null)
                    combo.ItemSelected += () => change((int)combo.GetSelectedKey());
                Controls.Add(combo);
                return combo;
            }

            public void NotifyStatus(string text)
            {
                Status(text);
            }

            private void Status(string text)
            {
                status = T(text);
                if (output != null)
                {
                    output.Clear();
                    output.AppendText(status);
                }
            }

            private void Run(string command, bool destructive = false)
            {
                if (destructive && confirm != command)
                {
                    confirm = command;
                    Status(T("Click the same action again to confirm. No changes yet."));
                    return;
                }

                confirm = null;
                Status(owner.PanelCommand(command));
            }

            private void Switch(int next)
            {
                if (next == tab)
                    return;
                if (RecoveryFocus)
                    owner.EndPanelRecovery();
                if (pathBox != null)
                    path = pathBox.Text;
                history.Push(tab);
                tab = next;
                confirm = null;
                Status("");
                RecreateControls(false);
            }

            private void Back()
            {
                if (history.Count == 0)
                    return;
                if (RecoveryFocus)
                    owner.EndPanelRecovery();
                if (pathBox != null)
                    path = pathBox.Text;
                tab = history.Pop();
                confirm = null;
                Status("");
                RecreateControls(false);
            }

            private int Section
            {
                get
                {
                    return tab == 1 || tab == 2 || tab == 6 || tab == 7 || tab == 10 || tab == 15 || tab == 18
                        ? 1
                        : tab == 0 || tab == 3 || tab == 14 ? 0 : 2;
                }
            }

            private static readonly string[] PageNames =
            {
                "Ship",
                "Baseline",
                "Baseline / Import blueprint",
                "Ship / View",
                "Customize / HUD layout",
                "Customize / System filters",
                "Baseline / Saved versions",
                "Baseline / Recovery alignment",
                "Customize / Health bars",
                "Customize",
                "Baseline / Saved ships",
                "Customize / Shortcuts",
                "Customize / Preferences",
                "Save display",
                "Ship / Diagnostics",
                "Baseline / Recovery",
                "Customize / Colors",
                "Customize / Share config",
                "Baseline / Find blueprint",
                "Customize / Preferences / Performance"
            };

            private void MenuRow(float y, string title, string description, int next)
            {
                Button(-.28f, y, title, () => Switch(next));
                Controls.Add(new MyGuiControlLabel(
                    new Vector2(-.15f, y),
                    text: T(description),
                    textScale: .60f,
                    maxWidth: .54f,
                    isAutoEllipsisEnabled: true
                ));
            }

            public override void RecreateControls(bool constructor)
            {
                base.RecreateControls(constructor);
                draggingRecovery = false;
                pathBox = null;
                blueprintSearchBox = null;
                blueprintResultButtons = null;
                blueprintPageLabel = null;
                captureShortcut = -1;
                shortcutBox = null;
                Label(
                    -.4f,
                    -.40f,
                    "Marigold " + typeof(Plugin).Assembly.GetName().Version.ToString(3) + "  /  " + T(PageNames[tab])
                );
                if (RecoveryFocus)
                {
                    if (history.Count > 0)
                        Button(-.31f, .40f, "Back", Back);
                    Button(.32f, .40f, "Close", () => CloseScreen());
                    output = new MyGuiControlMultilineText(
                        new Vector2(0, .325f),
                        new Vector2(.8f, .085f),
                        textScale: .56f,
                        drawScrollbarH: false,
                        contents: new StringBuilder(status)
                    );
                    Controls.Add(output);
                    Alignment();
                    return;
                }

                Button(-.28f, -.33f, Section == 0 ? "[ Ship ]" : "Ship", () => Switch(0));
                Button(0, -.33f, Section == 1 ? "[ Baseline ]" : "Baseline", () => Switch(1));
                Button(.28f, -.33f, Section == 2 ? "[ Customize ]" : "Customize", () => Switch(9));
                if (history.Count > 0)
                    Button(-.31f, .40f, "Back", Back);
                if (tab != 13)
                    Button(0, .40f, "Save display...", () => Switch(13));
                Button(.32f, .40f, "Close", () => CloseScreen());
                output = new MyGuiControlMultilineText(
                    new Vector2(0, .29f),
                    new Vector2(.8f, .14f),
                    textScale: .60f,
                    drawScrollbarH: false,
                    contents: new StringBuilder(status)
                );
                Controls.Add(output);
                switch (tab)
                {
                    case 0:
                        Display();
                        break;
                    case 1:
                        Baseline();
                        break;
                    case 2:
                        Blueprints();
                        break;
                    case 3:
                        Views();
                        break;
                    case 4:
                        Appearance();
                        break;
                    case 5:
                        Systems();
                        break;
                    case 6:
                        Library();
                        break;
                    case 7:
                        Alignment();
                        break;
                    case 8:
                        HealthBars();
                        break;
                    case 9:
                        Customize();
                        break;
                    case 10:
                        Profiles();
                        break;
                    case 11:
                        Shortcuts();
                        break;
                    case 12:
                        Preferences();
                        break;
                    case 13:
                        SavedDisplays();
                        break;
                    case 14:
                        Diagnostics();
                        break;
                    case 15:
                        Recovery();
                        break;
                    case 16:
                        Colors();
                        break;
                    case 17:
                        ClipboardConfig();
                        break;
                    case 18:
                        BlueprintBrowser();
                        break;
                    case 19:
                        Performance();
                        break;
                }
            }

            private void Choice(float x, float y, string label, string values, string selected, string command)
            {
                Label(x - .17f, y - .032f, label);
                var items = values.Split(' ');
                if (Array.IndexOf(items, selected) < 0)
                {
                    var extended = new List<string>(items);
                    extended.Add(selected);
                    items = extended.ToArray();
                }

                Combo(
                    x,
                    y,
                    Array.ConvertAll(
                        items,
                        item => item.Contains(",")
                            ? "Multiple systems"
                            : item == "none"
                            ? "No systems"
                            : item == "weapons"
                            ? "Offensive"
                            : item == "broken"
                            ? "Non-functional"
                            : item == "all"
                            ? "All"
                            : item == "faults"
                            ? "Damage & missing"
                            : System.Globalization.CultureInfo.InvariantCulture.TextInfo.ToTitleCase(item)
                    ),
                    Array.IndexOf(items, selected),
                    i => Run(command + " " + items[i])
                );
            }

            private void Display()
            {
                var c = owner.controls;
                Label(-.39f, -.25f, owner.grid == null ? "Sit in a cockpit to view your ship." : owner.grid.DisplayName);
                Button(
                    -.21f,
                    -.17f,
                    c.Visible ? "Hide HUD" : "Show HUD",
                    () =>
                    {
                        Run(c.Visible ? "hide" : "show");
                        RecreateControls(false);
                    }
                );
                Button(
                    .21f,
                    -.17f,
                    c.Spin ? "Pause rotation" : "Start rotation",
                    () =>
                    {
                        Run("spin");
                        RecreateControls(false);
                    }
                );
                Choice(-.21f, -.06f, "Show system", DisplayFilter.Categories, c.Category, "isolate");
                Choice(.21f, -.06f, "Show block states", DisplayFilter.States, c.StateFilter, "filter");
                MenuRow(.04f, "View & zoom", "Choose a side, rotate or reset the schematic.", 3);
                MenuRow(.12f, "Damage report", "Counts, marker pages and performance details.", 14);
                Label(-.39f, .19f, "Changes preview immediately. Use Save display to keep your setup.");
            }

            private void Views()
            {
                Label(-.39f, -.25f, "Views follow the saved cockpit orientation.");
                Button(-.31f, -.16f, "Left", () => Run("left"));
                Button(-.10f, -.16f, "Right", () => Run("right"));
                Button(.11f, -.16f, "Top", () => Run("top"));
                Button(.32f, -.16f, "Bottom", () => Run("bottom"));
                Button(-.21f, -.06f, "Zoom in", () => Run("in"));
                Button(.21f, -.06f, "Zoom out", () => Run("out"));
                Label(-.39f, .015f, "Fine rotation (pauses automatic rotation)");
                Button(-.31f, .07f, "Turn left", () => owner.controls.RotateView(-Math.PI / 12, 0));
                Button(-.10f, .07f, "Turn right", () => owner.controls.RotateView(Math.PI / 12, 0));
                Button(.11f, .07f, "Tilt up", () => owner.controls.RotateView(0, -Math.PI / 12));
                Button(.32f, .07f, "Tilt down", () => owner.controls.RotateView(0, Math.PI / 12));
                Button(0, .17f, "Reset view", () => Run("reset"));
            }

            private void Baseline()
            {
                string name;
                try
                {
                    name = owner.SelectedProfile().Name;
                }
                catch (InvalidOperationException)
                {
                    name = T("None - capture your ship or import a blueprint.");
                }

                Label(-.39f, -.25f, T("Current baseline: ") + name);
                Button(-.21f, -.17f, "Capture this ship", () => Run("capture", true));
                Button(.21f, -.17f, "Import blueprint", () => Switch(2));
                Label(-.39f, -.105f, "Capture/import preserves the old baseline in Saved versions.");
                MenuRow(-.03f, "Saved ships", "Choose an existing baseline or detach it.", 10);
                MenuRow(.05f, "Saved versions", "Keep named copies before changing your ship.", 6);
                MenuRow(.13f, "Recovery tools", "Match a damaged ship or restore a backup.", 15);
            }

            private void Customize()
            {
                MenuRow(-.25f, "HUD layout", "Position, size, transparency and detail.", 4);
                MenuRow(-.18f, "Colors", "Change individual state colors and missing-block pulse.", 16);
                MenuRow(-.11f, "Health bars", "Choose which bars to show and arrange them.", 8);
                MenuRow(-.04f, "System filters", "Combine systems and adjust their outline styles.", 5);
                MenuRow(.03f, "Shortcuts", "Assign keyboard controls.", 11);
                MenuRow(.10f, "Preferences", "Performance, automatic matching, docks and language.", 12);
                MenuRow(.17f, "Share config", "Copy or paste display and health-bar settings.", 17);
            }

            private void ClipboardConfig()
            {
                Label(-.39f, -.25f, "Share your display layout, health bars, colors and filters.");
                Button(
                    -.21f,
                    -.13f,
                    "Copy config",
                    () =>
                    {
                        new Sandbox.Graphics.MyClipboardService().SetText(DisplaySettings.Export(owner.controls));
                        Status("Display config copied. Ship baselines and shortcuts are not included.");
                    }
                );
                Button(
                    .21f,
                    -.13f,
                    "Paste config",
                    () =>
                    {
                        DisplaySettings.Import(new Sandbox.Graphics.MyClipboardService().GetText(), owner.controls);
                        owner.RefreshReference("Display config imported.");
                        Status("Imported. Use Save display to keep it after restarting.");
                    }
                );
                Label(-.39f, -.01f, "Import applies immediately; invalid configs leave your settings unchanged.");
                Label(-.39f, .07f, "Save display keeps the imported setup as a default, ship setup or preset.");
            }

            private void Shortcuts()
            {
                Label(-.39f, -.25f, "Choose an action, then press the shortcut you want to use.");
                owner.LoadHotkeys();
                var action = Combo(
                    0,
                    -.16f,
                    new[]
                    {
                        "Cycle system",
                        "Cycle block states",
                        "Toggle expanded HUD",
                        "Show / hide HUD",
                        "Open this panel"
                    },
                    0
                );
                var binding = new MyGuiControlTextbox(new Vector2(0, -.05f), owner.hotkeys[0] ?? "", maxLength: 80, textScale: .65f)
                {
                    Size = new Vector2(.34f, .04f)
                };
                Controls.Add(binding);
                shortcutBox = binding;
                action.ItemSelected += () => binding.Text = owner.hotkeys[(int)action.GetSelectedKey()] ?? "";
                Button(
                    -.21f,
                    .05f,
                    "Press a key",
                    () =>
                    {
                        captureShortcut = (int)action.GetSelectedKey();
                        Status("Press your shortcut with any modifiers. Escape cancels. The binding saves automatically.");
                    }
                );
                Button(
                    .21f,
                    .05f,
                    "Save typed shortcut",
                    () => Status(owner.SaveHotkey((int)action.GetSelectedKey(), binding.Text))
                );
                Button(
                    0,
                    .15f,
                    "Remove shortcut",
                    () =>
                    {
                        Status(owner.SaveHotkey((int)action.GetSelectedKey(), ""));
                        binding.Text = "";
                    }
                );
            }

            private void Preferences()
            {
                Choice(
                    -.21f,
                    -.19f,
                    "Show connector-docked ships",
                    "off on",
                    owner.controls.Station ? "on" : "off",
                    "station"
                );
                Choice(
                    .21f,
                    -.19f,
                    "Automatically match saved ships",
                    "off on",
                    owner.controls.AutoAssociate ? "on" : "off",
                    "auto"
                );
                Label(-.39f, -.09f, "Station mode includes connector docks, not rotors or pistons.");
                Label(-.17f, -.01f, "Language");
                Combo(
                    0,
                    .03f,
                    new[] { "English", "Español" },
                    owner.controls.Language == "es" ? 1 : 0,
                    i =>
                    {
                        Run("language " + (i == 1 ? "es" : "en"));
                        RecreateControls(false);
                    }
                );
                Button(-.21f, .14f, "Performance options", () => Switch(19));
                Button(.21f, .14f, "Save preferences", () => Run("settings save"));
                Label(-.39f, .20f, "Performance options include rendering and both damage pulses.");
            }

            private void SavedDisplays()
            {
                Label(-.39f, -.25f, "Keep your current HUD layout, filters, colors and health bars.");
                Button(-.21f, -.16f, "Save as default", () => Run("settings save"));
                Button(
                    .21f,
                    -.16f,
                    "Restore default",
                    () =>
                    {
                        Run("settings load");
                        RecreateControls(false);
                    }
                );
                Button(-.21f, -.06f, "Save for this ship", () => Status(owner.DisplayProfileAction("ship-save", null)));
                Button(.21f, -.06f, "Restore ship setup", () => Status(owner.DisplayProfileAction("ship-load", null)));
                Label(-.39f, .015f, "Or save a named display preset:");
                var name = new MyGuiControlTextbox(new Vector2(-.21f, .08f), "My preset", maxLength: 40, textScale: .65f)
                {
                    Size = new Vector2(.34f, .04f)
                };
                Controls.Add(name);
                Button(.11f, .08f, "Save preset", () => Status(owner.DisplayProfileAction("preset-save", name.Text)));
                Button(.32f, .08f, "Load preset", () => Status(owner.DisplayProfileAction("preset-load", name.Text)));
                Label(-.39f, .17f, "Display presets change how the HUD looks; ship baselines stay intact.");
            }

            private void Diagnostics()
            {
                Label(-.39f, -.25f, "Inspect your ship's condition or troubleshoot performance.");
                Button(-.21f, -.15f, "Damage report", () => Run("report"));
                Button(.21f, -.15f, "Performance", () => Run("stats"));
                Label(-.39f, -.04f, "Cycle damage marker pages when there are too many to show at once.");
                Button(-.21f, .04f, "Previous page", () => Run("page prev"));
                Button(.21f, .04f, "Next page", () => Run("page next"));
            }

            private void Recovery()
            {
                Label(-.39f, -.25f, "Use when the original cockpit is gone or automatic matching fails.");
                var profiles = new List<ShipProfile>(owner.profileStore.Profiles);
                var names = new List<string>();
                foreach (var p in profiles)
                    names.Add(p.Name + " [" + p.Number + "]");
                var choice = Combo(0, -.17f, names.ToArray(), 0);
                Button(
                    -.21f,
                    -.07f,
                    "Preview recovery",
                    () =>
                    {
                        int i = (int)choice.GetSelectedKey();
                        if (i < 0 || i >= profiles.Count)
                        {
                            Status("Select a saved ship first.");
                            return;
                        }

                        Run("profile force " + profiles[i].Id);
                        if (owner.recoveryProfile != null)
                        {
                            string message = status;
                            Switch(7);
                            Status(message);
                        }
                    }
                );
                Button(.21f, -.07f, "Review alignment", () => Switch(7));
                Button(-.21f, .03f, "Matching status", () => Run("auto status"));
                Button(.21f, .03f, "Retry matching", () => Run("auto retry"));
                Button(-.21f, .13f, "Set main cockpit", () => Run("anchor"));
                Button(.21f, .13f, "Restore backup", () => Run("recover", true));
                Button(0, .19f, "Clear baseline", () => Run("clear", true));
            }

            private void Colors()
            {
                Label(-.39f, -.25f, "Edit one state at a time. Enter a six-digit RGB color, e.g. FF8214.");
                var choice = Combo(
                    -.21f,
                    -.15f,
                    new[]
                    {
                        "Healthy outline",
                        "Missing",
                        "Damaged",
                        "Replaced",
                        "Added",
                        "Unknown",
                        "Non-functional"
                    },
                    0
                );
                var value = new MyGuiControlTextbox(
                    new Vector2(.11f, -.15f),
                    owner.controls.Appearance.Colors.Split(',')[0],
                    maxLength: 6,
                    textScale: .65f
                )
                {
                    Size = new Vector2(.19f, .04f)
                };
                Controls.Add(value);
                choice.ItemSelected += () => value.Text = owner.controls.Appearance.Colors.Split(',')[(int)choice.GetSelectedKey()];
                Button(
                    .32f,
                    -.15f,
                    "Apply color",
                    () =>
                    {
                        var colors = owner.controls.Appearance.Colors.Split(',');
                        colors[(int)choice.GetSelectedKey()] = value.Text;
                        owner.controls.Appearance.Set("colors", string.Join(",", colors));
                        Status("Color updated. Save display to keep it.");
                    }
                );
                Button(
                    0,
                    -.02f,
                    "Restore colors",
                    () =>
                    {
                        owner.controls.Appearance.Colors = new HudAppearance().Colors;
                        RecreateControls(false);
                    }
                );
                Button(0, .07f, "Pulse settings in Preferences", () => Switch(19));
                Label(-.39f, .15f, "Orange/red alerts pulse for four seconds. Yellow stays steady.");
                Label(-.39f, .20f, "Health bars keep healthy blocks blue and use these damage colors.");
            }

            private void HealthBars()
            {
                Label(-.39f, -.26f, "Baseline block states: blue / yellow / orange / red / gray");
                Combo(
                    -.21f,
                    -.20f,
                    ShipHealth.Names,
                    healthBarSelected,
                    i =>
                    {
                        healthBarSelected = i;
                        RecreateControls(false);
                    }
                );
                var layout = owner.controls.HealthBars.Bars[healthBarSelected];
                Button(
                    .11f,
                    -.20f,
                    layout.Enabled ? "Disable bar" : "Enable bar",
                    () =>
                    {
                        layout.Enabled = !layout.Enabled;
                        RecreateControls(false);
                    }
                );
                Button(
                    .32f,
                    -.20f,
                    layout.Label ? "Hide label" : "Show label",
                    () =>
                    {
                        layout.Label = !layout.Label;
                        RecreateControls(false);
                    }
                );
                var fields = new[]
                {
                    "x",
                    "y",
                    "width",
                    "height",
                    "opacity"
                };
                var field = Combo(-.21f, -.10f, new[]
                {
                    "Horizontal position",
                    "Vertical position",
                    "Width",
                    "Height",
                    "Opacity"
                }, 0);
                var value = new MyGuiControlTextbox(
                    new Vector2(.11f, -.10f),
                    layout.X.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    maxLength: 24,
                    textScale: .65f
                )
                {
                    Size = new Vector2(.19f, .04f)
                };
                Controls.Add(value);
                field.ItemSelected += () => value.Text = layout.Encode().Split(',')[(int)field.GetSelectedKey() + 2];
                Button(
                    .32f,
                    -.10f,
                    "Apply",
                    () =>
                    {
                        layout.Set(fields[(int)field.GetSelectedKey()], value.Text);
                        Status("Bar updated. Use Save display to keep it.");
                    }
                );
                Button(
                    -.31f,
                    0,
                    "Left",
                    () =>
                    {
                        layout.X = Math.Max(0, layout.X - .02);
                        RecreateControls(false);
                    }
                );
                Button(
                    -.10f,
                    0,
                    "Right",
                    () =>
                    {
                        layout.X = Math.Min(1, layout.X + .02);
                        RecreateControls(false);
                    }
                );
                Button(
                    .11f,
                    0,
                    "Up",
                    () =>
                    {
                        layout.Y = Math.Max(0, layout.Y - .02);
                        RecreateControls(false);
                    }
                );
                Button(
                    .32f,
                    0,
                    "Down",
                    () =>
                    {
                        layout.Y = Math.Min(1, layout.Y + .02);
                        RecreateControls(false);
                    }
                );
                Button(
                    -.21f,
                    .08f,
                    "Hide all",
                    () =>
                    {
                        foreach (var b in owner.controls.HealthBars.Bars)
                            b.Enabled = false;
                        RecreateControls(false);
                    }
                );
                Button(
                    .21f,
                    .08f,
                    "Reset this bar",
                    () =>
                    {
                        owner.controls.HealthBars.Bars[healthBarSelected] = new HealthBarSettings().Bars[healthBarSelected];
                        RecreateControls(false);
                    }
                );
                Label(-.39f, .15f, "X/Y/opacity: 0-1; width: .05-.8; height: .005-.08");
                Label(
                    -.39f,
                    .19f,
                    "Utility includes systems without a dedicated bar. Added blocks excluded; replacements count red."
                );
                if (healthBarSelected == 2)
                {
                    Label(-.39f, .25f, "Separate power sections (unchecked types stay in Other):");
                    for (int i = 0; i < 3; i++)
                    {
                        int bit = 1 << i;
                        float x = -.36f + i * .27f;
                        var check = new MyGuiControlCheckbox
                        {
                            Position = new Vector2(x, .30f),
                            IsChecked = (layout.PowerMask & bit) != 0
                        };
                        check.IsCheckedChanged += c =>
                        {
                            layout.PowerMask = c.IsChecked ? layout.PowerMask | bit : layout.PowerMask & ~bit;
                        };
                        Controls.Add(check);
                        Label(x + .035f, .30f, ShipHealth.PowerNames[i]);
                    }
                }
            }

            private void Appearance()
            {
                Button(
                    0,
                    -.25f,
                    owner.controls.SchematicVisible ? "Hide ship display" : "Show ship display",
                    () =>
                    {
                        owner.controls.SchematicVisible = !owner.controls.SchematicVisible;
                        RecreateControls(false);
                    }
                );
                var fields = new[]
                {
                    "x",
                    "y",
                    "size",
                    "background",
                    "outline",
                    "fill"
                };
                var names = new[]
                {
                    "Horizontal position",
                    "Vertical position",
                    "Schematic size",
                    "Background opacity",
                    "Outline opacity",
                    "Damage fill opacity"
                };
                var choice = Combo(-.21f, -.15f, names, 0);
                var value = new MyGuiControlTextbox(
                    new Vector2(.11f, -.15f),
                    owner.controls.Appearance.Encode().Split(';')[0],
                    maxLength: 24,
                    textScale: .65f
                )
                {
                    Size = new Vector2(.19f, .04f)
                };
                Controls.Add(value);
                choice.ItemSelected += () => value.Text = owner.controls.Appearance.Encode().Split(';')[(int)choice.GetSelectedKey()];
                Button(
                    .32f,
                    -.15f,
                    "Apply",
                    () =>
                    {
                        owner.controls.Appearance.Set(fields[(int)choice.GetSelectedKey()], value.Text);
                        Status("Updated. Save display to keep this layout.");
                    }
                );
                Label(-.39f, -.07f, "Position and opacity: 0 to 1. Size: 0.12 to 0.7.");
                Choice(
                    -.21f,
                    .02f,
                    "Schematic layout",
                    "compact expanded",
                    owner.controls.Expanded ? "expanded" : "compact",
                    "layout"
                );
                Choice(.21f, .02f, "Outline detail", "low medium high", owner.controls.DetailName, "detail");
                Button(
                    0,
                    .10f,
                    "Reset HUD layout",
                    () =>
                    {
                        var colors = owner.controls.Appearance.Colors;
                        bool fresh = owner.controls.Appearance.NewDamagePulse;
                        double pulse = owner.controls.Appearance.Pulse;
                        owner.controls.Appearance = new HudAppearance
                        {
                            Colors = colors,
                            Pulse = pulse,
                            NewDamagePulse = fresh
                        };
                        RecreateControls(false);
                        Status("Position, size and opacity reset. Colors unchanged.");
                    }
                );
            }

            private void Performance()
            {
                Label(-.39f, -.25f, "Reduce drawing cost on heavily damaged ships.");
                Combo(
                    0,
                    -.15f,
                    new[]
                    {
                        "Full display",
                        "Outlines only",
                        "Faults with fills",
                        "Fault outlines only"
                    },
                    owner.controls.Appearance.Rendering,
                    i =>
                    {
                        owner.controls.Appearance.Rendering = i;
                        owner.RefreshReference("Rendering mode changed.");
                        Status("Updated. Save display to keep this setting.");
                    }
                );
                Label(-.39f, -.04f, "Outlines only skips translucent shading. Fault modes hide hull context.");
                Label(-.39f, .03f, "Every filtered fault remains represented; health counts stay the same.");
                Button(
                    -.21f,
                    .10f,
                    owner.controls.Appearance.Pulse > 0 ? "Missing pulse: On" : "Missing pulse: Off",
                    () =>
                    {
                        owner.controls.Appearance.Pulse = owner.controls.Appearance.Pulse > 0 ? 0 : 1;
                        RecreateControls(false);
                    }
                );
                Button(
                    .21f,
                    .10f,
                    owner.controls.Appearance.NewDamagePulse ? "Fresh pulse: On" : "Fresh pulse: Off",
                    () =>
                    {
                        owner.controls.Appearance.NewDamagePulse = !owner.controls.Appearance.NewDamagePulse;
                        RecreateControls(false);
                    }
                );
                Label(-.39f, .18f, "Save display keeps these settings. Recovery uses full rendering.");
            }

            private void Systems()
            {
                Label(-.39f, -.25f, "Choose several systems to display together");
                var items = DisplayFilter.Categories.Split(' ');
                var choice = Combo(0, -.17f, Array.ConvertAll(items, SystemName), 1);
                Action<bool> toggle = add =>
                {
                    string item = items[(int)choice.GetSelectedKey()];
                    if (item == "all")
                    {
                        Run(add ? "isolate all" : "isolate none");
                        return;
                    }

                    var selected = new List<string>(owner.controls.Category == "all" ? items : owner.controls.Category.Split(','));
                    selected.Remove("all");
                    selected.Remove("none");
                    if (add && !selected.Contains(item))
                        selected.Add(item);
                    if (!add)
                        selected.Remove(item);
                    Run("isolate " + (selected.Count == 0 ? "none" : string.Join(",", selected)));
                };
                Button(-.31f, -.08f, "Add system", () => toggle(true));
                Button(-.10f, -.08f, "Remove system", () => toggle(false));
                Button(.11f, -.08f, "All systems", () => Run("isolate all"));
                Button(.32f, -.08f, "No systems", () => Run("isolate none"));
                Button(
                    -.31f,
                    .02f,
                    "Damage overview",
                    () =>
                    {
                        Run("isolate all");
                        Run("filter faults");
                    }
                );
                Button(-.10f, .02f, "Propulsion", () => Run("isolate thrusters,power,gas,control"));
                Button(.11f, .02f, "Conveyors", () => Run("isolate conveyors,cargo"));
                Button(.32f, .02f, "Show all states", () => Run("filter all"));
                var style = Combo(-.16f, .12f, new[]
                {
                    "hidden",
                    "wireframe",
                    "transparent",
                    "highlighted"
                }, 1);
                Button(
                    .21f,
                    .12f,
                    "Apply style",
                    () =>
                    {
                        int index = (int)choice.GetSelectedKey();
                        var values = owner.controls.SystemStyles.ToCharArray();
                        if (index == 0)
                        {
                            for (int i = 1; i < values.Length; i++)
                                values[i] = (char)('0' + style.GetSelectedKey());
                        }
                        else
                            values[index] = (char)('0' + style.GetSelectedKey());
                        owner.controls.SystemStyles = new string (values);
                        owner.RefreshReference("System presentation updated; damage markers retain their state colors.");
                        Status(owner.comparisonReport);
                    }
                );
                Label(-.39f, .19f, "Styles affect ship and docked hulls. Save display keeps changes.");
            }

            private string SystemName(string category)
            {
                string name = T(category == "weapons" ? "Offensive" : category == "other" ? "Utility" : category);
                return name.Length == 0 ? name : char.ToUpperInvariant(name[0]) + name.Substring(1);
            }

            private void Library()
            {
                ShipProfile profile;
                try
                {
                    profile = owner.SelectedProfile();
                }
                catch (Exception e)
                {
                    Label(-.39f, -.20f, e.Message);
                    return;
                }

                if (string.IsNullOrEmpty(status))
                    Status("Active baseline: " + profile.ReferenceName + "\nSource: " + profile.Source + " | "
                        + (profile.CreatedUtcTicks == 0
                        ? "Creation date unknown"
                        : new DateTime(profile.CreatedUtcTicks, DateTimeKind.Utc).ToString("u")) + "\n" + profile.BlueprintIdentity);
                Label(-.39f, -.26f, "Current ship name");
                var shipName = new MyGuiControlTextbox(new Vector2(-.12f, -.21f), profile.Name, maxLength: 40, textScale: .65f)
                {
                    Size = new Vector2(.5f, .04f)
                };
                Controls.Add(shipName);
                Button(
                    .32f,
                    -.21f,
                    "Rename ship",
                    () =>
                    {
                        Status(owner.RevisionAction("rename", shipName.Text));
                        RecreateControls(false);
                    }
                );
                Label(-.39f, -.145f, "Version name (for saving or renaming)");
                var name = new MyGuiControlTextbox(new Vector2(-.12f, -.10f), "", maxLength: 40, textScale: .65f)
                {
                    Size = new Vector2(.5f, .04f)
                };
                Controls.Add(name);
                Button(
                    .32f,
                    -.10f,
                    "Save new version",
                    () =>
                    {
                        Status(owner.RevisionAction("save", name.Text));
                        RecreateControls(false);
                    }
                );
                var revisions = new List<ShipProfile>(owner.Revisions(profile).Profiles);
                var names = new List<string>();
                foreach (var r in revisions)
                    names.Add((r.ReferenceId == profile.ReferenceId ? "[Active] " : "") + r.Number + ": " + r.Name + " | " + r.Source + " | " + (r.CreatedUtcTicks == 0
                        ? "date unknown"
                        : new DateTime(r.CreatedUtcTicks, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm") + " UTC"));
                Label(-.39f, -.035f, "Saved versions");
                var selected = Combo(-.12f, .01f, names.ToArray(), 0);
                Action<string> revision = verb =>
                {
                    int i = (int)selected.GetSelectedKey();
                    if (i < 0 || i >= revisions.Count)
                    {
                        Status("Save or select a version first.");
                        return;
                    }

                    string key = verb + revisions[i].Id;
                    if (confirm != key)
                    {
                        confirm = key;
                        Status("Click again to confirm " + verb + " of " + revisions[i].Name + ".");
                        return;
                    }

                    confirm = null;
                    Status(owner.RevisionAction(verb, revisions[i].Id, name.Text));
                    if (verb == "load" && owner.recoveryIsRevision && owner.recoveryProfile != null)
                    {
                        string message = status;
                        Switch(7);
                        Status(message);
                    }
                    else
                        RecreateControls(false);
                };
                Button(.32f, .01f, "Use version", () => revision("load"));
                Button(.32f, .10f, "Delete version", () => revision("delete"));
                Button(-.21f, .10f, "Rename version", () => revision("rename-version"));
                Button(0, .10f, "Replace version", () => revision("replace"));
                Label(-.39f, .18f, "Replace copies the active baseline into the selected version.");
            }

            private void Alignment()
            {
                Label(-.39f, -.345f, "Drag the preview to look around; buttons adjust the baseline");
                Button(-.30f, .09f, "X -", () => AdjustRecovery("move x-"), .12f);
                Button(-.17f, .09f, "X +", () => AdjustRecovery("move x+"), .12f);
                Button(-.04f, .09f, "Y -", () => AdjustRecovery("move y-"), .12f);
                Button(.09f, .09f, "Y +", () => AdjustRecovery("move y+"), .12f);
                Button(.22f, .09f, "Z -", () => AdjustRecovery("move z-"), .12f);
                Button(.35f, .09f, "Z +", () => AdjustRecovery("move z+"), .12f);
                Button(-.30f, .145f, "Rotate X 90", () => AdjustRecovery("rotate x"), .18f);
                Button(-.10f, .145f, "Rotate Y 90", () => AdjustRecovery("rotate y"), .18f);
                Button(.10f, .145f, "Rotate Z 90", () => AdjustRecovery("rotate z"), .18f);
                Button(
                    .30f,
                    .145f,
                    "Other candidate",
                    () =>
                    {
                        confirm = null;
                        Run("recovery next");
                    },
                    .18f
                );
                Button(-.30f, .20f, "Top", () => RecoveryView.Execute("/mg top"), .12f);
                Button(-.17f, .20f, "Left", () => RecoveryView.Execute("/mg left"), .12f);
                Button(-.04f, .20f, "Zoom out", () => RecoveryView.Execute("/mg out"), .12f);
                Button(.09f, .20f, "Zoom in", () => RecoveryView.Execute("/mg in"), .12f);
                Button(
                    .29f,
                    .20f,
                    "Reset view",
                    () =>
                    {
                        RecoveryView.Execute("/mg reset");
                        RecoveryView.Execute("/mg spin");
                        RecoveryView.Zoom = .85;
                    },
                    .22f
                );
                Button(
                    -.12f,
                    .255f,
                    "Accept recovery",
                    () =>
                    {
                        Run("recovery accept", true);
                        if (owner.recoveryProfile == null)
                        {
                            string message = status;
                            Switch(15);
                            Status(message);
                        }
                    },
                    .22f
                );
                Button(
                    .12f,
                    .255f,
                    "Cancel",
                    () =>
                    {
                        Run("recovery cancel");
                        if (owner.recoveryProfile == null)
                        {
                            string message = status;
                            Switch(15);
                            Status(message);
                        }
                    },
                    .22f
                );
            }

            private void AdjustRecovery(string action)
            {
                confirm = null;
                Status(owner.RecoveryCommand(action));
            }

            private void Profiles()
            {
                var profiles = new List<ShipProfile>(owner.profileStore.Profiles);
                var names = new List<string>();
                foreach (var p in profiles)
                    names.Add(p.Name + " [" + p.Number.ToString() + "]");
                Label(-.39f, -.25f, "Choose a saved ship to use as this grid's baseline.");
                var choice = Combo(0, -.16f, names.ToArray(), 0);
                Func<string> id = () =>
                {
                    int i = (int)choice.GetSelectedKey();
                    if (i < 0 || i >= profiles.Count)
                        throw new InvalidOperationException("Select a saved profile first.");
                    return profiles[i].Id;
                };
                Button(-.21f, -.06f, "Use selected ship", () => Run("profile load " + id()));
                Button(.21f, -.06f, "Find matches", () => Run("profile suggest"));
                Label(-.39f, .025f, "Detach stops comparison; it does not delete your saved ship.");
                Button(-.21f, .09f, "Detach baseline", () => Run("profile detach"));
                Button(
                    .21f,
                    .09f,
                    "Delete selected",
                    () =>
                    {
                        string command = "profile delete " + id();
                        bool confirmed = confirm == command;
                        Run(command, true);
                        if (confirmed && status.StartsWith("Deleted profile ", StringComparison.Ordinal))
                            RecreateControls(false);
                    }
                );
            }

            private void BlueprintBrowser()
            {
                Button(
                    -.28f,
                    -.25f,
                    (browseLocal ? "[x] " : "[ ] ") + "Local",
                    () =>
                    {
                        browseLocal = !browseLocal;
                        RecreateControls(false);
                    }
                );
                Button(
                    0,
                    -.25f,
                    (browseWorkshop ? "[x] " : "[ ] ") + "Workshop",
                    () =>
                    {
                        browseWorkshop = !browseWorkshop;
                        RecreateControls(false);
                    }
                );
                Button(
                    .28f,
                    -.25f,
                    (browseCloud ? "[x] " : "[ ] ") + "Cloud",
                    () =>
                    {
                        browseCloud = !browseCloud;
                        RecreateControls(false);
                    }
                );
                blueprintSearchBox = new MyGuiControlTextbox(new Vector2(-.10f, -.16f), searchText, maxLength: 100, textScale: .65f)
                {
                    Size = new Vector2(.56f, .04f)
                };
                Controls.Add(blueprintSearchBox);
                blueprintSearchBox.TextChanged += box => searchText = box.Text;
                blueprintSearchBox.EnterPressed += SearchBlueprints;
                Button(
                    .32f,
                    -.16f,
                    catalog == null ? "Search" : "Searching...",
                    () => StartBlueprintSearch(blueprintSearchBox)
                );
                blueprintResultButtons = new MyGuiControlButton[BlueprintCatalog.ResultsPerPage];
                for (int row = 0; row < BlueprintCatalog.ResultsPerPage; row++)
                {
                    int resultRow = row;
                    var result = new MyGuiControlButton(
                        new Vector2(0, -.07f + row * .055f),
                        size: new Vector2(.76f, .042f),
                        text: new StringBuilder(),
                        textScale: .62f,
                        onButtonClick: b => LoadBlueprintResult(resultRow)
                    );
                    Controls.Add(result);
                    blueprintResultButtons[row] = result;
                }

                if (localFiles.Length == 0)
                    Label(-.39f, -.04f, "Select sources, enter a search, then press Enter or Search.");
                Button(-.28f, .18f, "Previous", () => ChangeBlueprintPage(-1), .24f);
                blueprintPageLabel = new MyGuiControlLabel(new Vector2(-.11f, .18f), text: "", textScale: .65f);
                Controls.Add(blueprintPageLabel);
                Button(.28f, .18f, "Next", () => ChangeBlueprintPage(1), .24f);
                UpdateBlueprintPage();
            }

            private void ChangeBlueprintPage(int offset)
            {
                browsePage = BlueprintCatalog.ClampPage(browsePage + offset, localFiles.Length);
                UpdateBlueprintPage();
            }

            private void UpdateBlueprintPage()
            {
                if (blueprintResultButtons == null)
                    return;
                browsePage = BlueprintCatalog.ClampPage(browsePage, localFiles.Length);
                int pageStart = BlueprintCatalog.PageStart(browsePage, localFiles.Length);
                for (int row = 0; row < blueprintResultButtons.Length; row++)
                {
                    int index = pageStart + row;
                    bool visible = index < localFiles.Length;
                    blueprintResultButtons[row].Visible = visible;
                    blueprintResultButtons[row].Text = visible ? localNames[index] : "";
                }

                if (blueprintPageLabel != null)
                    blueprintPageLabel.Text = (browsePage + 1) + " / " + BlueprintCatalog.PageCount(localFiles.Length) + " (" + localFiles.Length
                        + ")";
            }

            private void LoadBlueprintResult(int row)
            {
                int index = BlueprintCatalog.PageStart(browsePage, localFiles.Length) + row;
                if (index < 0 || index >= localFiles.Length)
                    return;
                path = localFiles[index];
                Run("blueprint load \"" + path + "\"");
                Switch(2);
            }

            private void SearchBlueprints(MyGuiControlTextbox searchBox)
            {
                StartBlueprintSearch(searchBox);
            }

            private void StartBlueprintSearch(MyGuiControlTextbox searchBox)
            {
                if (catalog != null)
                    return;
                searchText = searchBox.Text;
                if (string.IsNullOrWhiteSpace(searchText))
                {
                    Status("Search can't be empty. Enter a blueprint name or part of a name and try again.");
                    FocusedControl = searchBox;
                    return;
                }

                if (!browseLocal && !browseWorkshop && !browseCloud)
                {
                    Status("Select at least one source.");
                    return;
                }

                bool local = browseLocal, workshop = browseWorkshop;
                string query = searchText;
                var cloud = browseCloud ? CloudBlueprints.List() : new string[0];
                string localRoot = Sandbox.Game.GUI.MyBlueprintUtils.BLUEPRINT_FOLDER_LOCAL;
                string workshopRoot = Path.Combine(
                    new DirectoryInfo(VRage.FileSystem.MyFileSystem.ContentPath).Parent.Parent.Parent.FullName,
                    "workshop",
                    "content",
                    "244850"
                );
                catalog = Task.Run(() =>
                {
                    var sources = new List<BlueprintCatalog.Listing>();
                    if (local)
                        sources.Add(BlueprintCatalog.Scan(localRoot));
                    if (workshop)
                        sources.Add(BlueprintCatalog.Scan(workshopRoot));
                    sources.Add(new BlueprintCatalog.Listing { Paths = cloud, Names = Array.ConvertAll(cloud, BlueprintCatalog.CloudName) });
                    return BlueprintCatalog.Merge(sources.ToArray(), query);
                });
                Status("Searching selected sources. Click a result to load it.");
            }

            private void Blueprints()
            {
                Label(-.39f, -.26f, "1. Find a blueprint or paste an XML path below");
                Button(0, -.18f, "Find blueprint", () => Switch(18), .34f);
                if (owner.blueprint == null)
                {
                    Label(-.39f, -.025f, "Or paste the path to a blueprint XML file:");
                    pathBox = new MyGuiControlTextbox(new Vector2(-.08f, .025f), path, maxLength: 2048, textScale: .65f)
                    {
                        Size = new Vector2(.60f, .04f)
                    };
                    Controls.Add(pathBox);
                    Button(
                        .32f,
                        .025f,
                        "Load file",
                        () =>
                        {
                            path = pathBox.Text;
                            Run("blueprint load \"" + path + "\"");
                        }
                    );
                    Label(-.39f, .12f, "After loading, choose the grid and its main cockpit.");
                    Label(-.39f, .18f, "XML only. For PB/archives, save a local copy in-game and select bp.sbc.");
                    return;
                }

                var grids = new List<string>();
                foreach (var g in owner.blueprint.Grids)
                    grids.Add(g.Name);
                Label(-.39f, -.07f, "2. Choose the blueprint grid");
                var gridChoice = Combo(-.12f, -.025f, grids.ToArray(), owner.blueprintGrid, i => Run("blueprint grid " + (i + 1)));
                Button(.32f, -.025f, "Prepare grid", () => Run("blueprint grid " + (gridChoice.GetSelectedKey() + 1)));
                var anchors = new List<string>();
                foreach (var a in owner.blueprintAnchors)
                    anchors.Add(a.Name);
                Label(-.39f, .035f, "3. Choose the cockpit that matches the one you are sitting in");
                var anchorChoice = Combo(-.12f, .08f, anchors.ToArray(), 0);
                Button(.32f, .08f, "Omitted blocks", () => Status(owner.OmittedReport()));
                Button(-.21f, .17f, "Preview", () => Run("blueprint preview " + (anchorChoice.GetSelectedKey() + 1)));
                Button(0, .17f, "Use baseline", () => Run("blueprint accept", true));
                Button(.21f, .17f, "Cancel preview", () => Run("blueprint cancel"));
            }

            public override bool Draw()
            {
                bool visible = base.Draw();
                if (visible)
                    owner.DrawSafely();
                return visible;
            }

            public override bool Update(bool hasFocus)
            {
                var mouseInput = VRage.Input.MyInput.Static;
                if (!hasFocus || !RecoveryFocus || mouseInput == null)
                    draggingRecovery = false;
                else
                {
                    var cursor = MyGuiManager.GetScreenCoordinateFromNormalizedCoordinate(MyGuiManager.MouseCursorPosition);
                    var size = MyGuiManager.GetScreenSizeFromNormalizedSize(new Vector2(.78f, .37f));
                    float side = Math.Min(size.X, size.Y);
                    var center = MyGuiManager.GetScreenCoordinateFromNormalizedCoordinate(new Vector2(.5f, .365f));
                    bool overPreview = Math.Abs(cursor.X - center.X) <= side / 2 && Math.Abs(cursor.Y - center.Y) <= side / 2;
                    if (overPreview)
                    {
                        int wheel = mouseInput.DeltaMouseScrollWheelValue();
                        if (wheel != 0)
                            RecoveryView.Execute(wheel > 0 ? "/mg in" : "/mg out");
                    }

                    if (!mouseInput.IsLeftMousePressed())
                        draggingRecovery = false;
                    if (mouseInput.IsNewLeftMousePressed() && overPreview)
                    {
                        draggingRecovery = true;
                        recoveryMouse = cursor;
                    }

                    if (draggingRecovery)
                    {
                        var delta = cursor - recoveryMouse;
                        recoveryMouse = cursor;
                        // Independent inspection camera: never modify the recovery anchor or saved HUD.
                        if (side > 0 && delta.LengthSquared() > 0)
                            RecoveryView.RotateView(delta.X / side * Math.PI, -delta.Y / side * Math.PI);
                    }
                }

                if (hasFocus && captureShortcut >= 0 && VRage.Input.MyInput.Static != null)
                {
                    var input = VRage.Input.MyInput.Static;
                    foreach (VRage.Input.MyKeys key in Enum.GetValues(typeof(VRage.Input.MyKeys)))
                    {
                        string name = key.ToString();
                        if (key == VRage.Input.MyKeys.None
                            || name.Contains("Shift")
                            || name.Contains("Control")
                            || name.Contains("Alt")
                            || name.Contains("Menu")
                            || !input.IsNewKeyPressed(key))
                            continue;
                        if (key == VRage.Input.MyKeys.Escape)
                        {
                            captureShortcut = -1;
                            Status("Shortcut capture canceled.");
                            break;
                        }

                        string value = (input.IsAnyCtrlKeyPressed() ? "Ctrl+" : "") + (input.IsAnyAltKeyPressed() ? "Alt+" : "")
                            + (input.IsAnyShiftKeyPressed() ? "Shift+" : "") + name;
                        Status(owner.SaveHotkey(captureShortcut, value));
                        if (shortcutBox != null)
                            shortcutBox.Text = value;
                        captureShortcut = -1;
                        break;
                    }
                }

                if (catalog != null && catalog.IsCompleted)
                {
                    try
                    {
                        var listing = catalog.GetAwaiter().GetResult();
                        localFiles = listing.Paths;
                        localNames = listing.Names;
                        if (localFiles.Length > 0 && Array.IndexOf(localFiles, path) < 0)
                            path = localFiles[0];
                        browsePage = 0;
                        Status(localFiles.Length + " blueprints found. Click a title to load it.");
                    }
                    catch (Exception e)
                    {
                        Status(e.Message);
                    }

                    catalog = null;
                    if (tab == 18)
                    {
                        RecreateControls(false);
                    }
                }

                if (tab == 2 && (refreshBlueprint || blueprintUiRevision != owner.blueprintRevision))
                {
                    refreshBlueprint = false;
                    blueprintUiRevision = owner.blueprintRevision;
                    if (pathBox != null)
                        path = pathBox.Text;
                    Status(owner.blueprintStatus);
                    RecreateControls(false);
                }

                return base.Update(hasFocus);
            }
        }
    }
}
