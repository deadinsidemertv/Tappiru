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

        public float ScaleMultiply = 1f;

        //[Поля текста кнопки]
        private readonly string _text; //Текст кнопки
        public Color4 TextColor { get; set; } = Color4.White; //Цвет текста в кнопке
        public float textXoffset { get; set; } = 0f; //смещение относительно кнопки по X
        public float textYoffset { get; set; } = 0f; //смещение относительно кнопки по Y
        public float TextBtnScale { get; set; } = 0.5f;

        //[Поля кнопки]
        private readonly Color4 _normalColor = new Color4(1.0f, 1.0f, 1.0f, 1.0f);
        private readonly Color4 _hoverColor = new Color4(1.15f, 1.15f, 1.05f, 1.0f);
        private readonly Color4 _pressColor = new Color4(0.75f, 0.75f, 0.75f, 1.0f);

        private Color4 _currentColor;
        

        public event Action OnClick;

        public bool IsHovered { get; private set; }

      

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
            TextColor = color;
        }

        // ←←← Вот правильная перегрузка
        public override void Update(double deltaTime, MouseState mouse)
        {
            float left = Position.X * CanvasScale.X;
            float right = left + Scale.X * CanvasScale.X;
            float top = Position.Y * CanvasScale.Y;
            float bottom = top + Scale.Y * CanvasScale.Y;

            // Мышь уже в пикселях окна, не нужно умножать на CanvasScale
            IsHovered = mouse.X >= left && mouse.X <= right && mouse.Y >= top && mouse.Y <= bottom;

            bool isPressed = IsHovered && mouse.IsButtonDown(MouseButton.Left);
            if (isPressed)
                _currentColor = _pressColor;
            else if (IsHovered)
                _currentColor = _hoverColor;
            else
                _currentColor = _normalColor;

            if (IsHovered && mouse.IsButtonPressed(MouseButton.Left))
                OnClick?.Invoke();
        }

        public override void Draw(Matrix4 projection)
        {
            _spriteBatch.Draw(_textureId,
                Position.X*CanvasScale.X*ScaleMultiply, Position.Y*CanvasScale.Y * ScaleMultiply, Scale.X*CanvasScale.X*ScaleMultiply, Scale.Y*CanvasScale.Y*ScaleMultiply,
                0, 0, 1, 1,
                _currentColor.R, _currentColor.G, _currentColor.B, _currentColor.A,
                projection);
            

            float textX = ((Position.X + Scale.X / 2f)+textXoffset)*ScaleMultiply;
            float textY = ((Position.Y + Scale.Y / 2f)+ textYoffset)*ScaleMultiply;
            var buttonText = new TextObject(_textRenderer, _text, textX, textY, TextBtnScale) { Color = TextColor, ScaleMultiply = ScaleMultiply};
            buttonText.CanvasScale = new Vector2(CanvasScale.X, CanvasScale.Y);
            buttonText.Draw(projection);
        }
    }
}