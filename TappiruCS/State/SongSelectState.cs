using Cairo;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Text.Json;
using TappiruCS.Core;
using TappiruCS.GameLogic;
using TappiruCS.Render;
using TappiruCS.Server.Player;
using TappiruCS.UI;

namespace TappiruCS.State
{
    public class SongSelectState : IGameState
    {
        private readonly Game _game;
        private readonly SpriteBatch _spriteBatch;
        private readonly TextRender _textRenderer;
        private readonly TextRender _manrope;
        private readonly AudioManager _audio;

        public MapData SelectedMap;

        private readonly Scene _scene = new Scene();

        public Background bg;

        public ScrollList list;

        public TextObject MapTitle;
        public TextObject Creator;
        public TextObject MetaData;

        public SpriteObject SongSelectorTop;
        public SpriteObject SelectionMode;

        public int _bgPreview;

        public string songPath = "";
        public int songCount;

        List<PlayerScore> topScores;

        public SongSelectState(Game game, SpriteBatch spriteBatch, TextRender textRenderer, AudioManager audio)
        {
            _game = game;
            _spriteBatch = spriteBatch;
            _textRenderer = textRenderer;
            //_textRenderer = new TextRender(spriteBatch, "Textures\\Font\\manrope.fnt");
            _audio = audio;
        }
        
        public void OnEnter()
        {
            Console.WriteLine("Открыт выбор песни (SongSelectState)");


            songCount = Directory.GetDirectories("Songs/").Length;
            string[] folders = Directory.GetDirectories("Songs/");

            Random rnd = new Random();
            int selectID = rnd.Next(0, folders.Length);


            list = new ScrollList(_spriteBatch, _textRenderer, 1500, 190, 1400, 700)
            {
                Layer = 1,
            };
            for (int i = 0; i < songCount; i++)
            {
                string folderPath = folders[i];
                string folderName = System.IO.Path.GetFileName(folderPath);

                string bgImagePath = Directory.GetFiles(folderPath, "*.jpg").FirstOrDefault()
                             ?? Directory.GetFiles(folderPath, "*.png").FirstOrDefault();
                Console.WriteLine(folderPath);
                string tappFile = Directory.GetFiles(folderPath, "*.tapp").FirstOrDefault();
                string displayName = System.IO.Path.GetFileName(folderPath); // fallback
                string json = File.ReadAllText(tappFile);
                var mapData = JsonSerializer.Deserialize<JsonMap>(json);
                // Например: "Название (автор)" или "Название - автор"
                displayName = $"{mapData?.title ?? "?"} - [{mapData?.artist ?? "?"}] StarRait: {mapData.StarRating}";

                var button = new Button(_spriteBatch, _textRenderer,
                    0, 0, 1400, 212, "SongButton", displayName, Color4.White)
                {
                    ScaleMultiply = 0.3f,
                    TextScale = 0.4f,
                    TextAlign = TextRender.TextAlign.Left,
                    IsImaged = true,
                    TextOffset = new Vector2(-45f, -70f),
                    ImageScale = new Vector2(0.16f, 0.75f),
                    ImageOffset = new Vector2(-570f, 0f),
                    Layer = list.Layer,
                    Tag = "List"
                };

                if (bgImagePath != null)
                    button.ButtonImage = TextureLoader.Load(bgImagePath);

                string capturedPath = folderPath;
                button.OnClick += () => _ = SelectSong(capturedPath);

                list.AddButton(button);        // ← вот так добавляем
                _scene.Add(button);            // можно оставить, если хочешь (но лучше убрать)
            }


            bg = new Background(_spriteBatch, _bgPreview, _game) { Layer = 0, AllowHover = false, ParalaxEffect = true };
            var bgblack = new Background(_spriteBatch, 0, _game) { AllowHover = false, Opacity = 0.5f };

            _scene.Add(bg);
            _scene.Add(bgblack);

            var UserName = new TextObject(_textRenderer, PlayerProfile.Instance.UserName, 1050, 965, 0.26f) { Layer = 3 };
            var UserAvatar = new SpriteObject(_spriteBatch, PlayerProfile.Instance.AvatarTextureId, 940, 1025, 85, 85) { Layer = 1 };

            _scene.Add(UserName);
            _scene.Add(UserAvatar);
            int _songSelectorTop = TextureManager.GetTexture("SongSelectorTop");
            SongSelectorTop = new SpriteObject(_spriteBatch, _songSelectorTop, 960, 110, 1920, 220) { Color = new Color4(1f, 1f, 1f, 1f), AutoScale = true, Layer = 2, AllowHover = false };



            MapTitle = new TextObject(_textRenderer, "", 10, 5, 0.4f) { Layer = 3, Align = TextRender.TextAlign.Left };
            Creator = new TextObject(_textRenderer, "", 10, 50, 0.25f) { Layer = 3, Align = TextRender.TextAlign.Left };
            MetaData = new TextObject(_textRenderer, "", 10, 90, 0.30f) { Layer = 3, Align = TextRender.TextAlign.Left };



            int _selectionmode = TextureManager.GetTexture("SelectionMode");
            SelectionMode = new SpriteObject(_spriteBatch, _selectionmode, 1120, 930, 2140, 400) { ScaleMultiply = 0.75f, Color = new Color4(1f, 1f, 1f, 1f), AutoScale = true, Layer = 2, AllowHover = false };


            var playButton = new Button(_spriteBatch, _textRenderer,
                1766, 938, 500, 500, "playButton", "Play", Color4.White)   // "btn" — имя текстуры через TextureManager
            {
                Layer = 2,
                TextColor = new Color4(0f, 0f, 0f, 0f),
                HoverColor = new Color4(1.2f, 1.2f, 1.2f, 1f),
                TextScale = 0.8f,
                ScaleMultiply = 0.8f,
                Tag = "play"


            };
            var backButton = new Button(_spriteBatch, _textRenderer,
                160, 1011.8f, 449, 192, "back", "", Color4.White)   // "btn" — имя текстуры через TextureManager
            {
                Layer = 2,
                TextColor = new Color4(0f, 0f, 0f, 0f),
                HoverColor = new Color4(1.2f, 1.2f, 1.2f, 1f),
                TextScale = 0.8f,
                ScaleMultiply = 0.72f,
                Tag = "play"


            };
            backButton.OnClick += BackMenu;
            playButton.OnClick += () => PlaySong(songPath);


            _ = SelectSong(folders[selectID]);

            _scene.Add(MapTitle);
            _scene.Add(Creator);
            _scene.Add(MetaData);

            _scene.Add(list);
            _scene.Add(SelectionMode);
            _scene.Add(SongSelectorTop);
            _scene.Add(playButton);
            _scene.Add(backButton);

        }


