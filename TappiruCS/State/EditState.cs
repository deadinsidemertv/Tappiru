using Gtk;
using Microsoft.Win32;
using OpenTK.Mathematics;
using OpenTK.Platform;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using SharpFileDialog;
using System;
using System.Collections.Generic;
using System.Formats.Tar;
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

        public List<Slider> colors;

        public Button createmap;
        public Button loadmap;

        public Button PlayPauseButton;

        private Button _addMarkerButton;
        private Button _saveMarkersButton;
        private InputField _markerTextInput;

        public int bgTexture;
        public Background bg;

        public TextObject renderText;

        public string? mp3Path = null;
        public string? pngPath = null;
        public string? tappPath = null;

        public bool InEditMode= false;

        private bool _isMusicPlaying = true;

        private bool isInput = false;

        private List<(float time, string text, SpriteObject visual)> _markers = new List<(float, string, SpriteObject)>();
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


            renderText = new TextObject(_textRenderer, "", 960, 500) { ScaleMultiply = 0.6f};

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
            loadmap = new Button(_spriteBatch, _textRenderer,
                440, 30, 700, 120, "button", "load", Color4.White)   // "btn" — имя текстуры через TextureManager
            {
                Layer = 0,
                TextColor = Color4.White,
                TextOffset = new Vector2(-10f, -20f),
                TextScale = 0.7f,
                ScaleMultiply = 0.4f,
                Tag = "load"
            };

            createmap.OnClick += CreateProject;
            loadmap.OnClick += LoadProject;

            _scene.Add(bg);
            _scene.Add(bgBlack);

            _scene.Add(createmap);
            _scene.Add(loadmap);
            _scene.Add(renderText);


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

            RenderEditedText(renderText);

        }

        private void TogglePlayPause()
        {
            if (!isInput&&InEditMode)
            {
                if (_isMusicPlaying)
                {

                    _audio.Pause();

                    PlayPauseButton.NormalColor = Color4.Orange;


                }
                else
                {

                    _audio.Resume();

                    PlayPauseButton.NormalColor = Color4.White;

                }
                _isMusicPlaying = !_isMusicPlaying;
            }
            
        }


        public void Render(Matrix4 projection)
        {
            _scene.Draw(projection);
        }

        public void HandleKeyDown(KeyboardKeyEventArgs e) 
        {
            if (e.Key == Keys.Space)
                TogglePlayPause();
            if (InEditMode && e.Key == Keys.Left)
                _audio.SetCurrentTime(_audio.GetCurrentTime() - 0.1f);
            if (InEditMode && e.Key == Keys.Right)
                _audio.SetCurrentTime(_audio.GetCurrentTime() + 0.1f);
        }
        public void RenderEditedText(TextObject text)
        {
            if (InEditMode && _markers.Count > 0)
            {
                double currentTime = _audio.GetCurrentTime();
                // Находим маркер с самым поздним временем, не превышающим текущее
                var activeMarker = _markers
                    .OrderBy(m => m.time)
                    .LastOrDefault(m => m.time <= currentTime);
                text.Text = activeMarker.text ?? "";
            }
            else if (InEditMode)
            {
                text.Text = "";
            }

        }
       
        public void LoadProject()
        {
            var filters = new[]
                {
                    new SharpFileDialog.NativeFileDialog.Filter
                    {
                        Name = "TAPP Files",
                        Extensions = new[] { "tapp" }
                    }
                };

            if (SharpFileDialog.NativeFileDialog.OpenDialog(filters, "Edit\\", out string path))
            {
                tappPath = path;  // Пользователь выбрал файл
                Console.WriteLine(mp3Path);
            }
            ActiveEditMode(tappPath);
        }
        public void CreateProject()
        {
            var moduleWindow = new SpriteObject(_spriteBatch, TextureManager.GetTexture("module"), 960, 540, 600, 800) ;
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
            _scene.Add(moduleWindow);
            _scene.Add(inputTitle);

            _scene.Add(mp3Upload);
            _scene.Add(JPGUpload);

            _scene.Add(agree);
            agree.OnClick += () =>
            {
                if (mp3Path != null && pngPath != null)
                {
                    string tappPath = CreateMap(inputTitle.Text);

                    if (tappPath != null)
                    {
                        _scene.Remove(moduleWindow);
                        _scene.Remove(inputTitle);
                        _scene.Remove(mp3Upload);
                        _scene.Remove(JPGUpload);
                        _scene.Remove(agree);


                    }
                    this.tappPath = tappPath;
                    ActiveEditMode(this.tappPath);
                }
                else
                {
                    Console.WriteLine("Сначала выберите MP3 и фоновое изображение");
                }
            };
        }

        private void ShowTextInputDialog(string title, Action<string> onComplete)
        {
            isInput = true;
            _audio.Pause();
            _isMusicPlaying = false;
            var panel = new SpriteObject(_spriteBatch, TextureManager.GetTexture("module"), 960, 540, 1200, 600);
            var input = new InputField(_game, _spriteBatch, _textRenderer, 960, 520, 1100, 70);
            var okButton = new Button(_spriteBatch, _textRenderer, 960, 650, 400, 100, "button", "OK", Color4.White)
            {
                Layer = 1,
                TextScale = 0.5f,
                TextOffset = new Vector2(0f, -30f)
            };
            okButton.OnClick += () => {
                onComplete?.Invoke(input.Text);
                _scene.Remove(panel);
                _scene.Remove(input);
                _scene.Remove(okButton);
                isInput = false;
                _audio.Resume();
                _isMusicPlaying = true;
            };
            _scene.Add(panel);
            _scene.Add(input);
            _scene.Add(okButton);
        }
        private void AddMarkerAtTime(float time, string text)
        {
            
            float x = slider.GetPositionFromTime(time);
            // Создаём визуальный маркер (кружок)
            var markerVisual = new SpriteObject(_spriteBatch, TextureManager.GetTexture("marker"), x, slider.line.Position.Y , 25, 25)
            {
                Color = Color4.Yellow,
                Pivot = new Vector2(0.5f, 0.5f),
                CanvasScale = _scene.CanvasScale
            };
            _scene.Add(markerVisual);
            _markers.Add((time, text, markerVisual));
            Console.WriteLine($"Маркер добавлен: время={time}, текст={text}");
        }
        private void SaveMarkersToFile()
        {
            if (string.IsNullOrEmpty(tappPath)) return;
            
            JsonMap map;
            try
            {
                string json = File.ReadAllText(tappPath);
                map = JsonSerializer.Deserialize<JsonMap>(json);
            }
            catch { map = new JsonMap(); }

            map.events = _markers.Select(m => new TimingEvent { time = m.time, text = m.text }).ToList();

            string newJson = JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(tappPath, newJson);
            Console.WriteLine($"Сохранено {map.events.Count} событий в {tappPath}");
        }
        private void LoadMarkersFromFile(string tappPath)
        {
            var json = File.ReadAllText(tappPath);
            var map = JsonSerializer.Deserialize<JsonMap>(json);
            if (map?.events != null)
            {
                foreach (var ev in map.events)
                {
                    AddMarkerAtTime((float)ev.time, ev.text);
                }
            }
        }

        public string CreateMap(string title)
        {
            if (string.IsNullOrWhiteSpace(title)) return null;

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
                var jsonMap = new JsonMap
                {
                    title = title,
                    artist = "",
                    creator = "",
                    difficulty = "Normal",
                    previewTime = 0,
                    endTime = 1,
                    events = new List<TimingEvent>(),
                    tappedR = 0.4f,
                    tappedG = 0.3f,
                    tappedB = 0.6f,
                    needR = 0.7f,
                    needG = 0.3f,
                    needB = 0.8f,
                    completeR = 0.2f,
                    completeG = 0.1f,
                    completeB =0.4f
                };
                string json = JsonSerializer.Serialize(jsonMap, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(Path.Combine(projectDir, "data.tapp"), json);

                Console.WriteLine($"Карта создана: {projectDir}");
                string tappFullPath = Directory.GetFiles(projectDir, "*.tapp").FirstOrDefault();

                return tappFullPath;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
                return null;
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
        public void ActiveEditMode(string tappPath)
        {
            string projectDir = System.IO.Path.GetDirectoryName(tappPath);
            if (string.IsNullOrEmpty(projectDir))
            {
                Console.WriteLine("Ошибка: не удалось определить папку проекта.");
                return;
            }
            JsonMap loadedMap;
            try
            {
                string jsonContent = System.IO.File.ReadAllText(tappPath);
                loadedMap = System.Text.Json.JsonSerializer.Deserialize<JsonMap>(jsonContent);
                if (loadedMap == null) throw new Exception("Deserialization returned null.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка загрузки .tapp файла: {ex.Message}");
                return;
            }


            string mp3File = System.IO.Directory.GetFiles(projectDir, "*.mp3").FirstOrDefault();
            string bgFile = System.IO.Directory.GetFiles(projectDir, "*.jpg").FirstOrDefault()
                            ?? System.IO.Directory.GetFiles(projectDir, "*.png").FirstOrDefault();

            if (string.IsNullOrEmpty(mp3File) || string.IsNullOrEmpty(bgFile))
            {
                Console.WriteLine("Ошибка: в папке проекта не найдены MP3 или фоновое изображение.");
                return;
            }

            bgTexture = TextureLoader.Load(bgFile);
            bg._textureId = bgTexture;

            _audio.LoadMusic(mp3File);
            _audio.Play();
            _audio.SetLooping(true);

            slider = new Slider(_spriteBatch, _textRenderer, 0, 1000, 960, 900, 1800) { AllowHover = false };
            _scene.Add(slider);
            _scene.Add(slider.point);

            colors = new List<Slider>();
            for (int i = 0; i < 9; i++) 
            {
                colors.Add(new Slider(_spriteBatch, _textRenderer, 0, 1, 1740, 200 + i * 80, 300) { ScaleMultiply =0.5f }) ;
                _scene.Add(colors[i]);
                _scene.Add(colors[i].point);

            }




            slider.maxValue = (float)_audio.Duration;
            slider.maxValueText.Text = slider.maxValue.ToString("F2");

            PlayPauseButton = new Button(_spriteBatch, _textRenderer, 960, 1000, 100, 100, "pause", "", Color4.White) { Layer = 1 };
            PlayPauseButton.OnClick += TogglePlayPause;
            _scene.Add(PlayPauseButton);

            _addMarkerButton = new Button(_spriteBatch, _textRenderer,
                1200, 1000, 400, 100, "button", "add mark", Color4.White)
            {
                Layer = 1,
                TextColor = Color4.White,
                TextOffset = new Vector2(-10f, -20f),
                TextScale = 0.4f,
                ScaleMultiply = 0.6f,
                Tag = "create"
            };
            _addMarkerButton.OnClick += () => {
                float currentTime = (float)_audio.GetCurrentTime();

                ShowTextInputDialog("Введите текст для события", (text) => {
                    if (!string.IsNullOrEmpty(text))
                    {
                        AddMarkerAtTime(currentTime, text);
                    }
                });
            };
            _scene.Add(_addMarkerButton);

            _saveMarkersButton = new Button(_spriteBatch, _textRenderer, 1780, 30, 700, 120, "button", "Save markers", Color4.White)
            {
                Layer = 1,
                TextColor = Color4.White,
                TextOffset = new Vector2(-10f, -20f),
                TextScale = 0.7f,
                ScaleMultiply = 0.4f,
                Tag = "create"
            };
            _saveMarkersButton.OnClick += SaveMarkersToFile;
            _scene.Add(_saveMarkersButton);

            LoadMarkersFromFile(tappPath);

            InEditMode = true;
        }
    }
}
