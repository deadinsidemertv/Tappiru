using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core;
using TappiruCS.Render;
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

        public string PlaceHolderText { get; set; } = "Введите текст...";
        public Color4 PlaceHolderColor { get; set; } = Color4.DarkGray;
        public string _input = "";

        public bool IsPassword = false;
        public bool IsFocused { get; private set; } = false;

        public string Text
        {
            get => _input;
            set { _input = value ?? ""; UpdateDisplayedText(); }
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

            InputBackground = new SpriteObject(spriteBatch, _bgTextureId, x, y, width, height)
            {
                ScaleMultiply = 1f
            };

            // Тексты — относительный масштаб 0.3f
            InputText = new TextObject(textRenderer, "", x, y, 1f)
            {
                ScaleMultiply = 0.3f,
                Color = Color4.White,
                Align = TextAlign.Left,
                Pivot = new Vector2(0f, 0f),   // left align
            };

            PlaceHolder = new TextObject(textRenderer, PlaceHolderText, x, y, 1f)
            {
                ScaleMultiply = 0.3f,
                Color = PlaceHolderColor,
                Align = TextAlign.Left,
                Pivot = new Vector2(0f, 0f),
            };

            _game.KeyDown += HandleKeyDown;
            _game.TextInput += HandleTextInput;

            AddChild(InputText);
            AddChild(InputBackground);
            AddChild(PlaceHolder);
        }

        private void HandleTextInput(TextInputEventArgs e)
        {
            if (!IsFocused) return;
            char c = (char)e.Unicode;
            if (c >= ' ') { _input += c; UpdateDisplayedText(); }
        }

        private void HandleKeyDown(KeyboardKeyEventArgs e)
        {
            if (!IsFocused) return;

            switch (e.Key)
            {
                case Keys.Backspace:
                    if (_input.Length > 0) { _input = _input[..^1]; UpdateDisplayedText(); }
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

        public void OnEnterPressed() => IsFocused = false;

        public override void Update(double deltaTime, MouseState mouse)
        {
            // Сначала обновляем CanvasScale у детей через базовый метод
            base.Update(deltaTime, mouse);        // ← ВАЖНО: вызываем ПЕРВЫМ

            PlaceHolder.Color = PlaceHolderColor;
            PlaceHolder.Text = PlaceHolderText;

            float designMouseX = mouse.X / CanvasScale.X;
            float designMouseY = mouse.Y / CanvasScale.Y;
            bool hovered = IsPointInside(designMouseX, designMouseY);

            if (mouse.IsButtonPressed(MouseButton.Left))
                IsFocused = hovered;

            // ====================== ЛОГИКА InputField ======================
            InputBackground.ScaleMultiply = 1f;
            InputText.ScaleMultiply = 0.3f;
            PlaceHolder.ScaleMultiply = 0.3f;

            // Позиции + отступы
            var (left, top, _, _) = GetDesignBounds();
            float padding = 10f * ScaleMultiply;

            InputText.Position = new Vector2(left + padding, top + padding);
            PlaceHolder.Position = new Vector2(left + padding, top + padding);

            // Мигающий курсор
            if (IsFocused)
            {
                bool showCursor = DateTime.Now.Millisecond % 800 < 400;
                string displayedText = IsPassword ? new string('*', _input.Length) : _input;
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
            base.Draw(projection);
        }

        public void Dispose()
        {
            _game.KeyDown -= HandleKeyDown;
            _game.TextInput -= HandleTextInput;
        }
    }
}