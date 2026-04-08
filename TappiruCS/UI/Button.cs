using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core;
using TappiruCS.Render;
using static TappiruCS.Render.TextRender;

namespace TappiruCS.UI
{
    public class Button : GameObject
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly TextRender _textRenderer;

        private readonly int _textureId;

        public string Text { get; set; }
        public Color4 TextColor { get; set; } = Color4.White;
        public Vector2 TextOffset { get; set; } = new Vector2(0f, 0f);
        public float TextScale { get; set; } = 0.5f;
        public TextAlign TextAlign { get; set; } = TextAlign.Center;

        public int ButtonImage { get; set; } = 0;
        public Vector2 ImagePadding { get; set; } = new Vector2(0f, 0f);
        public Vector2 ImageOffset { get; set; } = Vector2.Zero;
        public Vector2 ImageScale { get; set; } = new Vector2(0.18f, 1f);
        public bool IsImaged { get; set; } = false;

        public Color4 NormalColor { get; set; } = new Color4(1f, 1f, 1f, 1f);
        public Color4 HoverColor { get; set; } = new Color4(0.5f, 0.5f, 1.05f, 1f);
        public Color4 PressColor { get; set; } = new Color4(0.75f, 0.75f, 0.75f, 1f);

        private Color4 _currentColor;

        private readonly TextObject _textObject;
        private readonly SpriteObject _imageObject;

        public event Action OnClick;
        public event Action<Button, bool> HoverStateChanged;

        public Button(SpriteBatch spriteBatch, TextRender textRenderer,
                      float x, float y, float width, float height,
                      string textureName, string text, Color4 color)
        {
            _spriteBatch = spriteBatch;
            _textRenderer = textRenderer;
            Text = text;

            Position = new Vector2(x, y);
            Scale = new Vector2(width, height);

            _textureId = TextureManager.GetTexture(textureName);

            TextColor = color;
            _currentColor = NormalColor;

            _textObject = new TextObject(_textRenderer, text, x, y, 1f)
            {
                Color = TextColor,
                ScaleMultiply = TextScale,
                Align = TextAlign,
                Pivot = new Vector2(0.5f, 0.5f),
                Parent = this
            };

            _imageObject = new SpriteObject(_spriteBatch, 0, x, y, 1f, 1f)
            {
                Pivot = new Vector2(0.5f, 0.5f),
                Parent = this,
                Active = false
            };
        }

        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime);

            bool isPressed = IsHovered && mouse.IsButtonDown(MouseButton.Left);

            if (isPressed)
                _currentColor = PressColor;
            else if (IsHovered)
                _currentColor = HoverColor;
            else
                _currentColor = NormalColor;

            if (IsHovered && mouse.IsButtonPressed(MouseButton.Left))
                OnClick?.Invoke();

            // Автоматическое масштабирование детей
            _textObject.CanvasScale = CanvasScale;
            _textObject.Text = Text;
            _textObject.Color = TextColor;
            _textObject.ScaleMultiply = TextScale;

            float offsetScale = ScaleMultiply;
            _textObject.Position = new Vector2(
                Position.X + TextOffset.X * offsetScale,
                Position.Y + TextOffset.Y * offsetScale);

            _imageObject.CanvasScale = CanvasScale;

            if (IsImaged)
            {
                _imageObject.Active = true;
                _imageObject._textureId = ButtonImage;

                Vector2 offset = ImageOffset.LengthSquared > 0.0001f ? ImageOffset : ImagePadding;
                float scaledOffsetX = offset.X * offsetScale;
                float scaledOffsetY = offset.Y * offsetScale;

                _imageObject.Position = new Vector2(Position.X + scaledOffsetX, Position.Y + scaledOffsetY);
                _imageObject.Scale = new Vector2(Scale.X * ImageScale.X, Scale.Y * ImageScale.Y);
                _imageObject.ScaleMultiply = 1f;
            }
            else
            {
                _imageObject.Active = false;
            }
        }

        public override void Draw(Matrix4 projection)
        {
            var (dLeft, dTop, effW, effH) = GetDesignBounds();
            float sLeft = dLeft * CanvasScale.X;
            float sTop = dTop * CanvasScale.Y;
            float sW = effW * CanvasScale.X;
            float sH = effH * CanvasScale.Y;

            _spriteBatch.Draw(_textureId, sLeft, sTop, sW, sH, 0, 0, 1, 1,
                _currentColor.R, _currentColor.G, _currentColor.B, _currentColor.A, projection);

            _textObject.Draw(projection);
            if (_imageObject.Active)
                _imageObject.Draw(projection);
        }

        
        public override void SetHover(bool hover)
        {
            // Если состояние не изменилось с прошлого вызова — выходим
            if (IsHovered == hover)
                return;

            bool wasHovered = IsHovered;           // старое состояние до изменения
            IsHovered = hover;

            // === АНИМАЦИИ И ЦВЕТ ===
            if (hover)
            {
                if (Tag == "")
                    this.AnimScale(1.15f, 0.18f);

                _currentColor = HoverColor;
            }
            else
            {
                this.AnimScaleReset(0.22f);
                _currentColor = NormalColor;
            }

            // === ЗВУК НА HOVER — ТОЛЬКО ПРИ РЕАЛЬНОМ ВХОДЕ ===
            if (hover && !wasHovered)
            {
                if (AudioManager.Instance != null && !Tag.Contains("NoHoverSound"))
                {
                    AudioManager.Instance.PlaySoundEffect("hover", 0.6f);
                }
            }

            HoverStateChanged?.Invoke(this, hover);

            // Вызываем базовые события
            base.SetHover(hover);
        }
    }
}