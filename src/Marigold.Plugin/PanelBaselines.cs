using System;
using System.Collections.Generic;
using Sandbox.Graphics.GUI;

namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private sealed partial class Panel
        {
            private void CaptureBaseline()
            {
                bool confirmed = confirm == "capture";
                Run("capture", true);
                if (confirmed)
                    RecreateControls(false);
            }

            private void Baseline()
            {
                Intro("Choose the layout your ship should match. Damage and repairs are compared with this saved baseline.");
                try
                {
                    TextRow(T("Current baseline: ") + owner.SelectedProfile().Name);
                }
                catch (InvalidOperationException)
                {
                    Note("No baseline selected. Capture this ship or import a blueprint to get started.");
                }

                Heading("Create a baseline");
                Note("Capture records the blocks present now. Import a blueprint to include sections that are already missing.");
                ActionRow("Capture this ship", CaptureBaseline, "Import blueprint", () => Switch(2));
                Note("Capture and import preserve the previous baseline in Saved versions. Click capture again to confirm.");

                Heading("Use a saved layout");
                ActionRow("Saved ships", () => Switch(10), "Saved versions", () => Switch(6));
                Note("Choose an existing ship or keep named versions before a refit.");
                ActionRow("Recovery tools", () => Switch(15));
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
                    Intro(e.Message);
                    Note("Capture or select a saved ship before managing its baseline versions.");
                    ActionRow("Capture this ship", CaptureBaseline, "Saved ships", () => Switch(10));
                    return;
                }

                Intro("Keep named copies of your baseline, such as Original or Before refit.");
                TextRow(T("Active baseline: ") + profile.ReferenceName);
                TextRow(T("Source: ") + profile.Source);
                if (profile.CreatedUtcTicks != 0)
                    TextRow(T("Saved: ") + new DateTime(profile.CreatedUtcTicks, DateTimeKind.Utc).ToString("u"));
                if (!string.IsNullOrEmpty(profile.BlueprintIdentity))
                    Note(profile.BlueprintIdentity);
                var shipName = TextField("Ship name", profile.Name, 40);
                ActionRow("Rename ship", () =>
                {
                    Status(owner.RevisionAction("rename", shipName.Text));
                    RecreateControls(false);
                });

                Heading("Save a new version");
                var name = TextField("Version name", "", 40);
                Note("Save new version makes a separate copy. It does not overwrite another version.");
                PrimaryAction("Save new version", () =>
                {
                    Status(owner.RevisionAction("save", name.Text));
                    RecreateControls(false);
                });

                Heading("Existing versions");
                var revisions = new List<ShipProfile>(owner.Revisions(profile).Profiles);
                var names = new List<string>();
                foreach (var revision in revisions)
                    names.Add(revision.Name + (revision.ReferenceId == profile.ReferenceId ? " (" + T("Active") + ")" : "")
                        + " | " + (revision.CreatedUtcTicks == 0 ? T("Date unknown")
                        : new DateTime(revision.CreatedUtcTicks, DateTimeKind.Utc).ToString("yyyy-MM-dd HH:mm") + " UTC"));

                if (revisions.Count == 0)
                {
                    Note("No saved versions yet. Enter a name above to save your first copy.");
                    return;
                }

                var selected = ChoiceField("Saved version", names.ToArray(), 0, index =>
                {
                    confirm = null;
                    if (index >= 0 && index < revisions.Count)
                        Status(T("Source: ") + revisions[index].Source + "\n" + revisions[index].BlueprintIdentity);
                });
                Action<string> revisionAction = verb =>
                {
                    int index = (int)selected.GetSelectedKey();
                    if (index < 0 || index >= revisions.Count)
                    {
                        Status("Save or select a version first.");
                        return;
                    }

                    var revision = revisions[index];
                    string key = verb + revision.Id;
                    if (confirm != key)
                    {
                        confirm = key;
                        string action = verb == "load" ? "Use version" : verb == "delete" ? "Delete version"
                            : verb == "rename-version" ? "Rename version" : "Replace version";
                        Status(T(action) + ": " + revision.Name + ". " + T("Click the same action again to confirm. No changes yet."));
                        return;
                    }

                    confirm = null;
                    Status(owner.RevisionAction(verb, revision.Id, name.Text));
                    if (verb == "load" && owner.recoveryIsRevision && owner.recoveryProfile != null)
                    {
                        string message = status;
                        Switch(7);
                        Status(message);
                    }
                    else
                        RecreateControls(false);
                };
                ActionRow("Use version", () => revisionAction("load"), "Rename version", () => revisionAction("rename-version"));
                Note("Rename uses the version name entered above. These actions require confirmation.");

                Heading("Replace or delete a version");
                Note("Replace copies the active baseline into the selected version and preserves its previous contents. Delete removes only the selected version.");
                ActionRow("Replace version", () => revisionAction("replace"), "Delete version", () => revisionAction("delete"));
            }

            private void Recovery()
            {
                Intro("Recover the saved layout when the original cockpit is gone or automatic matching cannot choose a ship.");
                var profiles = new List<ShipProfile>(owner.profileStore.Profiles);
                var names = new List<string>();
                foreach (var profile in profiles)
                    names.Add(profile.Name + " [" + profile.Number + "]");

                if (profiles.Count == 0)
                {
                    Note("No saved ships are available to recover. Capture a ship or import a blueprint first.");
                    ActionRow("Capture this ship", CaptureBaseline, "Import blueprint", () => Switch(2));
                }
                else
                {
                    var choice = ChoiceField("Saved ship to recover", names.ToArray(), 0);
                    PrimaryAction("Preview recovery", () =>
                    {
                        int index = (int)choice.GetSelectedKey();
                        if (index < 0 || index >= profiles.Count)
                        {
                            Status("Select a saved ship first.");
                            return;
                        }

                        Run("profile force " + profiles[index].Id);
                        if (owner.recoveryProfile != null)
                        {
                            string message = status;
                            Switch(7);
                            Status(message);
                        }
                    });
                    ActionRow("Review alignment", () => Switch(7));
                    Note("Inspect the preview before accepting. Looking around does not change the baseline alignment.");
                }

                Heading("Matching and cockpit");
                ActionRow("Matching status", () => Run("auto status"), "Retry matching", () => Run("auto retry"));
                ActionRow("Set main cockpit", () => Run("anchor"));
                Note("Sit in the cockpit you want to use as the ship's orientation reference before setting it.");

                Heading("Restore or clear data");
                Note("Restore backup replaces the saved database with its backup. Clear baseline removes the current reference. Both require confirmation.");
                ActionRow("Restore backup", () => Run("recover", true), "Clear baseline", () => Run("clear", true));
            }

            private void Profiles()
            {
                Intro("Choose a saved ship to use as this grid's baseline.");
                var profiles = new List<ShipProfile>(owner.profileStore.Profiles);
                var names = new List<string>();
                foreach (var profile in profiles)
                    names.Add(profile.Name + " [" + profile.Number + "]");

                if (profiles.Count == 0)
                {
                    Note("No saved ships yet. Capture the ship you are piloting or import a blueprint.");
                    ActionRow("Capture this ship", CaptureBaseline, "Import blueprint", () => Switch(2));
                    return;
                }

                var choice = ChoiceField("Saved ship", names.ToArray(), 0, i => confirm = null);
                Func<string> selectedId = () =>
                {
                    int index = (int)choice.GetSelectedKey();
                    if (index < 0 || index >= profiles.Count)
                        throw new InvalidOperationException("Select a saved profile first.");
                    return profiles[index].Id;
                };
                PrimaryAction("Use selected ship", () => Run("profile load " + selectedId()));
                ActionRow("Find matches", () => Run("profile suggest"));

                Heading("Stop comparing this ship");
                Note("Detach stops comparison without deleting your saved ship.");
                ActionRow("Detach baseline", () => Run("profile detach"));

                Heading("Delete a saved ship");
                Note("Delete removes the selected ship profile. Click again to confirm.");
                ActionRow("Delete selected", () =>
                {
                    string command = "profile delete " + selectedId();
                    bool confirmed = confirm == command;
                    Run(command, true);
                    if (confirmed && status.StartsWith("Deleted profile ", StringComparison.Ordinal))
                        RecreateControls(false);
                });
            }

            private void BlueprintBrowser()
            {
                Intro("Choose where to search, then enter all or part of the blueprint name.");
                Heading("Search sources");
                CheckField("Local", browseLocal, value => browseLocal = value);
                CheckField("Workshop", browseWorkshop, value => browseWorkshop = value);
                CheckField("Cloud", browseCloud, value => browseCloud = value);
                Note("Workshop search uses blueprints already cached on your computer.");
                blueprintSearchBox = TextField("Blueprint name", searchText, 100);
                blueprintSearchBox.TextChanged += box => searchText = box.Text;
                blueprintSearchBox.EnterPressed += SearchBlueprints;
                var search = ActionControl(catalog == null ? "Search" : "Searching...", () => StartBlueprintSearch(blueprintSearchBox));
                search.Enabled = catalog == null;

                Heading("Results");
                if (localFiles.Length == 0)
                {
                    Note(catalog == null
                        ? "No results to show. Choose sources and search for a blueprint name."
                        : "Searching selected sources...");
                    blueprintResultButtons = new MyGuiControlButton[0];
                    blueprintPageLabel = null;
                    return;
                }

                blueprintPageLabel = TextRow("");
                blueprintResultButtons = new MyGuiControlButton[BlueprintCatalog.ResultsPerPage];
                for (int row = 0; row < BlueprintCatalog.ResultsPerPage; row++)
                {
                    int resultRow = row;
                    blueprintResultButtons[row] = ActionControl("", () => LoadBlueprintResult(resultRow));
                }
                ActionRow("Previous", () => ChangeBlueprintPage(-1), "Next", () => ChangeBlueprintPage(1));
                Note("Select a result to choose its grid and cockpit.");
                UpdateBlueprintPage();
            }

            private void Blueprints()
            {
                Intro("Use a blueprint as the layout your ship should match. Source blueprint files are never changed.");
                Heading("Choose a blueprint");
                PrimaryAction("Find blueprint", () => Switch(18));
                if (owner.blueprint == null)
                {
                    Note("Or load a blueprint XML file directly:");
                    pathBox = TextField("Blueprint XML path", path, 2048);
                    ActionRow("Load file", () =>
                    {
                        path = pathBox.Text;
                        Run("blueprint load \"" + path + "\"");
                    });
                    Note("After loading, choose the grid and its main cockpit. For protobuf-only or archived blueprints, save a local copy in-game and select its XML bp.sbc file.");
                    return;
                }

                Heading("Choose the grid");
                var grids = new List<string>();
                foreach (var grid in owner.blueprint.Grids)
                    grids.Add(grid.Name);
                var gridChoice = ChoiceField("Blueprint grid", grids.ToArray(), owner.blueprintGrid,
                    index => Run("blueprint grid " + (index + 1)));
                ActionRow("Prepare grid", () => Run("blueprint grid " + (gridChoice.GetSelectedKey() + 1)));

                Heading("Choose the cockpit");
                Note("Select the blueprint cockpit that matches the one you are sitting in.");
                var anchors = new List<string>();
                foreach (var anchor in owner.blueprintAnchors)
                    anchors.Add(anchor.Name);
                var anchorChoice = ChoiceField("Matching cockpit", anchors.ToArray(), 0);
                ActionRow("Omitted blocks", () => Status(owner.OmittedReport()));
                if (anchors.Count == 0)
                    Note("No matching cockpits are available. Check the chosen grid and prepare it before previewing.");

                Heading("Inspect and use the baseline");
                var preview = ActionControl("Preview", () => Run("blueprint preview " + (anchorChoice.GetSelectedKey() + 1)));
                preview.Enabled = anchors.Count > 0;
                Note("Inspect the preview, then choose Use baseline and confirm. Cancel preview leaves your saved baseline unchanged.");
                ActionRow("Use baseline", () => Run("blueprint accept", true), "Cancel preview", () => Run("blueprint cancel"));
            }
        }
    }
}
