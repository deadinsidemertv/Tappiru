// EditState.cs — координатор редактора (тонкий класс)
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using TappiruCS.Core;
using TappiruCS.Core.GameObject;
using TappiruCS.GameLogic;
using TappiruCS.Render;
using TappiruCS.Render.Text;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;

namespace TappiruCS.State.Edit
{
    /// <summary>
    /// Точка входа в редактор. Владеет сценой и делегирует ответственности:
    ///   • ProjectIO          — открытие / сохранение файлов
    ///   • PhraseSerializer   — конвертация Phrase ↔ TimingEvent
    ///   • ColorPreviewPanel  — RGB-слайдеры и демо-текст
    ///   • Timeline           — визуал таймлайна и drag
    /// </summary>
    internal class EditState : IGameState
    {
        public EditState(RenderContext context)
        {
            _context = context;
        }

        private readonly RenderContext _context;
        private readonly Scene _scene = new();

        // ── Делегаты ─────────────────────────────────────────────────────────────
        private readonly ProjectIO _projectIO = new();
        private readonly ColorPreviewPanel _colorPanel = new();

        // ── UI ───────────────────────────────────────────────────────────────────
        private SpriteObject _background = null!;
        private Background _darkOverlay = null!;
        private Timeline _timeline = null!;
        private Button _playPauseButton = null!;
        private Button _addPhraseButton = null!;
        private Button _saveButton = null!;

        // ── Состояние ────────────────────────────────────────────────────────────
        private bool _inEditMode;
        private bool _isPlaying = true;
        private bool _isInputDialogOpen = false;

        private readonly List<Phrase> _phrases = new();

        // ── Посимвольный текст фразы ─────────────────────────────────────────────
        private readonly List<TextObject> _charTexts = new();
        private Phrase? _activePhrase;

        // ── IGameState ───────────────────────────────────────────────────────────
        public void OnEnter()
        {
            _scene.Initialize(_context);
            _context.Audio.Stop();
            BuildStartScreen();
        }

        public void OnExit()
        {
            _scene.Clear();
            _phrases.Clear();
            ClearCharTexts();
            _inEditMode = false;
            _projectIO.Cleanup();
        }

        public void Update(double deltaTime)
        {
            _scene.Update(deltaTime, _context.Game.MouseState, _context.Game);

            if (_inEditMode && _timeline != null)
            {
                _timeline.SetCurrentTime((float)_context.Audio.GetCurrentTime());
                SyncActivePhraseText();
                _colorPanel.Tick();
            }
        }

        public void Render(Matrix4 projection) => _scene.Draw(projection);

        public void HandleKeyDown(KeyboardKeyEventArgs e)
        {
            if (_isInputDialogOpen || !_inEditMode) return;
            if (e.Key == Keys.Space) TogglePlayPause();
        }

        // ── Начальный экран (Create / Load) ─────────────────────────────────────
        private void BuildStartScreen()
        {
            var _editor_overlay = new Background(TextureManager.GetTexture("editor_overlay"));
            _background = new SpriteObject(TextureManager.GetTexture("defaultBG"), 960, 450, 1152, 648) { ScaleMultiply = 1.1f};
            _background.Color = new Color4(0.2f, 0.2f, 0.2f, 1f);

            _scene.Add(_background);
            _scene.Add(_editor_overlay);
            _scene.Add(MakeTopButton(160, "Create Project", OpenCreateDialog));
            _scene.Add(MakeTopButton(440, "Load Project", OpenLoadDialog));
        }

        private Button MakeTopButton(float x, string label, Action onClick)
        {
            var btn = new Button(x, 30, 700, 120, "button", label)
            {
                Layer = 1,
                TextOffset = new Vector2(-180f, -60f),
                Pivot = new Vector2(0.5f, 0.5f),
                ScaleMultiply = 0.4f,
                Tag = "editbutton"
            };
            btn.Label.Align = TextAlign.Center;
            btn.Label.Color = Color4.White;
            btn.OnClick += onClick;
            return btn;
        }

        // ── Диалоги создания / открытия ──────────────────────────────────────────
        private void OpenCreateDialog()
        {
            var module = new CreateProjectModule(_scene, OnProjectOpened);
            _context.Game.CloseModalWindow();
            _context.Game.OpenModalWindow(module);
        }

        private void OpenLoadDialog()
        {
            var filters = new[]
            {
                new SharpFileDialog.NativeFileDialog.Filter
                {
                    Name       = "Tappiru Project Files",
                    Extensions = new[] { "tappz" }
                }
            };

            if (SharpFileDialog.NativeFileDialog.OpenDialog(filters, "Edit\\", out string? path)
                && !string.IsNullOrEmpty(path))
            {
                OnProjectOpened(path);
            }
        }

        // ── Активация режима редактирования ─────────────────────────────────────
        private void OnProjectOpened(string tappzPath)
        {
            JsonMap? map = _projectIO.Open(tappzPath);

            LoadAssets();
            BuildEditorUI();

            _phrases.Clear();
            _phrases.AddRange(PhraseSerializer.FromEvents(map?.events));

            _colorPanel.Build(_scene);
            if (map != null) _colorPanel.LoadFrom(map);

            _timeline.SetDuration((float)_context.Audio.Duration);
            _timeline.SetPhrases(_phrases);
            _timeline.RefreshAllVisuals();

            _inEditMode = true;
            Console.WriteLine($"[EditState] Загружено фраз: {_phrases.Count}");
        }

