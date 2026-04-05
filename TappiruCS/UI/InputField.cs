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
        private readonly TextRender _textRender;


        private readonly int _bgTextureId;

        private readonly SpriteObject InputBackground;

        private readonly TextObject InputText;
        private readonly TextObject PlaceHolder;

        public string PlaceHolderText = "Input your text";
        private string Input = "";
        

        public bool IsFocused { get; private set; } = false;

        public InputField(SpriteBatch spriteBatch, TextRender textRenderer,
                          float x, float y, float width, float height)
        {
            _spriteBatch = spriteBatch;
            _bgTextureId = TextureManager.GetTexture("btn");

            Position = new Vector2(x, y);
            Scale = new Vector2(width, height);

            InputBackground = new SpriteObject(spriteBatch, _bgTextureId, x, y, width, height);

            PlaceHolder = new TextObject(textRenderer, PlaceHolderText, InputBackground.Position.X * 1.2f, InputBackground.Position.Y * 1.02f, 1f)
            {
                Color = new Color4(0.1f, 0.1f, 0.1f, 1f),
                ScaleMultiply = 0.3f,
                Align = TextAlign.Left

            };

            InputText = new TextObject(textRenderer, Input, InputBackground.Position.X*1.2f, InputBackground.Position.Y*1.02f, 1f)
            {   
                ScaleMultiply = 0.3f
            }; ;

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
                InputText.Text = Input + (DateTime.Now.Millisecond % 800 < 400 ? "|" : "");
                PlaceHolder.Active = false;
            }
            else
            {
                InputText.Text = Input;
                PlaceHolder.Active = true;
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
            InputText.CanvasScale = CanvasScale;
            InputText.Text = Input;
            InputText.Draw(projection);

            PlaceHolder.CanvasScale = CanvasScale;
            PlaceHolder.Text = PlaceHolderText;
            PlaceHolder.Draw(projection);
        }
    }
}