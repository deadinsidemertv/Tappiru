using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Security.Cryptography.X509Certificates;
using TappiruCS.Core;
using TappiruCS.Render;
using TappiruCS.State;
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

        public SpriteObject blackBG;
        public SpriteObject blackBG2;



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
                610, 400, 700, 120, "btn", "Play", Color4.White)   // "btn" — имя текстуры через TextureManager
            {
                Layer = 0,
                TextColor = Color4.White,
                TextOffset = new Vector2(270f,-19f),
                TextScale = 0.8f
                

            };
            var editButton = new Button(_spriteBatch, _textRenderer,
                610, 510, 700, 120, "btn", "Edit", Color4.White)   // "btn" — имя текстуры через TextureManager
            {
                Layer = 0,
                TextColor = Color4.White,
                TextOffset = new Vector2(270f, -19f),
                TextScale = 0.8f

            };
            var optionButton = new Button(_spriteBatch, _textRenderer,
                610, 620, 700, 120, "btn", "Options", Color4.White)   // "btn" — имя текстуры через TextureManager
            {
                Layer = 0,
                TextColor = Color4.White,
                TextOffset = new Vector2(270f, -19f),
                TextScale = 0.8f

            };

            // 3. Кнопка "Выход"
            var exitButton = new Button(_spriteBatch, _textRenderer,
                610, 730, 700, 120, "btn", "exit", Color4.White)
            {
                Layer = 0,
                TextColor = Color4.White,
                TextOffset = new Vector2(270f, -19f),
                TextScale = 0.8f
            };

            int _bgmenu = TextureManager.GetTexture("menubg");
            var bgmenu = new Background(_spriteBatch, _bgmenu,_game);

            int _bgtexture = TextureManager.GetTexture("logo");
            var bgCycle = new SpriteObject(_spriteBatch, _bgtexture, 680, 150, 606, 256) { ScaleMultiply = 0.9f};

            int _blackTexture = TextureManager.GetTexture("black");
            blackBG = new SpriteObject(_spriteBatch, 0, 0, 0, _game.ClientSize.X, _game.ClientSize.Y/8) { Color = new Color4(0f,0f,0f,0.5f),AutoScale = false };
            blackBG2 = new SpriteObject(_spriteBatch, 0, 0, _game.ClientSize.Y - _game.ClientSize.Y / 8, _game.ClientSize.X, _game.ClientSize.Y/8) { Color = new Color4(0f, 0f, 0f, 0.5f),AutoScale = false };

            // Подписываемся на клики
            playButton.OnClick += StartGame;
            exitButton.OnClick += ExitGame;
            editButton.OnClick += GoEdit;

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

        private void GoEdit()
        {
            _game.ChangeState(new EditState(_game, _spriteBatch, _textRenderer, _audio));
        }
        // ====================== UPDATE ======================
        
        public void Update(double deltaTime)
        {
            
            var mouse = _game.MouseState;   // ← предполагаем, что в классе Game есть MouseState
            BlackBarUpdate();

            // Важно: передаём MouseState только объектам, которым он нужен
            _scene.Update(deltaTime, mouse,_game);
        }

        public void BlackBarUpdate()
        {
            
            float blackbarYscale = _game.ClientSize.Y/8;
            float bbYpos = _game.ClientSize.Y-_game.ClientSize.Y/8;

            blackBG2.Position = new Vector2(0,bbYpos);
            blackBG.Scale = new Vector2(_game.ClientSize.X, blackbarYscale);
            blackBG2.Scale = new Vector2(_game.ClientSize.X, blackbarYscale);

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