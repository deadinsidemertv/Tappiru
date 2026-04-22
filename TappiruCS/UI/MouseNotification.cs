using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Text;
using TappiruCS.Core.GameObject;
using TappiruCS.UI.TextAbstract;

namespace TappiruCS.UI
{
    public class MouseNotification : GameObject
    {
        public string text { get; set; } = "5022222222%";

        public SpriteObject spriteBG;
        public TextObject Text;
        public Scene _scene;

        public MouseNotification(Scene scene)
        {
            _scene = scene;
            Pivot = Vector2.Zero;
            spriteBG = new SpriteObject(0, 0, 0, 40, 40) { Parent = this,AllowHover =false,Pivot = Vector2.Zero };
            Text = new TextObject("", 0, 0)
            {
                Parent = this,
                AllowHover = false,
                ScaleMultiply = 0.25f,
                HasShadow = true,
                Pivot = Vector2.Zero
            };
            Layer = 10;
            AllowHover = false;

            AddChild(spriteBG);
            AddChild(Text);
        }

        public override void Update(double deltaTime,MouseState mouse)
        {
            base.Update(deltaTime);

            var VirtualMouse = _scene.GetVirtualMousePosition(mouse);

            spriteBG.Opacity = 0.5f;

            this.Position = VirtualMouse;
            GetSpriteScale();
            spriteBG.Position = new Vector2(this.Position.X + 15, this.Position.Y + 20);
            Text.Position = new Vector2(spriteBG.Position.X+15, spriteBG.Position.Y);
            Text.Text = text;
        }

        private void GetSpriteScale()
        {
            spriteBG.Scale = new Vector2(Text.Scale.X*40,Text.Scale.Y*40);
        }

        public override void Draw(Matrix4 projection)
        {
            base.Draw(projection);
        }

    }
}
