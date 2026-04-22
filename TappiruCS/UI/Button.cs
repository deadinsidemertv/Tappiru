using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using static TappiruCS.Render.TextRender;
using TappiruCS.Tween;
using TappiruCS.UI.TextAbstract;

namespace TappiruCS.UI
{
    public class Button : GameObject
    {

        private readonly int _textureId;

        public string Text { get; set; }
        public Color4 TextColor { get; set; } = Color4.White;
        public Vector2 TextOffset { get; set; } = Vector2.Zero;
        public float FontSize { get; set; } = 72f;
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

        private readonly SpriteObject _buttonBackground;
        private readonly TextObject _textObject;
        private readonly SpriteObject _imageObject;

        public event Action OnClick;
        public event Action<Button, bool> HoverStateChanged;

        public Button(float x, float y, float width, float height,string textureName, string text)
        {
            NormalColor  = new Color4(1f, 1f, 1f, Opacity);
            HoverColor  = new Color4(0.5f, 0.5f, 1.05f, Opacity);
            PressColor  = new Color4(0.75f, 0.75f, 0.75f, Opacity);
            Text = text;

            Position = new Vector2(x, y);
            Scale = new Vector2(width, height);

            Pivot = new Vector2(0.5f, 0.5f);

            _textureId = TextureManager.GetTexture(textureName);

            TextColor = TextColor;
            _currentColor = NormalColor;

            _buttonBackground = new SpriteObject( _textureId, x, y, width, height) 
            { 
                
            };

            _textObject = new TextObject(text, x, y,FontSize)
            {
                Align = TextRender.TextAlign.Center,
                Pivot = new Vector2(0.5f, 0.5f),
                Color = TextColor,
                AllowHover = false
            };

            _imageObject = new SpriteObject(0, x, y, 1f, 1f)
            {
                Pivot = new Vector2(0.5f, 0.5f),
                Active = false
            };

            AddChild(_buttonBackground);
            AddChild(_textObject);
            AddChild(_imageObject);
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

            _buttonBackground.Color = _currentColor;

            if (IsHovered && mouse.IsButtonPressed(MouseButton.Left))
                OnClick?.Invoke();

            // === ОБНОВЛЕНИЕ ДЕТЕЙ ===
            _textObject.Position = Position;
            _textObject.Scale = new Vector2(ScaleMultiply,ScaleMultiply);
            _textObject.FontSize = FontSize;
            _textObject.Text = Text;
            _textObject.Color = TextColor;
            _textObject.Pivot = new Vector2(0.5f, 0.5f);


            _textObject.Align = TextRender.TextAlign.Left;
            // ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←
            // Главное исправление:
            _buttonBackground.Position = Position;
            // ←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←←
            if (TextOffset != Vector2.Zero)
            {
                float s = ScaleMultiply;
                _textObject.Position += new Vector2(TextOffset.X * s, TextOffset.Y * s);
            }

            float offsetScale = ScaleMultiply;

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


            if (hover)
            {
                if (string.IsNullOrEmpty(Tag))
                    this.AnimScale(1.15f, 0.18f);

                _currentColor = HoverColor;
            }
            else
            {
                this.AnimScaleReset(0.22f);
                _currentColor = NormalColor;
            }

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
    }
}