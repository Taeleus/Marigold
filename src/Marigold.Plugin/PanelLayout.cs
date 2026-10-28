using System;
using System.Collections.Generic;
using System.Text;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using VRage.Game;
using VRage.Utils;
using VRageMath;

namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private sealed partial class Panel
        {
            private const float ContentWidth = .60f;
            private readonly List<KeyValuePair<MyGuiControlBase, Vector2>> flowControls =
                new List<KeyValuePair<MyGuiControlBase, Vector2>>();
            private readonly Dictionary<int, float> pageScroll = new Dictionary<int, float>();
            private MyGuiControlParent content;
            private MyGuiControlScrollablePanel scroll;
            private MyGuiControlBase firstContentFocus;
            private float flowY;
            private int renderedTab = -1;

            public RectangleF PreviewBounds
            {
                get
                {
                    var area = RecoveryFocus ? new Vector2(.78f, .37f) : new Vector2(.18f, .14f);
                    var center = RecoveryFocus ? new Vector2(.5f, .365f) : new Vector2(.15f, .72f);
                    var pixels = MyGuiManager.GetScreenSizeFromNormalizedSize(area);
                    float side = Math.Min(pixels.X, pixels.Y);
                    var pixelCenter = MyGuiManager.GetScreenCoordinateFromNormalizedCoordinate(center);
                    return new RectangleF(pixelCenter.X - side / 2, pixelCenter.Y - side / 2, side, side);
                }
            }

            private string SaveHint
            {
                get
                {
                    if (tab == 22)
                        return "Custom bar draft. Save bar keeps these edits; leaving this page discards them.";
                    if (tab == 21)
                        return "Create bar saves this selection with the current ship.";
                    if (tab == 26)
                        return "Color preview. Use color applies it; Cancel keeps the original.";
                    if (tab == 11)
                        return "Shortcuts save when you bind, save or remove them.";
                    if (tab == 12)
                        return "Language and automatic matching save when changed. Connector context belongs to this ship.";
                    if (tab == 1 || tab == 2 || tab == 6 || tab == 7 || tab == 10 || tab == 15 || tab == 18 || tab == 20)
                        return "Ship and baseline actions save when confirmed. Display settings are separate.";
                    return "Display changes apply now. Save display keeps them after restarting.";
                }
            }

            private int NavigationGroup
            {
                get
                {
                    if (tab == 0 || tab == 3 || tab == 5 || tab == 14 || tab == 24)
                        return 0;
                    if (tab == 1 || tab == 2 || tab == 6 || tab == 7 || tab == 10 || tab == 15 || tab == 18)
                        return 1;
                    if (tab == 8 || tab == 20 || tab == 21 || tab == 22 || tab == 23 || tab == 25 || (tab == 26 && pickingBarColor))
                        return 3;
                    if (tab == 11 || tab == 12 || tab == 19)
                        return 4;
                    return tab == 13 || tab == 17 ? 5 : 2;
                }
            }

            private MyGuiControlButton MakeAction(string text, Action action, float width)
            {
                return new MyGuiControlButton(
                    visualStyle: MyGuiControlButtonStyleEnum.Rectangular,
                    size: new Vector2(width, .046f),
                    text: new StringBuilder(T(text)),
                    textScale: .70f,
                    onButtonClick: button =>
                    {
                        try { action(); }
                        catch (Exception exception) { Status(exception.Message); }
                    }
                );
            }

            private void RememberScroll()
            {
                if (scroll != null && renderedTab == tab)
                    pageScroll[tab] = scroll.ScrollbarVPosition;
            }

            private void CreateWorkspace()
            {
                string[] titles = { "Ship", "Baselines", "Ship display", "Health bars", "Preferences", "Save & share" };
                int[] routes = { 0, 1, 4, 8, 12, 13 };
                Label(-.44f, -.425f, "Marigold " + typeof(Plugin).Assembly.GetName().Version.ToString(3));
                for (int i = 0; i < routes.Length; i++)
                {
                    int route = routes[i];
                    var item = MakeAction(titles[i], () => Switch(route), .19f);
                    item.Position = new Vector2(-.35f, -.285f + i * .066f);
                    item.Size = new Vector2(.19f, .052f);
                    item.Checked = NavigationGroup == i;
                    Controls.Add(item);
                }
                var separators = new MyGuiControlSeparatorList();
                separators.AddVertical(new Vector2(-.235f, -.39f), .77f);
                separators.AddHorizontal(new Vector2(-.20f, .29f), .64f);
                Controls.Add(separators);
                Controls.Add(new MyGuiControlLabel(
                    new Vector2(-.19f, -.36f), text: T(PageNames[tab]), textScale: .88f,
                    font: "White", maxWidth: .63f, isAutoEllipsisEnabled: true
                ));
                var inspect = MakeAction("Preview HUD", InspectHud, .19f);
                inspect.Position = new Vector2(-.35f, .115f);
                inspect.Size = new Vector2(.19f, .044f);
                Controls.Add(inspect);
                var hint = new MyGuiControlMultilineText(
                    new Vector2(-.35f, .335f), new Vector2(.19f, .075f), font: "White", textScale: .56f,
                    contents: new StringBuilder(T("Preview HUD shows the actual layout without leaving your edits.")),
                    drawScrollbarV: false, drawScrollbarH: false
                );
                Controls.Add(hint);
                var back = MakeAction("Back", Back, .18f);
                back.Position = new Vector2(-.35f, .425f);
                back.Enabled = history.Count > 0;
                Controls.Add(back);
                if (tab != 13 && tab != 21 && tab != 22 && tab != 26)
                {
                    var save = MakeAction("Save display...", () => Switch(13), .29f);
                    save.Position = new Vector2(.08f, .425f);
                    Controls.Add(save);
                }
                var close = MakeAction("Close", () => CloseScreen(), .18f);
                close.Position = new Vector2(.35f, .425f);
                Controls.Add(close);
                output = new MyGuiControlMultilineText(
                    new Vector2(.12f, .345f), new Vector2(.64f, .09f), font: "White", textScale: .64f,
                    contents: new StringBuilder(string.IsNullOrEmpty(status) ? T(SaveHint) : status),
                    drawScrollbarH: false
                );
                Controls.Add(output);
                content = new MyGuiControlParent(size: new Vector2(ContentWidth, .57f));
                scroll = new MyGuiControlScrollablePanel(content)
                {
                    Position = new Vector2(.12f, -.015f),
                    Size = new Vector2(.64f, .59f),
                    ScrollbarVEnabled = true,
                    ScrollbarHEnabled = false
                };
                Controls.Add(scroll);
                flowY = 0;
                flowControls.Clear();
                firstContentFocus = null;
            }

            private void PlaceContent(MyGuiControlBase control, float x, float y)
            {
                control.OriginAlign = MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP;
                content.Controls.Add(control);
                flowControls.Add(new KeyValuePair<MyGuiControlBase, Vector2>(control, new Vector2(x, y)));
                if (firstContentFocus == null && control.CanHaveFocus && !(control is MyGuiControlParent))
                    firstContentFocus = control;
            }

            private void AddContent(MyGuiControlBase control, float height)
            {
                PlaceContent(control, 0, flowY);
                flowY += height + .014f;
            }

            private void FinishWorkspace()
            {
                float height = Math.Max(.57f, flowY);
                content.Size = new Vector2(ContentWidth, height);
                foreach (var row in flowControls)
                    row.Key.Position = new Vector2(-ContentWidth / 2, -height / 2) + row.Value;
                scroll.RefreshInternals();
                // A form always has a usable initial focus; the native scroll panel reveals it as needed.
                if (firstContentFocus != null)
                    FocusedControl = firstContentFocus;
                float previous;
                if (pageScroll.TryGetValue(tab, out previous))
                    scroll.ScrollbarVPosition = previous;
                renderedTab = tab;
            }

            private void Intro(string text)
            {
                Note(text);
                flowY += .008f;
            }

            private void Note(string text)
            {
                string translated = T(text);
                var paragraph = new MyGuiControlMultilineText(
                    size: new Vector2(ContentWidth, .04f), font: "White", textScale: .68f,
                    contents: new StringBuilder(translated), drawScrollbarV: false, drawScrollbarH: false
                );
                float height = Math.Max(.030f, paragraph.TextSize.Y + .014f);
                paragraph.Size = new Vector2(ContentWidth, height);
                AddContent(paragraph, height);
            }

            private void Heading(string text)
            {
                flowY += .014f;
                AddContent(new MyGuiControlLabel(
                    text: T(text), textScale: .80f, font: "White", maxWidth: ContentWidth,
                    isAutoEllipsisEnabled: true
                ), .034f);
            }

            private MyGuiControlLabel TextRow(string text)
            {
                var label = new MyGuiControlLabel(
                    text: T(text), textScale: .67f, font: "White", maxWidth: ContentWidth,
                    isAutoEllipsisEnabled: true
                );
                label.SetToolTip(T(text));
                AddContent(label, .029f);
                return label;
            }

            private MyGuiControlButton ActionControl(string text, Action action)
            {
                var button = MakeAction(text, action, ContentWidth);
                AddContent(button, button.Size.Y);
                return button;
            }

            private void PrimaryAction(string text, Action action)
            {
                var button = ActionControl(text, action);
                button.ColorMask = new Vector4(.52f, .79f, .83f, 1);
            }

            private void ActionRow(string left, Action leftAction, string right = null, Action rightAction = null)
            {
                if (right == null)
                {
                    ActionControl(left, leftAction);
                    return;
                }
                float width = (ContentWidth - .018f) / 2;
                var first = MakeAction(left, leftAction, width);
                var second = MakeAction(right, rightAction, width);
                PlaceContent(first, 0, flowY);
                PlaceContent(second, width + .018f, flowY);
                flowY += .060f;
            }

            private MyGuiControlTextbox TextField(string label, string value, int maxLength = 128)
            {
                TextRow(label);
                var field = new MyGuiControlTextbox(defaultText: value ?? "", maxLength: maxLength, textScale: .70f)
                {
                    Size = new Vector2(ContentWidth, .044f)
                };
                AddContent(field, field.Size.Y);
                return field;
            }

            private MyGuiControlCombobox ChoiceField(string label, string[] items, int selected, Action<int> change = null)
            {
                TextRow(label);
                var field = new MyGuiControlCombobox(size: new Vector2(ContentWidth, .044f), openAreaItemsCount: 6);
                for (int i = 0; i < items.Length; i++)
                    field.AddItem(i, T(items[i]), sort: false);
                if (items.Length > 0)
                    field.SelectItemByKey(Math.Max(0, Math.Min(selected, items.Length - 1)), false);
                else
                    field.Enabled = false;
                if (change != null)
                    field.ItemSelected += () => change((int)field.GetSelectedKey());
                AddContent(field, field.Size.Y);
                return field;
            }

            private MyGuiControlCheckbox CheckField(string label, bool selected, Action<bool> change)
            {
                var row = new MyGuiControlParent(size: new Vector2(ContentWidth, .043f));
                var field = new MyGuiControlCheckbox(new Vector2(-ContentWidth / 2 + .016f, 0), isChecked: selected);
                field.IsCheckedChanged += checkbox => change(checkbox.IsChecked);
                row.Controls.Add(field);
                var text = new MyGuiControlLabel(
                    new Vector2(-ContentWidth / 2 + .045f, 0), text: T(label), textScale: .69f, font: "White",
                    maxWidth: ContentWidth - .05f, isAutoEllipsisEnabled: true
                );
                text.SetToolTip(T(label));
                row.Controls.Add(text);
                AddContent(row, row.Size.Y);
                if (firstContentFocus == null)
                    firstContentFocus = field;
                return field;
            }
        }
    }
}
