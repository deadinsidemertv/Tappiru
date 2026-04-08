using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core;
using TappiruCS.Render;

namespace TappiruCS.UI
{
    public class ListButtons : GameObject
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly TextRender _textRenderer;

        public float ScaleMultiplyList = 0.6f;

        public float ScrollOffsetY { get; private set; } = 0f;
        public float ScrollSpeed = 0.5f;
        private const float MaxScrollSpeed = 120f;

        public List<Button> Buttons;

        public ListButtons(SpriteBatch spriteBatch, TextRender textRenderer, int count,
                           float x, float y, float width, float height,
                           string textureid, string text)
        {
            Buttons = new List<Button>(count);
            for (int i = 0; i < count; i++)
            {
                // Теперь создаём кнопки от центра (pivot = 0.5f)
                float centerX = x + width * 0.5f;
                float centerY = y + (height * 0.5f) + (height*0.6f) * i;

                Buttons.Add(new Button(spriteBatch, textRenderer,
                    centerX, centerY, width, height,
                    textureid, text, Color4.White)
                {
                    ScaleMultiply = ScaleMultiplyList
                });
            }
            var btn = Buttons[^1];   // или Buttons[Buttons.Count - 1]
            btn.Parent = this;
        }

        public override void Draw(Matrix4 projection)
        {
            foreach (Button button in Buttons)
            {
                button.CanvasScale = this.CanvasScale;
                button.Draw(projection);
            }
        }

        public override void Update(double deltaTime, MouseState mouse)
        {
            foreach (Button button in Buttons)
            {
                button.CanvasScale = this.CanvasScale;
                button.ScaleMultiply = ScaleMultiplyList;   // ← обновляем локальный масштаб
                button.Parent = this;                       // на всякий случай

                float originalY = button.Position.Y;
                button.Position = new Vector2(button.Position.X, originalY - ScrollOffsetY);

                button.Update(deltaTime, mouse);
            }
        }

        public void Scroll(float deltaY)
        {
            ScrollOffsetY -= deltaY * ScrollSpeed;

            float maxScroll = Math.Max(0, Buttons.Count * (100f + 10f) - 600f);
            // ScrollOffsetY = Math.Clamp(ScrollOffsetY, 0, maxScroll); // раскомментируйте если нужно
        }
    }
}