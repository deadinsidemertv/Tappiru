using Gtk;
using Microsoft.Win32;
using OpenTK.Mathematics;
using OpenTK.Platform;
using OpenTK.Windowing.Common;
using SharpFileDialog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using TappiruCS.Core;
using TappiruCS.GameLogic;
using TappiruCS.Render;
using TappiruCS.UI;

namespace TappiruCS.State
{
    internal class EditState : IGameState
    {
        private readonly Game _game;
        private readonly SpriteBatch _spriteBatch;
        private readonly TextRender _textRenderer;
        private readonly AudioManager _audio;

        private readonly Scene _scene = new Scene();

        public Slider slider;

        public Button createmap;

        public int bgTexture;
        public Background bg;


        public string? mp3Path = null;
        public string? pngPath = null;

        public bool InEditMode=false;
        public EditState(Game game, SpriteBatch spriteBatch, TextRender textRenderer, AudioManager audio)
        {
            _game = game;
            _spriteBatch = spriteBatch;
            _textRenderer = textRenderer;
            _audio = audio;
        }
        public void OnEnter()
        {
            bgTexture = TextureManager.GetTexture("defaultBG");
            bg = new Background(_spriteBatch, bgTexture, _game) { ParalaxEffect = true };
            var bgBlack = new Background(_spriteBatch, 0, _game) { Opacity = 0.75f };

            


            createmap = new Button(_spriteBatch, _textRenderer,
                160, 30, 700, 120, "button", "create", Color4.White)   // "btn" — имя текстуры через TextureManager
            {
                Layer = 0,
                TextColor = Color4.White,
                TextOffset = new Vector2(-10f, -20f),
                TextScale = 0.7f,
                ScaleMultiply = 0.4f,
                Tag = "create"
            };

            createmap.OnClick += CreateProject;

            _scene.Add(bg);
            _scene.Add(bgBlack);

            _scene.Add(createmap);

            
        }


        public void OnExit()
        {
            _scene.Clear();
            Console.WriteLine("Мы вышли из edit");
        }
        public void Update(double currentTime)
        {
            var mouse = _game.MouseState;
            _scene.Update(currentTime, mouse, _game);

            if (InEditMode && slider != null)
            {
                // Обновляем слайдер от музыки, только если не перетаскиваем
                if (!slider._isDragging)
                {
                    double t = _audio.GetCurrentTime() / _audio.Duration;
                    float newValue = (float)(slider.minValue + t * (slider.maxValue - slider.minValue));
                    slider.SetValue(newValue); // SetValue обновит позицию точки и текст
                }
                else if (InEditMode && slider._isDragging) // добавь флаг в Slider
                {

                    float newTime = (slider.Value - slider.minValue) / (slider.maxValue - slider.minValue) * (float)_audio.Duration;
                    _audio.SetCurrentTime(newTime); // реализуй этот метод
                }
            }
            

            
        }
        public void Render(Matrix4 projection)
        {
            _scene.Draw(projection);
        }

        public void HandleKeyDown(KeyboardKeyEventArgs e) { }

