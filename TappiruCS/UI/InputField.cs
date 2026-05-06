using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.Render.Text;
using TappiruCS.UI.TextAbstract;

namespace TappiruCS.UI
{
    public class InputField : GameObject
    {
        private readonly int _bgTextureId;

        private readonly SpriteObject InputBackground;
        private readonly SpriteObject _selectionBackground;
        private readonly TextObject InputText;
        private readonly TextObject PlaceHolder;


        private int texIcon1;
        private int texIcon2;
        public SpriteObject ico1;
        public SpriteObject ico2;

        public string PlaceHolderText { get; set; } = "Введите текст...";
        public Color4 PlaceHolderColor { get; set; } = Color4.DarkGray;

        private string _input = "";
        public bool IsPassword = false;
        public bool IsFocused { get; private set; } = false;

        private bool _isAllSelected = false;

        // Отступ текста от левого края фона
        public float TextLeftPadding { get; set; } = 28f;

        public string Text
        {
            get => _input;
            set { _input = value ?? ""; UpdateDisplayedText(); }
        }

        public InputField(float x, float y, float width, float height)
        {
            LocalPosition = new Vector2(x, y);
            Scale = new Vector2(width, height);

            _bgTextureId = TextureManager.GetTexture("input-field");
            // Основной фон
            InputBackground = new SpriteObject(_bgTextureId, 0, 0, width, height)
            {
                ScaleMultiply = 1f,
                Color = new Color4(0.2f, 0.2f, 0.25f, 1f)
            };

            // Фон выделения при Ctrl+A
            _selectionBackground = new SpriteObject(_bgTextureId, 0, 0, width, height)
            {
                ScaleMultiply = 1f,
                Color = new Color4(0.3f, 0.6f, 1.0f, 0.35f),
                Pivot = new Vector2(0f, 0f),
                Active = false
            };

            // Текст ввода — прижат к левому краю
            InputText = new TextObject("", 0, 20, 24f)
            {
                ScaleMultiply = 1f,
                Color = Color4.White,
                Align = TextAlign.Left,
                Pivot = new Vector2(0f, 0.5f),     // важно: Pivot.X = 0
                Layer = 5
            };

            // Placeholder
            PlaceHolder = new TextObject(PlaceHolderText, 0, 20, 24f)
            {
                ScaleMultiply = 1f,
                Color = PlaceHolderColor,
                Align = TextAlign.Left,
                Pivot = new Vector2(0f, 0.5f),
            };


            AddChild(InputBackground);
            AddChild(_selectionBackground);
            AddChild(PlaceHolder);
            AddChild(InputText);


        }
        public void SetupIcon()
        {
            if (Tag == "username")
            {
                texIcon1 = TextureManager.GetTexture("userico");
                ico1 = new SpriteObject(texIcon1, 0, 0, 35, 35) { Color = Color4.DarkGray };
                ico2 = new SpriteObject(texIcon2, 0, 0, 35, 35) { Active = false };
                AddChild(ico1);
                AddChild(ico2);
            }
            else if (Tag == "password")
            {
                texIcon1 = TextureManager.GetTexture("locked");
                texIcon2 = TextureManager.GetTexture("view");
                ico1 = new SpriteObject(texIcon1, 0, 0, 35, 35) { Color = Color4.DarkGray };
                ico2 = new SpriteObject(texIcon2, 0, 0, 35, 35) { Color = Color4.DarkGray };
                AddChild(ico1);
                AddChild(ico2);
            }
            else if (Tag == "email")
                texIcon1 = TextureManager.GetTexture("emailico");
            else
                texIcon1 = TextureManager.GetTexture("default-icon"); 
        }
        protected override void OnContextSet()
        {
            if (Game != null)
            {
                Game.KeyDown += HandleKeyDown;
                Game.TextInput += HandleTextInput;
            }
        }

        private void HandleTextInput(TextInputEventArgs e)
        {
            if (!IsFocused) return;

            char c = (char)e.Unicode;
            if (c >= ' ' && c != 127)
            {
                if (_isAllSelected)
                {
                    _input = "";
                    _isAllSelected = false;
                }
                _input += c;
                UpdateDisplayedText();
            }
        }