        public void PlaySong(string SongPath)
        {
            Console.WriteLine("игра началась");
            _audio.PlaySoundEffect("matchStart");
            _game.ChangeState(new GameSessionState(_game, _spriteBatch, _textRenderer, _audio, SelectedMap));
        }

        List<ScoreButton> _rankingButtons = new List<ScoreButton>();

        private void UpdateRankingDisplay(List<PlayerScore> scores)
        {
            ClearRankingButtons();

            const int startY = 275;
            const int spacing = 102;
            const int maxItems = 10;

            if (scores == null || scores.Count == 0)
            {
                AddNoResultsButton(startY);
                return;
            }

            for (int i = 0; i < Math.Min(scores.Count, maxItems); i++)
            {
                var currentScore = scores[i];
                var button = new ScoreButton(_spriteBatch, _textRenderer, 180, startY + i * spacing, scores[i])
                {
                    Layer = 3,
                    Position = new Vector2(180, startY + i * spacing),
                    ScaleMultiply = 1.0f
                };
                
                button.SetRank(i + 1);
                button.OnClick += () => CheckScore(currentScore);
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

        private void AddNoResultsButton(float y)
        {
            var emptyScore = new PlayerScore
            {
                PlayerName = "Нет результатов",
                _score = 0,
                _accuraci = 0,
                _maxCobmo = 0
            };

            var btn = new ScoreButton(_spriteBatch, _textRenderer, 180, y, emptyScore)
            {
                Layer = 3,
                Position = new Vector2(180, y)
            };

            btn.Avatar.Active = false;
            btn.Grade.Active = false;

            _rankingButtons.Add(btn);
            _scene.Add(btn);
        }

        public void CheckScore(PlayerScore score)
        {
            _game.ChangeState(new ScoreBoardState(_game, _spriteBatch, _textRenderer, _audio,score,SelectedMap));
        }
        public async Task SelectSong(string SP)
        {
            

            Console.WriteLine($"[SelectSong] Начало для {SP}");

            _audio.Stop();

            MapData tempMap = null;
            string bgPath = null;

            Console.WriteLine("[SelectSong] Запускаем Task.Run (MapLoad)...");

            await Task.Run(() =>
            {
                Console.WriteLine("[SelectSong] Внутри Task.Run — начинаем MapLoad");
                var watch = System.Diagnostics.Stopwatch.StartNew();

                tempMap = LoadMap.MapLoad(SP);

                watch.Stop();
                Console.WriteLine($"[SelectSong] MapLoad завершён за {watch.ElapsedMilliseconds} мс");

                if (tempMap != null)
                    bgPath = tempMap.backGroundPath;
            });

            Console.WriteLine("[SelectSong] Вернулись в главный поток, вызываем InvokeOnMainThread");

            _game.InvokeOnMainThread(() =>
            {
                Console.WriteLine("[SelectSong] Выполняется в главном потоке");

                if (tempMap == null)
                {
                    Console.WriteLine("Не удалось загрузить карту");
                    return;
                }

                SelectedMap = tempMap;

                PlayerScore? best = ScoreManager.GetBestScoreForMap(SelectedMap.MapHash);
                if (best != null)
                {
                    Console.WriteLine($"Лучший счёт: {best._score} (Точность: {best._accuraci:F1}%)");
                    // отобразите в UI
                }

                Console.WriteLine("[SelectSong] Начинаем загрузку текстуры фона...");
                var watch = System.Diagnostics.Stopwatch.StartNew();

                if (!string.IsNullOrEmpty(bgPath))
                {
                    try
                    {
                        _bgPreview = TextureLoader.Load(bgPath);
                        if (bg != null)
                            bg._textureId = _bgPreview;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Ошибка текстуры: {ex.Message}");
                    }
                }

                watch.Stop();
                Console.WriteLine($"[SelectSong] Текстура загружена за {watch.ElapsedMilliseconds} мс");

                Console.WriteLine("[SelectSong] Загружаем музыку...");
                _audio.LoadMusic(SelectedMap.audioPath);
                _audio.Play();

                Console.WriteLine("[SelectSong] Обновляем текст...");

                MapTitle.Text = SelectedMap.title + $" - [{SelectedMap.artist}]";
                Creator.Text = "Автор: " + SelectedMap.creator;

                int totalSeconds = (int)_audio.Duration;
                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;
                MetaData.Text = $"Длина: {minutes}:{seconds:D2}  Строк: {SelectedMap.Events.Count} Сложность:{SelectedMap.StarRating}";

                Console.WriteLine($"[SelectSong] Успешно завершено для {SelectedMap.title}");

                topScores = ScoreManager.GetTopScoresForMap(SelectedMap.MapHash, 10);
                UpdateRankingDisplay(ScoreManager.GetTopScoresForMap(SelectedMap.MapHash, 10));
            });
        }

        public void OnExit()
        {
            _scene.Clear();
            Console.WriteLine("Закрыт выбор песни");
        }
        public void Update(double currentTime)
        {

            var mouse = _game.MouseState;
            _scene.Update(currentTime, mouse, _game);
        }
        public void BackMenu()
        {
            _game.ChangeState(new MenuState(_game, _spriteBatch, _textRenderer, _audio));
        }
        public void Render(Matrix4 projection)
        {
            
            _scene.Draw(projection);
        }
        public void OnMouseWheel(MouseWheelEventArgs e)
        {

            list.Scroll(e.Offset.Y);
            
        }

        public void HandleKeyDown(KeyboardKeyEventArgs e)
        {
            if (e.Key == Keys.Backspace)
            {
                _game.ChangeState(new MenuState(_game, _spriteBatch, _textRenderer, _audio));
            }

        }

    }
}
