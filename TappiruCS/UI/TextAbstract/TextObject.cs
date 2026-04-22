// TextObject.cs — адаптированная версия
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core.GameObject;
using TappiruCS.Render.Text;
using static TappiruCS.Render.Text.Font;

namespace TappiruCS.UI.TextAbstract
{
    public class TextObject : GameObject
    {
        public string Text { get; set; } = "";

        // Размер шрифта в логических единицах (пикселях дизайна)
        public float FontSize { get; set; } = 144f;

        private Color4 _baseColor = Color4.White;
        private Color4 _displayColor = Color4.White;

        public Color4 Color
        {
            get => _baseColor;
            set
            {
                _baseColor = value;
                if (!FixedColor && !IsHovered)
                    _displayColor = _baseColor;
            }
        }

        public TextAlign Align { get; set; } = TextAlign.Center;
        public Action<Vector2>? OnClick { get; set; }
        public bool FixedColor { get; set; } = false;

        // Эффекты
        public bool HasShadow { get; set; } = false;
        public Vector2 ShadowOffset { get; set; } = new Vector2(3f, 3f);
        public float ShadowOpacity { get; set; } = 0.65f;

        public bool HasOutline { get; set; } = false;
        public float OutlineThickness { get; set; } = 2.5f;
        public Color4 OutlineColor { get; set; } = new Color4(0f, 0f, 0f, 1f);

        public TextObject(string text, float x, float y, float fontSize = 144f)
        {
            Text = text;
            Position = new Vector2(x, y);
            FontSize = fontSize;
            Scale = Vector2.One;
            Pivot = new Vector2(0.5f, 0.5f);
            AllowHover = false;
            Layer = 5;
            _baseColor = Color4.White;
            _displayColor = Color4.White;
        }

        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime, mouse);

            if (!FixedColor)
            {
                _displayColor = IsHovered
                    ? new Color4(1f, 0.9f, 0.4f, 1f)
                    : _baseColor;
            }
            else
            {
                _displayColor = _baseColor;
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

            // Вычисляем финальные масштабы с учётом FontSize, Scale и CanvasScale
            float baseScale = TR.GetScaleFromFontSize(FontSize);
            float finalScaleX = baseScale * Scale.X * CanvasScale.X;
            float finalScaleY = baseScale * Scale.Y * CanvasScale.Y;

            // Преобразуем мировые координаты в локальные относительно позиции текста
            float localMouseX = worldX - Position.X * CanvasScale.X;
            float localMouseY = worldY - Position.Y * CanvasScale.Y;

            return TR.TryGetCharIndexAtPoint(
                Text,
                localMouseX,
                localMouseY,
                finalScaleX,
                finalScaleY,
                Align,
                out _
            );
        }

        public override void Draw(Matrix4 projection)
        {
            if (TR == null || string.IsNullOrEmpty(Text))
                return;

            float baseScale = TR.GetScaleFromFontSize(FontSize);
            float finalScaleX = baseScale * Scale.X * CanvasScale.X;
            float finalScaleY = baseScale * Scale.Y * CanvasScale.Y;

            float finalX = Position.X * CanvasScale.X;
            float finalY = Position.Y * CanvasScale.Y;

            if (HasOutline)
            {
                TR.DrawStringOutline(
                    Text, finalX, finalY,
                    finalScaleX, finalScaleY,
                    _displayColor.R, _displayColor.G, _displayColor.B, _displayColor.A,
                    projection, Align,
                    OutlineThickness, OutlineColor
                );
            }
            else if (HasShadow)
            {
                TR.DrawStringShadow(
                    Text, finalX, finalY,
                    finalScaleX, finalScaleY,
                    _displayColor.R, _displayColor.G, _displayColor.B, _displayColor.A,
                    projection, Align,
                    ShadowOffset, ShadowOpacity
                );
            }
            else
            {
                TR.DrawString(
                    Text, finalX, finalY,
                    finalScaleX, finalScaleY,
                    _displayColor.R, _displayColor.G, _displayColor.B, _displayColor.A,
                    projection, Align
                );
            }
        }
    }
}