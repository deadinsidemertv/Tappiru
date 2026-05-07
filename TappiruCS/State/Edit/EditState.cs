using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TappiruCS.Core;
using TappiruCS.Core.GameObject;
using TappiruCS.GameLogic;
using TappiruCS.Render;
using TappiruCS.Render.Text;
using TappiruCS.State.Edit.Core;
using TappiruCS.State.Edit.Panels;
using TappiruCS.State.Edit.SaveLoad;
using TappiruCS.State.Edit.TimelineSystem;
using TappiruCS.State.Edit.UI.Panels;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;
using TappiruCS.State.Menu;

namespace TappiruCS.State.Edit
{
    internal class EditState : IGameState
    {
        private readonly RenderContext _context;
        private readonly Scene _scene = new();

        private SpriteObject _background = null!;
        private Timeline _timeline = null!;
        private PhraseTextDisplay _phraseDisplay = null!;
        private PhrasePropertiesPanel _propertiesPanel = null!;
        private Button _playPauseButton = null!;
        private Button _addPhraseButton = null!;
        private Button _saveButton = null!;
        private Button _exitToMenuButton = null!;

        private readonly List<Phrase> _phrases = new();
        private readonly ProjectIO _projectIO = new();
        private readonly ColorPreviewPanel _colorPanel = new();

        private bool _inEditMode;
        private bool _isPlaying = true;
        private bool _isInputDialogOpen = false;

        private Phrase? _activePhrase;

        public ITimelineSelectable? SelectedObject { get; private set; }
        public event Action? OnSelectionChanged;

        public EditState(RenderContext context)
        {
            _context = context;
        }

        public void OnEnter()
        {
            _scene.Initialize(_context);
            _context.Audio.Stop();
            BuildStartScreen();
        }

        public void OnExit()
        {
            CleanupEditorUI();
            _scene.Clear();
            _projectIO.Cleanup();
        }

        private void CleanupEditorUI()
        {
            _inEditMode = false;

            // Правильное удаление через Scene
            if (_timeline != null) _scene.Remove(_timeline);
            _phraseDisplay?.Dispose();
            _propertiesPanel = null!;

            if (_playPauseButton != null) _scene.Remove(_playPauseButton);
            if (_addPhraseButton != null) _scene.Remove(_addPhraseButton);
            if (_saveButton != null) _scene.Remove(_saveButton);
            if (_exitToMenuButton != null) _scene.Remove(_exitToMenuButton);

            _timeline = null!;
            _phraseDisplay = null!;
            _phrases.Clear();
        }

        private void BuildStartScreen()
        {
            var _editor_overlay = new Background(TextureManager.GetTexture("editor_overlay2"));
            _background = new SpriteObject(TextureManager.GetTexture("defaultBG"), 960, 450, 1152, 648) { ScaleMultiply = 1.1f,AllowHover =false };
            _background.Color = new Color4(0.2f, 0.2f, 0.2f, 1f);

            _exitToMenuButton = new Button(55, 1048, 400, 200, "blue_panel", "Back")
            {
                Layer = 1,
                TextOffset = new Vector2(0f, 0f),
                Pivot = new Vector2(0.5f, 0.5f),
                ScaleMultiply = 0.4f,
            };
            _exitToMenuButton.Label.Align = TextAlign.Center;
            _exitToMenuButton.Label.Color = Color4.White;
            _exitToMenuButton.Label.FontSize = 36f;
            _exitToMenuButton.OnClick += ShowExitConfirmation;


            _scene.Add(_background);
            _scene.Add(_editor_overlay);
            _scene.Add(MakeTopButton(325, "Create", OpenCreateDialog));
            _scene.Add(MakeTopButton(450, "Load", OpenLoadDialog));
            _scene.Add(_exitToMenuButton);
        }

