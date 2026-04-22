using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;
using TappiruCS.Core;
using TappiruCS.Core.GameObject;
using TappiruCS.GameLogic;
using TappiruCS.Render;
using TappiruCS.Render.Text;
using TappiruCS.Server.MapLogic;
using TappiruCS.Server.Player;
using TappiruCS.State.Menu;
using TappiruCS.State.Session;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;
using static TappiruCS.Render.Text.Font;

namespace TappiruCS.State.SongSelector
{
    public class SongSelectState : IGameState
    {
        // ── Зависимости ──
        private readonly RenderContext _context;

        // ── Глобальное состояние ──
        public static MapData SelectedMap;

        // ── Сцена ──
        private readonly Scene _scene = new Scene();

        // ── UI-элементы, обновляемые во время игры ──
        private Background _background;
        private ScrollList _mapList;
        private TextObject _mapTitleText;
        private TextObject _creatorText;
        private TextObject _metaDataText;

        // ── Лидерборд ──
        private readonly List<ScoreButton> _rankingButtons = new List<ScoreButton>();

        // ── Управление фоновой загрузкой ──
        private CancellationTokenSource _songSelectCts = new CancellationTokenSource();

        private string _currentSongPath = "";
        private int _bgPreviewTexture;

        public SongSelectState(RenderContext context)
        {
            _context = context;
        }

        // ─────────────────────────────────────────────
        //  IGameState
        // ─────────────────────────────────────────────

        public void OnEnter()
        {
            _scene.Initialize(_context);
            BuildStaticUI();

            _mapList = BuildMapList();
            _scene.Add(_mapList);

            _ = SelectSongAsync(SelectedMap.Path);
            _ = LoadMapListAsync();
        }

        public void OnExit()
        {
            _songSelectCts.Cancel();
            _scene.Clear();
        }

        public void Update(double currentTime)
        {
            _scene.Update(currentTime, _context.Game.MouseState, _context.Game);
        }

        public void Render(Matrix4 projection)
        {
            _scene.Draw(projection);
        }

        public void OnMouseWheel(MouseWheelEventArgs e)
        {
            _mapList.Scroll(e.Offset.Y);
        }

        public void HandleKeyDown(KeyboardKeyEventArgs e) { }

        // ─────────────────────────────────────────────
        //  Построение статического UI
        // ─────────────────────────────────────────────

        private void BuildStaticUI()
        {
            AddBackgrounds();
            AddUserPanel();
            AddMapInfoTexts();
            AddDecorationSprites();
            AddButtons();
        }

        private void AddBackgrounds()
        {
            _background = new Background(_bgPreviewTexture) { Layer = 0, AllowHover = false, ParalaxEffect = true };
            var dimOverlay = new Background(0) { AllowHover = false, Opacity = 0.5f };

            _scene.Add(_background);
            _scene.Add(dimOverlay);
        }

        private void AddUserPanel()
        {
            var userName = new TextObject(PlayerProfile.Instance.UserName, 1050, 965, 0.26f) { Layer = 3 };
            var userAvatar = new SpriteObject(PlayerProfile.Instance.AvatarTextureId, 940, 1025, 85, 85) { Layer = 1 };

            _scene.Add(userName);
            _scene.Add(userAvatar);
        }

        private void AddMapInfoTexts()
        {
            _mapTitleText = new TextObject("", 10, 5, 64f) { Layer = 3, Align = TextAlign.Left };
            _creatorText = new TextObject("", 10, 60, 48f) { Layer = 3, Align = TextAlign.Left };
            _metaDataText = new TextObject("", 10, 110, 36f) { Layer = 3, Align = TextAlign.Left };

            _scene.Add(_mapTitleText);
            _scene.Add(_creatorText);
            _scene.Add(_metaDataText);
        }

