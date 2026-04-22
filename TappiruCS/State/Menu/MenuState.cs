using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
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
using TappiruCS.State.SongSelector;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;
using static TappiruCS.Render.Text.Font;

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
            _loginInput = new InputField(350, 535, 500, 70)
            {
                PlaceHolderText = "login",
                Layer = 4,
                

            };

            _passwordInput = new InputField(350, 615, 500, 70)
            {
                PlaceHolderText = "password",
                IsPassword = true,
                Layer = 4,
                
            };

            _loginButton = new Button(350, 695, 250, 70, "button", "Войти")
            {
                Layer = 4,
                TextColor = Color4.White,
                ScaleMultiply = 0.8f,
                TextOffset = new Vector2(-45, -30)
            };
            _loginButton.OnClick += async () => await AttemptLoginAsync();

            // Main menu buttons (всегда видны)
            var playBtn = CreateMenuButton(540, "Play", StartGame);
            var editBtn = CreateMenuButton(640, "Edit", GoEdit);
            var optionsBtn = CreateMenuButton(740, "Options", ToggleSettings);
            var exitBtn = CreateMenuButton(840, "exit", ExitGame);

            _scene.Add(playBtn);
            _scene.Add(editBtn);
            _scene.Add(optionsBtn);
            _scene.Add(exitBtn);
        }

        private void AddBackgroundAndDecorations()
        {
            var bg = new Background(TextureManager.GetTexture("menubg")) { ParalaxEffect = true };
            var logo = new SpriteObject(TextureManager.GetTexture("logo"), 960, 300, 606, 256)
            {
                ScaleMultiply = 1.1f
            };

            
            var blackTop = new SpriteObject(0, 960, 0, 1920, 200)
            {
                Color = new Color4(0f, 0f, 0f, 0.5f),
                AutoScale = true,
                Opacity = 0.6f
            };
            var blackBottom = new SpriteObject(0, 960, 1080, 1920, 200)
            {
                Color = new Color4(0f, 0f, 0f, 0.5f),
                AutoScale = true,
                Opacity = 0.6f
            };

            _scene.Add(bg);
            _scene.Add(logo);
            _scene.Add(blackTop);
            _scene.Add(blackBottom);
        }

        private Button CreateMenuButton(int y, string text, Action? onClick)
        {
            var btn = new Button(960, y, 700, 120, "button", text)
            {
                Layer = 2,
                TextAlign = TextAlign.Center,
                TextColor = Color4.White,
                TextOffset = new Vector2(-70f, -50f),
                ScaleMultiply = 0.8f,
            };
            if (onClick != null) btn.OnClick += onClick;
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
            
        }

        private void ShowLoggedInUI()
        {
            RemoveLoginUI();

            if (_welcomeText == null)
            {
                _welcomeText = new TextObject("", 250, 460, 0.5f)
                {
                    Align = TextAlign.Left,
                    Color = Color4.White,
                    Layer = 3
                };

                _ratingText = new TextObject("", 250, 540, 0.35f)
                {
                    Align = TextAlign.Left,
                    Color = Color4.White,
                    Layer = 3
                };

                _scene.Add(_welcomeText);
                _scene.Add(_ratingText);
            }

            _welcomeText.Text = PlayerProfile.Instance.UserName;
            _ratingText.Text = $"Ranking MMR: {PlayerProfile.Instance.Rating}";

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