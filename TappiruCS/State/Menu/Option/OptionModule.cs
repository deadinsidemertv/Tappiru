using System;
using System.Collections.Generic;
using Atk;
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
            _bgBlack = new SpriteObject(0, 35, 540, 70, 1080)
            {
                AllowHover = false,
                Layer = 5,
            };
            obj.Add(_bgBlack);
            // Левая панель – кнопки-вкладки

            var sections = Enum.GetValues(typeof(SettingsSection));
            int idx = 0;
            _SectionButtonList = new ScrollContainer(25, 80, 100, 600,30) { Layer = 6};
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


            _scrollContainer = new ScrollContainer(300, 200, 500, 1080,20)
            {
                Layer = 6
            };
            obj.Add(_scrollContainer);

            // Определяем ширину секции с учётом отступов ScrollContainer
            float horizontalPadding = 10f; // должен совпадать с _horizontalPadding в ScrollContainer
            float sectionWidth = _scrollContainer.Width - horizontalPadding * 2; // 440 - 20 = 420

            foreach (SettingsSection sec in sections)
            {
                CreateSection(sec, sectionWidth);
            }

            _scrollContainer.RecalcMaxScroll();
        }

        private void CreateSection(SettingsSection section, float width)
        {
            var container = new SectionContainer(section.ToString(), width,0, 0, 0);
            _scrollContainer.AddItem(container);
            obj.Add(container);
            container.RecalculateSize();

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
            var decorationLine = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 3, 300) { Color = Color4.Gray,Layer=8 };
            container.AddControl(decorationLine, -225, 0);
            obj.Add(decorationLine);

            var Label = new TextObject("Звук", 0, 0, 64) { Align = TextAlign.Right, Color = Color4.Bisque };
            container.AddControl(Label, 270, -170);
            obj.Add(Label);

            var decorativeMaster = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 512, 50){Color = Color4.Black,Pivot = new Vector2(0, 0.5f)};
            var decorativeMusic = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 512, 50) { Color = Color4.Black, Pivot = new Vector2(0, 0.5f) };
            var decorativeSFX = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 512, 50) { Color = Color4.Black, Pivot = new Vector2(0, 0.5f) };
            container.AddControl(decorativeMaster, -225, -60);
            container.AddControl(decorativeMusic, -225, 0);
            container.AddControl(decorativeSFX, -225, 60);
            obj.Add(decorativeMaster);
            obj.Add(decorativeMusic);
            obj.Add(decorativeSFX);

            var volumeSliderMaster = new Slider(0f, 1f, 0, 0, 490) { Layer = 8};
            var volumeSliderMusic = new Slider(0f, 1f, 0, 0, 490) { Layer = 8 };
            var volumeSliderSFX = new Slider(0f, 1f, 0, 0, 490) { Layer = 8 };
            container.AddControl(volumeSliderMaster, 25, -60);
            container.AddControl(volumeSliderMusic, 25, 0);
            container.AddControl(volumeSliderSFX, 25, 60);
            obj.Add(volumeSliderMaster);
            obj.Add(volumeSliderMusic);
            obj.Add(volumeSliderSFX);


            volumeSliderMaster.OnValueChanged += OnVolumeChanged;
            volumeSliderMaster.SetValue(OptionFile.Volume);

            container.RecalculateSize();
        }

        private void CreateVideoControls(SectionContainer container)
        {
            var decorationLine = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 3, 300) { Color = Color4.Gray };

            var fullscreenToggle = new CheckBox(0, 0, 30, 30)
            {
                Layer = 7,
                IsSelected = OptionFile.IsFullscreen
            };
            var Label = new TextObject("Экран", 0, -30, 64)
            {
                Layer = 7,
                ScaleMultiply = 1f,
                Color = Color4.Bisque,
                Align = TextAlign.Right,
            };

            fullscreenToggle.OnSelectedChanged += (isChecked) =>
            {
                OptionFile.IsFullscreen = isChecked;
            };


            container.AddControl(decorationLine, -225, 0);
            container.AddControl(fullscreenToggle, 50, 0);
            container.AddControl(Label, 180, -170);

            obj.Add(decorationLine);

            obj.Add(fullscreenToggle);
            obj.Add(Label);

            container.RecalculateSize();
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