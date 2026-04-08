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

        public ListButtons list;

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

        public void OnEnter()
        {
            Console.WriteLine("Открыт выбор песни (SongSelectState)");
            

            songCount = Directory.GetDirectories("Songs/").Length;
            string[] folders = Directory.GetDirectories("Songs/");

            Random rnd = new Random();
            int selectID = rnd.Next(0,folders.Length);


            list = new ListButtons(_spriteBatch, _textRenderer, songCount, 960, 170, 1400, 212, "SongButton", "lol")
            {
                Layer = 1,
            };
            for (int i = 0; i < songCount; i++)
            {
                string folderPath = folders[i];
                string folderName = Path.GetFileName(folderPath);

                string bgImagePath = Directory.GetFiles(folderPath, "*.jpg").FirstOrDefault()
                             ?? Directory.GetFiles(folderPath, "*.png").FirstOrDefault();
                Console.WriteLine(folderPath);
                string tappFile = Directory.GetFiles(folderPath, "*.tapp").FirstOrDefault();
                string displayName = Path.GetFileName(folderPath); // fallback
                string json = File.ReadAllText(tappFile);
                var mapData = JsonSerializer.Deserialize<JsonMap>(json);
                // Например: "Название (автор)" или "Название - автор"
                displayName = $"{mapData?.title ?? "?"} - [{mapData?.artist ?? "?"}]";

                if (bgImagePath != null)
                {
                    list.Buttons[i].ButtonImage = TextureLoader.Load(bgImagePath); // загружаем текстуру
                }
                else
                {
                    
                    list.Buttons[i].ButtonImage = 0;
                }
                list.Buttons[i].Parent = list;
                list.Buttons[i].Text = displayName;
                list.Buttons[i].TextScale = 0.4f;
                list.Buttons[i].TextAlign = TextRender.TextAlign.Left;
                list.Buttons[i].IsImaged = true;
                list.Buttons[i].TextOffset = new Vector2(-90f, -50f);
                list.Buttons[i].ImageScale = new Vector2(0.16f, 0.75f);
                list.Buttons[i].ImageOffset = new Vector2(-570f, 0f);
                list.Buttons[i].ScaleMultiply = 0.3f;
                list.Buttons[i].Layer = list.Layer;
                list.Buttons[i].Tag = "List";
                



                string capturedPath = folderPath;
                list.Buttons[i].OnClick += () => SelectSong(folderPath);
                _scene.Add(list.Buttons[i]);
            }


            bg = new Background(_spriteBatch, _bgPreview, _game) { Layer = 0, AllowHover = false ,ParalaxEffect = true};
            var bgblack = new Background(_spriteBatch, 0, _game) { AllowHover = false,Opacity = 0.5f };
            _scene.Add(bg);
            _scene.Add(bgblack);

            int _songSelectorTop = TextureManager.GetTexture("SongSelectorTop");
            SongSelectorTop = new SpriteObject(_spriteBatch, _songSelectorTop, 960, 110, 1920, 220) { Color = new Color4(1f, 1f, 1f, 1f), AutoScale = true,Layer = 2, AllowHover = false };



            MapTitle = new TextObject(_textRenderer, "", 10, 5, 0.4f) { Layer = 3, Align = TextRender.TextAlign.Left };
            Creator = new TextObject(_textRenderer, "" , 10, 50, 0.25f) { Layer = 3, Align = TextRender.TextAlign.Left };
            MetaData = new TextObject(_textRenderer, "", 10, 90, 0.30f) { Layer = 3,Align = TextRender.TextAlign.Left };



            int _selectionmode = TextureManager.GetTexture("SelectionMode");
            SelectionMode = new SpriteObject(_spriteBatch, _selectionmode, 960, 280,2283, 1888) {ScaleMultiply =0.85f, Color = new Color4(1f, 1f, 1f, 1f), AutoScale = true, Layer = 2, AllowHover = false };


            var playButton = new Button(_spriteBatch, _textRenderer,
                1750, 900, 500, 500, "playButton", "Play", Color4.White)   // "btn" — имя текстуры через TextureManager
            {
                Layer = 2,
                TextColor = new Color4(0f,0f,0f,0f),
                HoverColor = new Color4(1f,0.95f,0f,1f),
                TextScale = 0.8f,
                ScaleMultiply = 0.8f,
                Tag = "play"


            };
            playButton.OnClick += () => PlaySong(songPath);

            
            SelectSong(folders[selectID]);

            _scene.Add(MapTitle);
            _scene.Add(Creator);
            _scene.Add(MetaData);

            _scene.Add(list);
            _scene.Add(SelectionMode);
            _scene.Add(SongSelectorTop);
            _scene.Add(playButton);

        }


        public void PlaySong(string SongPath)
        {
            Console.WriteLine("игра началась");
            _audio.PlaySoundEffect("matchStart");
            _game.ChangeState(new GameSessionState(_game, _spriteBatch, _textRenderer, _audio, SelectedMap));
        }

        public void SelectSong(string SP)
        {
            _audio.Stop();
           
            SelectedMap = GameSessionState.MapLoad(SP);
            _bgPreview = TextureLoader.Load(SelectedMap.backGroundPath);
            bg._textureId = _bgPreview;

            _audio.LoadMusic(SelectedMap.audioPath);
            _audio.Play();

            MapTitle.Text = SelectedMap.title+$" - [{SelectedMap.artist}]";
            Creator.Text = "Автор:" + SelectedMap.creator;

            int totalSeconds = (int)_audio.Duration;
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;
            string formattedTime = $"{minutes}:{seconds:D2}"; // D2 — всегда две цифры для секунд
            MetaData.Text = "Длина: " + formattedTime + " Строк: " + SelectedMap.Events.Count;

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
        
        public void Render(Matrix4 projection)
        {
            
            _scene.Draw(projection);
        }
        public void OnMouseWheel(MouseWheelEventArgs e)
        {
            // e.OffsetY — это значение прокрутки (обычно ±1, иногда больше на высокоточных мышах)
            //list.Scroll(e.OffsetY);
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