        private void LoadAssets()
        {
            if (!string.IsNullOrEmpty(_projectIO.BgPath))
                _background._textureId = TextureLoader.Load(_projectIO.BgPath);

            if (!string.IsNullOrEmpty(_projectIO.Mp3Path))
            {
                _context.Audio.LoadMusic(_projectIO.Mp3Path);
                _context.Audio.Play();
                _context.Audio.SetLooping(true);
            }
        }

        private void BuildEditorUI()
        {
            _timeline = new Timeline(952, 708, 1220, 80);
            _timeline.SetDuration((float)_context.Audio.Duration);
            _timeline.OnTimeClicked += time => _context.Audio.SetCurrentTime(time);
            _scene.Add(_timeline);

            _playPauseButton = new Button(960, 1000, 100, 100, "pause", "") { Layer = 1 };
            _playPauseButton.OnClick += TogglePlayPause;

            _addPhraseButton = new Button(1200, 1000, 420, 100, "button", "ADD PHRASE")
            {
                Layer = 1,
                TextOffset = new Vector2(-120, -30),
                Tag = "noanim",
                ScaleMultiply = 0.5f
            };
            _addPhraseButton.OnClick += BeginAddPhrase;

            _saveButton = new Button(1780, 30, 500, 100, "button", "Save Project")
            {
                Layer = 1,
                TextOffset = new Vector2(-180f, -60f),
                Pivot = new Vector2(0.5f, 0.5f),
                ScaleMultiply = 0.4f,
                Tag = "topButton"
            };
            _saveButton.Label.Align = TextAlign.Center;
            _saveButton.Label.Color = Color4.White;
            _saveButton.OnClick += SaveProject;

            _scene.Add(_playPauseButton);
            _scene.Add(_addPhraseButton);
            _scene.Add(_saveButton);
        }

        // ── Play / Pause ─────────────────────────────────────────────────────────
        private void TogglePlayPause()
        {
            if (_isPlaying)
            {
                _context.Audio.Pause();
                _playPauseButton.NormalColor = Color4.Orange;
            }
            else
            {
                _context.Audio.Resume();
                _playPauseButton.NormalColor = Color4.White;
            }
            _isPlaying = !_isPlaying;
        }

        // ── Добавление фразы ─────────────────────────────────────────────────────
        private void BeginAddPhrase()
        {
            if (_isInputDialogOpen) return;

            _context.Audio.Pause();
            _isPlaying = false;

            float time = (float)_context.Audio.GetCurrentTime();

            var dialog = new TextInputModule(
                _scene,
                "Введите текст фразы",
                text =>
                {
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        _phrases.Add(new Phrase(time, time + 4f, text));
                        _timeline?.SetPhrases(_phrases);
                    }
                },
                () => _isInputDialogOpen = false);

            _isInputDialogOpen = true;
            _context.Game.CloseModalWindow();
            _context.Game.OpenModalWindow(dialog);
        }

        // ── Посимвольный текст активной фразы ───────────────────────────────────
        private void SyncActivePhraseText()
        {
            if (_phrases.Count == 0) { ClearCharTexts(); _activePhrase = null; return; }

            float now = (float)_context.Audio.GetCurrentTime();
            Phrase? active = _phrases.LastOrDefault(p => p.ContainsTime(now));

            if (active == _activePhrase) return;

            _activePhrase = active;
            RebuildCharTexts();
        }

        private void RebuildCharTexts()
        {
            ClearCharTexts();
            if (_activePhrase == null || string.IsNullOrEmpty(_activePhrase.Text)) return;

            string text = _activePhrase.Text;
            const float spacing = 42f;
            float startX = 960 - text.Length * spacing / 2f;

            for (int i = 0; i < text.Length; i++)
            {
                bool hasSlider = _activePhrase.Sliders.Any(s => s.charIndex == i);

                var charObj = new TextObject(text[i].ToString(), startX + i * spacing, 480, 96)
                {
                    AllowHover = true,
                    FixedColor = hasSlider,
                    Color = hasSlider ? Color4.Red : Color4.White
                };

                int idx = i;
                charObj.OnClick = _ => AddSliderForChar(_activePhrase!, idx);

                _scene.Add(charObj);
                _charTexts.Add(charObj);
            }
        }

        private void ClearCharTexts()
        {
            foreach (var t in _charTexts) _scene.Remove(t);
            _charTexts.Clear();
        }

        private void AddSliderForChar(Phrase phrase, int charIndex)
        {
            float now = (float)_context.Audio.GetCurrentTime();
            float endT = Math.Min(now + 2.0f, phrase.EndTime);
            if (endT - now < 0.2f) endT = now + 0.2f;

            phrase.Sliders.Add(new SliderTiming
            {
                charIndex = charIndex,
                startTime = now,
                endTime = endT
            });

            _timeline?.SetPhrases(_phrases);
            RebuildCharTexts();
        }

        // ── Сохранение ───────────────────────────────────────────────────────────
        private void SaveProject()
        {
            var map = new JsonMap();
            PhraseSerializer.ToEvents(_phrases).ForEach(e => map.events ??= new());
            map.events = PhraseSerializer.ToEvents(_phrases);
            _colorPanel.SaveTo(map);

            try { _projectIO.Save(map); }
            catch (Exception ex) { Console.WriteLine($"[EditState] Ошибка сохранения: {ex.Message}"); }
        }
    }
}