        private void AddDecorationSprites()
        {
            var topBar = new SpriteObject(TextureManager.GetTexture("SongSelectorTop"), 960, 110, 1920, 220)
            {
                Color = Color4.White,
                AutoScale = true,
                Layer = 2,
                AllowHover = false,
            };

            var selectionMode = new SpriteObject(TextureManager.GetTexture("SelectionMode"), 1120, 930, 2140, 400)
            {
                ScaleMultiply = 0.75f,
                Color = Color4.White,
                AutoScale = true,
                Layer = 2,
                AllowHover = false,
            };

            _scene.Add(topBar);
            _scene.Add(selectionMode);
        }

        private void AddButtons()
        {
            var playButton = new Button(1766, 938, 500, 500, "playButton", "Play")
            {
                Layer = 2,
                TextColor = new Color4(0f, 0f, 0f, 0f),
                HoverColor = new Color4(1.2f, 1.2f, 1.2f, 1f),
                ScaleMultiply = 0.8f,
                Tag = "play",
            };

            var backButton = new Button(160, 1011.8f, 449, 192, "back", "")
            {
                Layer = 2,
                TextColor = new Color4(0f, 0f, 0f, 0f),
                HoverColor = new Color4(1.2f, 1.2f, 1.2f, 1f),
                ScaleMultiply = 0.72f,
                Tag = "back",
            };

            var modsButton = new Button(480, 1015, 77, 90, "selection-mode", "")
            {
                Layer = 5,
                Tag = "selection-mode",
                ScaleMultiply = 1.4f,
            };

            playButton.OnClick += () => PlaySong(_currentSongPath);
            backButton.OnClick += BackMenu;
            modsButton.OnClick += OpenModsPanel;

            _scene.Add(playButton);
            _scene.Add(backButton);
            _scene.Add(modsButton);
        }

        private static ScrollList BuildMapList() =>
            new ScrollList(1600, 400, 1400, 400)
            {
                Layer = 1,
                Opacity = 0.8f,
            };

        // ─────────────────────────────────────────────
        //  Загрузка списка карт
        // ─────────────────────────────────────────────

        private async Task LoadMapListAsync()
        {
            var serverHashesTask = FetchServerHashesIfOnlineAsync();
            var folders = await Task.Run(() => Directory.GetDirectories("Songs/"));
            var serverHashes = await serverHashesTask;

            var mapItems = await Task.Run(() => ScanAndSortMaps(folders, serverHashes));

            await PopulateMapListAsync(mapItems);

            Console.WriteLine($"[SongSelect] Список загружен: {mapItems.Count} карт");
        }

        /// <summary>
        /// Онлайн: запрашиваем хэши карт с сервера.
        /// Офлайн: возвращаем пустой набор — карты будут без серверной отметки.
        /// </summary>
        private Task<HashSet<string>> FetchServerHashesIfOnlineAsync()
        {
            if (PlayerProfile.Instance.IsLoggedIn)
                return LoadMapHashes.GetServerMapHashesAsync();

            return Task.FromResult(new HashSet<string>());
        }

        private List<(MapData map, string folder, string displayName, float stars, string bgImagePath)>
            ScanAndSortMaps(string[] folders, HashSet<string> serverHashes)
        {
            var items = new List<(MapData, string, string, float, string)>();

            foreach (string folder in folders)
            {
                try
                {
                    var map = LoadMap.MapLoad(folder);

                    if (PlayerProfile.Instance.IsLoggedIn)
                        map.IsOnServer = serverHashes.Contains(map.MapHash);

                    string displayName = $"{map.title} - [{map.artist}]";
                    string bgImagePath = Directory.GetFiles(folder, "*.jpg").FirstOrDefault()
                                       ?? Directory.GetFiles(folder, "*.png").FirstOrDefault();

                    items.Add((map, folder, displayName, map.StarRating, bgImagePath));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SongSelect] Ошибка загрузки карты из {folder}: {ex.Message}");
                }
            }

            return items
                .OrderBy(x => x.Item4)        // по звёздам
                .ThenBy(x => x.Item1.title)    // затем по названию
                .ToList();
        }

