using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Reflection;
using TappiruCS.Core;
using TappiruCS.Core.TappiruCS.Core;
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



        public string songPath = "Songs/TestSong";
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


            list = new ListButtons(_spriteBatch, _textRenderer, songCount, 1000, 0, 1400, 212, "SongButton", "lol") ;
            for (int i = 0; i < songCount; i++)
            {
                string folderPath = folders[i];
                string folderName = Path.GetFileName(folderPath);

                string bgImagePath = Directory.GetFiles(folderPath, "*.jpg").FirstOrDefault()
                             ?? Directory.GetFiles(folderPath, "*.png").FirstOrDefault();
                Console.WriteLine(folderPath);
                if (bgImagePath != null)
                {
                    list.buttons[i].ButtonImage = TextureLoader.Load(bgImagePath); // загружаем текстуру
                }
                else
                {
                    
                    list.buttons[i].ButtonImage = 0;
                }

                list.buttons[i].Text = folderName;
                list.buttons[i].TextScale = 0.3f;
                list.buttons[i].TextAlign = TextRender.TextAlign.Left;
                list.buttons[i].IsImaged = true;
                list.buttons[i].TextOffset = new Vector2(5.4f, 4.2f);
                list.buttons[i].ImageScale = new Vector2(0.16f, 0.75f);
                list.buttons[i].ImagePadding = new Vector2(19f, 27f);
                list.buttons[i].ScaleMultiply = 0.8f;



                string capturedPath = folderPath;
                list.buttons[i].OnClick += () => PlaySong(capturedPath);
            }


            int _songSelectorTop = TextureManager.GetTexture("SongSelectorTop");
            SongSelectorTop = new SpriteObject(_spriteBatch, _songSelectorTop, 0, 0, 1920, 220) { Color = new Color4(1f, 1f, 1f, 1f), AutoScale = true };

            int _selectionmode = TextureManager.GetTexture("SelectionMode");
            SelectionMode = new SpriteObject(_spriteBatch, _selectionmode, 0, -520,1920, 1600) { Color = new Color4(1f, 1f, 1f, 1f), AutoScale = true };


            _scene.Add(list);

            _scene.Add(SelectionMode);
            _scene.Add(SongSelectorTop);
            

        }


        public void PlaySong(string SongPath)
        {
            Console.WriteLine("игра началась");
            _game.ChangeState(new GameSessionState(_game, _spriteBatch, _textRenderer, _audio,SongPath));
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
            Console.WriteLine("offset: "+e.Offset.Y);
        }

        public void HandleKeyDown(KeyboardKeyEventArgs e)
        {
            if (e.Key == Keys.Backspace)
            {
                _game.ChangeState(new MenuState(_game, _spriteBatch, _textRenderer, _audio));
            }
            if (e.Key == Keys.Enter)
            {
                _game.ChangeState(new GameSessionState(_game, _spriteBatch, _textRenderer, _audio,songPath));
            }
        }
    }
}
