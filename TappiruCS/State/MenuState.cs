using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Security.Cryptography.X509Certificates;
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



            // 2. Кнопка "Начать игру"
            var playButton = new Button(_spriteBatch, _textRenderer,
                400, 180, 600, 75, "btn", "Play", Color4.Azure)   // "btn" — имя текстуры через TextureManager
            {
                Layer = 0,
                _TextColor = Color4.Azure,
                textXoffset = 50f,
                textYoffset = -20f

            };
            var editButton = new Button(_spriteBatch, _textRenderer,
                400, 250, 600, 75, "btn", "Edit", Color4.Azure)   // "btn" — имя текстуры через TextureManager
            {
                Layer = 0,
                _TextColor = Color4.Azure,
                textXoffset = 50f,
                textYoffset = -20f

            };
            var optionButton = new Button(_spriteBatch, _textRenderer,
                400, 320, 600, 75, "btn", "Options", Color4.Azure)   // "btn" — имя текстуры через TextureManager
            {
                Layer = 0,
                _TextColor = Color4.Azure,
                textXoffset = 50f,
                textYoffset = -20f

            };

            // 3. Кнопка "Выход"
            var exitButton = new Button(_spriteBatch, _textRenderer,
                400, 390, 600, 75, "btn", "exit", Color4.Azure)
            {
                Layer = 0,
                textXoffset = 50f,
                textYoffset = -20f
            };

            int _bgmenu = TextureManager.GetTexture("menubg");
            var bgmenu = new SpriteObject(_spriteBatch, _bgmenu, 0, 0, Game.WindowWidth, Game.WindowHeight) { ScaleMultiply = 1f};

            int _bgtexture = TextureManager.GetTexture("tappCycle");
            var bgCycle = new SpriteObject(_spriteBatch, _bgtexture, 150, 80, 1024, 1024) { ScaleMultiply = 0.55f};

            int _blackTexture = TextureManager.GetTexture("black");
            var blackBG = new SpriteObject(_spriteBatch, 0, 0, 0, Game.WindowWidth, 110) { Color = new Color4(0f,0f,0f,0.5f) };
            var blackBG2 = new SpriteObject(_spriteBatch, 0, 0, Game.WindowHeight-120, Game.WindowWidth, 120) { Color = new Color4(0f, 0f, 0f, 0.5f) };

            // Подписываемся на клики
            playButton.OnClick += StartGame;
            exitButton.OnClick += ExitGame;

            // Добавляем всё в сцену
            _scene.Add(bgmenu);


            _scene.Add(playButton);
            _scene.Add(editButton);
            _scene.Add(optionButton);
            _scene.Add(exitButton);
            


            _scene.Add(bgCycle);
            _scene.Add(blackBG);
            _scene.Add(blackBG2);

            
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