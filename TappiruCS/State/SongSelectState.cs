using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Reflection;
using TappiruCS.Core;
using TappiruCS.Core.TappiruCS.Core;
using TappiruCS.GameLogic;
using TappiruCS.Render;     // если нужно для SpriteBatch и TextRender
using TappiruCS.UI;

namespace TappiruCS
{
    public class SongSelectState : IGameState
    {
        private readonly Game _game;
        private readonly SpriteBatch _spriteBatch;
        private readonly TextRender _textRenderer;
        private readonly AudioManager _audio;

        private readonly Scene _scene = new Scene();

        public ListButtons list;

        public SpriteObject SongSelectorTop;
        public SpriteObject SelectionMode;
        public Background bg;

        public int _bgPreview;
        

        public string songPath = "";
        public int songCount;

        public SongSelectState(Game game, SpriteBatch spriteBatch, TextRender textRenderer, AudioManager audio)
        {
            _game = game;
            _spriteBatch = spriteBatch;
            _textRenderer = textRenderer;
            _audio = audio;
        }

        public void OnEnter()
        {
            Console.WriteLine("Открыт выбор песни (SongSelectState)");
            

            songCount = Directory.GetDirectories("Songs/").Length;
            string[] folders = Directory.GetDirectories("Songs/");


            

            list = new ListButtons(_spriteBatch, _textRenderer, songCount, 1000, 170, 1400, 212, "SongButton", "lol") { Layer =1} ;
            for (int i = 0; i < songCount; i++)
            {
                string folderPath = folders[i];
                string folderName = Path.GetFileName(folderPath);

                string bgImagePath = Directory.GetFiles(folderPath, "*.jpg").FirstOrDefault()
                             ?? Directory.GetFiles(folderPath, "*.png").FirstOrDefault();
                Console.WriteLine(folderPath);
                if (bgImagePath != null)
                {
                    list.Buttons[i].ButtonImage = TextureLoader.Load(bgImagePath); // загружаем текстуру
                }
                else
                {
                    
                    list.Buttons[i].ButtonImage = 0;
                }

                list.Buttons[i].Text = folderName;
                list.Buttons[i].TextScale = 0.3f;
                list.Buttons[i].TextAlign = TextRender.TextAlign.Left;
                list.Buttons[i].IsImaged = true;
                list.Buttons[i].TextOffset = new Vector2(150f, 2f);
                list.Buttons[i].ImageScale = new Vector2(0.16f, 0.75f);
                list.Buttons[i].ImagePadding = new Vector2(19f, 27f);
                list.Buttons[i].ScaleMultiply = 0.8f;
                list.Buttons[i].Layer = list.Layer;



                string capturedPath = folderPath;
                list.Buttons[i].OnClick += () => SelectSong(folderPath);
                _scene.Add(list.Buttons[i]);
            }


            bg = new Background(_spriteBatch, _bgPreview, _game) { Layer = 0, AllowHover = false };
            _scene.Add(bg);

            int _songSelectorTop = TextureManager.GetTexture("SongSelectorTop");
            SongSelectorTop = new SpriteObject(_spriteBatch, _songSelectorTop, 0, 0, 1920, 220) { Color = new Color4(1f, 1f, 1f, 1f), AutoScale = true,Layer = 2, AllowHover = false };

            int _selectionmode = TextureManager.GetTexture("SelectionMode");
            SelectionMode = new SpriteObject(_spriteBatch, _selectionmode, 0, -470,1920, 1550) { Color = new Color4(1f, 1f, 1f, 1f), AutoScale = true, Layer = 2, AllowHover = false };

            var playButton = new Button(_spriteBatch, _textRenderer,
                2050, 1000, 260, 240, "button", "Play", Color4.White)   // "btn" — имя текстуры через TextureManager
            {
                Layer = 2,
                TextColor = Color4.White,
                TextOffset = new Vector2(70f, 45f),
                TextScale = 0.8f,
                ScaleMultiply = 0.8f


            };
            playButton.OnClick += () => PlaySong(songPath);


            _scene.Add(list);
            _scene.Add(SelectionMode);
            _scene.Add(SongSelectorTop);
            _scene.Add(playButton);

        }


        public void PlaySong(string SongPath)
        {
            Console.WriteLine("игра началась");
            _game.ChangeState(new GameSessionState(_game, _spriteBatch, _textRenderer, _audio, SongPath));
        }

        public void SelectSong(string SP)
        {
            _audio.Stop();
            songPath = SP;
            string bgPath = Directory.GetFiles(SP, "*.jpg").FirstOrDefault()
                             ?? Directory.GetFiles(SP, "*.png").FirstOrDefault();
            _bgPreview = TextureLoader.Load(bgPath);
            bg._textureId = _bgPreview;
            _audio.LoadMusic(Directory.GetFiles(SP,"*.mp3").FirstOrDefault());
            _audio.Play();


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
