using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Reflection;
using TappiruCS.Core;
using TappiruCS.Core.TappiruCS.Core;
using TappiruCS.Render;     // если нужно для SpriteBatch и TextRender

namespace TappiruCS
{
    public class SongSelectState : IGameState
    {
        private readonly Game _game;
        private readonly SpriteBatch _spriteBatch;
        private readonly TextRender _textRenderer;
        private readonly AudioManager _audio;

        private readonly Scene _scene = new Scene();

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
            for (int i = 0; i < songCount; i++)
            {
                string folderPath = Directory.GetDirectories("Songs/")[i];  // полный путь к папке

                float y = 150 + i * 75;

                var button = new Button(
                    _spriteBatch,
                    _textRenderer,
                    300, y, 150, 60,
                    "btn",
                    folderPath, Color4.Azure   // показываем название папки как текст кнопки
                )
                { Layer = 50 };

                button.OnClick += () => PlaySong(folderPath);   // передаём именно этот путь

                _scene.Add(button);
            }

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
            // Просто выводим текст на экран
           // _textRenderer.DrawString("выбор песни", 450, 200, 0.5f, 0.7f, 1f, 1f, 0f, 1f, projection);
           // _textRenderer.DrawString("пока здесь будет список карт", 320, 380, 0.5f, 0.7f, 1f, 0f, 1f, 1f, projection);
           // _textRenderer.DrawString("бекспейс назад в меню", 380, 450, 0.5f, 0.7f, 0.7f, 0.7f, 0.7f, 1f, projection);

            _scene.Draw(projection);
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
