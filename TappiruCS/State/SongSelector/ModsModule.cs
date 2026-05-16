using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;
using TappiruCS.Core.GameObject;
using TappiruCS.GameLogic;
using TappiruCS.GameLogic.Mod;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;
using TappiruCS.Render.Text;
using TappiruCS.UI.Toggle;

namespace TappiruCS.State.SongSelector
{
    public class ModsModule : ModuleWindow
    {
        public Background background;
        public TextObject EasyMods;
        public CheckBox NoFailBox;


        public ModsModule(Scene scene, List<GameMod> mods) : base(scene)
        {

            background = new Background(0) { Opacity = 0.7f, Layer = 10,AllowHover = true };
            EasyMods = new TextObject("Easy mods", 50, 340,96)
            {
                Color = Color4.Green,
                HasOutline = false,
                Align = TextAlign.Left,
                Layer = 12,
                AllowHover = false,
                ScaleMultiply = 0.8f
            };


            NoFailBox = new CheckBox(470, 400, 86, 96,"NoFail")
            {
                Description = "Вы не можете умереть как бы не старались.",
                Layer = 11,
                ScaleMultiply = 2f
            };

            NoFailBox.IsSelected = mods.Any(m => m.GetType() == typeof(NoFailMod));

            NoFailBox.OnSelectedChanged += (isSelected) =>
            {
                if (isSelected)
                {
                    if (!mods.Any(m => m is NoFailMod))
                        mods.Add(new NoFailMod());
                }
                else
                {
                    var mod = mods.FirstOrDefault(m => m is NoFailMod);
                    if (mod != null)
                        mods.Remove(mod);
                }
            };

            obj.Add(background);
            obj.Add(NoFailBox);
            obj.Add(EasyMods);

        }
    }
}
