using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.UI.TextAbstract;
using TappiruCS.Render.Text;

namespace TappiruCS.UI
{
    public class MouseNotification : GameObject
    {
        public string text { get; set; } = "12%";

        public SpriteObject spriteBG;
        public TextObject Text;
        public Scene _scene;

        // Отступы вокруг текста (в логических единицах)
        public Vector2 Padding { get; set; } = new Vector2(10, 5);

        // Смещение от курсора
        public Vector2 CursorOffset { get; set; } = new Vector2(15, 20);

        public MouseNotification(Scene scene)
        {
            Active = false;

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
                HasShadow = true,
                Pivot = Vector2.Zero,
                Align = TextAlign.Left,
                FontSize = 24f   // задайте желаемый размер шрифта
            };

            Layer = 1000;
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

            // 1. Вычисляем финальный масштаб текста (как это делает TextObject.Draw)
            float baseScale = TR.GetScaleFromFontSize(Text.FontSize);
            float finalScaleX = baseScale * Text.Scale.X * Text.ScaleMultiply;
            float finalScaleY = baseScale * Text.Scale.Y * Text.ScaleMultiply;

            // 2. Получаем размер текста в логических единицах
            Vector2 textSize = TR.MeasureString(text, finalScaleX, finalScaleY);

            // 3. Задаём размер фона (с отступами) в логических единицах
            spriteBG.Scale = new Vector2(
                textSize.X + Padding.X * 2,
                textSize.Y + Padding.Y * 2
            );

            // 4. Позиционируем всё относительно курсора
            WorldPosition = virtualMouse;

            spriteBG.WorldPosition = new Vector2(
                WorldPosition.X + CursorOffset.X,
                WorldPosition.Y + CursorOffset.Y
            );

            Text.WorldPosition = new Vector2(
                spriteBG.WorldPosition.X + Padding.X,
                spriteBG.WorldPosition.Y + Padding.Y
            );
        }


        public override void Draw(Matrix4 projection)
        {
            base.Draw(projection);
        }
    }
}