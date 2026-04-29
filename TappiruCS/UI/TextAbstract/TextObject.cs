// TextObject.cs — исправленная версия
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core.GameObject;
using static TappiruCS.Render.Text.BMFont.Font;

namespace TappiruCS.UI.TextAbstract
{
    public class TextObject : GameObject
    {
        public string Text { get; set; } = "";
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
            LocalPosition = new Vector2(x, y);
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

            _displayColor = (!FixedColor && IsHovered)
                ? new Color4(1f, 0.9f, 0.4f, 1f)
                : _baseColor;

            if (IsHovered && mouse.IsButtonPressed(MouseButton.Left))
                OnClick?.Invoke(new Vector2(mouse.X / CanvasScale.X, mouse.Y / CanvasScale.Y));
        }

        public override bool IsPointInside(float worldX, float worldY)
        {
            if (string.IsNullOrEmpty(Text)) return false;

            // Выбираем активный рендерер
            if (FT != null)
            {
                float baseScale = FT.GetScaleFromFontSize(FontSize);
                float finalScaleX = baseScale * ScaleMultiply * CanvasScale.X;
                float finalScaleY = baseScale * ScaleMultiply * CanvasScale.Y;

                float screenBaseX = WorldPosition.X * CanvasScale.X;
                float screenBaseY = WorldPosition.Y * CanvasScale.Y;

                float textWidth = FT.CalculateTextWidth(Text, finalScaleX);
                float startX = Align switch
                {
                    TextAlign.Center => screenBaseX - textWidth / 2f,
                    TextAlign.Right => screenBaseX - textWidth,
                    _ => screenBaseX
                };

                float localX = worldX * CanvasScale.X - startX;
                float localY = worldY * CanvasScale.Y - screenBaseY;

                return FT.TryGetCharIndexAtPoint(
                    Text, localX, localY,
                    finalScaleX, finalScaleY,
                    TextAlign.Left, out _);
            }

            if (TR != null)
            {
                float baseScale = TR.GetScaleFromFontSize(FontSize);
                float finalScaleX = baseScale * ScaleMultiply * CanvasScale.X;
                float finalScaleY = baseScale * ScaleMultiply * CanvasScale.Y;

                float screenBaseX = WorldPosition.X * CanvasScale.X;
                float screenBaseY = WorldPosition.Y * CanvasScale.Y;

                float textWidth = TR.CalculateTextWidth(Text, finalScaleX);
                float startX = Align switch
                {
                    TextAlign.Center => screenBaseX - textWidth / 2f,
                    TextAlign.Right => screenBaseX - textWidth,
                    _ => screenBaseX
                };

                float localX = worldX * CanvasScale.X - startX;
                float localY = worldY * CanvasScale.Y - screenBaseY;

                return TR.TryGetCharIndexAtPoint(
                    Text, localX, localY,
                    finalScaleX, finalScaleY,
                    TextAlign.Left, out _);
            }

            return false;
        }

        public override void Draw(Matrix4 projection)
        {
            if (string.IsNullOrEmpty(Text)) return;

            float finalX = WorldPosition.X * CanvasScale.X;
            float finalY = WorldPosition.Y * CanvasScale.Y;

            // ── FreeType рендерер ──────────────────────────────────────────────────
            if (FT != null)
            {
                float baseScale = FT.GetScaleFromFontSize(FontSize);
                float finalScaleX = baseScale * ScaleMultiply * CanvasScale.X;
                float finalScaleY = baseScale * ScaleMultiply * CanvasScale.Y;

                if (HasOutline)
                {
                    FT.DrawStringOutline(
                        Text, finalX, finalY,
                        finalScaleX, finalScaleY,
                        _displayColor.R, _displayColor.G, _displayColor.B, _displayColor.A,
                        projection, Align,
                        OutlineThickness, OutlineColor);
                }
                else if (HasShadow)
                {
                    FT.DrawStringShadow(
                        Text, finalX, finalY,
                        finalScaleX, finalScaleY,
                        _displayColor.R, _displayColor.G, _displayColor.B, _displayColor.A,
                        projection, Align,
                        ShadowOffset, ShadowOpacity);
                }
                else
                {
                    FT.DrawString(
                        Text, finalX, finalY,
                        finalScaleX, finalScaleY,
                        _displayColor.R, _displayColor.G, _displayColor.B, _displayColor.A,
                        projection, Align);
                }
                return; // FT нарисован — выходим
            }

            // ── BMFont рендерер (fallback) ─────────────────────────────────────────
            if (TR == null) return;

            float bBaseScale = TR.GetScaleFromFontSize(FontSize);
            float bFinalScaleX = bBaseScale * ScaleMultiply * CanvasScale.X;
            float bFinalScaleY = bBaseScale * ScaleMultiply * CanvasScale.Y;

            if (HasOutline)
            {
                TR.DrawStringOutline(
                    Text, finalX, finalY,
                    bFinalScaleX, bFinalScaleY,
                    _displayColor.R, _displayColor.G, _displayColor.B, _displayColor.A,
                    projection, Align,
                    OutlineThickness, OutlineColor);
            }
            else if (HasShadow)
            {
                TR.DrawStringShadow(
                    Text, finalX, finalY,
                    bFinalScaleX, bFinalScaleY,
                    _displayColor.R, _displayColor.G, _displayColor.B, _displayColor.A,
                    projection, Align,
                    ShadowOffset, ShadowOpacity);
            }
            else
            {
                TR.DrawString(
                    Text, finalX, finalY,
                    bFinalScaleX, bFinalScaleY,
                    _displayColor.R, _displayColor.G, _displayColor.B, _displayColor.A,
                    projection, Align);
            }
        }
    }
}