        private void HandleKeyDown(KeyboardKeyEventArgs e)
        {
            if (!IsFocused) return;

            bool ctrlPressed = e.Modifiers.HasFlag(KeyModifiers.Control) ||
                               e.Modifiers.HasFlag(KeyModifiers.Super);

            if (ctrlPressed)
            {
                switch (e.Key)
                {
                    case Keys.V:
                        string clipboard = Clipboard.GetText();
                        if (!string.IsNullOrEmpty(clipboard))
                        {
                            if (_isAllSelected) _input = "";
                            _input += clipboard;
                            _isAllSelected = false;
                            UpdateDisplayedText();
                        }
                        return;

                    case Keys.C:
                        if (!string.IsNullOrEmpty(_input)) Clipboard.SetText(_input);
                        return;

                    case Keys.A:
                        if (!string.IsNullOrEmpty(_input))
                        {
                            _isAllSelected = true;
                            UpdateDisplayedText();
                        }
                        return;

                    case Keys.X:
                        if (!string.IsNullOrEmpty(_input))
                        {
                            Clipboard.SetText(_input);
                            _input = "";
                            _isAllSelected = false;
                            UpdateDisplayedText();
                        }
                        return;
                }
            }

            switch (e.Key)
            {
                case Keys.Backspace:
                    if (_isAllSelected)
                    {
                        _input = "";
                        _isAllSelected = false;
                    }
                    else if (_input.Length > 0)
                        _input = _input[..^1];
                    UpdateDisplayedText();
                    break;

                case Keys.Enter:
                case Keys.KeyPadEnter:
                    OnEnterPressed();
                    break;

                case Keys.Escape:
                    IsFocused = false;
                    _isAllSelected = false;
                    break;
            }
        }

        private void UpdateDisplayedText()
        {
            string displayed = IsPassword ? new string('*', _input.Length) : _input;

            _selectionBackground.Active = _isAllSelected && IsFocused && !string.IsNullOrEmpty(_input);

            if (IsFocused)
            {
                bool showCursor = DateTime.Now.Millisecond % 800 < 400;
                InputText.Text = displayed + (showCursor ? "|" : "");
            }
            else
            {
                InputText.Text = displayed;
            }

            InputText.Color = _isAllSelected && IsFocused ? new Color4(1f, 1f, 1f, 1f) : Color4.White;
            PlaceHolder.Active = string.IsNullOrEmpty(_input);
        }

        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime, mouse);

            PlaceHolder.Color = PlaceHolderColor;
            PlaceHolder.Text = PlaceHolderText;


            // Обработка фокуса
            float designMouseX = mouse.X / CanvasScale.X;
            float designMouseY = mouse.Y / CanvasScale.Y;
            bool hovered = IsPointInside(designMouseX, designMouseY);

            if (mouse.IsButtonPressed(MouseButton.Left))
            {
                IsFocused = hovered;
                if (hovered) _isAllSelected = false;
            }

            // Позиционируем текст с отступом от левого края
            var bounds = GetDesignBounds();
            float textX = bounds.designLeft + TextLeftPadding;
            float textXright = Scale.X - bounds.effWidth;

            InputText.LocalPosition = new Vector2(textX + 10 - WorldPosition.X, 5);
            PlaceHolder.LocalPosition = new Vector2(textX + 10 - WorldPosition.X, 5);

            if (ico1 != null && ico2 != null)
            {
                ico1.LocalPosition = new Vector2(textX - 7 - WorldPosition.X, 0);
                ico2.LocalPosition = new Vector2(textXright, 0);
            }

            // Позиционируем фон выделения
            _selectionBackground.WorldPosition = new Vector2(bounds.designLeft, bounds.designTop);
            _selectionBackground.Scale = new Vector2(Scale.X, Scale.Y);

            UpdateDisplayedText();
        }

        public void Clear() => Text = "";

        public void OnEnterPressed()
        {
            IsFocused = false;
            _isAllSelected = false;
        }

        public void Dispose()
        {
            if (Game != null)
            {
                Game.KeyDown -= HandleKeyDown;
                Game.TextInput -= HandleTextInput;
            }
        }
    }
}