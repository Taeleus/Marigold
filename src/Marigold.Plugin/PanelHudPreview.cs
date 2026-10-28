using Sandbox.Graphics.GUI;
using VRageMath;

namespace Marigold
{
    internal sealed partial class SchematicOverlay
    {
        private sealed partial class Panel
        {
            private MyGuiScreenBase hudPreview;

            public bool PreviewingHud
            {
                get { return hudPreview != null; }
            }

            private void InspectHud()
            {
                if (hudPreview != null)
                    return;
                captureShortcut = -1;
                saturationPicker?.CancelDrag();
                huePicker?.CancelDrag();
                var preview = new HudPreviewScreen(this);
                hudPreview = preview;
                preview.Closed += (screen, unloading) =>
                {
                    if (ReferenceEquals(hudPreview, preview))
                        hudPreview = null;
                };
                MyGuiSandbox.AddScreen(preview);
            }

            public override bool CloseScreen(bool isUnloading = false)
            {
                var preview = hudPreview;
                hudPreview = null;
                preview?.CloseScreen(isUnloading);
                return base.CloseScreen(isUnloading);
            }

            private sealed class HudPreviewScreen : MyGuiScreenBase
            {
                private readonly Panel editor;

                public HudPreviewScreen(Panel editor)
                    : base(new Vector2(.5f, .5f), null, new Vector2(1, 1))
                {
                    this.editor = editor;
                    CanHideOthers = true;
                    EnabledBackgroundFade = false;
                    RecreateControls(true);
                }

                public override string GetFriendlyName()
                {
                    return "MarigoldHudPreview";
                }

                public override void RecreateControls(bool constructor)
                {
                    base.RecreateControls(constructor);
                    var back = editor.MakeAction("Return to editor", () => CloseScreen(), .28f);
                    back.Position = new Vector2(0, .42f);
                    Controls.Add(back);
                    FocusedControl = back;
                }

                public override bool Draw()
                {
                    editor.owner.DrawSafely();
                    return base.Draw();
                }
            }
        }
    }
}
