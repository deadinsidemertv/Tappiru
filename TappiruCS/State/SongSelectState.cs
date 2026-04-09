using Cairo;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Text.Json;
using TappiruCS.Core;
using TappiruCS.GameLogic;
using TappiruCS.Render;     
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

        public SongSelectState(Game game, SpriteBatch spriteBatch, TextRender textRenderer, AudioManager audio)
        {
            _game = game;
            _spriteBatch = spriteBatch;
            _textRenderer = textRenderer;
            //_textRenderer = new TextRender(spriteBatch, "Textures\\Font\\manrope.fnt");
            _audio = audio;
        }
        private Task? _loadingTask;
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
                displayName = $"{mapData?.title ?? "?"} - [{mapData?.artist ?? "?"}]";

                var button = new Button(_spriteBatch, _textRenderer,
                    0, 0, 1400, 212, "SongButton", displayName, Color4.White)
                {
                    ScaleMultiply = 0.3f,
                    TextScale = 0.4f,
                    TextAlign = TextRender.TextAlign.Left,
                    IsImaged = true,
                    TextOffset = new Vector2(-90f, -50f),
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


            _loadingTask = SelectSong(folders[selectID]);

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

        public async Task SelectSong(string SP)
        {
            _audio.Stop();
            MapData temp = null;
            string bgpath = null;
            await Task.Run(() =>
            {
                temp = GameSessionState.MapLoad(SP);
                bgpath = temp?.backGroundPath;

            });

            _game.InvokeOnMainThread(() =>
            {
                if (temp == null)
                {
                    Console.WriteLine("Не удалось загрузить карту");
                    return;
                }

                SelectedMap = temp;

                // 3. Загрузка текстуры строго в главном потоке
                if (!string.IsNullOrEmpty(bgpath))
                {
                    try
                    {
                        _bgPreview = TextureLoader.Load(bgpath);
                        if (bg != null)
                            bg._textureId = _bgPreview;
                        Console.WriteLine($"Фон загружен: {bgpath}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"GL ошибка при загрузке фона: {ex.Message}");
                    }
                }

                _audio.LoadMusic(SelectedMap.audioPath);
                _audio.Play();

                MapTitle.Text = SelectedMap.title + $" - [{SelectedMap.artist}]";
                Creator.Text = "Автор: " + SelectedMap.creator;

                int totalSeconds = (int)_audio.Duration;
                int minutes = totalSeconds / 60;
                int seconds = totalSeconds % 60;
                MetaData.Text = $"Длина: {minutes}:{seconds:D2}  Строк: {SelectedMap.Events.Count}";
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
