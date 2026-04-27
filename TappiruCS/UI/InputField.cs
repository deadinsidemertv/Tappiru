// InputField.cs — с красивым визуальным выделением при Ctrl+A
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using TappiruCS.Core;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.UI.TextAbstract;
using static TappiruCS.Render.Text.Font;

namespace TappiruCS.UI
{
    public class InputField : GameObject
    {

        private readonly int _bgTextureId;

        private readonly SpriteObject InputBackground;
        private readonly TextObject InputText;
        private readonly TextObject PlaceHolder;

        // Новое: фон для выделения
        private readonly SpriteObject _selectionBackground;

        public string PlaceHolderText { get; set; } = "Введите текст...";
        public Color4 PlaceHolderColor { get; set; } = Color4.DarkGray;
        private string _input = "";

        public bool IsPassword = false;
        public bool IsFocused { get; private set; } = false;

        private bool _isAllSelected = false;

        public string Text
        {
            get => _input;
            set { _input = value ?? ""; UpdateDisplayedText(); }
        }

        public InputField(float x, float y, float width, float height)
        {
            LocalPosition = new Vector2(x, y);
            Scale = new Vector2(width, height);

            _bgTextureId = TextureManager.GetTexture("btn");

            InputBackground = new SpriteObject(_bgTextureId, 0, 0, width, height)
            {
                ScaleMultiply = 1f,
                Color = new Color4(0.2f, 0.2f, 0.25f, 1f)
            };

            _selectionBackground = new SpriteObject(_bgTextureId, 0, 0, width, height)
            {
                ScaleMultiply = 1f,
                Color = new Color4(0.3f, 0.6f, 1.0f, 0.35f),
                Pivot = new Vector2(0.0f, 0.0f),
                Active = false
            };

            InputText = new TextObject("", x, y, 44f)
            {
                ScaleMultiply = 1f,
                Color = Color4.White,
                Align = TextAlign.Left,
                Pivot = new Vector2(0f, 0f),
                Layer = 5
            };

            PlaceHolder = new TextObject(PlaceHolderText, 0, 0, 44f)
            {
                ScaleMultiply = 1f,
                Color = PlaceHolderColor,
                Align = TextAlign.Left,
                Pivot = new Vector2(0f, 0f),
            };

            AddChild(InputBackground);
            AddChild(_selectionBackground);
            AddChild(PlaceHolder);
            AddChild(InputText);

        }

        protected override void OnContextSet()
        {
            Console.WriteLine($"[InputField] OnContextSet called for InputField at position {WorldPosition}");
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
                    case Keys.V: // Ctrl + V
                        string clipboard = Clipboard.GetText();
                        if (!string.IsNullOrEmpty(clipboard))
                        {
                            if (_isAllSelected) _input = "";
                            _input += clipboard;
                            _isAllSelected = false;
                            UpdateDisplayedText();
                        }
                        return;

                    case Keys.C: // Ctrl + C
                        if (!string.IsNullOrEmpty(_input))
                            Clipboard.SetText(_input);
                        return;

                    case Keys.A: // Ctrl + A — выделить всё + показать визуальное выделение
                        if (!string.IsNullOrEmpty(_input))
                        {
                            _isAllSelected = true;
                            UpdateDisplayedText();
                        }
                        return;

                    case Keys.X: // Ctrl + X
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
                    {
                        _input = _input[..^1];
                    }
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

            // Включаем/выключаем фон выделения
            _selectionBackground.Active = _isAllSelected && IsFocused && !string.IsNullOrEmpty(_input);

            if (IsFocused)
            {
                bool showCursor = DateTime.Now.Millisecond % 800 < 400;
                InputText.Text = displayed + (showCursor ? "|" : "");
                InputText.Color = _isAllSelected ? new Color4(1f, 1f, 1f, 1f) : Color4.White;
            }
            else
            {
                InputText.Text = displayed;
                InputText.Color = Color4.White;
            }

            PlaceHolder.Active = string.IsNullOrEmpty(_input);
        }

        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime, mouse);

            PlaceHolder.Color = PlaceHolderColor;
            PlaceHolder.Text = PlaceHolderText;

            float designMouseX = mouse.X / CanvasScale.X;
            float designMouseY = mouse.Y / CanvasScale.Y;
            bool hovered = IsPointInside(designMouseX, designMouseY);

            if (mouse.IsButtonPressed(MouseButton.Left))
            {
                IsFocused = hovered;
                if (hovered) _isAllSelected = false; // снимаем выделение при клике
            }

            // Позиционирование
            var (left, top, w, h) = GetDesignBounds();
            float padding = 10f * ScaleMultiply;

            InputText.WorldPosition = new Vector2(left + padding, top + padding);
            PlaceHolder.WorldPosition = new Vector2(left + padding, top + padding);
            _selectionBackground.WorldPosition = new Vector2(left, top);
            _selectionBackground.Scale = new Vector2(w, h);

            UpdateDisplayedText();
        }

        public void Clear() => Text = "";

        public void OnEnterPressed()
        {
            IsFocused = false;
            _isAllSelected = false;
        }

        public override void Draw(Matrix4 projection)
        {
            base.Draw(projection);
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