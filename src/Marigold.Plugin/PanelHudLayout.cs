using System;
using System.Collections.Generic;
using System.Globalization;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private sealed partial class Panel
        {
            private void ArrangeHud()
            {
                if (hudPreview != null)
                    return;

                var screen = new HudLayoutScreen(this, owner.CaptureHudLayout());
                OpenHudChild(screen, () =>
                {
                    if (screen.Applied)
                    {
                        RecreateControls(false);
                        Status(screen.CustomBarsSaved
                            ? "Custom bars saved for this ship. Use Save display to keep the schematic and system bars."
                            : "Layout applied. Use Save display to keep it.");
                    }
                });
            }

            private sealed class HudLayoutScreen : MyGuiScreenBase
            {
                private static readonly Vector2 ToolbarSize = new Vector2(.94f, .34f);
                private static readonly Vector4 InvalidValueColor = new Vector4(1, .65f, .55f, 1);

                private sealed class NumericField
                {
                    public MyGuiControlLabel Label, Unit;
                    public MyGuiControlSlider Slider;
                    public MyGuiControlTextbox Input;
                    public double Minimum, Maximum;
                    public Func<double> Read;
                    public Action<double> Write;
                }

                private readonly Panel editor;
                private readonly HudLayoutDraft draft;
                private readonly List<MyGuiControlBase> toolbar = new List<MyGuiControlBase>();
                private readonly NumericField[] fields = new NumericField[8];
                private MyGuiControlCombobox element;
                private MyGuiControlCheckbox showElement, showLabel, simpleLabel;
                private MyGuiControlLabel hint, simpleLabelText;
                private int selected;
                private bool controlsAtTop, syncing, dragging, dragMoved, resizing;
                private Vector2 dragOrigin, dragOffset, dragScreen, dragCursor;
                private Vector2 resizeAnchor, resizeBodySize;

                public bool Applied { get; private set; }
                public bool CustomBarsSaved { get; private set; }

                private float ToolbarY => controlsAtTop ? -.325f : .325f;
                private int SystemCount => draft.Bars.Bars.Length;
                private int ElementCount => 1 + SystemCount + draft.CustomBars.Count;
                private bool Textured => draft.Appearance.Rendering == HudAppearance.Textured;

                public HudLayoutScreen(Panel editor, HudLayoutDraft draft)
                    : base(new Vector2(.5f, .5f), null, Vector2.One)
                {
                    this.editor = editor;
                    this.draft = draft;
                    CanHideOthers = true;
                    EnabledBackgroundFade = false;
                    RecreateControls(true);
                }

                public override string GetFriendlyName()
                {
                    return "MarigoldHudLayout";
                }

                public override void RecreateControls(bool constructor)
                {
                    base.RecreateControls(constructor);
                    toolbar.Clear();
                    AddLabel("Arrange HUD", -.445f, -.132f, .23f, .83f);
                    AddButton("Move controls", -.024f, -.132f, .195f, MoveToolbar);
                    AddButton("Cancel", .16f, -.132f, .15f, () => CloseScreen());
                    var apply = AddButton("Apply layout", .341f, -.132f, .19f, Apply);
                    apply.ColorMask = new Vector4(.52f, .79f, .83f, 1);
                    hint = AddLabel("", -.445f, -.09f, .89f);

                    AddLabel("Element", -.445f, -.047f, .09f);
                    element = new MyGuiControlCombobox(
                        position: new Vector2(-.199f, -.047f), size: new Vector2(.298f, .044f),
                        openAreaItemsCount: 7, isAutoEllipsisEnabled: true
                    );
                    element.AddItem(0, editor.T("Ship schematic"), sort: false);
                    for (int i = 0; i < SystemCount; i++)
                        element.AddItem(i + 1, editor.T(ShipHealth.Names[i]), sort: false);
                    for (int i = 0; i < draft.CustomBars.Count; i++)
                        element.AddItem(1 + SystemCount + i, draft.CustomBars[i].Name, sort: false);
                    element.ItemSelected += () => Select((int)element.GetSelectedKey());
                    Add(element);

                    showElement = AddCheckbox(-.023f, "Show element");
                    AddLabel("Show element", .001f, -.047f, .132f);
                    showElement.IsCheckedChanged += checkbox =>
                    {
                        if (syncing)
                            return;
                        if (selected == 0)
                            draft.ShipVisible = checkbox.IsChecked;
                        else
                            Bar(selected).Enabled = checkbox.IsChecked;
                    };
                    showLabel = AddCheckbox(.143f, "Show label");
                    AddLabel("Show label", .167f, -.047f, .118f);
                    showLabel.IsCheckedChanged += checkbox =>
                    {
                        if (syncing)
                            return;
                        if (selected == 0)
                            draft.Appearance.ShowShipLabel = checkbox.IsChecked;
                        else
                        {
                            Bar(selected).Label = checkbox.IsChecked;
                            simpleLabel.Enabled = checkbox.IsChecked;
                            simpleLabelText.Enabled = checkbox.IsChecked;
                        }
                    };
                    simpleLabel = AddCheckbox(.303f, "Show only the bar title.");
                    simpleLabelText = AddLabel("Simple label", .327f, -.047f, .122f);
                    simpleLabel.IsCheckedChanged += checkbox =>
                    {
                        if (!syncing && selected != 0)
                            Bar(selected).TitleOnly = checkbox.IsChecked;
                    };

                    for (int i = 0; i < fields.Length; i++)
                    {
                        float x = -.445f + .224f * (i % 4), y = .034f + .084f * (i / 4);
                        var field = new NumericField
                        {
                            Label = AddLabel("", x, y - .028f, .212f),
                            Slider = new MyGuiControlSlider(new Vector2(x + .052f, y), width: .104f),
                            Input = new MyGuiControlTextbox(new Vector2(x + .153f, y), maxLength: 10, textScale: .65f)
                            {
                                Size = new Vector2(.078f, .034f)
                            }
                        };
                        field.Slider.ValueChanged += slider => SetFromSlider(field);
                        field.Input.TextChanged += box => SetFromText(field);
                        field.Input.EnterPressed += box =>
                        {
                            if (ValidateValue(field))
                                Synchronize(() => SyncValue(field));
                        };
                        Add(field.Slider);
                        Add(field.Input);
                        field.Unit = AddLabel("%", x + .195f, y, .023f);
                        fields[i] = field;
                    }

                    Select(selected);
                    FocusedControl = element;
                }

                private void Add(MyGuiControlBase control)
                {
                    control.Position += new Vector2(0, ToolbarY);
                    toolbar.Add(control);
                    Controls.Add(control);
                }

                private MyGuiControlLabel AddLabel(string text, float x, float y, float width, float scale = .63f)
                {
                    var label = new MyGuiControlLabel(
                        new Vector2(x, y), text: editor.T(text), font: "White", textScale: scale,
                        maxWidth: width, isAutoEllipsisEnabled: true
                    );
                    Add(label);
                    return label;
                }

                private MyGuiControlButton AddButton(string text, float x, float y, float width, Action action)
                {
                    var button = editor.MakeAction(text, action, width);
                    button.Position = new Vector2(x, y);
                    Add(button);
                    return button;
                }

                private MyGuiControlCheckbox AddCheckbox(float x, string tooltip)
                {
                    var checkbox = new MyGuiControlCheckbox(new Vector2(x, -.047f));
                    checkbox.SetToolTip(editor.T(tooltip));
                    Add(checkbox);
                    return checkbox;
                }

                private void MoveToolbar()
                {
                    float oldY = ToolbarY;
                    controlsAtTop = !controlsAtTop;
                    var delta = new Vector2(0, ToolbarY - oldY);
                    foreach (var control in toolbar)
                        control.Position += delta;
                }

                private void Apply()
                {
                    foreach (var field in fields)
                        if (field.Input.Visible && !ValidateValue(field))
                            return;
                    try
                    {
                        CustomBarsSaved = editor.owner.ApplyHudLayout(draft);
                        Applied = true;
                        CloseScreen();
                    }
                    catch (Exception error)
                    {
                        hint.Text = editor.T(error.Message);
                        hint.ColorMask = InvalidValueColor;
                    }
                }

                private HealthBarLayout Bar(int key)
                {
                    return key <= SystemCount
                        ? draft.Bars.Bars[key - 1]
                        : draft.CustomBars[key - SystemCount - 1].Layout;
                }

                private double ShipX
                {
                    get { return draft.Expanded ? draft.Appearance.ExpandedX : draft.Appearance.X; }
                    set
                    {
                        if (draft.Expanded)
                            draft.Appearance.ExpandedX = value;
                        else
                            draft.Appearance.X = value;
                    }
                }

                private void Select(int key)
                {
                    selected = key;
                    syncing = true;
                    try
                    {
                        element.SelectItemByKey(key, false);
                        var bar = key == 0 ? null : Bar(key);
                        showElement.IsChecked = bar == null ? draft.ShipVisible : bar.Enabled;
                        showLabel.IsChecked = bar?.Label ?? draft.Appearance.ShowShipLabel;
                        simpleLabel.Visible = simpleLabelText.Visible = bar != null;
                        simpleLabel.Enabled = simpleLabelText.Enabled = bar?.Label ?? false;
                        simpleLabel.IsChecked = bar?.TitleOnly ?? false;
                        ResetHint();

                        var appearance = draft.Appearance;
                        if (bar == null)
                        {
                            SetField(0, "Horizontal position", () => ShipX, value => ShipX = value);
                            SetField(1, "Vertical position", () => appearance.Y, value => appearance.Y = value);
                            SetField(2, "Scale", () => appearance.Size, value => appearance.Size = value, .12, .7);
                            SetField(3, "Outline opacity", () => appearance.Outline, value => appearance.Outline = value);
                            SetField(4, "Background width", () => appearance.BackgroundWidth, value => appearance.BackgroundWidth = value,
                                HudAppearance.MinBackgroundSize, HudAppearance.MaxBackgroundSize);
                            SetField(5, "Background height", () => appearance.BackgroundHeight, value => appearance.BackgroundHeight = value,
                                HudAppearance.MinBackgroundSize, HudAppearance.MaxBackgroundSize);
                            SetField(6, "Background opacity", () => appearance.Background, value => appearance.Background = value);
                            if (Textured)
                                SetField(7, "Textured ship opacity", () => appearance.ShipOpacity, value => appearance.ShipOpacity = value);
                        }
                        else
                        {
                            SetField(0, "Horizontal position", () => bar.X, value => bar.X = value);
                            SetField(1, "Vertical position", () => bar.Y, value => bar.Y = value);
                            SetField(2, "Width", () => bar.Width, value => bar.Width = value,
                                HealthBarLayout.MinWidth, HealthBarLayout.MaxWidth);
                            SetField(3, "Height", () => bar.Height, value => bar.Height = value,
                                HealthBarLayout.MinHeight, HealthBarLayout.MaxHeight);
                            SetField(4, "Opacity", () => bar.Opacity, value => bar.Opacity = value);
                        }

                        int visibleFields = bar != null ? 5 : Textured ? 8 : 7;
                        for (int i = 0; i < fields.Length; i++)
                        {
                            var field = fields[i];
                            field.Label.Visible = field.Slider.Visible = field.Input.Visible = field.Unit.Visible = i < visibleFields;
                        }
                    }
                    finally
                    {
                        syncing = false;
                    }
                }

                private void ResetHint()
                {
                    hint.Text = editor.T(selected == 0
                        ? "Drag to move. Apply updates the HUD. Save display keeps it."
                        : selected > SystemCount
                        ? "Drag the corner to resize. Apply layout saves custom bars for this ship."
                        : "Drag the corner to resize. Apply, then Save display.");
                    hint.ColorMask = Vector4.One;
                }

                private void SetField(int index, string text, Func<double> read, Action<double> write,
                    double minimum = 0, double maximum = 1)
                {
                    var field = fields[index];
                    field.Read = read;
                    field.Write = write;
                    field.Minimum = minimum;
                    field.Maximum = maximum;
                    field.Label.Text = editor.T(text);
                    field.Slider.SetToolTip(field.Label.Text);
                    field.Input.SetToolTip(field.Label.Text + " (%)");
                    field.Slider.SetBounds((float)(minimum * 100), (float)(maximum * 100));
                    SyncValue(field);
                }

                private static string Percentage(double value)
                {
                    return (value * 100).ToString("0.####", CultureInfo.InvariantCulture);
                }

                private static void SyncValue(NumericField field)
                {
                    double value = field.Read();
                    field.Slider.Value = (float)(value * 100);
                    field.Input.Text = Percentage(value);
                    field.Input.ColorMask = Vector4.One;
                }

                private void Synchronize(Action action)
                {
                    syncing = true;
                    try { action(); }
                    finally { syncing = false; }
                }

                private void SetFromSlider(NumericField field)
                {
                    if (syncing)
                        return;
                    field.Write(Math.Max(field.Minimum, Math.Min(field.Maximum, field.Slider.Value / 100d)));
                    Synchronize(() => SyncValue(field));
                    ResetHint();
                }

                private void SetFromText(NumericField field)
                {
                    if (syncing)
                        return;
                    bool valid = TryReadValue(field, out double value);
                    field.Input.ColorMask = valid ? Vector4.One : InvalidValueColor;
                    if (valid)
                    {
                        field.Write(value);
                        Synchronize(() => field.Slider.Value = (float)(value * 100));
                        ResetHint();
                    }
                }

                private static bool TryReadValue(NumericField field, out double value)
                {
                    string text = field.Input.Text;
                    if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value)
                        && !double.TryParse(text, NumberStyles.Float, CultureInfo.CurrentCulture, out value))
                        return false;
                    value /= 100;
                    return value >= field.Minimum && value <= field.Maximum;
                }

                private bool ValidateValue(NumericField field)
                {
                    if (TryReadValue(field, out _))
                        return true;
                    hint.Text = string.Format(editor.T("{0} must be between {1}% and {2}%."), field.Label.Text,
                        Percentage(field.Minimum), Percentage(field.Maximum));
                    hint.ColorMask = InvalidValueColor;
                    FocusedControl = field.Input;
                    return false;
                }

                public override bool Update(bool hasFocus)
                {
                    if (!hasFocus)
                        dragging = false;
                    return base.Update(hasFocus);
                }

                public override void InputLost()
                {
                    dragging = false;
                    base.InputLost();
                }

                public override void HandleInput(bool receivedFocusInThisUpdate)
                {
                    var input = MyInput.Static;
                    if (input == null || !IsLoaded || State != MyGuiScreenState.OPENED)
                    {
                        dragging = false;
                        return;
                    }

                    var normalized = MyGuiManager.MouseCursorPosition;
                    var cursor = MyGuiManager.GetScreenCoordinateFromNormalizedCoordinate(normalized);
                    if (dragging)
                    {
                        if (input.IsNewKeyPressed(MyKeys.Escape))
                        {
                            dragging = false;
                            CloseScreen();
                        }
                        else if (!input.IsLeftMousePressed() || ScreenSize() != dragScreen)
                            dragging = false;
                        else if (cursor != dragCursor && (dragMoved || (cursor - dragOrigin).LengthSquared() >= 9))
                        {
                            dragMoved = true;
                            dragCursor = cursor;
                            Drag(cursor);
                        }
                        return;
                    }

                    bool overToolbar = Math.Abs(normalized.X - .5f) <= ToolbarSize.X / 2
                        && Math.Abs(normalized.Y - .5f - ToolbarY) <= ToolbarSize.Y / 2;
                    if (!receivedFocusInThisUpdate && !overToolbar && !element.IsOpen && input.IsNewLeftMousePressed())
                    {
                        var screen = ScreenSize();
                        resizing = selected != 0 && Hit(ResizeHandle(screen, Bounds(screen, selected)), cursor);
                        int key = resizing ? selected : Pick(screen, cursor);
                        if (key >= 0)
                        {
                            if (key != selected)
                                Select(key);
                            var bounds = Bounds(screen, key);
                            dragScreen = screen;
                            dragOrigin = dragCursor = cursor;
                            dragOffset = cursor - new Vector2(bounds.X, bounds.Y);
                            if (resizing)
                            {
                                var bar = Bar(key);
                                resizeAnchor = new Vector2(bounds.X, bounds.Y);
                                resizeBodySize = new Vector2((float)(bar.Width * screen.X), (float)(bar.Height * screen.Y));
                            }
                            dragMoved = false;
                            dragging = true;
                            return;
                        }
                    }
                    base.HandleInput(receivedFocusInThisUpdate);
                }

                private static Vector2 ScreenSize()
                {
                    return MyGuiManager.GetScreenSizeFromNormalizedSize(Vector2.One, true);
                }

                private RectangleF Bounds(Vector2 screen, int key)
                {
                    if (key == 0)
                        return HudLayoutGeometry.Ship(screen, draft.Appearance, draft.Expanded);
                    var bar = Bar(key);
                    return HudLayoutGeometry.Bar(screen, bar, key == 3 && bar.PowerMask != 0);
                }

                private bool IsShown(int key)
                {
                    return key == 0 ? draft.ShipVisible : Bar(key).Enabled;
                }

                private int Pick(Vector2 screen, Vector2 cursor)
                {
                    // Test frontmost bars first, matching their draw order over the ship.
                    for (int key = ElementCount - 1; key >= 0; key--)
                        if (IsShown(key) && Hit(Bounds(screen, key), cursor))
                            return key;
                    return !IsShown(selected) && Hit(Bounds(screen, selected), cursor) ? selected : -1;
                }

                private static bool Hit(RectangleF bounds, Vector2 cursor)
                {
                    const float margin = 5;
                    return cursor.X >= bounds.X - margin && cursor.X <= bounds.Right + margin
                        && cursor.Y >= bounds.Y - margin && cursor.Y <= bounds.Bottom + margin;
                }

                private void Drag(Vector2 cursor)
                {
                    var bar = selected == 0 ? null : Bar(selected);
                    if (bar == null)
                        HudLayoutGeometry.MoveShip(dragScreen, draft.Appearance, draft.Expanded, cursor - dragOffset);
                    else if (resizing)
                        HudLayoutGeometry.ResizeBar(dragScreen, bar, selected == 3 && bar.PowerMask != 0,
                            resizeAnchor, resizeBodySize + (cursor - dragOrigin));
                    else
                        HudLayoutGeometry.MoveBar(dragScreen, bar, selected == 3 && bar.PowerMask != 0, cursor - dragOffset);

                    syncing = true;
                    SyncValue(fields[0]);
                    SyncValue(fields[1]);
                    if (resizing)
                    {
                        SyncValue(fields[2]);
                        SyncValue(fields[3]);
                    }
                    syncing = false;
                    ResetHint();
                }

                public override bool Draw()
                {
                    editor.owner.DrawHudLayout(draft);
                    var screen = ScreenSize();
                    var bounds = Bounds(screen, selected);
                    var selectionColor = IsShown(selected) ? new Color(143, 216, 225) : new Color(135, 164, 176);
                    DrawSelection(bounds, selectionColor, screen.Y);
                    if (selected != 0)
                        DrawEdge(ResizeHandle(screen, bounds), new Color(143, 216, 225));
                    MyGuiManager.DrawSpriteBatch(
                        Texture, new Vector2(.5f, .5f + ToolbarY), ToolbarSize, new Color(9, 22, 31, 248),
                        MyGuiDrawAlignEnum.HORISONTAL_CENTER_AND_VERTICAL_CENTER
                    );
                    return base.Draw();
                }

                private static RectangleF ResizeHandle(Vector2 screen, RectangleF bounds)
                {
                    float side = Math.Max(10, screen.Y * .01f);
                    return new RectangleF(bounds.Right - side, bounds.Bottom - side, side, side);
                }

                private static void DrawSelection(RectangleF rect, Color color, float screenHeight)
                {
                    const float edge = 2;
                    DrawEdge(new RectangleF(rect.X - edge, rect.Y - edge, rect.Width + 2 * edge, edge), color);
                    DrawEdge(new RectangleF(rect.X - edge, Math.Min(rect.Bottom, screenHeight - edge), rect.Width + 2 * edge, edge), color);
                    DrawEdge(new RectangleF(rect.X - edge, rect.Y, edge, rect.Height), color);
                    DrawEdge(new RectangleF(rect.Right, rect.Y, edge, rect.Height), color);
                }

                private static void DrawEdge(RectangleF rect, Color color)
                {
                    MyRenderProxy.DrawSprite(Texture, ref rect, null, color, 0, false, false);
                }
            }
        }
    }
}
