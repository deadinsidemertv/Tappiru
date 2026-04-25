using OpenTK.Mathematics;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;

namespace TappiruCS.State.Menu
{
    public class OptionModule : ModuleWindow
    {
        public SpriteObject Bg;
        public Slider VolumeSlider;
        public TextObject SettingVol;

        public OptionModule(Scene scene):base(scene)
        {

            Bg = new SpriteObject(0, 300, 540, 600, 1080)
            {
                AllowHover = false,
                Layer = 5,
                Opacity = 0.85f,

            };


            ////////////////////VOLUME SETTING PART////////////////////////////////////////////        
            VolumeSlider = new Slider(0f, 1f, 0, -300, 500)
            {
                Layer = 6,
            };
            SettingVol = new TextObject("Звук", -200, -100,36) 
            {
                Layer = VolumeSlider.Layer,
                ScaleMultiply =0.5f,
                AllowHover=false,
                Color = Color4.BlueViolet
            };

    ////////////////////VOLUME SETTING PART//////////////////////////////////////////// 
            VolumeSlider.OnValueChanged += OnVolumeChanged;

            VolumeSlider.SetValue(OptionFile.Volume);

            obj.Add(Bg);
            obj.Add(SettingVol);
            obj.Add(VolumeSlider);

            Bg.AddChild(VolumeSlider);
            VolumeSlider.AddChild(SettingVol);

            Console.WriteLine(SettingVol.Parent);
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