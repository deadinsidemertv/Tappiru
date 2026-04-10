using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using TappiruCS.Core;
using TappiruCS.GameLogic;
using TappiruCS.Render;
using TappiruCS.UI;

namespace TappiruCS.State.Edit
{
    internal class EditState : IGameState
    {
        private readonly Game _game;
        private readonly SpriteBatch _spriteBatch;
        private readonly TextRender _textRenderer;
        private readonly AudioManager _audio;

        private readonly Scene _scene = new Scene();

        // Основные элементы
        private Background _background = null!;
        private Background _darkOverlay = null!;
        private TextObject _renderedMarkerText = null!;

        private Slider _timeSlider = null!;

        private Button _playPauseButton = null!;
        private Button _addMarkerButton = null!;
        private Button _saveMarkersButton = null!;

        private bool _inEditMode = false;
        private bool _isMusicPlaying = true;
        private bool _isInputDialogOpen = false;

        private string? _tappPath;
        private readonly List<Marker> _markers = new();

        public EditState(Game game, SpriteBatch spriteBatch, TextRender textRenderer, AudioManager audio)
        {
            _game = game;
            _spriteBatch = spriteBatch;
            _textRenderer = textRenderer;
            _audio = audio;
        }

        public void OnEnter()
        {
            _audio.Stop();
            CreateInitialUI();
        }

        public void OnExit()
        {
            _scene.Clear();
            _markers.Clear();
            _inEditMode = false;
        }

        private void CreateInitialUI()
        {
            _background = new Background(_spriteBatch, TextureManager.GetTexture("defaultBG"), _game)
            {
                ParalaxEffect = true
            };

            _darkOverlay = new Background(_spriteBatch, 0, _game) { Opacity = 0.75f };

            _renderedMarkerText = new TextObject(_textRenderer, "", 960, 500)
            {
                ScaleMultiply = 0.6f
            };

            var createBtn = CreateTopButton(160, "Create Project", CreateProject);
            var loadBtn = CreateTopButton(440, "Load Project", LoadProject);

            _scene.Add(_background);
            _scene.Add(_darkOverlay);
            _scene.Add(createBtn);
            _scene.Add(loadBtn);
            _scene.Add(_renderedMarkerText);
        }

        private Button CreateTopButton(float x, string text, Action onClick)
        {
            var btn = new Button(_spriteBatch, _textRenderer, x, 30, 700, 120, "button", text, Color4.White)
            {
                Layer = 0,
                TextColor = Color4.White,
                TextOffset = new Vector2(-10f, -50f),
                TextScale = 0.7f,
                ScaleMultiply = 0.4f
            };
            btn.OnClick += onClick;
            return btn;
        }

        public void Update(double deltaTime)
        {
            var mouse = _game.MouseState;
            _scene.Update(deltaTime, mouse, _game);

            if (_inEditMode)
            {
                UpdateTimeSlider();
                UpdateRenderedMarkerText();
                UpdateColorPreviews();
            }
        }

        private void UpdateTimeSlider()
        {
            if (_timeSlider == null) return;

            if (!_timeSlider._isDragging)
            {
                double progress = _audio.GetCurrentTime() / Math.Max(1, _audio.Duration);
                float newValue = (float)(_timeSlider.minValue + progress * (_timeSlider.maxValue - _timeSlider.minValue));
                _timeSlider.SetValue(newValue);
            }
            else
            {
                float newTime = (_timeSlider.Value / Math.Max(1, _timeSlider.maxValue)) * (float)_audio.Duration;
                _audio.SetCurrentTime(newTime);
            }
        }

        private void UpdateRenderedMarkerText()
        {
            if (_markers.Count == 0)
            {
                _renderedMarkerText.Text = "";
                return;
            }

            double current = _audio.GetCurrentTime();
            var active = _markers.OrderBy(m => m.Time).LastOrDefault(m => m.Time <= current);
            _renderedMarkerText.Text = active?.Text ?? "";
        }

        public void Render(Matrix4 projection)
        {
            _scene.Draw(projection);
        }

