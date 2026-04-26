using OpenTK.Mathematics;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;

namespace TappiruCS.State.Menu.Option
{
    public class SectionContainer : GameObject
    {
        private SpriteObject _background;
        private TextObject _title;

        public SectionContainer(string title, float width, float height, float x, float y)
        {
            LocalPosition = new Vector2(x, y);

            _background = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, width, height)
            {
                Color = new Color4(0.2f, 0.2f, 0.2f, 0.8f),
                Opacity = 0.8f,
                Parent = this
            };
            AddChild(_background);

            _title = new TextObject(title, 20, 15, 48f)
            {
                Color = Color4.White,
                Parent = this,
                AllowHover = false,
                ScaleMultiply = 0.6f
            };
            AddChild(_title);
        }

        public void AddControl(GameObject control, float localX, float localY)
        {
            control.LocalPosition = new Vector2(localX, localY);
            control.Parent = this;
            AddChild(control);
        }
    }
}