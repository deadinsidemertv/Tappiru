using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using TappiruCS.Core;
using TappiruCS.Render;
using TappiruCS.UI;

namespace TappiruCS
{
    public class MenuState : IGameState
    {
        private readonly Game _game;
        private readonly SpriteBatch _spriteBatch;
        private readonly TextRender _textRenderer;
        private readonly AudioManager _audio;

        private readonly Scene _scene = new Scene();

        

        public MenuState(Game game, SpriteBatch spriteBatch, TextRender textRenderer, AudioManager audio)
        {
            _game = game;
            _spriteBatch = spriteBatch;
            _textRenderer = textRenderer;
            _audio = audio;
        }

        public void OnEnter()
        {
            Console.WriteLine("Мы вошли в главное меню");

            // 1. Заголовок
            var title = new TextObject(_textRenderer, "таппиру", 640, 180, 0.75f)
            {
                Color = new Color4(1.0f, 0.95f, 0.7f, 1.0f),
                Layer = 100
            };

            // 2. Кнопка "Начать игру"
            var playButton = new Button(_spriteBatch, _textRenderer,
                440, 300, 400, 90, "btn", "начать игру",Color4.Azure)   // "btn" — имя текстуры через TextureManager
            {
                Layer = 50
            };

            // 3. Кнопка "Выход"
            var exitButton = new Button(_spriteBatch, _textRenderer,
                440, 420, 400, 90, "btn", "выход", Color4.Black)
            {
                Layer = 50
            };

            // Подписываемся на клики
            playButton.OnClick += StartGame;
            exitButton.OnClick += ExitGame;

            // Добавляем всё в сцену
            _scene.Add(title);
            _scene.Add(playButton);
            _scene.Add(exitButton);
        }

        private void StartGame()
        {
            Console.WriteLine("Игрок нажал 'Начать игру'");
            _game.ChangeState(new SongSelectState(_game, _spriteBatch, _textRenderer, _audio));
        }

        private void ExitGame()
        {
            Console.WriteLine("Игрок нажал 'Выход'");
            _game.Close();
        }

        // ====================== UPDATE ======================
        public void Update(double deltaTime)
        {
            var mouse = _game.MouseState;   // ← предполагаем, что в классе Game есть MouseState

            // Важно: передаём MouseState только объектам, которым он нужен
            _scene.Update(deltaTime, mouse);
        }

        // ====================== RENDER ======================
        public void Render(Matrix4 projection)
        {
            _scene.Draw(projection);
        }

        public void OnExit()
        {
            _scene.Clear();
            Console.WriteLine("Мы вышли из главного меню");
        }

        public void HandleKeyDown(KeyboardKeyEventArgs e)
        {
            if (e.Key == Keys.Enter)
                StartGame();

            if (e.Key == Keys.Escape || e.Key == Keys.Backspace)
                ExitGame();
        }
    }
}