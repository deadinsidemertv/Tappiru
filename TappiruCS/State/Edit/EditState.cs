// EditState.cs — ПОЛНАЯ РАБОЧАЯ ВЕРСИЯ (посимвольный текст + клик по букве + подсветка)
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Linq;
using System.Text.Json;
using TappiruCS.Core;
using TappiruCS.GameLogic;
using TappiruCS.Render;
using TappiruCS.UI;
using TappiruCS.Core.GameObject;
using TappiruCS.UI.TextAbstract;

namespace TappiruCS.State.Edit
{
    internal class EditState : IGameState
    {
        private readonly RenderContext _context;

        private readonly Scene _scene = new Scene();

        private Background _background = null!;
        private Background _darkOverlay = null!;

        private Timeline _timeline = null!;

        private Button _addPhraseButton = null!;
        private Button _playPauseButton = null!;
        private Button _saveProjectButton = null!;

        private bool _inEditMode = false;
        private bool _isMusicPlaying = true;
        private bool _isInputDialogOpen = false;

        private string? _tappPath;
        private readonly List<Phrase> _phrases = new();

        // Посимвольный текст
        private readonly List<TextObject> _charTexts = new();
        private Phrase? _currentActivePhrase = null;

        // Color Preview
        private List<ColorGroup> _colorGroups = new();
        private TextObject _demoNewT = null!;
        private TextObject _demoE = null!;
        private TextObject _demoXt = null!;
        private TextObject _demoCompleteText = null!;

        private string? _projectDir;
        public EditState(RenderContext context)
        {
            _context = context;
        }

        public void OnEnter()
        {
            _scene.Initialize(_context);
            _context.Audio.Stop();
            CreateInitialUI();
        }

        public void OnExit()
        {
            _scene.Clear();
            _phrases.Clear();
            ClearCharTexts();
            _inEditMode = false;

            if (!string.IsNullOrEmpty(_projectDir) && Directory.Exists(_projectDir))
            {
                try
                {
                    Directory.Delete(_projectDir, true);   // true = удалить папку вместе с содержимым
                    Console.WriteLine($"[INFO] Временная папка удалена: {_projectDir}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[WARNING] Не удалось удалить временную папку {_projectDir}: {ex.Message}");
                    // Можно оставить папку, если она заблокирована (например, файл MP3 всё ещё проигрывается)
                }
            }

            _projectDir = null;
        }

        private void CreateInitialUI()
        {
            _background = new Background( TextureManager.GetTexture("defaultBG")) { ParalaxEffect = true };
            _darkOverlay = new Background( 0) { Opacity = 0.75f };

            var createBtn = CreateTopButton(160, "Create Project", CreateProject);
            var loadBtn = CreateTopButton(440, "Load Project", LoadProject);

            _scene.Add(_background);
            _scene.Add(_darkOverlay);
            _scene.Add(createBtn);
            _scene.Add(loadBtn);
        }

        private Button CreateTopButton(float x, string text, Action onClick)
        {
            var btn = new Button(  x, 30, 700, 120, "button", text)
            {
                Layer = 1,
                TextColor = Color4.White,
                TextOffset = new Vector2(-180f, -60f),
                Pivot = new Vector2(0.5f,0.5f),
                TextScale = 0.7f,
                ScaleMultiply = 0.4f,
                TextAlign = TextRender.TextAlign.Center,
                Tag = "topButton"
            };
            btn.OnClick += onClick;
            return btn;
        }

        #region Update & Render
        public void Update(double deltaTime)
        {
            var mouse = _context.Game.MouseState;
            _scene.Update(deltaTime, mouse, _context.Game);

            if (_inEditMode && _timeline != null)
            {
                _timeline.SetCurrentTime((float)_context.Audio.GetCurrentTime());
                UpdateRenderedMarkerText();
                UpdateColorPreviews();
            }
        }

        private void UpdateRenderedMarkerText()
        {
            if (_phrases.Count == 0)
            {
                ClearCharTexts();
                _currentActivePhrase = null;
                return;
            }

            double current = _context.Audio.GetCurrentTime();
            var active = _phrases.LastOrDefault(p => p.ContainsTime((float)current));

            if (active != _currentActivePhrase)
            {
                Console.WriteLine($"[DEBUG] Active phrase changed to: '{active?.Text}'");
                _currentActivePhrase = active;
                RebuildCharTexts();
            }
        }

        public void Render(Matrix4 projection)
        {
            _scene.Draw(projection);
        }

        public void HandleKeyDown(KeyboardKeyEventArgs e)
        {
            if (_isInputDialogOpen || !_inEditMode) return;
            if (e.Key == Keys.Space) TogglePlayPause();
        }

