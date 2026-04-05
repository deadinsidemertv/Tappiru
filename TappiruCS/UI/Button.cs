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

        // ==================== Поля текста кнопки ====================
        public string Text { get; set; }                                //Текст кнопки
        public Color4 TextColor { get; set; } = Color4.White;              //Цвет текста
        public Vector2 TextOffset { get; set; } = new Vector2(1f, 1f);           //Смещение текса внутри кнопки
        public float TextScale { get; set; } = 0.5f;                              //Scale текста внутри кнопки
        public TextAlign TextAlign { get; set; } = TextAlign.Center;              //Центрирование текста

        // ==================== Поля картинки на кнопке ====================
        public int ButtonImage { get; set; } = 0;                                   //Картинка внутри кнопки

        public Vector2 ImagePadding { get; set; } = new Vector2(0f, 0f);               //Отступ картинки внутри кнопки

        public Vector2 ImageOffset { get; set; } = Vector2.Zero;                        //Смещение внутри кнопки

        public Vector2 ImageScale { get; set; } = new Vector2(0.18f, 1f);                 //Размер картинки внутри кнопки

        public bool IsImaged { get; set; } = false;                                       //Включена ли картинка

        // ==================== Поля состояний кнопки (цвета) ====================
        public Color4 NormalColor { get; set; } = new Color4(1.0f, 1.0f, 1.0f, 1.0f);           
        public Color4 HoverColor { get; set; } = new Color4(1.15f, 1.15f, 1.05f, 0f);
        public Color4 PressColor { get; set; } = new Color4(0.75f, 0.75f, 0.75f, 1.0f);

        private Color4 _currentColor;

        public event Action OnClick;

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

            bool isPressed = IsHovered && mouse.IsButtonDown(MouseButton.Left);

            if (isPressed)
                _currentColor = PressColor;
            else if (IsHovered)
                _currentColor = HoverColor;
            else
                _currentColor = NormalColor;

            if (IsHovered && mouse.IsButtonPressed(MouseButton.Left))
                OnClick?.Invoke();
            if (IsHovered)
            {
                Console.WriteLine(this.Text + " HOVERED");
            }
            
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

        public override void SetHover(bool hover)
        {
            base.SetHover(hover);
            if (hover)
                _currentColor = HoverColor;
            else
                _currentColor = NormalColor;
        }
    }
}