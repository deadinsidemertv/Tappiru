using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
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
        private readonly Game _game;

        private readonly int _bgTextureId;

        private readonly SpriteObject InputBackground;
        private readonly TextObject InputText;
        private readonly TextObject PlaceHolder;

        public string PlaceHolderText = "Введите текст...";
        public string _input = "";

        public bool IsPassword=false;

        public bool IsFocused { get; private set; } = false;

        public string Text
        {
            get => _input;
            set
            {
                _input = value ?? "";
                UpdateDisplayedText();
            }
        }

        public InputField(Game game, SpriteBatch spriteBatch, TextRender textRenderer,
                          float x, float y, float width, float height)
        {
            _game = game ?? throw new ArgumentNullException(nameof(game));
            _spriteBatch = spriteBatch ?? throw new ArgumentNullException(nameof(spriteBatch));
            _textRender = textRenderer ?? throw new ArgumentNullException(nameof(textRenderer));

            _bgTextureId = TextureManager.GetTexture("btn");

            Position = new Vector2(x, y);
            Scale = new Vector2(width, height);

            // Фон
            InputBackground = new SpriteObject(spriteBatch, _bgTextureId, x, y, width, height);

            // Placeholder
            PlaceHolder = new TextObject(textRenderer, PlaceHolderText,
                InputBackground.Position.X * 1.2f,
                InputBackground.Position.Y * 1.02f, 1f)
            {
                Color = new Color4(0.1f, 0.1f, 0.1f, 1f),
                ScaleMultiply = 0.3f,
                Align = TextAlign.Left
            };

            // Основной текст
            InputText = new TextObject(textRenderer, "",
                InputBackground.Position.X * 1.2f,
                InputBackground.Position.Y * 1.02f, 1f)
            {
                ScaleMultiply = 0.3f,
                Color = Color4.White,
                Align = TextAlign.Left
            };

            // Подписка на события
            _game.KeyDown += HandleKeyDown;
            _game.TextInput += HandleTextInput;   // ← адаптировано под Action<TextInputEventArgs>
        }

        // Обработчик под Action<TextInputEventArgs>
        private void HandleTextInput(TextInputEventArgs e)
        {
            if (!IsFocused) return;

            char c = (char)e.Unicode;

            // Добавляем printable символы (русские буквы, пробел, знаки препинания и т.д.)
            if (c >= ' ')
            {
                _input += c;
                UpdateDisplayedText();
            }
        }

        private void HandleKeyDown(KeyboardKeyEventArgs e)
        {
            if (!IsFocused) return;

            switch (e.Key)
            {
                case Keys.Backspace:
                    if (_input.Length > 0)
                    {
                        _input = _input[..^1];
                        UpdateDisplayedText();
                    }
                    break;

                case Keys.Enter:
                case Keys.KeyPadEnter:
                    OnEnterPressed();
                    break;

                case Keys.Escape:
                    IsFocused = false;
                    break;
            }
        }

        private void UpdateDisplayedText()
        {
            InputText.Text = _input;
        }

        public void Clear() => Text = "";

        public void OnEnterPressed()
        {
            // Снимаем фокус при нажатии Enter (можно изменить поведение)
            IsFocused = false;
        }

        public override void Update(double deltaTime, MouseState mouse)
        {
            // Определяем, находится ли курсор над полем
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

            // Мигающий курсор
            if (IsFocused)
            {
                bool showCursor = DateTime.Now.Millisecond % 800 < 400;

                string displayedText;

                if (IsPassword)
                {
                    // Правильный и быстрый способ сделать маску ******
                    displayedText = new string('*', _input.Length);
                }
                else
                {
                    displayedText = _input;
                }

                InputText.Text = displayedText + (showCursor ? "|" : "");
                PlaceHolder.Active = false;
            }
            else
            {
                InputText.Text = IsPassword ? new string('*', _input.Length) : _input;
                PlaceHolder.Active = string.IsNullOrEmpty(_input);
            }
        }

        public override void Draw(Matrix4 projection)
        {
            // Фон
            _spriteBatch.Draw(_bgTextureId,
                Position.X * CanvasScale.X,
                Position.Y * CanvasScale.Y,
                Scale.X * CanvasScale.X,
                Scale.Y * CanvasScale.Y,
                0, 0, 1, 1,
                1f, 1f, 1f, 1f,
                projection);

            // Текст
            InputText.CanvasScale = CanvasScale;
            InputText.Draw(projection);

            PlaceHolder.CanvasScale = CanvasScale;
            PlaceHolder.Text = PlaceHolderText;
            PlaceHolder.Draw(projection);
        }

        // Вызывай этот метод при удалении InputField, чтобы снять подписки
        public void Dispose()
        {
            _game.KeyDown -= HandleKeyDown;
            _game.TextInput -= HandleTextInput;
        }
    }
}