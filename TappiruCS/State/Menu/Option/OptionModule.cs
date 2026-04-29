using System;
using System.Collections.Generic;
using Atk;
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;
using static TappiruCS.Render.Text.BMFont.Font;

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
            var decorationLine = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 3, 240) { Color = Color4.Gray,Layer=8 };
            container.AddControl(decorationLine, -225, 20);
            obj.Add(decorationLine);

            var Label = new TextObject("Звук", 0, 0, 64) { Align = TextAlign.Right, Color = Color4.Bisque };
            container.AddControl(Label, 270, -170);
            obj.Add(Label);

            var decorativeMaster = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 512, 50){Color = Color4.Black,Pivot = new Vector2(0, 0.5f),Opacity = 0.1f};
            var decorativeMusic = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 512, 50) { Color = Color4.Black, Pivot = new Vector2(0, 0.5f), Opacity = 0.1f };
            var decorativeSFX = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 512, 50) { Color = Color4.Black, Pivot = new Vector2(0, 0.5f), Opacity = 0.1f };
            container.AddControl(decorativeMaster, -225, -60);
            container.AddControl(decorativeMusic, -225, 20);
            container.AddControl(decorativeSFX, -225, 100);
            obj.Add(decorativeMaster);
            obj.Add(decorativeMusic);
            obj.Add(decorativeSFX);

            var masterText = new TextObject("Master", 0, 0, 32) { Align = TextAlign.Left };
            var musicText = new TextObject("Music", 0, 0, 32) { Align = TextAlign.Left };
            var sfxText = new TextObject("SFX", 0, 0, 32) { Align = TextAlign.Left };
            container.AddControl(masterText, -220, -115);
            container.AddControl(musicText, -220, -35);
            container.AddControl(sfxText, -220, 45);
            obj.Add(masterText);
            obj.Add(musicText);
            obj.Add(sfxText);



            var volumeSliderMaster = new Slider(0f, 1f, 0, 0, 490) { Layer = 8};
            var volumeSliderMusic = new Slider(0f, 1f, 0, 0, 490) { Layer = 8 };
            var volumeSliderSFX = new Slider(0f, 1f, 0, 0, 490) { Layer = 8 };
            container.AddControl(volumeSliderMaster, 25, -60);
            container.AddControl(volumeSliderMusic, 25, 20);
            container.AddControl(volumeSliderSFX, 25, 100);
            obj.Add(volumeSliderMaster);
            obj.Add(volumeSliderMusic);
            obj.Add(volumeSliderSFX);


            
            volumeSliderMaster.SetValue(OptionFile.MasterVolume);

            volumeSliderMaster.OnValueChanged += val => OptionFile.MasterVolume = val;
            volumeSliderMaster.OnValueChanged += val => AudioManager.MainVolume = val;


            container.RecalculateSize();
        }

        private void CreateVideoControls(SectionContainer container)
        {
            // Декоративная вертикальная линия
            var decorationLine = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 3, 300)
            {
                Color = Color4.Gray,
                Layer = 8
            };
            container.AddControl(decorationLine, -225, 0);
            obj.Add(decorationLine);

            // Заголовок "Видео"
            var titleLabel = new TextObject("Видео", 0, 0, 64)
            {
                Align = TextAlign.Right,
                Color = Color4.Bisque,
                Layer = 8
            };
            container.AddControl(titleLabel, 270, -170);
            obj.Add(titleLabel);

            // Декоративные фоны для трёх слайдеров (чёрные полосы)
            var decorativeBrightness = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 512, 50)
            {
                Color = Color4.Black,
                Pivot = new Vector2(0, 0.5f),
                Layer = 7
            };
            var decorativeContrast = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 512, 50)
            {
                Color = Color4.Black,
                Pivot = new Vector2(0, 0.5f),
                Layer = 7
            };
            var decorativeSaturation = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 512, 50)
            {
                Color = Color4.Black,
                Pivot = new Vector2(0, 0.5f),
                Layer = 7
            };
            container.AddControl(decorativeBrightness, -225, -60);
            container.AddControl(decorativeContrast, -225, 0);
            container.AddControl(decorativeSaturation, -225, 60);
            obj.Add(decorativeBrightness);
            obj.Add(decorativeContrast);
            obj.Add(decorativeSaturation);

            // Слайдеры
            var brightnessSlider = new Slider(0f, 1f, 0, 0, 490) { Layer = 8 };
            var contrastSlider = new Slider(0f, 1f, 0, 0, 490) { Layer = 8 };
            var saturationSlider = new Slider(0f, 1f, 0, 0, 490) { Layer = 8 };

            container.AddControl(brightnessSlider, 25, -60);
            container.AddControl(contrastSlider, 25, 0);
            container.AddControl(saturationSlider, 25, 60);
            obj.Add(brightnessSlider);
            obj.Add(contrastSlider);
            obj.Add(saturationSlider);

            // Загрузка сохранённых значений
            //brightnessSlider.SetValue(OptionFile.Brightness);
            //contrastSlider.SetValue(OptionFile.Contrast);
           // saturationSlider.SetValue(OptionFile.Saturation);

            // Обработчики изменений
            brightnessSlider.OnValueChanged += (val) => {
               // OptionFile.Brightness = val;
                //ApplyVideoSettings();
            };
            contrastSlider.OnValueChanged += (val) => {
                //OptionFile.Contrast = val;
                //ApplyVideoSettings();
            };
            saturationSlider.OnValueChanged += (val) => {
                //OptionFile.Saturation = val;
                //ApplyVideoSettings();
            };

            // ----- Чекбокс полноэкранного режима (дополнительно, в том же стиле) -----
            // Для красоты добавим отдельную декоративную полосу и метку под слайдерами
            var decorativeFullscreen = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 512, 50)
            {
                Color = Color4.Black,
                Pivot = new Vector2(0, 0.5f),
                Layer = 7
            };
            container.AddControl(decorativeFullscreen, -225, 120);
            obj.Add(decorativeFullscreen);

            var fullscreenLabel = new TextObject("Полный экран", 0, 0, 28)
            {
                Color = Color4.White,
                Layer = 8,
                Align = TextAlign.Left
            };
            container.AddControl(fullscreenLabel, -200, 120);
            obj.Add(fullscreenLabel);

            var fullscreenToggle = new CheckBox(0, 0, 30, 30)
            {
                Layer = 8,
                //IsSelected = OptionFile.IsFullscreen
            };
            container.AddControl(fullscreenToggle, 200, 120);
            obj.Add(fullscreenToggle);

            fullscreenToggle.OnSelectedChanged += (isChecked) =>
            {
                //OptionFile.IsFullscreen = isChecked;
                //ApplyVideoSettings(); // применение полноэкранного режима
            };

            container.RecalculateSize();
        }



        private void ScrollToSection(int index)
        {
            _scrollContainer.ScrollToIndex(index);
        }

    }
}