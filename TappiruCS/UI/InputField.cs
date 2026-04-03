using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using TappiruCS.Core.TappiruCS.Core;
using TappiruCS.Render;
using TappiruCS.UI;
using static TappiruCS.Render.TextRender;

namespace TappiruCS.UI
{
    public class InputField : GameObject
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly int _bgTextureId;
        private readonly TextObject _textObject;

        public string Text
        {
            get => _textObject.Text;
            set => _textObject.Text = value;
        }

        public bool IsFocused { get; private set; } = false;

        public InputField(SpriteBatch spriteBatch, TextRender textRenderer, int bgTextureId,
                          float x, float y, float width, float height)
        {
            _spriteBatch = spriteBatch;
            _bgTextureId = bgTextureId;

            Position = new Vector2(x, y);
            Scale = new Vector2(width, height);

            // Внутренний TextObject для отображения текста
            _textObject = new TextObject(textRenderer, "ку", x + 20, y + height * 0.5f, 0.75f)
            {
                Color = Color4.White,
                Align = TextAlign.Left,
                ScaleMultiply = 1f
            };
        }

        public override void Update(double deltaTime, MouseState mouse)
        {
            float left = Position.X * CanvasScale.X;
            float right = left + Scale.X * CanvasScale.X;
            float top = Position.Y * CanvasScale.Y;
            float bottom = top + Scale.Y * CanvasScale.Y;

            bool hovered = mouse.X >= left && mouse.X <= right &&
                          mouse.Y >= top && mouse.Y <= bottom;

            if (mouse.IsButtonPressed(MouseButton.Left))
            {
                IsFocused = hovered;
            }

            // Добавляем мигающий курсор
            if (IsFocused)
            {
                _textObject.Text = Text + (DateTime.Now.Millisecond % 800 < 400 ? "|" : "");
            }
            else
            {
                _textObject.Text = Text;
            }
        }

        public override void Draw(Matrix4 projection)
        {
            // Фон поля ввода
            _spriteBatch.Draw(_bgTextureId,
                Position.X * CanvasScale.X,
                Position.Y * CanvasScale.Y,
                Scale.X * CanvasScale.X,
                Scale.Y * CanvasScale.Y,
                0, 0, 1, 1,
                1f, 1f, 1f, 1f,
                projection);

            // Текст через TextObject
            _textObject.CanvasScale = CanvasScale;
            _textObject.Position = new Vector2(Position.X + 20, Position.Y + Scale.Y * 0.45f); // небольшой отступ
            _textObject.Draw(projection);
        }
    }
}