using System;
using System.Collections.Generic;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.Render.Text;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;
using static TappiruCS.Render.Text.Font;

namespace TappiruCS.State.Menu.Option
{
    public class OptionModule : ModuleWindow
    {
        public enum SettingsSection { Audio, Video }


        private SpriteObject _bg;
        private SpriteObject _bgBlack;

        private ScrollContainer _scrollContainer;
        private ScrollContainer _SectionButtonList;

        public OptionModule(Scene scene) : base(scene)
        {
            var sectionIcons = new Dictionary<SettingsSection, string>
            {
                { SettingsSection.Audio, "setting-button-volume" },
                { SettingsSection.Video, "setting-button-screen" },
            };

            _bg = new SpriteObject(0, 300, 540, 600, 1080)
            {
                AllowHover = false,
                Layer = 5,
                Opacity = 0.85f,
            };
            obj.Add(_bg);
            _bgBlack = new SpriteObject(0, 60, 540, 120, 1080)
            {
                AllowHover = false,
                Layer = 5,
            };
            obj.Add(_bgBlack);
            // Левая панель – кнопки-вкладки

            var sections = Enum.GetValues(typeof(SettingsSection));
            int idx = 0;
            _SectionButtonList = new ScrollContainer(50, 250, 100, 400, 120) { Layer = 6};
            obj.Add(_SectionButtonList);
            foreach (SettingsSection sec in sections)
            {
                string iconName = sectionIcons[sec];
                int texId = TextureManager.GetTexture(iconName);

                var bttn = new Button(0, 0, 50, 50, iconName, "") { Layer = 7,Tag = "settin-button"};
                _SectionButtonList.AddItem(bttn);
                obj.Add(bttn);
                int capture = idx;
                bttn.OnClick += () => ScrollToSection(capture);

                

            }

            // ScrollContainer
            const float sectionHeight = 350f;
            const float sectionSpacing = 20f;
            const float scrollWidth = 440f;
            const float scrollHeight = 700f;
            const float containerX = 350f;
            const float containerY = 200f;

            _scrollContainer = new ScrollContainer(containerX, containerY, scrollWidth, scrollHeight, sectionHeight, sectionSpacing)
            {
                Layer = 6
            };
            obj.Add(_scrollContainer);
            
            // Определяем ширину секции с учётом отступов ScrollContainer
            float horizontalPadding = 10f; // должен совпадать с _horizontalPadding в ScrollContainer
            float sectionWidth = _scrollContainer.Width - horizontalPadding * 2; // 440 - 20 = 420

            foreach (SettingsSection sec in sections)
            {
                CreateSection(sec, sectionWidth, sectionHeight);
            }

            _scrollContainer.RecalcMaxScroll();
        }

        private void CreateSection(SettingsSection section, float width, float height)
        {
            var container = new SectionContainer(section.ToString(), width, height, 0, 0);
            _scrollContainer.AddItem(container);
            obj.Add(container);

            switch (section)
            {
                case SettingsSection.Audio:
                    CreateAudioControls(container);
                    break;
                case SettingsSection.Video:
                    CreateVideoControls(container);
                    break;

            }
        }

        private void CreateAudioControls(SectionContainer container)
        {

            var supri = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 50, 50) { Color = Color4.Red};
            obj.Add(supri);
            container.AddControl(supri, 0, -50);

            var supri2 = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 50, 50) { Color = Color4.Red };
            obj.Add(supri2);
            container.AddControl(supri2, 0, 0);

            var supri3 = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 50, 50) { Color = Color4.Red };
            obj.Add(supri3);
            container.AddControl(supri3, 0, 50);

            var supri4 = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 50, 50) { Color = Color4.Red };
            obj.Add(supri4);
            container.AddControl(supri4, 0, 100);
            var supri5 = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 50, 50) { Color = Color4.Blue };
            obj.Add(supri5);
            container.AddControl(supri5, 0, 150);
            var supri6 = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 50, 50) { Color = Color4.Blue };
            obj.Add(supri6);
            container.AddControl(supri6, 0, 200);


            var volumeSlider = new Slider(0f, 1f, 0, 0, 400) { Layer = 7 };
            var volumeLabel = new TextObject("Громкость", 0, 0, 28)
            {
                Layer = 7,
                ScaleMultiply = 0.5f,
                AllowHover = false,
                Color = Color4.White,
                 Align = TextAlign.Left
             };
             volumeSlider.OnValueChanged += OnVolumeChanged;
             volumeSlider.SetValue(OptionFile.Volume);

             container.AddControl(volumeSlider, 0, 400);
             container.AddControl(volumeLabel, -170, 150);

             obj.Add(volumeSlider);
             obj.Add(volumeLabel);

            container.RecalculateSize();
            Console.WriteLine("///////////////////////////////////////////////////////STOP////////////////////////");
        }

        private void CreateVideoControls(SectionContainer container)
        {
            var fullscreenToggle = new CheckBox(0, 0, 30, 30)
            {
                Layer = 7,
                IsSelected = OptionFile.IsFullscreen
            };
            var fullscreenLabel = new TextObject("Полный экран", 0, -30, 28)
            {
                Layer = 7,
                ScaleMultiply = 0.5f,
                Color = Color4.White,
                Align = TextAlign.Left,
            };

            fullscreenToggle.OnSelectedChanged += (isChecked) =>
            {
                OptionFile.IsFullscreen = isChecked;
            };

            container.AddControl(fullscreenToggle, 50, 100);
            container.AddControl(fullscreenLabel, 50, 70);

            obj.Add(fullscreenToggle);
            obj.Add(fullscreenLabel);
        }

        private void ScrollToSection(int index)
        {
            _scrollContainer.ScrollToIndex(index);
        }

        private void OnVolumeChanged(float newVolume)
        {
            OptionFile.Volume = newVolume;
            AudioManager.MainVolume = newVolume;
            AudioManager.Instance?.ApplyMainVolume();
        }
    }
}