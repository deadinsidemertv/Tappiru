using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.Tween;
using TappiruCS.UI.TextAbstract;
using TappiruCS.Render.Text;
using TappiruCS.Render.Audio;

namespace TappiruCS.UI
{
    public class Button : GameObject
    {
        // Смещение текста относительно центра кнопки (в дизайн-единицах)
        public Vector2 TextOffset { get; set; } = Vector2.Zero;

        public Color4 NormalColor { get; set; } = new Color4(1f, 1f, 1f, 1f);
        public Color4 HoverColor { get; set; } = new Color4(1.1f, 1.1f, 1.2f, 1f);
        public Color4 PressColor { get; set; } = new Color4(0.75f, 0.75f, 0.75f, 1f);

        public Color4 _currentColor;

        // Публичный доступ — настраивать снаружи через .Label и ._buttonBackground
        public readonly SpriteObject _buttonBackground;
        public readonly TextObject Label;

        public event Action OnClick;
        public event Action<Button, bool> HoverStateChanged;

        public Button(float x, float y, float width, float height, string textureName, string text)
        {
            LocalPosition = new Vector2(x, y);
            Scale = new Vector2(width, height);
            Pivot = new Vector2(0.5f, 0.5f);

            _currentColor = NormalColor;

            _buttonBackground = new SpriteObject(TextureManager.GetTexture(textureName), 0, 0, width, height)
            {
                Color = _currentColor
            };

            Label = new TextObject(text, 0, 0, 64)
            {
                Align = TextAlign.Center,
                Pivot = new Vector2(0.5f, 0.5f),
                Color = Color4.White,
                HasShadow = true,
                AllowHover = false
            };

            AddChild(_buttonBackground);
            AddChild(Label);

            InitializeHoverState();
        }

        // Fluent-метод для настройки Label в одном блоке инициализации
        public Button WithLabel(Action<TextObject> configure)
        {
            configure(Label);
            return this;
        }

        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime, mouse);

            // Цвет фона по состоянию
            _currentColor = IsHovered
                ? mouse.IsButtonDown(MouseButton.Left) ? PressColor : HoverColor
                : NormalColor;

            _buttonBackground.Color = _currentColor;

            if (IsHovered && mouse.IsButtonPressed(MouseButton.Left))
                OnClick?.Invoke();

            // TextOffset: смещаем LocalPosition лейбла, не WorldPosition
            // Это безопасно — LocalPosition пересчитывается в Update каждый кадр
            Label.LocalPosition = new Vector2(
                TextOffset.X * ScaleMultiply,
                TextOffset.Y * ScaleMultiply
            );
        }

        public override void Draw(Matrix4 projection)
        {
            if (SB == null) return;
            base.Draw(projection);
        }

        public override void SetHover(bool hover)
        {
            if (IsHovered == hover) return;

            bool wasHovered = IsHovered;
            IsHovered = hover;

            // Меню-кнопки: фон появляется только при наведении
            if (Tag == "menuButton" || Tag == "topButton")
            {
                _buttonBackground.Active = hover;
                _currentColor = hover ? HoverColor : NormalColor with { A = 0f };
            }
            else
            {
                _currentColor = hover ? HoverColor : NormalColor;
            }

            _buttonBackground.Color = _currentColor;

            if (hover && !wasHovered && AudioManager.Instance != null && !Tag.Contains("NoHoverSound"))
                AudioManager.Instance.PlaySoundEffect("hover", 0.6f);

            HoverStateChanged?.Invoke(this, hover);
            base.SetHover(hover);
        }

        public void InitializeHoverState()
        {
            if (Tag == "menuButton" || Tag == "topButton")
            {
                _buttonBackground.Active = false;
                _currentColor = NormalColor with { A = 0f };
            }
            else
            {
                _buttonBackground.Active = true;
                _currentColor = NormalColor;
            }
        }
    }
}