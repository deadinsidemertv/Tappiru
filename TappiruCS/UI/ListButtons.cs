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

       

        public List<Button> buttons;

        public ListButtons(SpriteBatch spriteBatch, TextRender textRenderer, int count,float x ,float y,float width, float height,string textureid,string text)
        {
            buttons = new List<Button>(count);
            for(int i = 0; i < count; i++)
            {
                buttons.Add(new Button(spriteBatch, textRenderer, x, y + height * i + 10, width, height, textureid, text, Color4.White) { ScaleMultiply = 1f });
                
            }
        }
        public override void Draw(Matrix4 projection) 
        {
            foreach(Button button in buttons)
                button.Draw(projection);
        }

        public override void Update(double deltaTime, MouseState mouse)
        {
            foreach(Button button in buttons)
                button.Update(deltaTime, mouse);
        }

    }
}
