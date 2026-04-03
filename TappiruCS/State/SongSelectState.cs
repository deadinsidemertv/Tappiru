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

        public SpriteObject blackBG;
        public SpriteObject blackBG2;



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


            list = new ListButtons(_spriteBatch, _textRenderer, songCount, 1000, 120, 1000, _game.ClientSize.Y/6, "btn", "lol") ;
            for (int i = 0; i < songCount; i++)
            {
                string folderPath = folders[i];
                string folderName = Path.GetFileName(folderPath);

                string bgImagePath = Directory.GetFiles(folderPath, "*.jpg").FirstOrDefault()
                             ?? Directory.GetFiles(folderPath, "*.png").FirstOrDefault();
                Console.WriteLine(folderPath);
                if (bgImagePath != null)
                {
                    list.buttons[i].buttonImage = TextureLoader.Load(bgImagePath); // загружаем текстуру
                }
                else
                {
                    
                    list.buttons[i].buttonImage = 0;
                }

                list.buttons[i]._text = folderName;
                list.buttons[i].TextBtnScale = 0.3f;
                list.buttons[i].textAlign = TextRender.TextAlign.Left;
                list.buttons[i].IsImaged = true;
                list.buttons[i].textOffest = new Vector2(5.8f, 4.2f);



                string capturedPath = folderPath;
                list.buttons[i].OnClick += () => PlaySong(capturedPath);
            }

            int _blackTexture = TextureManager.GetTexture("black");
            blackBG = new SpriteObject(_spriteBatch, 0, 0, 0, _game.ClientSize.X, _game.ClientSize.Y / 8) { Color = new Color4(0f, 0f, 0f, 1f), AutoScale = false };
            blackBG2 = new SpriteObject(_spriteBatch, 0, 0, _game.ClientSize.Y - _game.ClientSize.Y / 8, _game.ClientSize.X, _game.ClientSize.Y / 8) { Color = new Color4(0f, 0f, 0f, 1f), AutoScale = false };


            _scene.Add(list);

            _scene.Add(blackBG);
            _scene.Add(blackBG2);

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
            BlackBarUpdate();

            var mouse = _game.MouseState;
            _scene.Update(currentTime, mouse, _game);
            
        }
        public void BlackBarUpdate()
        {

            float blackbarYscale = _game.ClientSize.Y / 8;
            float bbYpos = _game.ClientSize.Y - _game.ClientSize.Y / 8;

            blackBG2.Position = new Vector2(0, bbYpos);
            blackBG.Scale = new Vector2(_game.ClientSize.X, blackbarYscale);
            blackBG2.Scale = new Vector2(_game.ClientSize.X, blackbarYscale);

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