        private void TogglePlayPause()
        {
            if (_isMusicPlaying)
            {
                _context.Audio.Pause();
                _playPauseButton.NormalColor = Color4.Orange;
            }
            else
            {
                _context.Audio.Resume();
                _playPauseButton.NormalColor = Color4.White;
            }
            _isMusicPlaying = !_isMusicPlaying;
        }
        #endregion

        #region Phrase & Slider Management
        private void AddNewPhrase()
        {
            if (_isInputDialogOpen) return;
            _context.Audio.Pause();
            _isMusicPlaying = false;

            float time = (float)_context.Audio.GetCurrentTime();

            var dialog = new TextInputDialog(_context, _scene,
                "Введите текст фразы",
                text =>
                {
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        var phrase = new Phrase(time, time + 4f, text);
                        AddPhraseToEditor(phrase);
                    }
                },
                () => _isInputDialogOpen = false);

            _isInputDialogOpen = true;
            dialog.Show();
        }

        private void AddPhraseToEditor(Phrase phrase)
        {
            _phrases.Add(phrase);
            RefreshTimelineVisuals();
        }

        private void RefreshTimelineVisuals()
        {
            _timeline?.SetPhrases(_phrases);
        }

        private void ClearCharTexts()
        {
            foreach (var t in _charTexts) _scene.Remove(t);
            _charTexts.Clear();
        }

        private void RebuildCharTexts()
        {
            ClearCharTexts();
            if (_currentActivePhrase == null || string.IsNullOrEmpty(_currentActivePhrase.Text)) return;

            string text = _currentActivePhrase.Text;
            float charSpacing = 42f;
            float totalWidth = text.Length * charSpacing;
            float startX = 960 - totalWidth / 2f;

            for (int i = 0; i < text.Length; i++)
            {
                var charObj = new TextObject(text[i].ToString(),
                                             startX + i * charSpacing, 480, 1.05f);

                int index = i;

                // Фиксируем цвет, если для этой буквы уже есть слайдер
                charObj.FixedColor = _currentActivePhrase.Sliders.Any(s => s.charIndex == index);
                    

                charObj.OnClick = _ => CreateSliderForChar(_currentActivePhrase!, index);

                _scene.Add(charObj);
                _charTexts.Add(charObj);

            }
        }

        private void CreateSliderForChar(Phrase phrase, int charIndex)
        {
            Console.WriteLine("=== CreateSliderForChar called ===");
            if (phrase == null)
            {
                Console.WriteLine("ERROR: phrase is null!");
                return;
            }
            float currentTime = (float)_context.Audio.GetCurrentTime();
            float startT = currentTime;
            float endT = Math.Min(startT + 2.0f, phrase.EndTime);
            if (endT - startT < 0.2f) endT = startT + 0.2f;

            var slider = new SliderTiming
            {
                charIndex = charIndex,
                startTime = startT,
                endTime = endT
            };

            phrase.Sliders.Add(slider);
            RefreshTimelineVisuals();
            RebuildCharTexts();

            Console.WriteLine($"💕 Слайдер создан! Буква '{phrase.Text[charIndex]}' (индекс {charIndex})");
        }
        #endregion

        #region Project Management
        private void CreateProject()
        {
            var panel = new CreateProjectPanel(_context, _scene, OnProjectCreated);
            panel.Show();
        }

        private void OnProjectCreated(string tappPath) => ActiveEditMode(tappPath);

        private void LoadProject()
        {
            var filters = new[]
            {
        new SharpFileDialog.NativeFileDialog.Filter
        {
            Name = "Tappiru Project Files",
            Extensions = new[] { "tappz" }
        }
    };

            if (SharpFileDialog.NativeFileDialog.OpenDialog(filters, "Edit\\", out string? path) && !string.IsNullOrEmpty(path))
                ActiveEditMode(path);
        }

        private void ActiveEditMode(string tappzPath)
        {
            _tappPath = tappzPath;                    // путь к .tappz файлу (ZIP)

            string projectName = Path.GetFileNameWithoutExtension(tappzPath);
            _projectDir = Path.Combine(Directory.GetCurrentDirectory(), "Edit", projectName);

            Directory.CreateDirectory(_projectDir);

            // Распаковываем .tappz в рабочую папку
            if (File.Exists(tappzPath))
            {
                ZipFile.ExtractToDirectory(tappzPath, _projectDir, overwriteFiles: true);
            }

            // === ИЩЕМ ЛЮБОЙ ФАЙЛ *.tapp внутри распакованной папки ===
            string[] tappFiles = Directory.GetFiles(_projectDir, "*.tapp", SearchOption.TopDirectoryOnly);

            string dataFilePath;
            if (tappFiles.Length > 0)
            {
                dataFilePath = tappFiles[0];           // берём первый найденный .tapp файл
                Console.WriteLine($"[INFO] Загружен файл карты: {Path.GetFileName(dataFilePath)}");
            }
            else
            {
                Console.WriteLine("[WARNING] Не найден .tapp файл внутри проекта! Создаём новый.");
                dataFilePath = Path.Combine(_projectDir, "data.tapp");
                // Можно создать пустой JsonMap здесь, если нужно
            }

            LoadProjectAssets(_projectDir);

            CreateTimeline();
            CreateEditorButtons();
            CreateColorSliders();

            JsonMap? map = LoadMapData(dataFilePath);
            LoadPhrasesFromMap(map);
            LoadColorsFromMap(map);

            _inEditMode = true;
        }