        public void HandleKeyDown(KeyboardKeyEventArgs e)
        {
            if (_isInputDialogOpen || !_inEditMode) return;

            if (e.Key == Keys.Space)
                TogglePlayPause();

            if (e.Key == Keys.Left)
                _audio.SetCurrentTime(_audio.GetCurrentTime() - 0.1f);
            if (e.Key == Keys.Right)
                _audio.SetCurrentTime(_audio.GetCurrentTime() + 0.1f);
        }

        private void TogglePlayPause()
        {
            if (_isMusicPlaying)
            {
                _audio.Pause();
                _playPauseButton.NormalColor = Color4.Orange;
            }
            else
            {
                _audio.Resume();
                _playPauseButton.NormalColor = Color4.White;
            }
            _isMusicPlaying = !_isMusicPlaying;
        }

        // ====================== PROJECT CREATION ======================

        private void CreateProject()
        {
            var panel = new CreateProjectPanel(_game, _spriteBatch, _textRenderer, _scene, OnProjectCreated);
            panel.Show();
        }

        private void OnProjectCreated(string tappPath)
        {
            ActiveEditMode(tappPath);
        }

        private void LoadProject()
        {
            var filters = new[]
            {
                new SharpFileDialog.NativeFileDialog.Filter { Name = "TAPP Files", Extensions = new[] { "tapp" } }
            };

            if (SharpFileDialog.NativeFileDialog.OpenDialog(filters, "Edit\\", out string? path) && !string.IsNullOrEmpty(path))
            {
                ActiveEditMode(path);
            }
        }

        // ====================== EDIT MODE ======================

        private void ActiveEditMode(string tappPath)
        {
            _tappPath = tappPath;
            LoadProjectAssets(tappPath);

            CreateTimeSlider();
            CreateColorSliders();
            CreateEditorButtons();

            JsonMap? map = LoadMapData(tappPath);

            LoadMarkersFromFile(map);
            LoadColorsFromMap(map);

            _inEditMode = true;
        }
        private JsonMap? LoadMapData(string tappPath)
        {
            try
            {
                string json = File.ReadAllText(tappPath);
                return JsonSerializer.Deserialize<JsonMap>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не удалось загрузить .tapp файл: {ex.Message}");
                return new JsonMap(); // возвращаем пустой, чтобы не падало
            }
        }

        private void LoadProjectAssets(string tappPath)
        {
            string dir = Path.GetDirectoryName(tappPath) ?? "";

            string mp3 = Directory.GetFiles(dir, "*.mp3").FirstOrDefault() ?? "";
            string bg = Directory.GetFiles(dir, "*.png").Concat(Directory.GetFiles(dir, "*.jpg")).FirstOrDefault() ?? "";

            if (!string.IsNullOrEmpty(bg))
                _background._textureId = TextureLoader.Load(bg);

            if (!string.IsNullOrEmpty(mp3))
            {
                _audio.LoadMusic(mp3);
                _audio.Play();
                _audio.SetLooping(true);
            }
        }

        private void CreateTimeSlider()
        {
            _timeSlider = new Slider(_spriteBatch, _textRenderer, 0, 1000, 960, 900, 1800)
            {
                AllowHover = false
            };

            _timeSlider.maxValue = (float)_audio.Duration;
            if (_timeSlider.maxValueText != null)
                _timeSlider.maxValueText.Text = _timeSlider.maxValue.ToString("F2");

            _scene.Add(_timeSlider);
        }
/// <summary>
/// //////////////////////////Color Preview
/// </summary>
        private List<ColorGroup> _colorGroups = new();

        private TextObject _demoNewT;   // "new t" - цвет Tapped
        private TextObject _demoE;      // "e"     - цвет Need
        private TextObject _demoXt;     // "xt"    - цвет Tapped

