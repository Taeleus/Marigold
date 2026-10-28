using System;
using Sandbox.Graphics;
using Sandbox.Graphics.GUI;
using VRage.Input;
using VRage.Utils;
using VRageMath;
using VRageRender;
using VRageRender.Messages;

namespace Marigold
{
    // Both drawing and hit testing use the GUI safe area, including its scaling on wide screens.
    internal sealed class ColorPickerControl : MyGuiControlBase
    {
        private readonly ColorPickerState selection;
        private readonly bool hueStrip;
        private readonly string textureName = "marigoldcolorpicker" + Guid.NewGuid().ToString("N");
        private Action changed;
        private bool dragging, textureCreated, removed;
        private double textureHue = double.NaN;

        public ColorPickerControl(Vector2 position, Vector2 size, ColorPickerState selection, bool hueStrip, Action changed)
            : base(position: position, size: size, canHaveFocus: true)
        {
            this.selection = selection;
            this.hueStrip = hueStrip;
            this.changed = changed;
            CanPlaySoundOnMouseOver = false;
        }

        public override MyGuiControlBase HandleInput()
        {
            var handled = base.HandleInput();
            var input = MyInput.Static;
            if (removed || !Enabled || !Visible || input == null)
            {
                dragging = false;
                return handled;
            }
            if (dragging && !input.IsPrimaryButtonPressed())
            {
                dragging = false;
                return this;
            }
            if (IsMouseOver && input.IsNewPrimaryButtonPressed())
                dragging = true;
            if (!dragging)
                return handled;

            var cursor = MyGuiManager.MouseCursorPosition - GetPositionAbsoluteTopLeft();
            if (hueStrip)
                selection.SelectHue(cursor.Y / Size.Y);
            else
                selection.SelectSquare(cursor.X / Size.X, cursor.Y / Size.Y);
            changed?.Invoke();
            return this;
        }

        public void CancelDrag()
        {
            dragging = false;
        }

        public override void OnFocusChanged(bool focus)
        {
            if (!focus)
                dragging = false;
            base.OnFocusChanged(focus);
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            if (removed)
                return;
            base.Draw(transitionAlpha, backgroundTransitionAlpha);
            UpdateTexture();
            var topLeft = GetPositionAbsoluteTopLeft();
            MyGuiManager.DrawSpriteBatch(
                textureName, topLeft, Size, Color.White * transitionAlpha,
                MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP
            );
            MyGuiManager.DrawBorders(topLeft, Size, new Color(180, 195, 200) * transitionAlpha, 1);
            var markerSize = MyGuiManager.GetNormalizedSizeFromScreenSize(new Vector2(11, 11));
            var marker = topLeft + (hueStrip
                ? new Vector2(Size.X / 2, (float)selection.Hue * Size.Y)
                : new Vector2((float)selection.Saturation * Size.X, (float)(1 - selection.Value) * Size.Y));
            if (hueStrip)
                markerSize.X = Size.X + MyGuiManager.GetNormalizedSizeFromScreenSize(new Vector2(6, 0)).X;
            MyGuiManager.DrawBorders(marker - markerSize / 2, markerSize, Color.Black * transitionAlpha, 3);
            MyGuiManager.DrawBorders(marker - markerSize / 2, markerSize, Color.White * transitionAlpha, 1);
        }

        private void UpdateTexture()
        {
            if (textureCreated && (hueStrip || textureHue == selection.Hue))
                return;
            int width = hueStrip ? 1 : 128;
            int height = hueStrip ? 256 : 128;
            // Render messages retain their byte arrays, so each update owns a fresh buffer.
            var data = new byte[width * height * 4];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    uint rgb = hueStrip
                        ? ColorPickerState.ToRgb((double)y / (height - 1), 1, 1)
                        : ColorPickerState.ToRgb(selection.Hue, (double)x / (width - 1), 1 - (double)y / (height - 1));
                    int index = (x + y * width) * 4;
                    data[index] = (byte)(rgb >> 16);
                    data[index + 1] = (byte)(rgb >> 8);
                    data[index + 2] = (byte)rgb;
                    data[index + 3] = 255;
                }
            if (textureCreated)
                MyRenderProxy.ResetGeneratedTexture(textureName, data);
            else
            {
                MyRenderProxy.CreateGeneratedTexture(
                    textureName, width, height, MyGeneratedTextureType.RGBA,
                    1, data, generateMipmaps: false, immediatelyReady: true
                );
                textureCreated = true;
            }
            textureHue = selection.Hue;
        }

        public override void OnRemoving()
        {
            dragging = false;
            changed = null;
            removed = true;
            if (textureCreated)
            {
                MyRenderProxy.DestroyGeneratedTexture(textureName);
                textureCreated = false;
            }
            base.OnRemoving();
        }
    }

    internal sealed class ColorSwatchControl : MyGuiControlBase
    {
        private readonly Func<uint> color;

        public ColorSwatchControl(Vector2 position, Vector2 size, Func<uint> color)
            : base(position: position, size: size, isActiveControl: false)
        {
            this.color = color;
        }

        public override void Draw(float transitionAlpha, float backgroundTransitionAlpha)
        {
            base.Draw(transitionAlpha, backgroundTransitionAlpha);
            uint rgb = color();
            var shade = new Color((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb) * transitionAlpha;
            var topLeft = GetPositionAbsoluteTopLeft();
            MyGuiManager.DrawSpriteBatch(
                "Textures\\GUI\\Blank.dds", topLeft, Size, shade,
                MyGuiDrawAlignEnum.HORISONTAL_LEFT_AND_VERTICAL_TOP
            );
            MyGuiManager.DrawBorders(topLeft, Size, new Color(180, 195, 200) * transitionAlpha, 1);
        }
    }
}