        private void CreateTimeline()
        {
            _timeline = new Timeline(960, 850, 1600, 80);
            _timeline.SetDuration((float)_context.Audio.Duration);
            _timeline.OnTimeClicked += HandleTimelineClick;
            _scene.Add(_timeline);
        }

        private void HandleTimelineClick(float clickedTime)
        {
            _context.Audio.SetCurrentTime(clickedTime);
        }

        private void CreateEditorButtons()
        {
            _playPauseButton = new Button( 960, 1000, 100, 100, "pause", "") { Layer = 1 };
            _playPauseButton.OnClick += TogglePlayPause;

            _addPhraseButton = new Button(300, 1000, 420, 100, "button", "ADD PHRASE")
            {
                Layer = 1,
                TextScale = 0.75f
            };
            _addPhraseButton.OnClick += AddNewPhrase;

            _saveProjectButton = new Button( 1780, 30, 500, 100, "button", "Save Project")
            {
                Layer = 1,
                TextColor = Color4.White,
                TextOffset = new Vector2(-180f, -60f),
                Pivot = new Vector2(0.5f, 0.5f),
                TextScale = 0.7f,
                ScaleMultiply = 0.4f,
                TextAlign = TextRender.TextAlign.Center,
                Tag = "topButton"
            };
            _saveProjectButton.OnClick += SaveProject;

            _scene.Add(_playPauseButton);
            _scene.Add(_addPhraseButton);
            _scene.Add(_saveProjectButton);
        }
        #endregion