        private TextObject _demoCompleteText;   // "new text" полностью в цвете Complete
        private void CreateColorSliders()
        {
            _colorGroups.Clear();

            float startX = 1620f;
            float startY = 130f;           // немного подняли

            float groupSpacingX = 200f;    // расстояние между первой и второй группой
            float groupVerticalSpacing = 200f;  // расстояние до третьей группы (стало меньше)

            // Группа 1: Tapped
            CreateColorGroup("Tapped", startX, startY, 0.4f, 0.3f, 0.6f);

            // Группа 2: Need
            CreateColorGroup("Need", startX + groupSpacingX, startY, 0.7f, 0.3f, 0.8f);

            // Группа 3: Complete (под двумя первыми, по центру)
            CreateColorGroup("Complete", startX + groupSpacingX / 2, startY + groupVerticalSpacing, 0.2f, 0.1f, 0.4f);

            CreateDemoTexts(startX, startY, groupSpacingX);
        }
        private void CreateColorGroup(string groupName, float x, float y, float defaultR, float defaultG, float defaultB)
        {
            float sliderSpacing = 45f;     // расстояние между R, G, B слайдерами
            float sliderWidth = 255f;

            var sliderR = new Slider(_spriteBatch, _textRenderer, 0f, 1f, x, y, sliderWidth)
            {
                ScaleMultiply = 0.72f,
                AllowHover = true,
                
            };
            sliderR.SetValue(defaultR);

            var sliderG = new Slider(_spriteBatch, _textRenderer, 0f, 1f, x, y + sliderSpacing, sliderWidth)
            {
                ScaleMultiply = 0.72f,
                AllowHover = true
            };
            sliderG.SetValue(defaultG);

            var sliderB = new Slider(_spriteBatch, _textRenderer, 0f, 1f, x, y + sliderSpacing * 2, sliderWidth)
            {
                ScaleMultiply = 0.72f,
                AllowHover = true
            };
            sliderB.SetValue(defaultB);
            
            _scene.Add(sliderR);
            _scene.Add(sliderG);
            _scene.Add(sliderB);
            

            _colorGroups.Add(new ColorGroup(groupName, sliderR, sliderG, sliderB));
        }
        private void CreateDemoTexts(float startX, float startY, float groupSpacingX)
        {
            float centerXTop = startX + groupSpacingX / 2;

            // === Разноцветный текст над первыми двумя группами ===
            _demoNewT = new TextObject(_textRenderer, "new t", centerXTop - 28, startY - 90, 1f)
            {
                ScaleMultiply = 0.6f,
                Align = TextRender.TextAlign.Center,
                Color = Color4.White
            };

            _demoE = new TextObject(_textRenderer, " e", centerXTop +45, startY - 90, 1f)
            {
                ScaleMultiply = 0.6f,
                Align = TextRender.TextAlign.Center,
                Color = Color4.White
            };

            _demoXt = new TextObject(_textRenderer, "xt", centerXTop +90, startY - 90, 1f)
            {
                ScaleMultiply = 0.6f,
                Align = TextRender.TextAlign.Center,
                Color = Color4.White
            };

            // === Текст над третьей группой ===
            _demoCompleteText = new TextObject(_textRenderer, "new text",
                                               startX + groupSpacingX / 2, startY + 90, 1f)
            {
                ScaleMultiply = 0.5f,
                Align = TextRender.TextAlign.Center,
                Color = Color4.White
            };

            _scene.Add(_demoNewT);
            _scene.Add(_demoE);
            _scene.Add(_demoXt);
            _scene.Add(_demoCompleteText);
        }

        // Обновление цветов каждые кадр
        private void UpdateColorPreviews()
        {
            if (_colorGroups.Count < 3) return;

            var tapped = _colorGroups[0];
            var need = _colorGroups[1];
            var complete = _colorGroups[2];

            Color4 tappedColor = new Color4(tapped.R.Value, tapped.G.Value, tapped.B.Value, 1f);
            Color4 needColor = new Color4(need.R.Value, need.G.Value, need.B.Value, 1f);
            Color4 completeColor = new Color4(complete.R.Value, complete.G.Value, complete.B.Value, 1f);

            // Обновляем цвета текста
            _demoNewT.Color = tappedColor;
            _demoE.Color = needColor;
            _demoXt.Color = Color4.White;     // "xt" тоже в цвет Tapped

            _demoCompleteText.Color = completeColor;
        }
        internal record ColorGroup(
        string Name,
        Slider R,
        Slider G,
        Slider B
        );