        private Button MakeTopButton(float x, string label, Action onClick)
        {
            var btn = new Button(x, 50, 300, 210, "blue_panel", label)
            {
                Layer = 1,
                TextOffset = new Vector2(0f, 25f),
                Pivot = new Vector2(0.5f, 0.5f),
                ScaleMultiply = 0.4f,
                Tag = "editbutton"
            };
            btn.Label.Align = TextAlign.Center;
            btn.Label.Color = Color4.White;
            btn.Label.FontSize = 48f;
            btn.Label.FontKey = "Game";
            btn.OnClick += onClick;
            return btn;
        }

        private void OpenCreateDialog()
        {
            var module = new CreateProjectModule(_scene, OnProjectOpened);
            _context.Game.CloseModalWindow();
            _context.Game.OpenModalWindow(module);
        }

        private void OpenLoadDialog()
        {
            var filters = new[] { new SharpFileDialog.NativeFileDialog.Filter { Name = "Tappiru Project Files", Extensions = new[] { "tappz" } } };

            if (SharpFileDialog.NativeFileDialog.OpenDialog(filters, "Edit\\", out string? path) && !string.IsNullOrEmpty(path))
                OnProjectOpened(path);
        }

        private void OnProjectOpened(string tappzPath)
        {
            CleanupEditorUI();

            JsonMap? map = _projectIO.Open(tappzPath);
            if (map == null) return;

            LoadAssets();
            BuildEditorUI();

            _phrases.Clear();
            _phrases.AddRange(PhraseSerializer.FromEvents(map.events));

            if (map != null) _colorPanel.LoadFrom(map);

            _timeline.SetDuration((float)_context.Audio.Duration);
            _timeline.SetPhrases(_phrases);
            _timeline.RefreshAllVisuals();

            _inEditMode = true;
            Console.WriteLine($"[EditState] Загружен проект: {tappzPath} | Фраз: {_phrases.Count}");
        }

        private void BuildEditorUI()
        {
            _timeline = new Timeline(952, 820, 1220, 80);
            _timeline.SetDuration((float)_context.Audio.Duration);
            _timeline.OnTimeClicked += time => _context.Audio.SetCurrentTime(time);
            _timeline.OnObjectSelected += obj => SelectObject(obj);
            _scene.Add(_timeline);

            _phraseDisplay = new PhraseTextDisplay(_scene);
            _phraseDisplay.OnSliderRequested += AddSliderForChar;

            _propertiesPanel = new PhrasePropertiesPanel(_scene, _phraseDisplay, _timeline);
            _propertiesPanel.Build();

            OnSelectionChanged += () => _propertiesPanel?.Sync(SelectedObject);

            _playPauseButton = new Button(960, 900, 50, 50, "pause", "") { Layer = 1 };
            _playPauseButton.OnClick += TogglePlayPause;

            _addPhraseButton = new Button(1200, 1000, 420, 100, "blue_panel", "add")
            {
                Layer = 1,
                TextOffset = new Vector2(-120, -30),
                Tag = "noanim",
                ScaleMultiply = 0.5f
            };
            _addPhraseButton.OnClick += BeginAddPhrase;

            _saveButton = new Button(1855, 50, 300, 210, "blue_panel", "Save")
            {
                Layer = 1,
                TextOffset = new Vector2(0, 0f),
                Pivot = new Vector2(0.5f, 0.5f),
                ScaleMultiply = 0.4f,
            };
            _saveButton.Label.FontSize = 48f;
            _saveButton.Label.FontKey = "Game";
            _saveButton.Label.Align = TextAlign.Center;
            _saveButton.Label.Color = Color4.White;
            _saveButton.OnClick += SaveProject;

            _exitToMenuButton = new Button(55, 1048, 400, 200, "blue_panel", "Back")
            {
                Layer = 1,
                Pivot = new Vector2(0.5f, 0.5f),
                ScaleMultiply = 0.4f,
            };
            _exitToMenuButton.Label.Align = TextAlign.Center;
            _exitToMenuButton.Label.Color = Color4.White;
            _exitToMenuButton.Label.FontSize = 36f;
            _exitToMenuButton.Label.FontKey = "Game";
            _exitToMenuButton.OnClick += ShowExitConfirmation;
            _scene.Add(_exitToMenuButton);

            _scene.Add(_playPauseButton);
            _scene.Add(_addPhraseButton);
            _scene.Add(_saveButton);
            
        }

