using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.Tween;
using TappiruCS.UI.TextAbstract;
using TappiruCS.Render.Text;

namespace TappiruCS.UI
{
    public class Button : GameObject
    {

        private readonly int _textureId;

        public string Text { get; set; }
        public Color4 TextColor { get; set; } = Color4.White;
        public Vector2 TextOffset { get; set; } = Vector2.Zero;
        public float FontSize { get; set; } = 64f;
        public string FontKey { get; set; } = "UI";
        public TextAlign TextAlign { get; set; } = TextAlign.Center;

        public int ButtonImage { get; set; } = 0;
        public Vector2 ImagePadding { get; set; } = new Vector2(0f, 0f);
        public Vector2 ImageOffset { get; set; } = Vector2.Zero;
        public Vector2 ImageScale { get; set; } = new Vector2(0.18f, 1f);
        public bool IsImaged { get; set; } = false;

        public Color4 NormalColor { get; set; } = new Color4(1f, 1f, 1f, 1f);
        public Color4 HoverColor { get; set; } = new Color4(0.5f, 0.5f, 1.05f, 1f);
        public Color4 PressColor { get; set; } = new Color4(0.75f, 0.75f, 0.75f, 1f);

        public Color4 _currentColor;

        public readonly SpriteObject _buttonBackground;
        private readonly TextObject _textObject;
        private readonly SpriteObject _imageObject;

        public event Action OnClick;
        public event Action<Button, bool> HoverStateChanged;

        public Button(float x, float y, float width, float height, string textureName, string text)
        {
            Text = text;

            LocalPosition = new Vector2(x, y);
            Scale = new Vector2(width, height);
            Pivot = new Vector2(0.5f, 0.5f);

            _textureId = TextureManager.GetTexture(textureName);

            // Правильная инициализация цветов
            NormalColor = new Color4(1f, 1f, 1f, 1f);
            HoverColor = new Color4(1.1f, 1.1f, 1.2f, 1f);   // можно подправить под свой вкус
            PressColor = new Color4(0.75f, 0.75f, 0.75f, 1f);

            _currentColor = NormalColor;

            _buttonBackground = new SpriteObject(_textureId, 0, 0, width, height)
            {
                Color = _currentColor
            };

            _textObject = new TextObject(text, 0, 0, FontSize)
            {
                Align = TextAlign.Center,
                Pivot = new Vector2(0.5f, 0.5f),
                Color = TextColor,
                AllowHover = false
            };

            _imageObject = new SpriteObject(0, 0, 0, 1f, 1f)
            {
                Pivot = new Vector2(0.5f, 0.5f),
                Active = false
            };

            AddChild(_buttonBackground);
            AddChild(_textObject);
            AddChild(_imageObject);

            InitializeHoverState();
        }

        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime,mouse);
            

            bool isPressed = IsHovered && mouse.IsButtonDown(MouseButton.Left);

            if (isPressed)
                _currentColor = PressColor;
            else if (IsHovered)
                _currentColor = HoverColor;
            else
                _currentColor = NormalColor;

            _buttonBackground.Color = _currentColor;

            if (IsHovered && mouse.IsButtonPressed(MouseButton.Left))
                OnClick?.Invoke();

            // === ОБНОВЛЕНИЕ ДЕТЕЙ ===
            _textObject.ScaleMultiply = ScaleMultiply;
            _textObject.FontSize = FontSize;
            _textObject.FontKey = FontKey;
            _textObject.Text = Text;
            _textObject.Color = TextColor;
            _textObject.HasShadow = true;
            _textObject.Pivot = new Vector2(0.5f, 0.5f);


            _textObject.Align = TextAlign.Left;
            // ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←

            // ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←
            if (TextOffset != Vector2.Zero)
            {
                float s = ScaleMultiply;
                _textObject.WorldPosition += new Vector2(TextOffset.X * s, TextOffset.Y * s);
            }

            float offsetScale = ScaleMultiply;

            if (IsImaged)
            {
                _imageObject.Active = true;
                _imageObject._textureId = ButtonImage;

                Vector2 offset = ImageOffset.LengthSquared > 0.0001f ? ImageOffset : ImagePadding;
                float scaledOffsetX = offset.X * offsetScale;
                float scaledOffsetY = offset.Y * offsetScale;

                _imageObject.WorldPosition = new Vector2(WorldPosition.X + scaledOffsetX, WorldPosition.Y + scaledOffsetY);
                _imageObject.Scale = new Vector2(Scale.X * ImageScale.X, Scale.Y * ImageScale.Y);
                _imageObject.ScaleMultiply = 1f;
                //_imageObject.Opacity = Opacity;
            }
            else
            {
                _imageObject.Active = false;
            }
        }

        public override void Draw(Matrix4 projection)
        {
            if (SB == null) return;

            var (dLeft, dTop, effW, effH) = GetDesignBounds();
            float sLeft = dLeft * CanvasScale.X;
            float sTop = dTop * CanvasScale.Y;
            float sW = effW * CanvasScale.X;
            float sH = effH * CanvasScale.Y;

            base.Draw(projection);
        }


        public override void SetHover(bool hover)
        {
            if (IsHovered == hover)
                return;

            bool wasHovered = IsHovered;
            IsHovered = hover;

            // === СПЕЦИАЛЬНОЕ ПОВЕДЕНИЕ ДЛЯ МЕНЮ-КНОПОК (menuButton / topButton) ===
            if (Tag == "menuButton" || Tag == "topButton")
            {
                if (hover)
                {
                    _buttonBackground.Active = true;
                    _currentColor = HoverColor;
                }
                else
                {
                    _buttonBackground.Active = false;
                    _currentColor = NormalColor with { A = 0f }; // полностью прозрачный
                }
            }
            // === ОБЫЧНОЕ ПОВЕДЕНИЕ ДЛЯ ВСЕХ ОСТАЛЬНЫХ КНОПОК ===
            else
            {
                if (hover)
                {


                    _currentColor = HoverColor;
                }
                else
                {
                    _currentColor = NormalColor;
                }
            }

            // Применяем текущий цвет к фону
            _buttonBackground.Color = _currentColor;

            // Звук наведения
            if (hover && !wasHovered)
            {
                if (AudioManager.Instance != null && !Tag.Contains("NoHoverSound"))
                {
                    AudioManager.Instance.PlaySoundEffect("hover", 0.6f);
                }
            }

            HoverStateChanged?.Invoke(this, hover);
            base.SetHover(hover);
        }

        public void InitializeHoverState()
        {
            // Для меню-кнопок фон изначально должен быть скрыт
            if (Tag == "menuButton" || Tag == "topButton")
            {
                _buttonBackground.Active = false;
                _currentColor = NormalColor with { A = 0f };   // полностью прозрачный


            }
            else
            {
                _buttonBackground.Active = true;
                _currentColor = NormalColor;
            }
        }
    }
}