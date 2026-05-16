// TextObject.cs
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core.GameObject;
using TappiruCS.Render.Text;
using TappiruCS.UI.API.LocalizationLanguage;

namespace TappiruCS.UI.TextAbstract
{
    public class TextObject : GameObject
    {
        private string _text = "";
        private string _textKey = "";
        public float FontSize { get; set; } = 144f;
        public string FontKey { get; set; } = "UI";

        private UIColor _baseColor = Color4.White;
        private UIColor _displayColor = Color4.White;

        // Если true — TextObject сам выравнивает себя вертикально по центру
        // относительно WorldPosition (baseline-поправка FreeType).
        // По умолчанию true, чтобы текст в кнопке всегда был по середине
        // без ручных TextOffset. Поставь false если нужна старая логика.
        public bool AutoVerticalCenter { get; set; } = true;

        public UIColor Color
        {
            get => _baseColor;
            set
            {
                _baseColor = value;
                if (!FixedColor && !IsHovered)
                    _displayColor = _baseColor;
            }
        }

        public string Text
        {
            get => _text;
            set
            {
                _text = value ?? "";
                _textKey = string.Empty;
            }
        }

        public string TextKey
        {
            get => _textKey;
            set
            {
                _textKey = value ?? "";
                if (!string.IsNullOrEmpty(_textKey))
                {
                    Text = Localization.Get(_textKey);
                    FontKey = Localization.GetFontKey();
                }
            }
        }

        public void SetLocalized(string key) => TextKey = key;

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
            LocalPosition = new Vector2(x, y);
            FontKey = "UI";
            FontSize = fontSize;
            Scale = Vector2.One;
            Pivot = new Vector2(0.5f, 0.5f);
            AllowHover = false;
            Layer = 5;
            _baseColor = Color4.White;
            _displayColor = Color4.White;
            Text = text;
        }

        // ── Вертикальная поправка ─────────────────────────────────────────────────
        // FreeType DrawString рисует глиф как: Y = startY - BearingY
        // Значит startY — это baseline. Визуальный центр текстового блока
        // находится на: baseline - Ascender + (Ascender - |Descender|) / 2
        //             = baseline - (Ascender + Descender) / 2
        // Чтобы визуальный центр совпал с WorldPosition, нужно сдвинуть
        // baseline вниз на (Ascender + Descender) / 2 * scaleY.
        // Descender в FreeType отрицательный, поэтому формула:
        //   baselineY = worldY + (Ascender + Descender) / 2 * scaleY
        //
        // Именно эту поправку вычисляем один раз здесь и используем везде.
        private float GetBaselineOffsetY(float scaleY)
        {
            if (!AutoVerticalCenter) return 0f;

            var font = FontManager.Get(FontKey);
            if (font == null) return 0f;

            // Ascender > 0, Descender < 0 в метриках FreeType
            // (Ascender + Descender) / 2 — смещение центра блока от baseline
            return (font.Ascender + font.GetDescender()) * 0.5f * scaleY;
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
            if (string.IsNullOrEmpty(Text) || FT == null)
                return false;

            float baseScale = FT.GetScaleFromFontSize(FontSize);
            float finalScaleX = baseScale * ScaleMultiply * CanvasScale.X;
            float finalScaleY = baseScale * ScaleMultiply * CanvasScale.Y;

            float objectScreenX = WorldPosition.X * CanvasScale.X;
            float objectScreenY = WorldPosition.Y * CanvasScale.Y;

            float clickScreenX = worldX * CanvasScale.X;
            float clickScreenY = worldY * CanvasScale.Y;

            // По X — выравнивание по Align (не изменилось)
            float textWidth = FT.CalculateTextWidth(Text, finalScaleX);
            float startX = Align switch
            {
                TextAlign.Center => objectScreenX - textWidth * 0.5f,
                TextAlign.Right => objectScreenX - textWidth,
                _ => objectScreenX
            };

            float localX = clickScreenX - startX;
            if (localX < 0 || localX > textWidth)
                return false;

            // По Y — baseline с той же поправкой что и в Draw,
            // чтобы hittest точно совпадал с визуальным положением текста.
            float baselineY = objectScreenY + GetBaselineOffsetY(finalScaleY);
            float localY = clickScreenY - baselineY;

            float penX = 0f;
            char prev = '\0';

            for (int i = 0; i < Text.Length; i++)
            {
                char c = Text[i];

                if (FT.TryGetRenderedGlyph(c, out var glyph) && glyph != null)
                {
                    if (prev != '\0')
                        penX += FT.GetKerning(prev, c) * finalScaleX;

                    float glyphX = penX + glyph.Info.BearingX * finalScaleX;
                    float glyphWidth = glyph.Info.Width * finalScaleX;

                    if (localX >= glyphX && localX < glyphX + glyphWidth)
                    {
                        float glyphTop = -glyph.Info.BearingY * finalScaleY;
                        float glyphBottom = glyphTop + glyph.Info.Height * finalScaleY;

                        const float tolerance = 8f;
                        if (localY >= glyphTop - tolerance && localY <= glyphBottom + tolerance)
                            return true;
                    }

                    penX += glyph.Info.XAdvance * finalScaleX;
                }
                else
                {
                    penX += FT.CalculateTextWidth(c.ToString(), finalScaleX);
                }

                prev = c;
            }

            return false;
        }

        public override void Draw(Matrix4 projection)
        {
            if (string.IsNullOrEmpty(Text)) return;

            var font = FontManager.Get(FontKey);
            if (font == null) return;

            float finalX = WorldPosition.X * CanvasScale.X;
            float finalY = WorldPosition.Y * CanvasScale.Y;

            float baseScale = font.GetScaleFromFontSize(FontSize);
            float finalScaleX = baseScale * ScaleMultiply * CanvasScale.X;
            float finalScaleY = baseScale * ScaleMultiply * CanvasScale.Y;

            // ── Вертикальная поправка baseline ───────────────────────────────────
            // Без неё текст рисуется выше центра, потому что DrawString
            // использует startY как baseline, а не как визуальный центр блока.
            float drawY = finalY + GetBaselineOffsetY(finalScaleY);
            // ────────────────────────────────────────────────────────────────────

            if (HasOutline)
            {
                font.DrawStringOutline(
                    Text, finalX, drawY,
                    finalScaleX, finalScaleY,
                    _displayColor.R, _displayColor.G, _displayColor.B, _displayColor.A,
                    projection, Align,
                    OutlineThickness, OutlineColor);
            }
            else if (HasShadow)
            {
                font.DrawStringShadow(
                    Text, finalX, drawY,
                    finalScaleX, finalScaleY,
                    _displayColor.R, _displayColor.G, _displayColor.B, _displayColor.A,
                    projection, Align,
                    ShadowOffset, ShadowOpacity);
            }
            else
            {
                font.DrawString(
                    Text, finalX, drawY,
                    finalScaleX, finalScaleY,
                    _displayColor.R, _displayColor.G, _displayColor.B, _displayColor.A,
                    projection, Align);
            }
        }
    }
}