        private void ShowExitConfirmation()
        {
            var confirmModule = new ConfirmDialogModule(
                _scene,
                "Вы действительно хотите выйти в меню?",
                "Все несохранённые изменения будут потеряны.",
                () => ExitToMainMenu());

            _context.Game.CloseModalWindow();
            _context.Game.OpenModalWindow(confirmModule);
        }

        private void ExitToMainMenu()
        {
            CleanupEditorUI();
            _context.Game.ChangeState(new MenuState(_context));
        }

        public void SelectObject(ITimelineSelectable? obj)
        {
            SelectedObject = obj;
            _timeline.SelectedObject = obj;
            OnSelectionChanged?.Invoke();

            if (obj is Phrase phrase)
            {
                _phraseDisplay.Sync(phrase);
                _context.Audio.SetCurrentTime(phrase.StartTime);
            }
            else if (obj is TappiruCS.State.Edit.Core.SliderTiming slider)
            {
                var ownerPhrase = _phrases.FirstOrDefault(p => p.Sliders?.Contains(slider) == true);
                _phraseDisplay.Sync(ownerPhrase);
                if (ownerPhrase != null)
                    _context.Audio.SetCurrentTime(ownerPhrase.StartTime);
            }

            _timeline.RefreshAllVisuals();
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

        private void SyncActivePhraseText()
        {
            if (_phrases.Count == 0)
            {
                _phraseDisplay.Sync(null);
                _activePhrase = null;
                return;
            }

            float now = (float)_context.Audio.GetCurrentTime();
            Phrase? active = _phrases.LastOrDefault(p => p.ContainsTime(now));

            if (active == _activePhrase) return;

            _activePhrase = active;
            _phraseDisplay.Sync(active);
        }

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

        private void BeginAddPhrase()
        {
            if (_isInputDialogOpen) return;

            _context.Audio.Pause();
            _isPlaying = false;

            float time = (float)_context.Audio.GetCurrentTime();

            var dialog = new TextInputModule(
                _scene,
                "Добавить новую фразу",
                (mainText, transText) =>
                {
                    if (!string.IsNullOrWhiteSpace(mainText))
                    {
                        string cleaned = CleanTranscription(transText);
                        var phrase = new Phrase(time, time + 4f, mainText, cleaned);
                        _phrases.Add(phrase);
                        _timeline?.SetPhrases(_phrases);
                    }
                },
                () => _isInputDialogOpen = false);

            _isInputDialogOpen = true;
            _context.Game.CloseModalWindow();
            _context.Game.OpenModalWindow(dialog);
        }

        private string CleanTranscription(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            string lower = input.ToLowerInvariant();
            return Regex.Replace(lower, @"[^\p{L}\p{N}\s\-']", "");
        }

        private void CleanAllTranscriptions()
        {
            foreach (var phrase in _phrases)
            {
                if (!string.IsNullOrEmpty(phrase.Transcription))
                    phrase.Transcription = CleanTranscription(phrase.Transcription);
            }
        }

        private void AddSliderForChar(Phrase phrase, int charIndex)
        {
            if (phrase.Sliders.Any(s => s.charIndex == charIndex)) return;

            float now = (float)_context.Audio.GetCurrentTime();
            float endT = Math.Min(now + 2.0f, phrase.EndTime);
            if (endT - now < 0.2f) endT = now + 0.2f;

            phrase.Sliders.Add(new TappiruCS.State.Edit.Core.SliderTiming
            {
                charIndex = charIndex,
                startTime = now,
                endTime = endT
            });

            _timeline?.SetPhrases(_phrases);
            _phraseDisplay.Sync(phrase);
        }

        private void SaveProject()
        {
            CleanAllTranscriptions();

            var map = new JsonMap();
            map.events = PhraseSerializer.ToEvents(_phrases);
            _colorPanel.SaveTo(map);

            try { _projectIO.Save(map); }
            catch (Exception ex) { Console.WriteLine($"[EditState] Ошибка сохранения: {ex.Message}"); }
        }
    }
}