        private async Task PopulateMapListAsync(
            List<(MapData map, string folder, string displayName, float stars, string bgImagePath)> mapItems)
        {
            for (int i = 0; i < mapItems.Count; i++)
            {
                var item = mapItems[i];

                var imageResult = await Task.Run(() => TryLoadImage(item.bgImagePath));

                int capturedIndex = i;
                string capturedFolder = item.folder;

                _context.Game.InvokeOnMainThread(() =>
                {
                    var button = BuildMapButton(item.map, item.displayName, capturedIndex, imageResult);
                    button.OnClick += () => _ = SelectSongAsync(capturedFolder);
                    _mapList.AddButton(button);
                });

                await Task.Delay(1); // даём кадру отрисоваться
            }
        }

        private static ImageResult TryLoadImage(string path)
        {
            if (string.IsNullOrEmpty(path)) return null;
            try
            {
                using var stream = File.OpenRead(path);
                return ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
            }
            catch { return null; }
        }

        private ListElementButton BuildMapButton(MapData map, string displayName, int index, ImageResult image)
        {
            var button = new ListElementButton(0, 0, 1400, 212, "SongButton", displayName, map)
            {
                TextAlign = TextAlign.Right,
                IsImaged = true,
                FontSize = 48,
                TextOffset = new Vector2(-430f, -70f),
                ImageScale = new Vector2(0.16f, 0.75f),
                ImageOffset = new Vector2(-570f, 0f),
                Layer = _mapList.Layer,
                Tag = "List",
            };

            button.SetIndex(index);

            if (map.IsOnServer)
            {
                button.Text += " !Сервер!";
                button.TextColor = new Color4(0.3f, 1f, 0.3f, 1f);
            }

            if (image != null)
            {
                button.ButtonImage = TextureLoader.CreateTextureFromRawDataAsync(
                    image.Data, image.Width, image.Height, generateMipmaps: false);
            }

            return button;
        }

        // ─────────────────────────────────────────────
        //  Выбор песни
        // ─────────────────────────────────────────────

        public async Task SelectSongAsync(string folderPath)
        {
            // Отменяем предыдущий незавершённый запрос
            _songSelectCts.Cancel();
            _songSelectCts = new CancellationTokenSource();
            var token = _songSelectCts.Token;

            Console.WriteLine($"[SelectSong] Начало: {folderPath}");

            MapData loadedMap = null;
            ImageResult bgImage = null;
            string audioPath = null;

            try
            {
                await Task.Run(() =>
                {
                    token.ThrowIfCancellationRequested();

                    loadedMap = LoadMap.MapLoad(folderPath);
                    audioPath = loadedMap.audioPath;

                    token.ThrowIfCancellationRequested();

                    bgImage = TryLoadImage(loadedMap.backGroundPath);
                    _context.Audio.LoadMusic(audioPath);

                }, token);
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"[SelectSong] Отменено: {folderPath}");
                return;
            }

            if (token.IsCancellationRequested) return;

