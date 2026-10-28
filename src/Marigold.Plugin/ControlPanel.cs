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

        private sealed partial class Panel : MyGuiScreenBase
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
                new Vector2(.96f, .96f),
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
                    visualStyle: VRage.Game.MyGuiControlButtonStyleEnum.Rectangular,
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
                    output.AppendText(string.IsNullOrEmpty(status) ? T(SaveHint) : status);
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
                RememberScroll();
                if (tab == 22)
                    customBarDraft = null;
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
                RememberScroll();
                if (tab == 22)
                    customBarDraft = null;
                if (RecoveryFocus)
                    owner.EndPanelRecovery();
                if (pathBox != null)
                    path = pathBox.Text;
                tab = history.Pop();
                confirm = null;
                Status("");
                RecreateControls(false);
            }

            private static readonly string[] PageNames =
            {
                "Ship",
                "Baselines",
                "Import a blueprint",
                "View and zoom",
                "Ship display",
                "Systems and filters",
                "Baseline versions",
                "Recovery alignment",
                "Health bars",
                "Appearance",
                "Saved ships",
                "Keyboard shortcuts",
                "Preferences",
                "Save and share",
                "Damage report",
                "Recovery tools",
                "State colors",
                "Share a display setup",
                "Find a blueprint",
                "Performance options",
                "Custom health bars",
                "Create a custom bar",
                "Edit a custom bar",
                "Power bar sections",
                "Background systems",
                "Health bar colors",
                "Choose a color"
            };

            public override void RecreateControls(bool constructor)
            {
                RememberScroll();
                base.RecreateControls(constructor);
                scroll = null;
                content = null;
                draggingRecovery = false;
                pathBox = null;
                blueprintSearchBox = null;
                blueprintResultButtons = null;
                blueprintPageLabel = null;
                captureShortcut = -1;
                shortcutBox = null;
                pickerHex = null;
                saturationPicker = huePicker = null;
                if (RecoveryFocus)
                {
                    Label(-.40f, -.40f, "Recovery alignment");
                    if (history.Count > 0)
                        Button(-.31f, .40f, "Back", Back);
                    Button(.32f, .40f, "Close", () => CloseScreen());
                    output = new MyGuiControlMultilineText(
                        new Vector2(0, .325f), new Vector2(.8f, .085f), font: "White", textScale: .60f,
                        drawScrollbarH: false, contents: new StringBuilder(string.IsNullOrEmpty(status) ? T(SaveHint) : status)
                    );
                    Controls.Add(output);
                    Alignment();
                    return;
                }
                CreateWorkspace();
                switch (tab)
                {
                    case 0: Display(); break;
                    case 1: Baseline(); break;
                    case 2: Blueprints(); break;
                    case 3: Views(); break;
                    case 4: Appearance(); break;
                    case 5: Systems(); break;
                    case 6: Library(); break;
                    case 8: HealthBars(); break;
                    case 9: Customize(); break;
                    case 10: Profiles(); break;
                    case 11: Shortcuts(); break;
                    case 12: Preferences(); break;
                    case 13: SavedDisplays(); break;
                    case 14: Diagnostics(); break;
                    case 15: Recovery(); break;
                    case 16: Colors(); break;
                    case 17: ClipboardConfig(); break;
                    case 18: BlueprintBrowser(); break;
                    case 19: Performance(); break;
                    case 20: CustomBars(); break;
                    case 21: CreateCustomBar(); break;
                    case 22: CustomBarLayout(); break;
                    case 23: PowerSections(); break;
                    case 24: ContextSettings(); break;
                    case 25: BarColors(); break;
                    case 26: ColorPicker(); break;
                }
                FinishWorkspace();
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

            public override bool Draw()
            {
                bool visible = base.Draw();
                if (visible && !PreviewingHud)
                    owner.DrawSafely();
                return visible;
            }

            public override bool Update(bool hasFocus)
            {
                if (PreviewingHud)
                    return base.Update(false);
                if (!hasFocus)
                {
                    saturationPicker?.CancelDrag();
                    huePicker?.CancelDrag();
                }
                var mouseInput = VRage.Input.MyInput.Static;
                if (!hasFocus || !RecoveryFocus || mouseInput == null)
                    draggingRecovery = false;
                else
                {
                    var cursor = MyGuiManager.GetScreenCoordinateFromNormalizedCoordinate(MyGuiManager.MouseCursorPosition);
                    var preview = PreviewBounds;
                    float side = preview.Width;
                    var center = new Vector2(preview.X + side / 2, preview.Y + side / 2);
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