        #region Save / Load
        private void SaveProject()
        {
            if (string.IsNullOrEmpty(_tappPath) || string.IsNullOrEmpty(_projectDir))
                return;

            try
            {
                // Находим .tapp файл в рабочей папке (любой)
                string[] tappFiles = Directory.GetFiles(_projectDir, "*.tapp", SearchOption.TopDirectoryOnly);
                string dataPath = tappFiles.Length > 0 ? tappFiles[0]
                                 : Path.Combine(_projectDir, "data.tapp");

                JsonMap? map;
                if (File.Exists(dataPath))
                {
                    string json = File.ReadAllText(dataPath);
                    map = JsonSerializer.Deserialize<JsonMap>(json) ?? new JsonMap();
                }
                else
                {
                    map = new JsonMap();
                }

                SavePhrasesToMap(map);
                SaveColorsToMap(map);

                string newJson = JsonSerializer.Serialize(map, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(dataPath, newJson);

                // Обновляем ZIP-файл (.tappz)
                using (var archive = ZipFile.Open(_tappPath, ZipArchiveMode.Update))
                {
                    // Удаляем старый .tapp файл (какое бы имя он ни имел)
                    var oldEntry = archive.Entries.FirstOrDefault(e => e.FullName.EndsWith(".tapp", StringComparison.OrdinalIgnoreCase));
                    oldEntry?.Delete();

                    // Добавляем актуальный .tapp файл с тем же именем
                    string entryName = Path.GetFileName(dataPath);
                    var newEntry = archive.CreateEntry(entryName);

                    using (var stream = newEntry.Open())
                    {
                        byte[] bytes = Encoding.UTF8.GetBytes(newJson);
                        stream.Write(bytes, 0, bytes.Length);
                    }
                }

                Console.WriteLine($"Проект успешно сохранён (.tappz)!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка сохранения: {ex.Message}");
            }
        }

        private void SavePhrasesToMap(JsonMap map)
        {
            map.events = _phrases.Select(p => new TimingEvent
            {
                startTime = p.StartTime,
                endTime = p.EndTime,
                text = p.Text,
                sliders = p.Sliders?.Select(s => new SliderTiming
                {
                    charIndex = s.charIndex,
                    startTime = s.startTime,
                    endTime = s.endTime
                }).ToList() ?? new List<SliderTiming>()
            }).ToList();
        }

        private void LoadPhrasesFromMap(JsonMap? map)
        {
            _phrases.Clear();
            if (map?.events == null) return;

            foreach (var e in map.events)
            {
                float start = (float)e.startTime;
                float end = (float)(e.endTime > 0 ? e.endTime : e.startTime + 4f);

                var phrase = new Phrase(start, end, e.text ?? "");

                if (e.sliders != null)
                {
                    phrase.Sliders = e.sliders.Select(s => new SliderTiming
                    {
                        charIndex = s.charIndex,
                        startTime = s.startTime,
                        endTime = s.endTime
                    }).ToList();
                }

                _phrases.Add(phrase);
            }

            RefreshTimelineVisuals();
        }

        private void SaveColorsToMap(JsonMap map)
        {
            if (_colorGroups.Count < 3) return;
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

        private void LoadProjectAssets(string projectDir)
        {
            string mp3 = Directory.GetFiles(projectDir, "*.mp3").FirstOrDefault() ?? "";
            string bg = Directory.GetFiles(projectDir, "*.png")
                                 .Concat(Directory.GetFiles(projectDir, "*.jpg"))
                                 .FirstOrDefault() ?? "";

            if (!string.IsNullOrEmpty(bg))
                _background._textureId = TextureLoader.Load(bg);

            if (!string.IsNullOrEmpty(mp3))
            {
                _context.Audio.LoadMusic(mp3);
                _context.Audio.Play();
                _context.Audio.SetLooping(true);
            }
        }

        private JsonMap? LoadMapData(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return new JsonMap();

                string json = File.ReadAllText(filePath);
                return JsonSerializer.Deserialize<JsonMap>(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Не удалось загрузить .tapp файл: {ex.Message}");
                return new JsonMap();
            }
        }
        #endregion

        #region Color Preview
        private void CreateColorSliders()
        {
            _colorGroups.Clear();
            float startX = 1620f;
            float startY = 130f;
            float groupSpacingX = 200f;
            float groupVerticalSpacing = 200f;

            CreateColorGroup("Tapped", startX, startY, 0.4f, 0.3f, 0.6f);
            CreateColorGroup("Need", startX + groupSpacingX, startY, 0.7f, 0.3f, 0.8f);
            CreateColorGroup("Complete", startX + groupSpacingX / 2, startY + groupVerticalSpacing, 0.2f, 0.1f, 0.4f);

            CreateDemoTexts(startX, startY, groupSpacingX);
        }

        private void CreateColorGroup(string groupName, float x, float y, float defaultR, float defaultG, float defaultB)
        {
            float sliderSpacing = 45f;
            float sliderWidth = 255f;

            var sliderR = new Slider(0f, 1f, x, y, sliderWidth) { ScaleMultiply = 0.72f, AllowHover = true };
            sliderR.SetValue(defaultR);
            var sliderG = new Slider(0f, 1f, x, y + sliderSpacing, sliderWidth) { ScaleMultiply = 0.72f, AllowHover = true };
            sliderG.SetValue(defaultG);
            var sliderB = new Slider(0f, 1f, x, y + sliderSpacing * 2, sliderWidth) { ScaleMultiply = 0.72f, AllowHover = true };
            sliderB.SetValue(defaultB);

            _scene.Add(sliderR);
            _scene.Add(sliderG);
            _scene.Add(sliderB);

            _colorGroups.Add(new ColorGroup(groupName, sliderR, sliderG, sliderB));
        }

        private void CreateDemoTexts(float startX, float startY, float groupSpacingX)
        {
            float centerXTop = startX + groupSpacingX / 2;
            _demoNewT = new TextObject( "new t", centerXTop - 28, startY - 90, 1f) { ScaleMultiply = 0.6f, Align = TextRender.TextAlign.Center, Color = Color4.White };
            _demoE = new TextObject( " e", centerXTop + 45, startY - 90, 1f) { ScaleMultiply = 0.6f, Align = TextRender.TextAlign.Center, Color = Color4.White };
            _demoXt = new TextObject( "xt", centerXTop + 90, startY - 90, 1f) { ScaleMultiply = 0.6f, Align = TextRender.TextAlign.Center, Color = Color4.White };
            _demoCompleteText = new TextObject( "new text", startX + groupSpacingX / 2, startY + 90, 1f) { ScaleMultiply = 0.5f, Align = TextRender.TextAlign.Center, Color = Color4.White };

            _scene.Add(_demoNewT);
            _scene.Add(_demoE);
            _scene.Add(_demoXt);
            _scene.Add(_demoCompleteText);
        }

        private void UpdateColorPreviews()
        {
            if (_colorGroups.Count < 3) return;
            var tapped = _colorGroups[0];
            var need = _colorGroups[1];
            var complete = _colorGroups[2];

            _demoNewT.Color = new Color4(tapped.R.Value, tapped.G.Value, tapped.B.Value, 1f);
            _demoE.Color = new Color4(need.R.Value, need.G.Value, need.B.Value, 1f);
            _demoXt.Color = Color4.White;
            _demoCompleteText.Color = new Color4(complete.R.Value, complete.G.Value, complete.B.Value, 1f);
        }

        internal record ColorGroup(string Name, Slider R, Slider G, Slider B);
        #endregion
    }
}