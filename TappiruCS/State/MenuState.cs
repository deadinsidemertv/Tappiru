using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using TappiruCS.Core;
using TappiruCS.Render;
using TappiruCS.Server;
using TappiruCS.State.Edit;
using TappiruCS.UI;

namespace TappiruCS.State
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

            
            var InputField = new InputField(_game,_spriteBatch, _textRenderer, 350, 535, 500, 70) { PlaceHolderText = "login"};
            var InputFieldPassword = new InputField(_game,_spriteBatch, _textRenderer, 350, 615, 500, 70) { PlaceHolderText = "password", IsPassword = true};
            var loginButton = new Button(_spriteBatch, _textRenderer, 350, 695, 250, 70,
                                "button", "Войти", Color4.White)
            {
                Layer = 0,
                TextColor = Color4.White,
                TextOffset = new Vector2(-5f, -25f),
                TextScale = 0.4f,
                ScaleMultiply = 0.8f,
            };

            loginButton.OnClick += async () =>
            {
                bool success = await Auth.Login(InputField.Text, InputFieldPassword.Text);
                if (success) 
                {

                    Console.WriteLine("Вход выполнен");
                    bool userDataOk = await User.FetchCurrentUser();
                    if (userDataOk)
                    {
                        var welcomeText = new TextObject(_textRenderer, $"Игрок: {User.UserName} | Рейтинг: {User.Rating}", 0, 0, 0.3f) { Align = TextRender.TextAlign.Left };
                        _scene.Add(welcomeText);
                    }
                    
                }
                else
                {
                    Console.WriteLine("Ошибка входа");
                }

            };

            // 2. Кнопка "Начать игру"
            var playButton = new Button(_spriteBatch, _textRenderer,
                960, 540, 700, 120, "button", "Play", Color4.White)   // "btn" — имя текстуры через TextureManager
            {
                Layer = 0,
                TextColor = Color4.White,
                TextOffset = new Vector2(-10f, -50f),
                TextScale = 0.7f,
                ScaleMultiply = 0.8f,
            };
            var editButton = new Button(_spriteBatch, _textRenderer,
                960, 640, 700, 120, "button", "Edit", Color4.White)   // "btn" — имя текстуры через TextureManager
            {
                Layer = 0,
                TextColor = Color4.White,
                TextOffset = new Vector2(-10f, -50f),
                TextScale = 0.7f,
                ScaleMultiply = 0.8f,

            };
            var optionButton = new Button(_spriteBatch, _textRenderer,
                960, 740, 700, 120, "button", "Options", Color4.White)   // "btn" — имя текстуры через TextureManager
            {
                Layer = 0,
                TextColor = Color4.White,
                TextOffset = new Vector2(-10f, -50f),
                TextScale = 0.7f,
                ScaleMultiply = 0.8f,

            };

            // 3. Кнопка "Выход"
            var exitButton = new Button(_spriteBatch, _textRenderer,
                960, 840, 700, 120, "button", "exit", Color4.White)
            {
                Layer = 0,
                TextColor = Color4.White,
                TextOffset = new Vector2(-10f, -50f),
                TextScale = 0.7f,
                ScaleMultiply = 0.8f,
            };

            int _bgmenu = TextureManager.GetTexture("menubg");
            var bgmenu = new Background(_spriteBatch, _bgmenu, _game) { ParalaxEffect = true};

            int _bgtexture = TextureManager.GetTexture("logo");
            var bgCycle = new SpriteObject(_spriteBatch, _bgtexture, 960, 300, 606, 256) { ScaleMultiply = 1.1f};

            int _blackTexture = TextureManager.GetTexture("black");
            blackBG = new SpriteObject(_spriteBatch, 0, 960, 0, 2000, _game.ClientSize.Y/3) { Color = new Color4(0f,0f,0f,0.5f),AutoScale = true};
            blackBG2 = new SpriteObject(_spriteBatch, 0, 960, 1080 , 2000, _game.ClientSize.Y/3) { Color = new Color4(0f, 0f, 0f, 0.5f),AutoScale = true };

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
            _scene.Add(InputField);
            _scene.Add(InputFieldPassword);
            _scene.Add(loginButton);


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
            
            var mouse = _game.MouseState;   
            
            _scene.Update(deltaTime, mouse,_game);
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
            
        }
    }
}