using System;
using System.Collections.Generic;
using System.Globalization;
using Sandbox.Graphics.GUI;
using VRageMath;

namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private sealed partial class Panel
        {
            private string customBarId;
            private CustomHealthBar customBarDraft;
            private bool customGroupSource;
            private string customSearch = "";
            private string customName = "My custom bar";
            private int customSourceIndex;
            private List<CustomBarSource> customSources;

            public CustomHealthBar CustomBarPreview
            {
                get { return tab == 22 ? customBarDraft : null; }
            }

            private CustomHealthBar SelectedCustomBar()
            {
                var bar = owner.SelectedProfile().CustomBars.Find(item => item.Id == customBarId);
                if (bar == null)
                    throw new InvalidOperationException("Select a custom bar first.");
                return bar;
            }

            private void ConfirmCustomAction(string key, Action action)
            {
                if (confirm != key)
                {
                    confirm = key;
                    Status("Click the same action again to confirm. No changes yet.");
                    return;
                }
                confirm = null;
                action();
            }

            private void CustomBars()
            {
                ShipProfile profile;
                try
                {
                    profile = owner.SelectedProfile();
                    if (profile.Blocks == null)
                        throw new InvalidOperationException("Capture or load a baseline before adding a custom bar.");
                }
                catch (InvalidOperationException e)
                {
                    Label(-.39f, -.23f, e.Message);
                    return;
                }
                Label(-.39f, -.26f, "Track one block or an existing terminal group on this ship.");
                FieldLabel(-.17f, -.21f, "Custom health bar");
                var names = new List<string>();
                foreach (var bar in profile.CustomBars)
                    names.Add(bar.Name + " (" + bar.Members.Length + " " + T("saved blocks") + ")");
                int selected = profile.CustomBars.FindIndex(item => item.Id == customBarId);
                if (selected < 0 && profile.CustomBars.Count > 0)
                    customBarId = profile.CustomBars[selected = 0].Id;
                Combo(0, -.17f, names.ToArray(), Math.Max(0, selected), i =>
                {
                    customBarId = profile.CustomBars[i].Id;
                    confirm = null;
                });
                Button(-.21f, -.07f, "Add custom bar...", () =>
                {
                    if (profile.CustomBars.Count >= CustomHealthBars.MaximumBars)
                        throw new InvalidOperationException("Limit: 16 custom bars per ship.");
                    customSources = null;
                    customSourceIndex = 0;
                    customName = T("My custom bar");
                    Switch(21);
                });
                Button(.21f, -.07f, "Edit custom bar...", () =>
                {
                    customBarDraft = SelectedCustomBar().Copy();
                    Switch(22);
                });
                Button(-.21f, .03f, "Refresh group members", () =>
                {
                    var bar = SelectedCustomBar();
                    if (string.IsNullOrEmpty(bar.GroupName))
                    {
                        Status("Single-block selections stay fixed. Create a new bar to choose a different block.");
                        return;
                    }
                    ConfirmCustomAction("refresh-custom-" + bar.Id, () =>
                    {
                        string message = owner.RefreshCustomGroup(bar);
                        RecreateControls(false);
                        Status(message);
                    });
                });
                Button(.21f, .03f, "Delete custom bar", () =>
                {
                    var bar = SelectedCustomBar();
                    ConfirmCustomAction("delete-custom-" + bar.Id, () =>
                    {
                        string message = owner.DeleteCustomBar(bar.Id);
                        customBarId = null;
                        RecreateControls(false);
                        Status(message);
                    });
                });
                Label(-.39f, .11f, "Missing members stay counted until you deliberately refresh the group.");
                Label(-.39f, .17f, "Custom bars save with this ship and its baseline versions.");
            }

            private void CreateCustomBar()
            {
                FieldLabel(-.36f, -.26f, "Bar name", .72f);
                var name = new MyGuiControlTextbox(new Vector2(0, -.21f), customName, maxLength: 128, textScale: .65f)
                {
                    Size = new Vector2(.72f, .04f)
                };
                Controls.Add(name);
                FieldLabel(-.38f, -.155f, "Selection type");
                FieldLabel(.025f, -.155f, "Find by name", .23f);
                var search = new MyGuiControlTextbox(new Vector2(.145f, -.115f), customSearch, maxLength: 128, textScale: .65f)
                {
                    Size = new Vector2(.24f, .04f)
                };
                Controls.Add(search);
                Combo(-.21f, -.115f, new[] { "Single block", "Terminal group" }, customGroupSource ? 1 : 0, i =>
                {
                    customName = name.Text;
                    customSearch = search.Text;
                    customGroupSource = i == 1;
                    customSources = null;
                    customSourceIndex = 0;
                    RecreateControls(false);
                });
                Button(.35f, -.115f, "Find", () =>
                {
                    customName = name.Text;
                    customSearch = search.Text;
                    customSources = null;
                    customSourceIndex = 0;
                    RecreateControls(false);
                }, .13f);
                if (customSources == null)
                    customSources = owner.CustomBarSources(customGroupSource, customSearch);
                var names = new string[customSources.Count];
                for (int i = 0; i < names.Length; i++)
                    names[i] = customSources[i].Name;
                FieldLabel(-.36f, -.055f, customGroupSource ? "Terminal group" : "Block", .72f);
                var choice = Combo(0, -.015f, names, customSourceIndex, i => customSourceIndex = i);
                choice.Size = new Vector2(.72f, .04f);
                Label(-.39f, .06f, "Only matching saved-baseline blocks become members.");
                Label(-.39f, .105f, "Destroyed members remain tracked even if the terminal group disappears.");
                Button(-.21f, .18f, "Create bar", () =>
                {
                    int index = (int)choice.GetSelectedKey();
                    if (index < 0 || index >= customSources.Count)
                        throw new InvalidOperationException("Choose a block or terminal group first.");
                    if (string.IsNullOrWhiteSpace(name.Text))
                        throw new InvalidOperationException("Enter a name for this bar.");
                    var source = customSources[index];
                    var bar = CustomHealthBars.Create(name.Text, source.GroupName, owner.CustomBarMembers(source));
                    bar.Layout.X = .45;
                    bar.Layout.Y = .20 + owner.SelectedProfile().CustomBars.Count * .045;
                    string message = owner.SaveCustomBar(bar);
                    customBarId = bar.Id;
                    customBarDraft = bar.Copy();
                    Switch(22);
                    Status(message);
                });
                Button(.21f, .18f, "Cancel", () => Switch(20));
            }

            private void CustomBarLayout()
            {
                if (customBarDraft == null)
                    customBarDraft = SelectedCustomBar().Copy();
                var layout = customBarDraft.Layout;
                FieldLabel(-.36f, -.26f, "Bar name", .72f);
                var name = new MyGuiControlTextbox(new Vector2(0, -.215f), customBarDraft.Name, maxLength: 128, textScale: .65f)
                {
                    Size = new Vector2(.72f, .04f)
                };
                Controls.Add(name);
                Action rememberName = () => customBarDraft.Name = name.Text;
                Button(-.21f, -.135f, layout.Enabled ? "Hide bar" : "Show bar", () =>
                {
                    rememberName();
                    layout.Enabled = !layout.Enabled;
                    RecreateControls(false);
                });
                Button(.21f, -.135f, layout.Label ? "Hide label" : "Show label", () =>
                {
                    rememberName();
                    layout.Label = !layout.Label;
                    RecreateControls(false);
                });
                FieldLabel(-.38f, -.085f, "Setting");
                FieldLabel(.015f, -.085f, "Value", .19f);
                var fields = new[] { "x", "y", "width", "height", "opacity" };
                var field = Combo(-.21f, -.045f, new[]
                {
                    "Horizontal position", "Vertical position", "Width", "Height", "Opacity"
                }, 0);
                var value = new MyGuiControlTextbox(new Vector2(.11f, -.045f), layout.X.ToString(CultureInfo.InvariantCulture), maxLength: 24, textScale: .65f)
                {
                    Size = new Vector2(.19f, .04f)
                };
                Controls.Add(value);
                field.ItemSelected += () => value.Text = layout.Encode().Split(',')[(int)field.GetSelectedKey() + 2];
                Button(.32f, -.045f, "Apply", () =>
                {
                    rememberName();
                    layout.Set(fields[(int)field.GetSelectedKey()], value.Text);
                    Status("Preview updated. Save bar keeps these changes for this ship.");
                });
                Button(-.31f, .04f, "Left", () =>
                {
                    rememberName();
                    layout.X = Math.Max(0, layout.X - .02);
                    RecreateControls(false);
                });
                Button(-.10f, .04f, "Right", () =>
                {
                    rememberName();
                    layout.X = Math.Min(1, layout.X + .02);
                    RecreateControls(false);
                });
                Button(.11f, .04f, "Up", () =>
                {
                    rememberName();
                    layout.Y = Math.Max(0, layout.Y - .02);
                    RecreateControls(false);
                });
                Button(.32f, .04f, "Down", () =>
                {
                    rememberName();
                    layout.Y = Math.Min(1, layout.Y + .02);
                    RecreateControls(false);
                });
                Button(-.21f, .13f, "Save bar", () =>
                {
                    rememberName();
                    customBarDraft.Name = customBarDraft.Name.Trim();
                    Status(owner.SaveCustomBar(customBarDraft));
                });
                Button(.21f, .13f, "Reset layout", () =>
                {
                    rememberName();
                    int index = owner.SelectedProfile().CustomBars.FindIndex(bar => bar.Id == customBarDraft.Id);
                    customBarDraft.Layout = new HealthBarLayout
                    {
                        Enabled = true,
                        X = .45,
                        Y = .20 + Math.Max(0, index) * .045
                    };
                    RecreateControls(false);
                });
                Label(-.39f, .195f, "Preview applies immediately. Save bar keeps this ship's edits.");
            }

            private void PowerSections()
            {
                Label(-.39f, -.25f, "Choose which power sources get their own section.");
                var layout = owner.controls.HealthBars.Bars[2];
                for (int i = 0; i < 3; i++)
                {
                    int bit = 1 << i;
                    float y = -.14f + i * .10f;
                    var check = new MyGuiControlCheckbox
                    {
                        Position = new Vector2(-.30f, y),
                        IsChecked = (layout.PowerMask & bit) != 0
                    };
                    check.IsCheckedChanged += control =>
                    {
                        layout.PowerMask = control.IsChecked ? layout.PowerMask | bit : layout.PowerMask & ~bit;
                    };
                    Controls.Add(check);
                    FieldLabel(-.25f, y, ShipHealth.PowerNames[i], .5f);
                }
                Label(-.39f, .14f, "Unchecked sources stay in Other; every baseline power block is counted.");
                Label(-.39f, .19f, "Use Save display to keep these choices.");
            }
        }
    }
}
