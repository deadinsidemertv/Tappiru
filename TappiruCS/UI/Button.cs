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

        public float ScaleMultiply = 1f;

        // ==================== Поля текста кнопки ====================
        public string Text { get; set; }
        public Color4 TextColor { get; set; } = Color4.White;
        public Vector2 TextOffset { get; set; } = new Vector2(1f, 1f);
        public float TextScale { get; set; } = 0.5f;
        public TextAlign TextAlign { get; set; } = TextAlign.Center;

        // ==================== Поля картинки на кнопке ====================
        public int ButtonImage { get; set; } = 0;

        /// <summary>
        /// Отступ картинки от краёв кнопки (в пикселях относительно размера кнопки)
        /// X — горизонтальный паддинг, Y — вертикальный паддинг
        /// </summary>
        public Vector2 ImagePadding { get; set; } = new Vector2(0f, 0f);

        /// <summary>
        /// Ручное смещение картинки (используется в приоритете, если нужно точное позиционирование)
        /// </summary>
        public Vector2 ImageOffset { get; set; } = Vector2.Zero;

        public Vector2 ImageScale { get; set; } = new Vector2(0.18f, 1f);

        public bool IsImaged { get; set; } = false;

        // ==================== Поля состояний кнопки (цвета) ====================
        public Color4 NormalColor { get; set; } = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
        public Color4 HoverColor { get; set; } = new Color4(1.15f, 1.15f, 1.05f, 1.0f);
        public Color4 PressColor { get; set; } = new Color4(0.75f, 0.75f, 0.75f, 1.0f);

        private Color4 _currentColor;

        public event Action OnClick;

        public bool IsHovered { get; private set; }

        public bool IsFocused { get; set; } = false;

        // ==================== Конструктор ====================
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
            float left = Position.X * CanvasScale.X * ScaleMultiply;
            float right = left + Scale.X * CanvasScale.X * ScaleMultiply;
            float top = Position.Y * CanvasScale.Y * ScaleMultiply;
            float bottom = top + Scale.Y * CanvasScale.Y * ScaleMultiply;

            IsHovered = mouse.X >= left && mouse.X <= right && mouse.Y >= top && mouse.Y <= bottom;

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
            _spriteBatch.Draw(_textureId,
                Position.X * CanvasScale.X * ScaleMultiply,
                Position.Y * CanvasScale.Y * ScaleMultiply,
                Scale.X * CanvasScale.X * ScaleMultiply,
                Scale.Y * CanvasScale.Y * ScaleMultiply,
                0, 0, 1, 1,
                _currentColor.R, _currentColor.G, _currentColor.B, _currentColor.A,
                projection);

            // ==================== Картинка на кнопке ====================
            if (IsImaged)
            {
                // Выбираем смещение: если ImageOffset задан — используем его, иначе ImagePadding
                Vector2 offset = ImageOffset.LengthSquared > 0.0001f ? ImageOffset : ImagePadding;

                float imageX = (Position.X + offset.X) * ScaleMultiply;
                float imageY = (Position.Y + offset.Y) * ScaleMultiply;

                // Размер картинки зависит ТОЛЬКО от ImageScale и размера кнопки
                // ImageScale.X — множитель ширины кнопки
                // ImageScale.Y — множитель высоты кнопки
                float imageWidth = Scale.X*ImageScale.X;
                float imageHeight = Scale.Y*ImageScale.Y ;

                var imageBtn = new SpriteObject(_spriteBatch, ButtonImage, imageX, imageY, imageWidth, imageHeight)
                {
                    ScaleMultiply = ScaleMultiply,
                    Layer = 2,
                    CanvasScale = CanvasScale
                };

                imageBtn.Draw(projection);
            }

            // ==================== Текст кнопки ====================
            float textX = (Position.X + Scale.X / 10+TextOffset.X) * ScaleMultiply;
            float textY = (Position.Y + Scale.Y / 10+TextOffset.Y) * ScaleMultiply;

            var buttonText = new TextObject(_textRenderer, Text, textX, textY, TextScale)
            {
                Color = TextColor,
                ScaleMultiply = ScaleMultiply,
                Align = TextAlign,
                CanvasScale = CanvasScale
            };

            buttonText.Draw(projection);
        }
    }
}