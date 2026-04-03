using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using TappiruCS.Core.TappiruCS.Core;
using TappiruCS.Render;

namespace TappiruCS.UI
{
    public class ListButtons : GameObject
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly TextRender _textRenderer;

        public float ScaleMultiplyList = 1f;

        public float ScrollOffsetY { get; private set; } = 0f;     // текущая позиция скролла (в пикселях дизайна)
        public float ScrollSpeed = 0.5f;                            // чувствительность (чем больше — тем быстрее)
        private const float MaxScrollSpeed = 120f;                 // ограничение

        public List<Button> buttons;

        public ListButtons(SpriteBatch spriteBatch, TextRender textRenderer, int count,float x ,float y,float width, float height,string textureid,string text)
        {
            buttons = new List<Button>(count);
            for(int i = 0; i < count; i++)
            {
                buttons.Add(new Button(spriteBatch, textRenderer, x, y + height * i *1.05f, width, height, textureid, text, Color4.White) { ScaleMultiply = ScaleMultiplyList });
                
            }
        }
        public override void Draw(Matrix4 projection)
        {
            foreach (Button button in buttons)
            {
                button.CanvasScale = this.CanvasScale;   // ← и здесь тоже (на всякий случай)
                button.Draw(projection);
            }

            
        }

        public override void Update(double deltaTime, MouseState mouse)
        {
            foreach (Button button in buttons)
            {
                button.CanvasScale = this.CanvasScale;   // ← вот это было нужно
                button.Update(deltaTime, mouse);
            }
            for (int i = 0; i < buttons.Count; i++)
            {
                Button btn = buttons[i];
                float originalY = btn.Position.Y;                    // оригинальная позиция из конструктора
                btn.Position = new Vector2(btn.Position.X, originalY - ScrollOffsetY); //originalY - ScrollOffsetY

                btn.Update(deltaTime, mouse);
            }

        }

        public void Scroll(float deltaY)
        {
            ScrollOffsetY -= deltaY * ScrollSpeed;   // знак минус — естественное направление

            // Ограничиваем скролл (чтобы не улетать слишком далеко)
            float maxScroll = Math.Max(0, buttons.Count * (100f + 10f) - 600f); // подбери под свой размер окна
            //ScrollOffsetY = Math.Clamp(5, -5, maxScroll);
        }
    }
}
