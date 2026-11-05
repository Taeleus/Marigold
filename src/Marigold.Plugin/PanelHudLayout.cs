using System;
using System.Collections.Generic;
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
                        Status("Layout applied. Use Save display to keep it. Custom bars use Save for this ship.");
                    }
                });
            }

            private sealed class HudLayoutScreen : MyGuiScreenBase
            {
                private static readonly Vector2 ToolbarSize = new Vector2(.94f, .24f);
                private readonly Panel editor;
                private readonly HudLayoutDraft draft;
                private readonly List<MyGuiControlBase> toolbar = new List<MyGuiControlBase>();
                private readonly MyGuiControlSlider[] sliders = new MyGuiControlSlider[5];
                private readonly MyGuiControlLabel[] sliderLabels = new MyGuiControlLabel[5];
                private MyGuiControlCombobox element;
                private MyGuiControlCheckbox showElement, showLabel;
                private MyGuiControlLabel hint;
                private int selected;
                private bool controlsAtTop, syncing, dragging, dragMoved, resizing;
                private Vector2 dragOrigin, dragOffset, dragScreen, dragCursor;
                private Vector2 resizeAnchor, resizeBodySize;

                public bool Applied { get; private set; }

                private float ToolbarY => controlsAtTop ? -.355f : .355f;
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
                    AddLabel("Arrange HUD", -.445f, -.087f, .23f, .83f);
                    AddButton("Move controls", -.024f, -.087f, .195f, MoveToolbar);
                    AddButton("Cancel", .16f, -.087f, .15f, () => CloseScreen());
                    var apply = AddButton("Apply layout", .341f, -.087f, .19f, Apply);
                    apply.ColorMask = new Vector4(.52f, .79f, .83f, 1);
                    hint = AddLabel("", -.445f, -.043f, .89f);

                    AddLabel("Element", -.445f, .001f, .09f);
                    element = new MyGuiControlCombobox(
                        position: new Vector2(-.183f, .001f), size: new Vector2(.334f, .044f),
                        openAreaItemsCount: 7, isAutoEllipsisEnabled: true
                    );
                    element.AddItem(0, editor.T("Ship schematic"), sort: false);
                    for (int i = 0; i < SystemCount; i++)
                        element.AddItem(i + 1, editor.T(ShipHealth.Names[i]), sort: false);
                    for (int i = 0; i < draft.CustomBars.Count; i++)
                        element.AddItem(1 + SystemCount + i, draft.CustomBars[i].Name, sort: false);
                    element.ItemSelected += () => Select((int)element.GetSelectedKey());
                    Add(element);

                    showElement = AddCheckbox(.027f, "Show element");
                    AddLabel("Show element", .049f, .001f, .17f);
                    showElement.IsCheckedChanged += checkbox =>
                    {
                        if (syncing)
                            return;
                        if (selected == 0)
                            draft.ShipVisible = checkbox.IsChecked;
                        else
                            Bar(selected).Enabled = checkbox.IsChecked;
                    };
                    showLabel = AddCheckbox(.25f, "Show label");
                    AddLabel("Show label", .272f, .001f, .17f);
                    showLabel.IsCheckedChanged += checkbox =>
                    {
                        if (syncing)
                            return;
                        if (selected == 0)
                            draft.Appearance.ShowShipLabel = checkbox.IsChecked;
                        else
                            Bar(selected).Label = checkbox.IsChecked;
                    };

                    for (int i = 0; i < sliders.Length; i++)
                    {
                        int index = i;
                        float x = -.445f + .179f * i;
                        sliderLabels[i] = AddLabel("", x, .047f, .169f);
                        sliders[i] = new MyGuiControlSlider(
                            position: new Vector2(x + .081f, .086f), minValue: 0, maxValue: 100, width: .162f,
                            labelText: "{0}%", labelDecimalPlaces: 1, labelScale: .57f, labelSpaceWidth: .044f,
                            showLabel: true
                        );
                        sliders[i].ValueChanged += slider => SetSlider(index, slider.Value / 100d);
                        Add(sliders[i]);
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
                    var checkbox = new MyGuiControlCheckbox(new Vector2(x, .001f));
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
                    try
                    {
                        editor.owner.ApplyHudLayout(draft);
                        Applied = true;
                        CloseScreen();
                    }
                    catch (Exception error)
                    {
                        hint.Text = editor.T(error.Message);
                        hint.ColorMask = new Vector4(1, .65f, .55f, 1);
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
                        hint.Text = editor.T(bar == null
                            ? "Drag to move. Apply updates the HUD. Save display keeps it."
                            : key > SystemCount
                            ? "Drag the corner to resize. Apply, then Save for this ship."
                            : "Drag the corner to resize. Apply, then Save display.");
                        hint.ColorMask = Vector4.One;
                        SetRange(0, "Horizontal position", bar?.X ?? ShipX, 0, 1);
                        SetRange(1, "Vertical position", bar?.Y ?? draft.Appearance.Y, 0, 1);
                        if (bar == null)
                        {
                            SetRange(2, "Scale", draft.Appearance.Size, .12, .7);
                            SetRange(3, "Background opacity", draft.Appearance.Background, 0, 1);
                            SetRange(4, Textured ? "Textured ship opacity" : "Outline opacity",
                                Textured ? draft.Appearance.ShipOpacity : draft.Appearance.Outline, 0, 1);
                        }
                        else
                        {
                            SetRange(2, "Width", bar.Width, HealthBarLayout.MinWidth, HealthBarLayout.MaxWidth);
                            SetRange(3, "Height", bar.Height, HealthBarLayout.MinHeight, HealthBarLayout.MaxHeight);
                            SetRange(4, "Opacity", bar.Opacity, 0, 1);
                        }
                    }
                    finally
                    {
                        syncing = false;
                    }
                }

                private void SetRange(int index, string text, double value, double min, double max)
                {
                    var slider = sliders[index];
                    sliderLabels[index].Text = editor.T(text);
                    slider.SetToolTip(editor.T(text));
                    slider.SetBounds((float)(min * 100), (float)(max * 100));
                    slider.Value = (float)(value * 100);
                }

                private void SetSlider(int index, double value)
                {
                    if (syncing)
                        return;
                    value = Clamp(value, 0, 1);
                    var appearance = draft.Appearance;
                    if (selected == 0)
                    {
                        switch (index)
                        {
                            case 0: ShipX = value; break;
                            case 1: appearance.Y = value; break;
                            case 2: appearance.Size = Clamp(value, .12, .7); break;
                            case 3: appearance.Background = value; break;
                            case 4:
                                if (Textured)
                                    appearance.ShipOpacity = value;
                                else
                                    appearance.Outline = value;
                                break;
                        }
                    }
                    else
                    {
                        var bar = Bar(selected);
                        switch (index)
                        {
                            case 0: bar.X = value; break;
                            case 1: bar.Y = value; break;
                            case 2: bar.Width = Clamp(value, HealthBarLayout.MinWidth, HealthBarLayout.MaxWidth); break;
                            case 3: bar.Height = Clamp(value, HealthBarLayout.MinHeight, HealthBarLayout.MaxHeight); break;
                            case 4: bar.Opacity = value; break;
                        }
                    }
                }

                private static double Clamp(double value, double min, double max)
                {
                    return Math.Max(min, Math.Min(max, value));
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
                    sliders[0].Value = (float)((bar?.X ?? ShipX) * 100);
                    sliders[1].Value = (float)((bar?.Y ?? draft.Appearance.Y) * 100);
                    if (resizing)
                    {
                        sliders[2].Value = (float)(bar.Width * 100);
                        sliders[3].Value = (float)(bar.Height * 100);
                    }
                    syncing = false;
                }

                public override bool Draw()
                {
                    editor.owner.DrawHudLayout(draft);
                    var screen = ScreenSize();
                    var bounds = Bounds(screen, selected);
                    DrawSelection(bounds, IsShown(selected), screen.Y);
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

                private static void DrawSelection(RectangleF rect, bool shown, float screenHeight)
                {
                    var color = shown ? new Color(143, 216, 225) : new Color(135, 164, 176);
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
