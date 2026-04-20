using OpenTK.Mathematics;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;

namespace TappiruCS.State.Menu
{
    public class OptionModule
    {
        public SpriteObject Bg;
        public Slider VolumeSlider;
        public TextObject SettingVol;

        public OptionModule(Scene scene, float x, float y, float width)
        {

            Bg = new SpriteObject(0, x, y, width, 1080)
            {
                Layer = 5,
                Opacity = 0.85f,
                
            };


    ////////////////////VOLUME SETTING PART////////////////////////////////////////////        
            VolumeSlider = new Slider(0f, 1f, x , y -400, width - 100)
            {
                Parent = Bg,
                Layer = 6,
            };
            SettingVol = new TextObject("Volume", VolumeSlider.Position.X+100, VolumeSlider.Position.Y+10) 
            {
                Layer = VolumeSlider.Layer,
                ScaleMultiply =0.5f,
                AllowHover=false,  
            };
            scene.Add(SettingVol);
    ////////////////////VOLUME SETTING PART//////////////////////////////////////////// 
    ///

            // Подписываемся на изменение
            VolumeSlider.OnValueChanged += OnVolumeChanged;

            VolumeSlider.SetValue(OptionFile.Volume);   // установит начальное значение

            Bg.AddChild(VolumeSlider);
            scene.Add(VolumeSlider);
            scene.Add(Bg);
        }

        // Этот метод будет вызываться каждый кадр, пока игрок двигает слайдер!
        private void OnVolumeChanged(float newVolume)
        {
            OptionFile.Volume = newVolume;
            AudioManager.MainVolume = newVolume;

            // Применяем громкость сразу — музыка будет реагировать в реальном времени
            AudioManager.Instance?.ApplyMainVolume();
        }
    }
}