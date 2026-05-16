using Atk;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.Render.Audio;
using TappiruCS.Render.Text;
using TappiruCS.UI;
using TappiruCS.UI.Sprite;
using TappiruCS.UI.TextAbstract;
using TappiruCS.UI.Toggle;
using TappiruCS.Tween;

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

            _bg = new SpriteObject(0, 1610, 540, 600, 1080)
            {
                AllowHover = false,
                Layer = 5,
                Opacity = 0.85f,
            };
            obj.Add(_bg);
            _bgBlack = new SpriteObject(0, 1885, 540, 70, 1080)
            {
                AllowHover = false,
                Layer = 5,
            };
            obj.Add(_bgBlack);
            // Левая панель – кнопки-вкладки

            var sections = Enum.GetValues(typeof(SettingsSection));
            int idx = 0;
            _SectionButtonList = new ScrollContainer(1875, 80, 100, 600,30);
            _SectionButtonList.Layer = 6;
            obj.Add(_SectionButtonList);
            foreach (SettingsSection sec in sections)
            {
                string iconName = sectionIcons[sec];
                int texId = TextureManager.GetTexture(iconName);

                var bttn = new Button(0, 0, 50, 50, iconName, "") { Layer = 7,Tag = "settin-button"};
                _SectionButtonList.AddItem(bttn);
                int capture = idx;
                bttn.OnClick += () => ScrollToSection(capture);

                

            }


            _scrollContainer = new ScrollContainer(1543, 70, 500, 700,20);
            _scrollContainer.SetZone(35, 480, 560, 1070);
            //_scrollContainer.Debug = true;
            _scrollContainer.Layer = 6;
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
            

            var Label = new TextObject("Звук", 0, 0, 64) { Align = TextAlign.Right, Color = Color4.Bisque };
            container.AddControl(Label, 270, -170);

            var decorativeMaster = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 512, 50){Color = Color4.Black,Pivot = new Vector2(0, 0.5f),Opacity = 0.1f};
            var decorativeMusic = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 512, 50) { Color = Color4.Black, Pivot = new Vector2(0, 0.5f), Opacity = 0.1f };
            var decorativeSFX = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 512, 50) { Color = Color4.Black, Pivot = new Vector2(0, 0.5f), Opacity = 0.1f };
            container.AddControl(decorativeMaster, -225, -60);
            container.AddControl(decorativeMusic, -225, 20);
            container.AddControl(decorativeSFX, -225, 100);


            var masterText = new TextObject("Master", 0, 0, 32) { Align = TextAlign.Left };
            var musicText = new TextObject("Music", 0, 0, 32) { Align = TextAlign.Left };
            var sfxText = new TextObject("SFX", 0, 0, 32) { Align = TextAlign.Left };
            container.AddControl(masterText, -220, -115);
            container.AddControl(musicText, -220, -35);
            container.AddControl(sfxText, -220, 45);
 

            var volumeSliderMaster = new Slider(0f, 1f, 0, 0, 490) { Layer = 8};
            var volumeSliderMusic = new Slider(0f, 1f, 0, 0, 490) { Layer = 8 };
            var volumeSliderSFX = new Slider(0f, 1f, 0, 0, 490) { Layer = 8 };
            container.AddControl(volumeSliderMaster, 25, -60);
            container.AddControl(volumeSliderMusic, 25, 20);
            container.AddControl(volumeSliderSFX, 25, 100);

            
            volumeSliderMaster.SetValue(OptionFile.MasterVolume);

            volumeSliderMaster.OnValueChanged += val => OptionFile.MasterVolume = val;
            volumeSliderMaster.OnValueChanged += val => AudioManager.MasterVolume = val;

            container.RecalculateSize();
        }

        private void CreateVideoControls(SectionContainer container)
        {
            // Декоративная вертикальная линия
            var decorationLine = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 3, 300);
            decorationLine.Color = Color4.Gray;
            decorationLine.Layer = 10;
            container.AddControl(decorationLine, -225, 0);


            // Заголовок "Видео"
            var titleLabel = new TextObject("Видео", 0, 0, 64);
            titleLabel.Align = TextAlign.Right;
            titleLabel.Color = Color4.Bisque;
            titleLabel.Layer = 8;
            container.AddControl(titleLabel, 270, -170);



            var VideoSettinBlackCover_1 = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 512, 50);
            VideoSettinBlackCover_1.Color = Color4.Black;
            VideoSettinBlackCover_1.Pivot = new Vector2(0, 0.5f);
            VideoSettinBlackCover_1.Layer = 5;
            VideoSettinBlackCover_1.Opacity = 0.4f;
            container.AddControl(VideoSettinBlackCover_1, -225, -100);

            var VideoSettinBlackCover_2 = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 512, 50);
            VideoSettinBlackCover_2.Color = Color4.Black;
            VideoSettinBlackCover_2.Pivot = new Vector2(0, 0.5f);
            VideoSettinBlackCover_2.Layer = 5;
            VideoSettinBlackCover_2.Opacity = 0.4f;
            container.AddControl(VideoSettinBlackCover_2, -225, -40);

            var VideoSettinBlackCover_3 = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 512, 50);
            VideoSettinBlackCover_3.Color = Color4.Black;
            VideoSettinBlackCover_3.Pivot = new Vector2(0, 0.5f);
            VideoSettinBlackCover_3.Layer = 5;
            VideoSettinBlackCover_3.Opacity = 0.4f;
            container.AddControl(VideoSettinBlackCover_3, -225, 20);


            var radio1080 = new RadioButton(480, 0, 40, 40) { Layer = 8 };
            VideoSettinBlackCover_1.AddChild(radio1080);

            var radio720 = new RadioButton(480, 0, 40, 40) { Layer = 8 };
            VideoSettinBlackCover_2.AddChild(radio720);

            var radio600 = new RadioButton(480, 0, 40, 40) { Layer = 8 };
            VideoSettinBlackCover_3.AddChild(radio600);

            var text1080 = new TextObject("1920x1080", 20, 5, 36) { Color = Color4.White, Layer = 8,Align = TextAlign.Left,FontKey ="Game" };
            VideoSettinBlackCover_1.AddChild(text1080);
            var text720 = new TextObject("1280x720", 20, 5, 36) { Color = Color4.White, Layer = 8, Align = TextAlign.Left, FontKey = "Game" };
            VideoSettinBlackCover_2.AddChild(text720);
            var text600 = new TextObject("800x600", 20, 5, 36) { Color = Color4.White, Layer = 8, Align = TextAlign.Left, FontKey = "Game" };
            VideoSettinBlackCover_3.AddChild(text600);


            var resolutionGroup = new RadioButtonGroup<(int width, int height)>();
            resolutionGroup.Add(radio1080, (1920, 1080));
            resolutionGroup.Add(radio720, (1280, 720));
            resolutionGroup.Add(radio600, (800, 600));

            var currentRes = (OptionFile.ScreenWidth, OptionFile.ScreenHeight);
            resolutionGroup.SetValue(currentRes, raiseEvent: false);

            resolutionGroup.SelectionChanged += (res) =>
            {
                OptionFile.ScreenWidth = res.width;
                OptionFile.ScreenHeight = res.height;
                ApplyVideoSettings();
            };






            var modeTitle = new TextObject("Режим экрана", 0, 0, 36)
            {
                Color = Color4.White,
                Align = TextAlign.Left,
                Layer = 8,
                FontKey = "Game"
            };
            container.AddControl(modeTitle, -200, 70);

            // Фоновые полосы для трёх опций
            var modeCoverFull = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 512, 50)
            {
                Color = Color4.Black,
                Pivot = new Vector2(0, 0.5f),
                Layer = 5,
                Opacity = 0.4f
            };
            container.AddControl(modeCoverFull, -225, 110);

            var modeCoverWindowed = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 512, 50)
            {
                Color = Color4.Black,
                Pivot = new Vector2(0, 0.5f),
                Layer = 5,
                Opacity = 0.4f
            };
            container.AddControl(modeCoverWindowed, -225, 170);


            // Радиокнопки
            var radioFull = new RadioButton(480, 0, 40, 40) { Layer = 8 };
            var radioWindowed = new RadioButton(480, 0, 40, 40) { Layer = 8 };


            modeCoverFull.AddChild(radioFull);
            modeCoverWindowed.AddChild(radioWindowed);

            // Текстовые метки
            var textFull = new TextObject("Полный экран", 20, 5, 36)
            {
                Color = Color4.White,
                Layer = 8,
                Align = TextAlign.Left,
                FontKey = "Game"
            };
            modeCoverFull.AddChild(textFull);

            var textWindowed = new TextObject("Оконный", 20, 5, 36)
            {
                Color = Color4.White,
                Layer = 8,
                Align = TextAlign.Left,
                FontKey = "Game"
            };
            modeCoverWindowed.AddChild(textWindowed);



            // Создаём группу. Для хранения режима используем кортеж (WindowState, WindowBorder)
            var modeGroup = new RadioButtonGroup<WindowState>();
            modeGroup.Add(radioFull, (WindowState.Fullscreen));       // полный экран
            modeGroup.Add(radioWindowed, (WindowState.Normal));      // оконный с рамкой


            // Устанавливаем текущий режим из сохранённых настроек
            var currentMode = (OptionFile.WindowState);

            modeGroup.SetValue(default(WindowState), false);
            modeGroup.SetValue(currentMode, raiseEvent: false);

            modeGroup.SelectionChanged += (mode) =>
            {
                OptionFile.WindowState = mode;
                ApplyVideoSettings();   // твой метод в OptionModule или Game.Instance.ApplyVideoSettings()
            };


            container.RecalculateSize();
            decorationLine.Scale = new Vector2(3, container.MaxHeight);
            decorationLine.LocalPosition = new Vector2(-225, 50);
            decorationLine.Pivot = new Vector2(0.5f, 0.5f);
        }

        public void ApplyVideoSettings()
        {
            Game.Instance.ClientSize = new Vector2i(OptionFile.ScreenWidth, OptionFile.ScreenHeight);
            Game.Instance.WindowState = OptionFile.WindowState;

            if (OptionFile.WindowState == WindowState.Normal)
            {
                CenterWindowOnCurrentMonitor(Game.Instance);
            }
        }
        private void CenterWindowOnCurrentMonitor(GameWindow window)
        {
            try
            {
            
                window.CenterWindow();

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка центрирования окна: {ex.Message}");
            }
        }

        private void ScrollToSection(int index)
        {
            _scrollContainer.ScrollToIndex(index);
        }



    }
}