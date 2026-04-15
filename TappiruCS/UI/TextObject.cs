// TextObject.cs — ТОЧНЫЙ HITBOX ЧЕРЕЗ TextRender (рекомендуемый вариант)

using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;

namespace TappiruCS.UI
{
    public class TextObject : GameObject
    {
        public string Text { get; set; } = "";
        public Color4 Color { get; set; } = Color4.White;
        public TextRender.TextAlign Align { get; set; } = TextRender.TextAlign.Center;

        public Action<Vector2>? OnClick { get; set; }

        public bool FixedColor { get; set; } = false;

        public TextObject(string text, float x, float y, float scale = 1f)
        {
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
            if (string.IsNullOrEmpty(Text) || TR == null)
                return false;

            var (dLeft, dTop, effScaleX, effScaleY) = GetDesignBounds();

            // Переводим мировые координаты в локальные относительно начала текста
            float localMouseX = worldX - dLeft;
            float localMouseY = worldY - dTop;

            return TR.TryGetCharIndexAtPoint(
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
            if (Context == null)
            {
                Console.WriteLine($"[NULL CONTEXT] {GetType().Name} | Parent: {Parent?.GetType().Name ?? "null"}");
                return;
            }

            var (dLeft, dTop, effScaleX, effScaleY) = GetDesignBounds();

            TR.DrawStringShadow(
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