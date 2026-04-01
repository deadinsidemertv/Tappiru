using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Runtime.InteropServices;
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


        private readonly string _text;
        public Color4 _TextColor { get; set; } = Color4.White;


        private readonly Color4 _normalColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
        private readonly Color4 _hoverColor = new Color4(1.15f, 1.15f, 1.05f, 1.0f);
        private readonly Color4 _pressColor = new Color4(0.75f, 0.75f, 0.75f, 1.0f);

        private Color4 _currentColor;
        

        public event Action OnClick;

        public bool IsHovered { get; private set; }

        public float textXoffset { get; set; } = 0f;
        public float textYoffset { get; set; } = 0f;

        public Button(SpriteBatch spriteBatch, TextRender textRenderer,
                      float x, float y, float width, float height,
                      string textureName, string text,Color4 color)
        {
            _spriteBatch = spriteBatch;
            _textRenderer = textRenderer;
            _text = text;

            Position = new Vector2(x, y);
            Scale = new Vector2(width, height);

            _textureId = TextureManager.GetTexture(textureName);
            _currentColor = _normalColor;
            _TextColor = color;
        }

        // ←←← Вот правильная перегрузка
        public override void Update(double deltaTime, MouseState mouse)
        {
            IsHovered = mouse.X >= Position.X && mouse.X <= Position.X + Scale.X &&
                        mouse.Y >= Position.Y && mouse.Y <= Position.Y + Scale.Y;

            bool isPressed = IsHovered && mouse.IsButtonDown(MouseButton.Left);

            // Цвет
            if (isPressed)
                _currentColor = _pressColor;
            else if (IsHovered)
                _currentColor = _hoverColor;
            else
                _currentColor = _normalColor;

            // Клик
            if (IsHovered && mouse.IsButtonPressed(MouseButton.Left))
            {
                OnClick?.Invoke();
            }
        }

        public override void Draw(Matrix4 projection)
        {
            _spriteBatch.Draw(_textureId,
                Position.X, Position.Y, Scale.X, Scale.Y,
                0, 0, 1, 1,
                _currentColor.R, _currentColor.G, _currentColor.B, _currentColor.A,
                projection);

            

            float textX = (Position.X + Scale.X / 2f)+textXoffset;
            float textY = (Position.Y + Scale.Y / 2f)+textYoffset;
            var buttonText = new TextObject(_textRenderer, _text, textX, textY, 0.45f) { Color = _TextColor};
            buttonText.Draw(projection);
        }
    }
}