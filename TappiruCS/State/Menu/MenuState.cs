using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using Pango;
using System;
using System.Threading.Tasks;
using TappiruCS.Core;
using TappiruCS.Core.GameObject;
using TappiruCS.GameLogic;
using TappiruCS.Render;
using TappiruCS.Render.Text;
using TappiruCS.Server;
using TappiruCS.Server.Player;
using TappiruCS.State.Edit;
using TappiruCS.State.Menu.Option;
using TappiruCS.State.SongSelector;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;

namespace TappiruCS.State.Menu
{
    public class MenuState : IGameState
    {
        private readonly RenderContext _context;
        private readonly Scene _scene = new Scene();

        // Login UI
        private InputField _loginInput;
        private InputField _passwordInput;
        private Button _loginButton;
        private TextObject _loginText;

        // Logged-in UI
        private TextObject _welcomeText;
        private TextObject _ratingText;
        private SpriteObject _avatarSprite;
        private SpriteObject _avatarBackground;

        public MenuState(RenderContext context)
        {
            _context = context;
        }

        public void OnEnter()
        {
            _scene.Initialize(_context);

            PlayerProfile.Instance.Initialize(_context.Game);

            AddBackgroundAndDecorations();
            CreateUI();

            var mouseN = new MouseNotification(_scene);
            _scene.Add(mouseN);

            PlayerProfile.Instance.OnProfileChanged += RefreshLoggedInUI;

            // === Главная логика показа нужного экрана ===
            if (PlayerProfile.Instance.IsLoggedIn)
            {
                RefreshLoggedInUI();        // уже залогинен (например, из другого стейта)
            }
            else
            {
                StartAutoLogin();           // пытаемся автологин
                // Если автологин не сработал — сразу показываем форму входа
                if (!PlayerProfile.Instance.IsLoggedIn)
                {
                    ShowLoginUI();
                }
            }
            GetRandomSong();

        }

        private void ToggleSettings()
        {
            _context.Game.OpenModalWindow(new OptionModule(_scene));
        }
        public void GetRandomSong()
        {
            string[] folders = Directory.GetDirectories("Songs/");
            Random rnd = new Random();
            int randomsong = rnd.Next(0, folders.Length);
            SongSelectState.SelectedMap = LoadMap.MapLoad(folders[randomsong]);
            _context.Audio.LoadMusic(SongSelectState.SelectedMap.audioPath);
            _context.Audio.Play();

        }
        private void CreateUI()
        {
            // Login fields
            _loginText = new TextObject("Login", 120, 720, 72)
            {
                FontKey = "Menu",
                Color = new Color4(230, 57, 186,255),
                FontSize = 16,
            };
            _loginInput = new InputField(240, 750, 430, 60)
            {
                PlaceHolderText = "username",
                Layer = 4,
                ScaleMultiply = 0.7f,
                Tag = "username"
                
            };
            _loginInput.SetupIcon();

            _passwordInput = new InputField(240, 795, 430, 60)
            {
                PlaceHolderText = "password",
                IsPassword = true,
                Layer = 4,
                ScaleMultiply = 0.7f,
                Tag = "password"

            };
            _passwordInput.SetupIcon();

            _loginButton = new Button(240, 860, 425, 75, "buttonSignUp", "Sign In")
            {
                Layer = 4,
                TextColor = Color4.White,
                TextOffset = new Vector2(-65, 10),
                ScaleMultiply = 0.7f,
                FontSize = 24f,
                FontKey = "Menu",
                Tag = "SignIn"

            };
            _loginButton.OnClick += async () => await AttemptLoginAsync();

            // Main menu buttons (всегда видны)
            var playBtn = CreateMenuButton(305, "Play", StartGame);
            var editBtn = CreateMenuButton(405, "Edit", GoEdit);
            var optionsBtn = CreateMenuButton(505, "Options", ToggleSettings);
            var exitBtn = CreateMenuButton(605, "exit", ExitGame);

            

            _scene.Add(playBtn);
            _scene.Add(editBtn);
            _scene.Add(optionsBtn);
            _scene.Add(exitBtn);
        }

        private void AddBackgroundAndDecorations()
        {
            Random rnd = new Random();
            string[] bgpatches = Directory.GetFiles("Textures\\Backgrounds");

            int randomBG = rnd.Next(0,bgpatches.Length);
            string menubgpath = bgpatches[randomBG];

            var bg = new Background(TextureLoader.Load(menubgpath)) { ParalaxEffect = true };
            var overlay = new Background(TextureManager.GetTexture("overlaynew"));
            var Logo = new SpriteObject(TextureManager.GetTexture("newlogo"), 350, 150, 630, 256);
            

            _scene.Add(bg);
            _scene.Add(overlay);
            
            _scene.Add(Logo);

        }

