using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using TappiruCS.Core;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.Render.Text;
using TappiruCS.UI.Sprite;
using TappiruCS.UI.TextAbstract;

namespace TappiruCS.UI
{
    public class InputField : GameObject
    {
        private readonly SpriteObject InputBackground;
        private readonly SpriteObject _selectionBackground;
        public  TextObject InputText;
        public  TextObject PlaceHolder;

        public SpriteObject? IconLeft { get; private set; }
        public SpriteObject? IconRight { get; private set; }

        public string PlaceHolderText { get; set; } = "Введите текст...";
        public Color4 PlaceHolderColor { get; set; } = new Color4(0.5f, 0.5f, 0.5f, 1f);

        private string _input = "";
        public bool IsPassword { get; set; } = false;

        public bool IsFocused => Game.FocusedInputField == this;

        private bool _isAllSelected = false;

        public float LeftPadding { get; set; } = 25f;
        public float RightPadding { get; set; } = 120f;
        public float IconSpacing { get; set; } = 3f;

        public event Action<string>? OnTextChanged;
        public event Action? OnEnterPressed;
        public event Action? OnFocusGained;
        public event Action? OnFocusLost;

        public string Text
        {
            get => _input;
            set
            {
                var oldValue = _input;
                _input = value ?? "";
                if (oldValue != _input)
                    OnTextChanged?.Invoke(_input);
                UpdateDisplayedText();
            }
        }

        public InputField(float x, float y, float width, float height)
        {
            LocalPosition = new Vector2(x, y);
            Scale = new Vector2(width, height);

            InputBackground = new SpriteObject(TextureManager.GetTexture("input-field2"), 0, 0, width, height)
            {
                Opacity = 0.8f,
            };

            _selectionBackground = new SpriteObject(TextureManager.GetTexture("input-field1"), 0, 0, width, height)
            {
                Color = new Color4(0.3f, 0.6f, 1.0f, 0.35f),
                Pivot = new Vector2(0f, 0f),
                Active = false
            };

            InputText = new TextObject("", 0, -5, 48f)
            {
                Color = Color4.White,
                Align = TextAlign.Left,
                Pivot = new Vector2(0f, 0.5f),
                Layer = 5
            };

            PlaceHolder = new TextObject(PlaceHolderText, 0, 0, 48f)
            {
                Color = PlaceHolderColor,
                Align = TextAlign.Left,
                Pivot = new Vector2(0f, 0.5f)
            };

            AddChild(InputBackground);
            AddChild(_selectionBackground);
            AddChild(PlaceHolder);
            AddChild(InputText);
        }

        public void SetIconLeft(string textureName, Color4? color = null)
        {
            if (IconLeft != null) RemoveChild(IconLeft);

            var texId = TextureManager.GetTexture(textureName);
            IconLeft = new SpriteObject(texId, 0, 0, 32, 32)
            {
                Color = color ?? Color4.DarkGray
            };
            AddChild(IconLeft);
        }

        public void SetIconRight(string textureName, Color4? color = null)
        {
            if (IconRight != null) RemoveChild(IconRight);

            var texId = TextureManager.GetTexture(textureName);
            IconRight = new SpriteObject(texId, 0, 0, 32, 32)
            {
                Color = color ?? Color4.DarkGray
            };
            AddChild(IconRight);
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
            if (Game.FocusedInputField != this) return;

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
                OnTextChanged?.Invoke(_input);
            }
        }

        private void HandleKeyDown(KeyboardKeyEventArgs e)
        {
            if (Game.FocusedInputField != this) return;

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
                            OnTextChanged?.Invoke(_input);
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
                            OnTextChanged?.Invoke(_input);
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
                    OnTextChanged?.Invoke(_input);
                    break;

                case Keys.Enter:
                case Keys.KeyPadEnter:
                    OnEnterPressed?.Invoke();
                    this.OnEnterPressed?.Invoke();
                    if (Game != null) Game.FocusedInputField = null;
                    break;

                case Keys.Escape:
                    if (Game != null) Game.FocusedInputField = null;
                    _isAllSelected = false;
                    OnFocusLost?.Invoke();
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

            PlaceHolder.Active = string.IsNullOrEmpty(_input) && !IsFocused;
            PlaceHolder.Text = PlaceHolderText;
        }

        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime, mouse);

            float designMouseX = mouse.X / CanvasScale.X;
            float designMouseY = mouse.Y / CanvasScale.Y;
            bool hovered = IsPointInside(designMouseX, designMouseY);

            if (mouse.IsButtonPressed(MouseButton.Left))
            {
                if (hovered)
                {
                    if (Game != null)
                        Game.FocusedInputField = this;

                    _isAllSelected = false;
                    OnFocusGained?.Invoke();
                }
                else if (Game.FocusedInputField == this)
                {
                    Game.FocusedInputField = null;
                    OnFocusLost?.Invoke();
                }
            }

            UpdateVisualPositions();
            UpdateDisplayedText();
        }

        private void UpdateVisualPositions()
        {
            var bounds = GetDesignBounds();
            float currentLeftPadding = LeftPadding;

            if (IconLeft != null)
            {
                currentLeftPadding += IconLeft.Scale.X + IconSpacing;
                IconLeft.LocalPosition = new Vector2(
                    bounds.designLeft + LeftPadding - WorldPosition.X, 0);
            }

            if (IconRight != null)
            {
                IconRight.LocalPosition = new Vector2(
                    bounds.designLeft + Scale.X - RightPadding - IconRight.Scale.X - WorldPosition.X, 0);
            }

            float textX = bounds.designLeft + currentLeftPadding - WorldPosition.X;
            float textY = 3f;

            InputText.LocalPosition = new Vector2(textX, textY);
            PlaceHolder.LocalPosition = new Vector2(textX, textY);

            _selectionBackground.WorldPosition = new Vector2(bounds.designLeft, bounds.designTop);
            _selectionBackground.Scale = Scale;
        }

        public void Clear() => Text = "";

        public void Focus()
        {
            if (Game != null)
                Game.FocusedInputField = this;
            OnFocusGained?.Invoke();
        }

        public void Dispose()
        {
            if (Game != null)
            {
                Game.KeyDown -= HandleKeyDown;
                Game.TextInput -= HandleTextInput;

                if (Game.FocusedInputField == this)
                    Game.FocusedInputField = null;
            }
        }
    }
}