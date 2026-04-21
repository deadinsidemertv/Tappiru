using Cairo;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using StbImageSharp;
using System.Net.Http.Json;
using System.Text.Json;
using TappiruCS.Core;
using TappiruCS.Core.GameObject;
using TappiruCS.GameLogic;
using TappiruCS.Render;
using TappiruCS.Server.Player;
using TappiruCS.State.Session;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;
using TappiruCS.Server.MapLogic;
using TappiruCS.State.Menu;
using TappiruCS.GameLogic.Mod;


namespace TappiruCS.State.SongSelector
{
    public class SongSelectState : IGameState
    {

        private readonly RenderContext _context;

        public static MapData SelectedMap;
        

        private readonly Scene _scene = new Scene();

        public Background bg;

        public ScrollList list;

        public TextObject MapTitle;
        public TextObject Creator;
        public TextObject MetaData;

        public SpriteObject SongSelectorTop;
        public SpriteObject SelectionMode;

        public ModuleWindow _moduleWND;

        public int _bgPreview;

        public string songPath = "";
        public int songCount;

        List<PlayerScore> topScores;


        public bool IsOpenModule=false;

        public SongSelectState(RenderContext context)
        {
            _context = context;
            
        }

        public void OnEnter()
        {
            
            _scene.Initialize(_context);
            var _serverMapHashes =  LoadMapHashes.GetServerMapHashesAsync().Result;
            // === 1. Собираем все карты в список ===
            var mapList = new List<(MapData mapData, string folderPath, string displayName, float starRating)>();

            string[] folders = Directory.GetDirectories("Songs/");

            foreach (string folderPath in folders)
            {
                try
                {
                    // Полностью загружаем карту через LoadMap (там уже считается StarRating)
                    MapData mapData = LoadMap.MapLoad(folderPath);

                    mapData.IsOnServer = _serverMapHashes.Contains(mapData.MapHash);

                    string displayName = $"{mapData.title} - [{mapData.artist}]";

                    mapList.Add((mapData, folderPath, displayName, mapData.StarRating));
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[SongSelect] Ошибка загрузки карты из {folderPath}: {ex.Message}");
                }
            }

            // === 2. Сортируем по StarRating (от сложных к лёгким) ===
            mapList = mapList
                .OrderBy(item => item.starRating)
                .ThenBy(item => item.mapData.title)
                .ToList();

            // === 3. Создаём кнопки ===
            list = new ScrollList(1600, 400, 1400, 400)
            {
                Layer = 1,
                Opacity = 0.8f,
            };

            for (int i = 0; i < mapList.Count; i++)
            {
                var item = mapList[i];
                var mapData = item.mapData;           // Теперь это MapData!
                var folderPath = item.folderPath;
                var displayName = item.displayName;

                // Ищем обложку
                string bgImagePath = Directory.GetFiles(folderPath, "*.jpg").FirstOrDefault()
                                  ?? Directory.GetFiles(folderPath, "*.png").FirstOrDefault();

                // ←←← ИСПРАВЛЕНИЕ ЗДЕСЬ ←←←
                var button = new ListElementButton(0, 0, 1400, 212, "SongButton", displayName, mapData)
                {
                    TextScale = 0.3f,
                    TextAlign = TextRender.TextAlign.Right,
                    IsImaged = true,
                    TextOffset = new Vector2(-430f, -70f),
                    ImageScale = new Vector2(0.16f, 0.75f),
                    ImageOffset = new Vector2(-570f, 0f),
                    Layer = list.Layer,
                    Tag = "List"
                };

                button.SetIndex(i);

                if (mapData.IsOnServer)
                {
                    button.Text += "!Сервер!";   // или любой другой текст
                    button.TextColor = new Color4(0.3f, 1f, 0.3f, 1f);                            // Можно сделать другой цвет текста, например:
                                                   // button.TextColor = new Color4(0.2f, 1f, 0.2f, 1f);
                }

                if (!string.IsNullOrEmpty(bgImagePath))
                    button.ButtonImage = TextureLoader.Load(bgImagePath);

                string capturedPath = folderPath;
                button.OnClick += () => _ = SelectSong(capturedPath);

                list.AddButton(button);
            }


            bg = new Background( _bgPreview ) { Layer = 0, AllowHover = false, ParalaxEffect = true };
            var bgblack = new Background( 0 ) { AllowHover = false, Opacity = 0.5f };

            _scene.Add(bg);
            _scene.Add(bgblack);

            var UserName = new TextObject( PlayerProfile.Instance.UserName, 1050, 965, 0.26f) { Layer = 3 };
            var UserAvatar = new SpriteObject( PlayerProfile.Instance.AvatarTextureId, 940, 1025, 85, 85) { Layer = 1 };

            _scene.Add(UserName);
            _scene.Add(UserAvatar);
            int _songSelectorTop = TextureManager.GetTexture("SongSelectorTop");
            SongSelectorTop = new SpriteObject( _songSelectorTop, 960, 110, 1920, 220) { Color = new Color4(1f, 1f, 1f, 1f), AutoScale = true, Layer = 2, AllowHover = false };



            MapTitle = new TextObject( "", 10, 5, 0.4f) { Layer = 3, Align = TextRender.TextAlign.Left };
            Creator = new TextObject( "", 10, 50, 0.25f) { Layer = 3, Align = TextRender.TextAlign.Left };
            MetaData = new TextObject( "", 10, 90, 0.30f) { Layer = 3, Align = TextRender.TextAlign.Left };



            int _selectionmode = TextureManager.GetTexture("SelectionMode");
            SelectionMode = new SpriteObject( _selectionmode, 1120, 930, 2140, 400) { ScaleMultiply = 0.75f, Color = new Color4(1f, 1f, 1f, 1f), AutoScale = true, Layer = 2, AllowHover = false };


            var playButton = new Button(1766, 938, 500, 500, "playButton", "Play")  
            {
                Layer = 2,
                TextColor = new Color4(0f, 0f, 0f, 0f),
                HoverColor = new Color4(1.2f, 1.2f, 1.2f, 1f),
                TextScale = 0.8f,
                ScaleMultiply = 0.8f,
                Tag = "play"


            };
            var backButton = new Button(160, 1011.8f, 449, 192, "back", "")   // "btn" — имя текстуры через TextureManager
            {
                Layer = 2,
                TextColor = new Color4(0f, 0f, 0f, 0f),
                HoverColor = new Color4(1.2f, 1.2f, 1.2f, 1f),
                TextScale = 0.8f,
                ScaleMultiply = 0.72f,
                Tag = "play"


            };
            var modsButton = new Button(480, 1015, 77, 90, "selection-mode", "") { Layer = 5,Tag = "selection-mode",ScaleMultiply = 1.4f };
            _scene.Add(modsButton);

            backButton.OnClick += BackMenu;
            playButton.OnClick += () => PlaySong(songPath);
            modsButton.OnClick += CreateModsModule;


            _ = SelectSong(SelectedMap.Path);

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
            SelectedMap.mods = Game._activeMods;
            Console.WriteLine("игра началась");
            _context.Audio.PlaySoundEffect("matchStart");
            _context.Game.ChangeState(new GameSessionState(_context, SelectedMap));
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
                var button = new ScoreButton(180, startY + i * spacing, scores[i])
                {
                    Layer = 3,
                    Position = new Vector2(180, startY + i * spacing),
                    ScaleMultiply = 1.0f,
                    Opacity = 0.5f
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

            var btn = new ScoreButton(180, y, emptyScore)
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
            _context.Game.ChangeState(new ScoreBoardState(_context,score,SelectedMap));
        }

        public void CreateModsModule()
        {
            if (IsOpenModule == false)
            {
                _moduleWND = new ModsModule(_scene, Game._activeMods);
                IsOpenModule = true;
            }
            else if (IsOpenModule) 
            {
                CloseModuleWindow(_moduleWND);
            }
                
        }

        public void CloseModuleWindow(ModuleWindow wind)
        {
            wind.Dispose();
            wind = null;
            IsOpenModule = false;
        }

        public async Task SelectSong(string SP)
        {
            Console.WriteLine($"[SelectSong] Начало для {SP}");

            MapData tempMap = null;
            string bgPath = null;

            string audioPath = null;

            //_context.Audio.Stop();
            Console.WriteLine("[SelectSong] Запускаем Task.Run (MapLoad)...");

            ImageResult? image =null;

            await Task.Run(() =>
            {
                Console.WriteLine("[SelectSong] Внутри Task.Run — начинаем MapLoad");
                var watch = System.Diagnostics.Stopwatch.StartNew();

                tempMap = LoadMap.MapLoad(SP);
                audioPath = tempMap.audioPath;

                if (tempMap != null)
                    bgPath = tempMap.backGroundPath;

                
                using (var stream = File.OpenRead(bgPath))
                {
                    image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
                }
                watch.Stop();
                _context.Audio.LoadMusic(audioPath);
                //_context.Audio.Stop();
                

                Console.WriteLine($"[SelectSong] MapLoad завершён за {watch.ElapsedMilliseconds} мс");
            });

            Console.WriteLine("[SelectSong] Вернулись в главный поток, вызываем InvokeOnMainThread");

            _context.Game.InvokeOnMainThread(() =>
            {
                Console.WriteLine("[SelectSong] Выполняется в главном потоке");
                _context.Audio.Play();
                if (tempMap == null)
                {
                    Console.WriteLine("Не удалось загрузить карту");
                    return;
                }

                SelectedMap = tempMap;

                //PlayerScore? best = ScoreManager.GetBestScoreForMap(SelectedMap.MapHash);

                Console.WriteLine("[SelectSong] Начинаем загрузку текстуры фона...");
                var watch = System.Diagnostics.Stopwatch.StartNew();


                _bgPreview = TextureLoader.CreateTextureFromRawDataAsync(image.Data, image.Width, image.Height, generateMipmaps: false);
                bg.TransitionTo(_bgPreview, 0.4f);

                watch.Stop();
                Console.WriteLine($"[SelectSong] Текстура загружена за {watch.ElapsedMilliseconds} мс");

                Console.WriteLine("[SelectSong] Загружаем музыку...");
                

                Console.WriteLine("[SelectSong] Обновляем текст...");

                MapTitle.Text = SelectedMap.title + $" - [{SelectedMap.artist}]";
                Creator.Text = "Автор: " + SelectedMap.creator;

                int totalSeconds = (int)_context.Audio.Duration;
                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;
                MetaData.Text = $"Длина: {minutes}:{seconds:D2}  Строк: {SelectedMap.Events.Count} Сложность:{SelectedMap.StarRating:F2}";

                Console.WriteLine($"[SelectSong] Успешно завершено для {SelectedMap.title}");

                //topScores = ScoreManager.GetTopScoresForMap(SelectedMap.MapHash, 10);
                //UpdateRankingDisplay(ScoreManager.GetTopScoresForMap(SelectedMap.MapHash, 10));
                
            });
        }

        public void OnExit()
        {
            _scene.Clear();
        }
        public void Update(double currentTime)
        {

            var mouse = _context.Game.MouseState;
            _scene.Update(currentTime, mouse, _context.Game);
        }
        public void BackMenu()
        {
            _context.Game.ChangeState(new MenuState(_context));
        }
        public void Render(Matrix4 projection)
        {
            
            _scene.Draw(projection);
        }
        public void OnMouseWheel(MouseWheelEventArgs e)
        {
            if (IsOpenModule)
                return;

            list.Scroll(e.Offset.Y);
            
        }

        public void HandleKeyDown(KeyboardKeyEventArgs e)
        {

            if (e.Key == Keys.Escape && IsOpenModule)
                CloseModuleWindow(_moduleWND);
            else if (e.Key == Keys.Escape && IsOpenModule == false)
                _context.Game.ChangeState(new MenuState(_context));

        }

    }
}