        private void CreateEditorButtons()
        {
            _playPauseButton = new Button(_spriteBatch, _textRenderer, 960, 1000, 100, 100, "pause", "", Color4.White) { Layer = 1 };
            _playPauseButton.OnClick += TogglePlayPause;
            _scene.Add(_playPauseButton);

            _addMarkerButton = CreateEditorButton(1200, 1000, 400, 100, "button", "Add Marker", 0.6f, AddMarker);
            _saveMarkersButton = CreateEditorButton(1780, 30, 700, 120, "button", "Save Project", 0.4f, SaveProject);

            _scene.Add(_addMarkerButton);
            _scene.Add(_saveMarkersButton);
        }

        private Button CreateEditorButton(float x, float y, float w, float h, string texture, string text, float scale, Action onClick)
        {
            var btn = new Button(_spriteBatch, _textRenderer, x, y, w, h, texture, text, Color4.White)
            {
                Layer = 1,
                TextColor = Color4.White,
                TextOffset = new Vector2(0f, -50f),
                TextScale = 0.7f,
                ScaleMultiply = scale
            };
            btn.OnClick += onClick;
            return btn;
        }

        private void AddMarker()
        {
            if (_isInputDialogOpen) return;
            _audio.Pause();
            _isMusicPlaying = false;

            float time = (float)_audio.GetCurrentTime();

            var dialog = new TextInputDialog(_game, _spriteBatch, _textRenderer, _scene,
                "Введите текст маркера",
                text => { if (!string.IsNullOrWhiteSpace(text)) AddMarkerAtTime(time, text); },
                () => _isInputDialogOpen = false);

            _isInputDialogOpen = true;
            dialog.Show();
        }

        private void AddMarkerAtTime(float time, string text)
        {
            float x = _timeSlider.GetPositionFromTime(time);

            var visual = new SpriteObject(_spriteBatch, TextureManager.GetTexture("marker"), x, _timeSlider.line.Position.Y, 25, 25)
            {
                Color = Color4.Yellow,
                Pivot = new Vector2(0.5f, 0.5f)
            };

            _scene.Add(visual);
            _markers.Add(new Marker(time, text, visual));
        }
        private void SaveProject()
        {
            if (string.IsNullOrEmpty(_tappPath))
            {
                Console.WriteLine("Ошибка: нет пути к проекту");
                return;
            }

            try
            {
                // Загружаем текущий JSON (чтобы не потерять другие данные)
                string json = File.ReadAllText(_tappPath);
                var map = JsonSerializer.Deserialize<JsonMap>(json) ?? new JsonMap();

                // Сохраняем маркеры
                map.events = _markers.Select(m => new TimingEvent
                {
                    time = m.Time,
                    text = m.Text
                }).ToList();

                // === Сохраняем цвета из слайдеров ===
                if (_colorGroups.Count >= 3)
                {
                    var tapped = _colorGroups[0];
                    var need = _colorGroups[1];
                    var complete = _colorGroups[2];

                    map.tappedR = tapped.R.Value;
                    map.tappedG = tapped.G.Value;
                    map.tappedB = tapped.B.Value;

                    map.needR = need.R.Value;
                    map.needG = need.G.Value;
                    map.needB = need.B.Value;

                    map.completeR = complete.R.Value;
                    map.completeG = complete.G.Value;
                    map.completeB = complete.B.Value;
                }

                // Сохраняем обратно в файл
                string newJson = JsonSerializer.Serialize(map, new JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(_tappPath, newJson);

                Console.WriteLine($"Проект успешно сохранён! ({_markers.Count} маркеров, цвета обновлены)");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения проекта: {ex.Message}");
            }
        }

        private void LoadColorsFromMap(JsonMap map)
        {
            if (map == null || _colorGroups.Count < 3) return;

            var tapped = _colorGroups[0];
            var need = _colorGroups[1];
            var complete = _colorGroups[2];

            tapped.R.SetValue(map.tappedR);
            tapped.G.SetValue(map.tappedG);
            tapped.B.SetValue(map.tappedB);

            need.R.SetValue(map.needR);
            need.G.SetValue(map.needG);
            need.B.SetValue(map.needB);

            complete.R.SetValue(map.completeR);
            complete.G.SetValue(map.completeG);
            complete.B.SetValue(map.completeB);
        }

        private void LoadMarkersFromFile(JsonMap? map)
        {
            if (map?.events == null) return;

            foreach (var e in map.events)
            {
                AddMarkerAtTime((float)e.time, e.text ?? "");
            }
        }
    }
}