        private Button CreateMenuButton(int y, string text, Action? onClick)
        {
            var btn = new Button(300, y, 520, 120, "menuButton", text)
            {
                Layer = 2,
                TextAlign = TextAlign.Center,
                TextColor = Color4.White,
                TextOffset = new Vector2(-230f, 20f),
                ScaleMultiply = 0.8f,
                Tag = "menuButton",
                FontSize = 64f,
                FontKey = "Menu"
                
            };
            if (onClick != null) btn.OnClick += onClick;

            btn.InitializeHoverState();
            return btn;
        }

        private async Task AttemptLoginAsync()
        {
            bool loginOk = await Auth.Login(_loginInput.Text, _passwordInput.Text);
            if (loginOk)
            {
                await User.FetchCurrentUser();   
            }
            
        }

        private void StartAutoLogin()
        {
            Auth.LoadToken();
            if (string.IsNullOrEmpty(Auth.AuthToken) || Auth.IsTokenExpired())
            {
                Console.WriteLine("Автологин пропущен: токен отсутствует или истёк");
                return;
            }

            Console.WriteLine("Попытка автологина...");
            _ = User.FetchCurrentUser();   // асинхронно обновит профиль → OnProfileChanged
        }

        private void RefreshLoggedInUI()
        {
            if (!PlayerProfile.Instance.IsLoggedIn)
            {
                ShowLoginUI();
                return;
            }

            ShowLoggedInUI();
        }

        private void ShowLoginUI()
        {
            RemoveLoggedInUI();

            _scene.Add(_loginInput);
            _scene.Add(_passwordInput);
            _scene.Add(_loginButton);
            _scene.Add(_loginText);
            
        }

        private void ShowLoggedInUI()
        {
            RemoveLoginUI();

            if (_welcomeText == null)
            {
                _welcomeText = new TextObject("", 250, 460, 48f)
                {
                    Align = TextAlign.Left,
                    Color = Color4.White,
                    Layer = 3
                };

                _ratingText = new TextObject("", 250, 540, 72f)
                {
                    Align = TextAlign.Left,
                    Color = Color4.White,
                    Layer = 3
                };

                _scene.Add(_welcomeText);
                _scene.Add(_ratingText);
            }

            _welcomeText.Text = PlayerProfile.Instance.UserName;
            _ratingText.Text = $"TP: {PlayerProfile.Instance.Rating}";

            // Аватарка
            if (PlayerProfile.Instance.AvatarTextureId != 0)
            {
                if (_avatarSprite == null)
                {
                    _avatarBackground = new SpriteObject(TextureManager.GetTexture("black"), 360, 540, 520, 150)
                    {
                        Color = new Color4(1f, 1f, 1f, 0.5f)
                    };

                    _avatarSprite = new SpriteObject(PlayerProfile.Instance.AvatarTextureId, 180, 540, 130, 130)
                    {
                        Layer = 3
                    };

                    _scene.Add(_avatarBackground);
                    _scene.Add(_avatarSprite);
                }
                else
                {
                    _avatarSprite._textureId = PlayerProfile.Instance.AvatarTextureId;   // исправил: было _textureId
                }
            }
        }

        private void RemoveLoginUI()
        {
            _scene.Remove(_loginInput);
            _scene.Remove(_passwordInput);
            _scene.Remove(_loginButton);
        }

        private void RemoveLoggedInUI()
        {
            if (_welcomeText != null) _scene.Remove(_welcomeText);
            if (_ratingText != null) _scene.Remove(_ratingText);
            if (_avatarSprite != null) _scene.Remove(_avatarSprite);
            if (_avatarBackground != null) _scene.Remove(_avatarBackground);
            if (_loginText!=null) _scene.Remove(_loginText);
        }

        public void Update(double deltaTime)
        {
            var mouse = _context.Game.MouseState;
            _scene.Update(deltaTime, mouse, _context.Game);
        }

        public void Render(Matrix4 projection)
        {
            _scene.Draw(projection);
        }

        public void OnExit()
        {
            PlayerProfile.Instance.OnProfileChanged -= RefreshLoggedInUI;
            _scene.Clear();
            Console.WriteLine("Выход из MenuState");
        }
        public void HandleKeyDown(KeyboardKeyEventArgs e) 
        {
            
        }

        private void StartGame() => _context.Game.ChangeState(new SongSelectState(_context));
        private void GoEdit() => _context.Game.ChangeState(new EditState(_context));
        private void ExitGame() => _context.Game.Close();
    }
}