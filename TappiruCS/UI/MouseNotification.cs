using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.UI.TextAbstract;

namespace TappiruCS.UI
{
    public class MouseNotification : GameObject
    {
        public string text { get; set; } = "5022222222%";

        public SpriteObject spriteBG;
        public TextObject Text;
        public Scene _scene;

        private readonly Vector2 _spriteBaseSize = new Vector2(40, 40); // те самые 40,40 из конструктор
        // Отступы вокруг текста (в логических единицах)
        public Vector2 Padding { get; set; } = new Vector2(15, 10);

        // Смещение от курсора
        public Vector2 CursorOffset { get; set; } = new Vector2(15, 20);

        public MouseNotification(Scene scene)
        {
            _scene = scene;
            Pivot = Vector2.Zero;

            spriteBG = new SpriteObject(0, 0, 0, 1, 1)
            {
                Parent = this,
                AllowHover = false,
                Pivot = Vector2.Zero
            };

            Text = new TextObject("", 0, 0)
            {
                Parent = this,
                AllowHover = false,
                ScaleMultiply = 0.25f,
                HasShadow = true,
                Pivot = Vector2.Zero,
                FontSize = 24f   // задайте желаемый размер шрифта
            };

            Layer = 10;
            AllowHover = false;

            AddChild(spriteBG);
            AddChild(Text);
        }

        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime, mouse);

            var virtualMouse = _scene.GetVirtualMousePosition(mouse);
            spriteBG.Opacity = 0.5f;
            Text.Text = text;

            // Вычисляем финальный масштаб текста
            float baseScale = TR.GetScaleFromFontSize(Text.FontSize);
            float finalScaleX = baseScale * Text.Scale.X * Text.ScaleMultiply;
            float finalScaleY = baseScale * Text.Scale.Y * Text.ScaleMultiply;

            // Размер текста в логических единицах
            Vector2 textSize = TR.MeasureString(text, finalScaleX, finalScaleY);

            // Желаемый размер фона (с отступами)
            Vector2 desiredSize = new Vector2(
                textSize.X + Padding.X * 2,
                textSize.Y + Padding.Y * 2
            );

            // Преобразуем в множители Scale
            spriteBG.Scale = new Vector2(
                desiredSize.X / _spriteBaseSize.X,
                desiredSize.Y / _spriteBaseSize.Y
            );

            // Позиционирование
            Position = virtualMouse;
            spriteBG.Position = new Vector2(
                Position.X + CursorOffset.X,
                Position.Y + CursorOffset.Y
            );
            Text.Position = new Vector2(
                spriteBG.Position.X + Padding.X,
                spriteBG.Position.Y + Padding.Y
            );
        }


        public override void Draw(Matrix4 projection)
        {
            base.Draw(projection);
        }
    }
}