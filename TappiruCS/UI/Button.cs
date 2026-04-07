using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using TappiruCS.Core.TappiruCS.Core;
using TappiruCS.Render;
using TappiruCS.UI;
using static TappiruCS.Render.TextRender;

namespace TappiruCS
{
    public class Button : GameObject
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly TextRender _textRenderer;

        private readonly int _textureId;

        public string Text { get; set; }
        public Color4 TextColor { get; set; } = Color4.White;
        public Vector2 TextOffset { get; set; } = new Vector2(0f, 0f);   // по умолчанию 0
        public float TextScale { get; set; } = 0.5f;
        public TextAlign TextAlign { get; set; } = TextAlign.Center;

        public int ButtonImage { get; set; } = 0;
        public Vector2 ImagePadding { get; set; } = new Vector2(0f, 0f);
        public Vector2 ImageOffset { get; set; } = Vector2.Zero;        // ← смещение в пикселях от ЦЕНТРА кнопки (при ScaleMultiply = 1)
        public Vector2 ImageScale { get; set; } = new Vector2(0.18f, 1f);
        public bool IsImaged { get; set; } = false;

        public Color4 NormalColor { get; set; } = new Color4(1f, 1f, 1f, 1f);
        public Color4 HoverColor { get; set; } = new Color4(0.5f, 0.5f, 1.05f, 1f);
        public Color4 PressColor { get; set; } = new Color4(0.75f, 0.75f, 0.75f, 1f);

        private Color4 _currentColor;

        public event Action OnClick;

        public event Action<Button,bool> HoverStateChanged;

        public Button(SpriteBatch spriteBatch, TextRender textRenderer,
                      float x, float y, float width, float height,
                      string textureName, string text, Color4 color)
        {
            _spriteBatch = spriteBatch;
            _textRenderer = textRenderer;
            Text = text;

            Position = new Vector2(x, y);
            Scale = new Vector2(width, height);

            _textureId = TextureManager.GetTexture(textureName);

            TextColor = color;
            _currentColor = NormalColor;

            
        }

        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime);
            bool isPressed = IsHovered && mouse.IsButtonDown(MouseButton.Left);

            if (isPressed)
                _currentColor = PressColor;
            else if (IsHovered)
                _currentColor = HoverColor;
            else
                _currentColor = NormalColor;

            if (IsHovered && mouse.IsButtonPressed(MouseButton.Left))
                OnClick?.Invoke();
        }

        public override void Draw(Matrix4 projection)
        {
            // Фон кнопки
            var (dLeft, dTop, effW, effH) = GetDesignBounds();
            float sLeft = dLeft * CanvasScale.X;
            float sTop = dTop * CanvasScale.Y;
            float sW = effW * CanvasScale.X;
            float sH = effH * CanvasScale.Y;

            _spriteBatch.Draw(_textureId,
                sLeft, sTop, sW, sH,
                0, 0, 1, 1,
                _currentColor.R, _currentColor.G, _currentColor.B, _currentColor.A,
                projection);

            // ==================== Текст кнопки ====================
            float textDesignX = Position.X + TextOffset.X;
            float textDesignY = Position.Y + TextOffset.Y;

            var buttonText = new TextObject(_textRenderer, Text, textDesignX, textDesignY, TextScale)
            {
                Color = TextColor,
                ScaleMultiply = ScaleMultiply,
                Align = TextAlign.Center,
                CanvasScale = CanvasScale,
                Pivot = new Vector2(0.5f, 0.5f)
            };
            buttonText.Draw(projection);

            // ==================== Картинка на кнопке ====================
            if (IsImaged)
            {
                Vector2 offset = ImageOffset.LengthSquared > 0.0001f ? ImageOffset : ImagePadding;

                // ← КЛЮЧЕВОЕ ИСПРАВЛЕНИЕ
                // Offset теперь масштабируется вместе с кнопкой → картинка остаётся в рамке
                float scaledOffsetX = offset.X * ScaleMultiply;
                float scaledOffsetY = offset.Y * ScaleMultiply;

                // Позиция ЦЕНТРА картинки в дизайн-координатах
                float imageDesignX = Position.X + scaledOffsetX;
                float imageDesignY = Position.Y + scaledOffsetY;

                // Базовый размер картинки (до ScaleMultiply)
                float imageBaseW = Scale.X * ImageScale.X;
                float imageBaseH = Scale.Y * ImageScale.Y;

                var imageBtn = new SpriteObject(_spriteBatch, ButtonImage, imageDesignX, imageDesignY, imageBaseW, imageBaseH)
                {
                    ScaleMultiply = ScaleMultiply,     // масштабируется вместе с кнопкой
                    Layer = 2,
                    CanvasScale = CanvasScale,
                    Pivot = new Vector2(0.5f, 0.5f)    // центр картинки находится в imageDesignX/Y
                };
                imageBtn.Draw(projection);
            }
        }

        public override void SetHover(bool hover)
        {
            if (IsHovered == hover) return;
            base.SetHover(hover);

            if (hover)
            {
                if (Tag == "") 
                { 
                    this.AnimScale(1.15f, 0.18f);
                }
                
                _currentColor = HoverColor;
            }
            else
            {
                this.AnimScaleReset(0.22f);
                _currentColor = NormalColor;
            }

            HoverStateChanged?.Invoke(this, hover);
        }
    }
}