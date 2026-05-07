using OpenTK.Mathematics;
using TappiruCS.Core.GameObject;
using TappiruCS.Render.Text;
using TappiruCS.State.Edit.Core;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;
using TappiruCS.Render;

namespace TappiruCS.State.Edit.UI.Panels
{
    internal class PhrasePropertiesPanel
    {
        private TextObject? _infoText;
        private readonly Scene _scene;

        public PhrasePropertiesPanel(Scene scene)
        {
            _scene = scene;
        }

        public void Build()
        {
            _infoText = new TextObject("", 1650, 280, 52f)
            {
                Align = TextAlign.Left,
                Color = Color4.White,
                FixedColor = true
            };
            _scene.Add(_infoText);
        }

        public void Sync(ITimelineSelectable? selected)
        {
            if (_infoText == null) return;

            if (selected == null)
            {
               
            }
            else if(selected.GetType() == typeof(Phrase))
            {
                Container PhraseProperties = new Container(1740, 400);
                var label = new TextObject("выбранный объект", -160, -270,36f);
                label.Color = "#919bb8";
                label.Align = TextAlign.Left;

                var spriteBackground = new SpriteObject(TextureManager.GetTexture("module-window"), 0, 0,345,600);
                
                PhraseProperties.AddChild(spriteBackground);
                PhraseProperties.AddChild(label);
                _scene.Add(PhraseProperties);
            }
            else if(selected.GetType() == typeof(TappiruCS.State.Edit.Core.SliderTiming))
            {
                Container PhraseProperties = new Container(1600, 540);
                var label = new TextObject("slider", 0, 0);
                var spriteBackground = new SpriteObject(TextureManager.GetTexture("window-module_3"), 0, 0, 300, 600);

                PhraseProperties.AddChild(spriteBackground);
                PhraseProperties.AddChild(label);
                _scene.Add(PhraseProperties);
            }
        }


        


    }
}