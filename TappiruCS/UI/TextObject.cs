// TextObject.cs — ТОЧНЫЙ HITBOX ЧЕРЕЗ TextRender (рекомендуемый вариант)

using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core;
using TappiruCS.Render;

namespace TappiruCS.UI
{
    public class TextObject : GameObject
    {
        public string Text { get; set; } = "";
        public Color4 Color { get; set; } = Color4.White;
        public TextRender.TextAlign Align { get; set; } = TextRender.TextAlign.Center;

        public readonly TextRender _textRender;
        public Action<Vector2>? OnClick { get; set; }

        public bool FixedColor { get; set; } = false;

        public TextObject(TextRender textRender, string text, float x, float y, float scale = 1f)
        {
            _textRender = textRender;
            Text = text;
            Position = new Vector2(x, y);
            Scale = new Vector2(scale, scale);

            Pivot = new Vector2(0.5f, 0.5f);
            AllowHover = true;
            Layer = 150;
        }

        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime, mouse);

            if (!FixedColor)
            {
                Color = IsHovered ? new Color4(1f, 0.9f, 0.4f, 1f) : Color4.White;
            }
            if (FixedColor)
            {
                Color = Color4.Red;
            }
            if (IsHovered && mouse.IsButtonPressed(MouseButton.Left))
            {
                OnClick?.Invoke(new Vector2(mouse.X / CanvasScale.X, mouse.Y / CanvasScale.Y));
            }
        }

        public override bool IsPointInside(float worldX, float worldY)
        {
            if (string.IsNullOrEmpty(Text) || _textRender == null)
                return false;

            var (dLeft, dTop, effScaleX, effScaleY) = GetDesignBounds();

            // Переводим мировые координаты в локальные относительно начала текста
            float localMouseX = worldX - dLeft;
            float localMouseY = worldY - dTop;

            return _textRender.TryGetCharIndexAtPoint(
                Text,
                localMouseX,
                localMouseY,
                effScaleX,
                effScaleY,
                Align,
                out _
            );
        }

        public override void Draw(Matrix4 projection)
        {
            if (string.IsNullOrEmpty(Text) || !Active) return;

            var (dLeft, dTop, effScaleX, effScaleY) = GetDesignBounds();

            _textRender.DrawString(
                Text,
                dLeft * CanvasScale.X,
                dTop * CanvasScale.Y,
                effScaleX * CanvasScale.X,
                effScaleY * CanvasScale.Y,
                Color.R, Color.G, Color.B, Color.A,
                projection,
                Align
            );
        }
    }
}