            _context.Game.InvokeOnMainThread(() =>
            {
                if (token.IsCancellationRequested) return;

                ApplySelectedSong(loadedMap, bgImage, folderPath);
            });
        }

        private void ApplySelectedSong(MapData map, ImageResult bgImage, string folderPath)
        {
            if (map == null)
            {
                Console.WriteLine("[SelectSong] Не удалось загрузить карту");
                return;
            }

            SelectedMap = map;
            _currentSongPath = folderPath;

            _context.Audio.Play();

            // Обновляем фон
            if (bgImage != null)
            {
                _bgPreviewTexture = TextureLoader.CreateTextureFromRawDataAsync(
                    bgImage.Data, bgImage.Width, bgImage.Height, generateMipmaps: false);
                _background.TransitionTo(_bgPreviewTexture, 0.4f);
            }

            // Обновляем тексты
            _mapTitleText.Text = $"{map.title} - [{map.artist}]";
            _creatorText.Text = $"Автор: {map.creator}";
            _metaDataText.Text = FormatMetaData(map);

            // Обновляем лидерборд
            var scores = LoadScoresForCurrentMap();
            UpdateRankingDisplay(scores);

            Console.WriteLine($"[SelectSong] Готово: {map.title}");
        }

        private string FormatMetaData(MapData map)
        {
            int total = (int)_context.Audio.Duration;
            int minutes = total / 60;
            int seconds = total % 60;
            return $"Длина: {minutes}:{seconds:D2}  Строк: {map.Events.Count}  Сложность: {map.StarRating:F2}";
        }

        // ─────────────────────────────────────────────
        //  Загрузка очков: онлайн / офлайн
        // ─────────────────────────────────────────────

        private List<PlayerScore> LoadScoresForCurrentMap()
        {
            return PlayerProfile.Instance.IsLoggedIn
                ? LoadOnlineScores(SelectedMap.MapHash)
                : LoadOfflineScores(SelectedMap.MapHash);
        }

        /// <summary>
        /// Офлайн: берём очки из локальной базы.
        /// </summary>
        private List<PlayerScore> LoadOfflineScores(string mapHash)
        {
            return ScoreManager.GetTopScoresForMap(mapHash, 7);
        }

        private List<PlayerScore> LoadOnlineScores(string mapHash)
        {
            // TODO: запросить очки с сервера и вернуть список
            return new List<PlayerScore>();
        }

        // ─────────────────────────────────────────────
        //  Отображение лидерборда
        // ─────────────────────────────────────────────

        private void UpdateRankingDisplay(List<PlayerScore> scores)
        {
            ClearRankingButtons();

            const int startY = 275;
            const int spacing = 102;
            const int maxShown = 10;

            if (scores == null || scores.Count == 0)
            {
                AddEmptyRankingPlaceholder(startY);
                return;
            }

            int count = Math.Min(scores.Count, maxShown);
            for (int i = 0; i < count; i++)
            {
                var score = scores[i];
                var button = new ScoreButton(180, startY + i * spacing, score)
                {
                    Layer = 3,
                    Position = new Vector2(180, startY + i * spacing),
                    ScaleMultiply = 1.0f,
                    Opacity = 0.5f,
                };

                button.SetRank(i + 1);
                button.OnClick += () => OpenScoreBoard(score);

                _rankingButtons.Add(button);
                _scene.Add(button);
            }
        }

        private void ClearRankingButtons()
        {
            foreach (var btn in _rankingButtons)
                _scene.Remove(btn);

            _rankingButtons.Clear();
        }

        private void AddEmptyRankingPlaceholder(float y)
        {
            var emptyScore = new PlayerScore
            {
                PlayerName = "Нет результатов",
                _score = 0,
                _accuraci = 0,
                _maxCobmo = 0,
            };

            var btn = new ScoreButton(180, y, emptyScore)
            {
                Layer = 3,
                Position = new Vector2(180, y),
            };

            btn.Avatar.Active = false;
            btn.Grade.Active = false;

            _rankingButtons.Add(btn);
            _scene.Add(btn);
        }

        // ─────────────────────────────────────────────
        //  Действия
        // ─────────────────────────────────────────────

        private void PlaySong(string songPath)
        {
            SelectedMap.mods = Game._activeMods;
            _context.Audio.PlaySoundEffect("matchStart");
            _context.Game.ChangeState(new GameSessionState(_context, SelectedMap));
        }

        private void BackMenu()
        {
            _context.Game.ChangeState(new MenuState(_context));
        }

        private void OpenScoreBoard(PlayerScore score)
        {
            _context.Game.ChangeState(new ScoreBoardState(_context, score, SelectedMap));
        }

        private void OpenModsPanel()
        {
            _context.Game.OpenModalWindow(new ModsModule(_scene, Game._activeMods));
        }
    }
}