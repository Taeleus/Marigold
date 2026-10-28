using System;
using System.Collections.Generic;
using Sandbox.Graphics.GUI;

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

            private void ConfirmCustomAction(string key, Action action, string prompt = null)
            {
                if (confirm != key)
                {
                    confirm = key;
                    Status(prompt ?? "Click the same action again to confirm. No changes yet.");
                    return;
                }
                confirm = null;
                action();
            }

            private void CustomBars()
            {
                Intro("Watch one named block or a terminal group. Each custom bar belongs to this ship and its saved baseline versions.");
                ShipProfile profile;
                try
                {
                    profile = owner.SelectedProfile();
                    if (profile.Blocks == null)
                        throw new InvalidOperationException("Capture or load a baseline before adding a custom bar.");
                }
                catch (InvalidOperationException e)
                {
                    Note(e.Message);
                    PrimaryAction("Set up a baseline", () => Switch(1));
                    return;
                }
                PrimaryAction("Add custom bar...", () =>
                {
                    if (profile.CustomBars.Count >= CustomHealthBars.MaximumBars)
                        throw new InvalidOperationException("Limit: 16 custom bars per ship.");
                    customSources = null;
                    customSourceIndex = 0;
                    customName = T("My custom bar");
                    Switch(21);
                });
                if (profile.CustomBars.Count == 0)
                {
                    Note("No custom bars yet. Add one for a block or terminal group you want to watch.");
                    return;
                }
                Heading("Your custom bars");
                var names = new List<string>();
                foreach (var bar in profile.CustomBars)
                    names.Add(bar.Name + " (" + bar.Members.Length + " " + T("saved blocks") + ")");
                int selected = profile.CustomBars.FindIndex(item => item.Id == customBarId);
                if (selected < 0)
                    customBarId = profile.CustomBars[selected = 0].Id;
                ChoiceField("Custom health bar", names.ToArray(), selected, i =>
                {
                    customBarId = profile.CustomBars[i].Id;
                    confirm = null;
                });
                ActionControl("Edit name and layout", () =>
                {
                    customBarDraft = SelectedCustomBar().Copy();
                    Switch(22);
                });
                Note("Damage and repairs update automatically. Destroyed blocks remain tracked as missing.");
                Heading("After changing a terminal group");
                Note("Use Update tracked blocks only when you want to add or remove tracked group members. It is not needed for repairs.");
                ActionControl("Update tracked blocks", () =>
                {
                    var bar = SelectedCustomBar();
                    if (string.IsNullOrEmpty(bar.GroupName))
                    {
                        Status("Single-block bars update damage and repairs automatically. Create another bar to track a different block.");
                        return;
                    }
                    ConfirmCustomAction("refresh-custom-" + bar.Id, () =>
                    {
                        string message = owner.RefreshCustomGroup(bar);
                        RecreateControls(false);
                        Status(message);
                    }, "Update the tracked list to the group's current blocks? Members absent from that list stop counting. Click again to confirm.");
                });
                Heading("Remove a bar");
                ActionControl("Delete custom bar", () =>
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
                Note("Deleting a custom bar leaves the ship baseline unchanged.");
            }

            private void CreateCustomBar()
            {
                Intro("Choose what to watch, then give the bar a name. Only blocks present in your saved baseline can be tracked.");
                var name = TextField("Bar name", customName, 128);
                MyGuiControlTextbox search = null;
                ChoiceField("Track", new[] { "Single block", "Terminal group" }, customGroupSource ? 1 : 0, i =>
                {
                    customName = name.Text;
                    customSearch = search == null ? customSearch : search.Text;
                    customGroupSource = i == 1;
                    customSources = null;
                    customSourceIndex = 0;
                    RecreateControls(false);
                });
                search = TextField("Find by name", customSearch, 128);
                Action find = () =>
                {
                    customName = name.Text;
                    customSearch = search.Text;
                    customSources = null;
                    customSourceIndex = 0;
                    RecreateControls(false);
                };
                search.EnterPressed += box => find();
                ActionControl("Find", find);
                if (customSources == null)
                    customSources = owner.CustomBarSources(customGroupSource, customSearch);
                var names = new string[customSources.Count];
                for (int i = 0; i < names.Length; i++)
                    names[i] = customSources[i].Name;
                var choice = ChoiceField(customGroupSource ? "Terminal group" : "Block", names,
                    customSourceIndex, i => customSourceIndex = i);
                if (names.Length == 0)
                    Note("No matching blocks or groups. Try another name or check your baseline.");
                Note("Damage and repairs update automatically. The tracked block list stays fixed until you choose to update it.");
                PrimaryAction("Create bar", () =>
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
                ActionControl("Cancel", () => Switch(20));
            }

            private void CustomBarLayout()
            {
                if (customBarDraft == null)
                    customBarDraft = SelectedCustomBar().Copy();
                var layout = customBarDraft.Layout;
                Intro("Changes preview on the HUD. Save bar keeps this ship's edits; leaving this page discards unsaved edits.");
                var name = TextField("Bar name", customBarDraft.Name, 128);
                Action rememberName = () => customBarDraft.Name = name.Text;
                CheckField("Show this bar", layout.Enabled, value =>
                {
                    rememberName();
                    layout.Enabled = value;
                });
                CheckField("Show its label", layout.Label, value =>
                {
                    rememberName();
                    layout.Label = value;
                });
                var applyLayout = BarLayoutFields(layout);
                ActionRow("Preview layout", () =>
                {
                    rememberName();
                    applyLayout();
                    Status("Preview updated. Save bar keeps these changes for this ship.");
                    InspectHud();
                }, "Save bar", () =>
                {
                    rememberName();
                    applyLayout();
                    customBarDraft.Name = customBarDraft.Name.Trim();
                    Status(owner.SaveCustomBar(customBarDraft));
                });
                Heading("Move in small steps");
                ActionRow("Left", () =>
                {
                    rememberName();
                    MoveBar(layout, -.02, 0);
                }, "Right", () =>
                {
                    rememberName();
                    MoveBar(layout, .02, 0);
                });
                ActionRow("Up", () =>
                {
                    rememberName();
                    MoveBar(layout, 0, -.02);
                }, "Down", () =>
                {
                    rememberName();
                    MoveBar(layout, 0, .02);
                });
                ActionControl("Reset layout", () =>
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
                Note("Custom bars use Save bar, separately from Save display.");
            }

            private void PowerSections()
            {
                Intro("Give selected power sources their own labeled section within the Power bar.");
                var layout = owner.controls.HealthBars.Bars[2];
                for (int i = 0; i < 3; i++)
                {
                    int bit = 1 << i;
                    CheckField(ShipHealth.PowerNames[i], (layout.PowerMask & bit) != 0, value =>
                    {
                        layout.PowerMask = value ? layout.PowerMask | bit : layout.PowerMask & ~bit;
                    });
                }
                Note("Unchecked sources stay in Other. Every baseline power block is still counted.");
                Note("Use Save display to keep these choices.");
            }
        }
    }
}
