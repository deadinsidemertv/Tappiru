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


            list = new ListButtons(_spriteBatch, _textRenderer, songCount, 1240, 0, 700, 100, "btn", "lol") { ScaleMultiplyList = 0.3f};
            for (int i = 0; i < songCount; i++)
            {
                string folderPath = folders[i];
                string folderName = Path.GetFileName(folderPath); // имя папки

                // Меняем текст кнопки (способ зависит от реализации Button)
                list.buttons[i]._text = folderName;        // если поле публичное
                list.buttons[i].TextBtnScale = 0.3f;
                list.buttons[i].textXoffset -= 300f;
                list.buttons[i].textYoffset -= 35f;
                list.buttons[i].textAlign = TextRender.TextAlign.Left;
                
                // или list.buttons[i].SetText(folderName); // если есть метод

                // Привязываем обработчик с ЗАХВАТОМ пути (важно!)
                string capturedPath = folderPath;
                list.buttons[i].OnClick += () => PlaySong(capturedPath);
            }

            _scene.Add(list);

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
