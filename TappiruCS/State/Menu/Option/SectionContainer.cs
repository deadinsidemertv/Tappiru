using OpenTK.Mathematics;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;

namespace TappiruCS.State.Menu.Option
{
    public class SectionContainer : Container
    {
        private SpriteObject _background;
        private TextObject _title;

        public SectionContainer(string title, float width, float height, float x, float y):base(x,y)
        {
            LocalPosition = new Vector2(x, y);
        }

        public void AddControl(GameObject control, float localX, float localY)
        {
            control.LocalPosition = new Vector2(localX, localY);
            AddChild(control);
            RecalculateSize();

        }
    }
}