        public void CreateProject()
        {
            bool mp3isdone = false;

            var moduleWindow = new SpriteObject(_spriteBatch, TextureManager.GetTexture("module"), 960, 540, 600, 800);
            var inputTitle = new InputField(_game, _spriteBatch, _textRenderer, 960, 240, 600, 60)
            {
                PlaceHolderColor = Color4.White,
                PlaceHolderText = "title..."
            };
            var mp3Upload = new Button(_spriteBatch, _textRenderer, 800, 530, 150, 150, "mp3", "", Color4.White) { Layer = 1 };
            mp3Upload.OnClick += LoadMp3;
            var JPGUpload = new Button(_spriteBatch, _textRenderer, 1100, 530, 150, 150, "png", "", Color4.White) { Layer = 1 };
            JPGUpload.OnClick += LoadBG;

            var agree = new Button(_spriteBatch, _textRenderer,
                960, 780, 700, 120, "black", "upload mp3", Color4.DarkGray)   // "btn" — имя текстуры через TextureManager
            {
                Layer = 1,
                TextColor = Color4.DarkGray,
                TextOffset = new Vector2(-10f, -30f),
                TextScale = 0.7f,
                ScaleMultiply = 0.6f,
                Tag = "create"
            };
            agree.OnClick += () =>
            {
                if (mp3Path != null && pngPath != null)
                {
                    if (CreateMap(inputTitle.Text))
                    {
                        _scene.Remove(moduleWindow);
                        _scene.Remove(inputTitle);
                        _scene.Remove(mp3Upload);
                        _scene.Remove(JPGUpload);
                        _scene.Remove(agree);

                        InEditMode = true;

                        string projectDir = Path.Combine("Edit", SanitizeFileName(inputTitle.Text));
                        string projectMp3 = Path.Combine(projectDir, Path.GetFileName(mp3Path));
                        string projectBg = Path.Combine(projectDir, "bg" + Path.GetExtension(pngPath));

                        bgTexture = TextureLoader.Load(projectBg);
                        bg._textureId = bgTexture;

                        _audio.LoadMusic(projectMp3);
                        _audio.Play();
                        _audio.SetLooping(true);

                        slider = new Slider(_spriteBatch, _textRenderer, 0, 1000, 960, 900, 1800) { AllowHover = false };
                        _scene.Add(slider);
                        _scene.Add(slider.point);

                        slider.maxValue = (float)_audio.Duration;
                        slider.maxValueText.Text = slider.maxValue.ToString("F2");

                        InEditMode = true;
                    }
                }
                else
                {
                    Console.WriteLine("Сначала выберите MP3 и фоновое изображение");
                }
            };




                _scene.Add(moduleWindow);
                _scene.Add(inputTitle);

                _scene.Add(mp3Upload);
                _scene.Add(JPGUpload);

                _scene.Add(agree);

                foreach (var obj in _scene._objects)
                {
                    Console.WriteLine(obj.ToString());
            }

        }

        public bool CreateMap(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return false;

            try
            {
                string baseDir = Path.Combine(Directory.GetCurrentDirectory(), "Edit");
                string projectDir = Path.Combine(baseDir, SanitizeFileName(title));
                Directory.CreateDirectory(projectDir);

                string mp3Dest = Path.Combine(projectDir, Path.GetFileName(mp3Path));
                File.Copy(mp3Path, mp3Dest, true);

                string bgDest = Path.Combine(projectDir, "bg" + Path.GetExtension(pngPath));
                File.Copy(pngPath, bgDest, true);

                // создаём .tapp
                var jsonMap = new JsonMap {
                    title = title,
                    artist = "",
                    creator = "",
                    difficulty = "Normal",
                    previewTime = 0,
                    endTime = 1,
                    events = new List<TimingEvent>()
                };
                string json = JsonSerializer.Serialize(jsonMap, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(Path.Combine(projectDir, "data.tapp"), json);

                Console.WriteLine($"Карта создана: {projectDir}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                return false;
            }
        }
        private string SanitizeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');
            return name;
        }

        public void LoadMp3()
        {
            var filters = new[]
                {
                    new SharpFileDialog.NativeFileDialog.Filter
                    {
                        Name = "MP3 Files",
                        Extensions = new[] { "mp3" }
                    }
                };

            if (SharpFileDialog.NativeFileDialog.OpenDialog(filters,"C:\\",out string path))
            {
                mp3Path = path;  // Пользователь выбрал файл
                Console.WriteLine(mp3Path);
            }

        }

        
        public void LoadBG()
        {
            var filters = new[]
                {
                    new SharpFileDialog.NativeFileDialog.Filter
                    {
                        Name = "PNG Files",
                        Extensions = new[] { "png","jpg" }
                    }
                };

            if (SharpFileDialog.NativeFileDialog.OpenDialog(filters, "C:\\", out string path))
            {
                pngPath = path;  // Пользователь выбрал файл
                Console.WriteLine(pngPath);
            }
        }
    }
}
