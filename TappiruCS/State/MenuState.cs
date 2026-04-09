using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using System;
using System.Threading.Tasks;
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

        // UI
        private InputField _inputField;
        private InputField _inputFieldPassword;
        private Button _loginButton;
        private TextObject _welcomeText;

        // Состояние
        private bool _isLoggedIn = false;
        private string _userName = "";
        private int _userRating = 0;

        private bool _loginInProgress = false;

        // Флаги для безопасного обновления UI из асинхронного кода
        private bool _pendingSwitchToLoggedIn = false;
        private string _pendingUserName = "";
        private int _pendingUserRating = 0;

        public MenuState(Game game, SpriteBatch spriteBatch, TextRender textRenderer, AudioManager audio)
        {
            _game = game;
            _spriteBatch = spriteBatch;
            _textRenderer = textRenderer;
            _audio = audio;
        }

        public void OnEnter()
        {
            Console.WriteLine("=== MenuState OnEnter ===");

            CreateLoginUI();
            CreateMainMenuButtons();

            // Фон и декорации
            var bgmenu = new Background(_spriteBatch, TextureManager.GetTexture("menubg"), _game) { ParalaxEffect = true };
            var logo = new SpriteObject(_spriteBatch, TextureManager.GetTexture("logo"), 960, 300, 606, 256) { ScaleMultiply = 1.1f };

            int blackTex = TextureManager.GetTexture("black");
            var blackBG = new SpriteObject(_spriteBatch, blackTex, 960, 0, 2000, _game.ClientSize.Y / 3)
            { Color = new Color4(0f, 0f, 0f, 0.5f), AutoScale = true };
            var blackBG2 = new SpriteObject(_spriteBatch, blackTex, 960, 1080, 2000, _game.ClientSize.Y / 3)
            { Color = new Color4(0f, 0f, 0f, 0.5f), AutoScale = true };

            _scene.Add(bgmenu);
            _scene.Add(logo);
            _scene.Add(blackBG);
            _scene.Add(blackBG2);

            StartAutoLogin();
        }

        private void CreateLoginUI()
        {
            _inputField = new InputField(_game, _spriteBatch, _textRenderer, 350, 535, 500, 70)
            {
                PlaceHolderText = "login",
                Layer = 4
            };

            _inputFieldPassword = new InputField(_game, _spriteBatch, _textRenderer, 350, 615, 500, 70)
            {
                PlaceHolderText = "password",
                IsPassword = true,
                Layer = 4
            };

            _loginButton = new Button(_spriteBatch, _textRenderer, 350, 695, 250, 70, "button", "Войти", Color4.White)
            {
                Layer = 4,
                TextColor = Color4.White,
                TextOffset = new Vector2(-5f, -25f),
                TextScale = 0.4f,
                ScaleMultiply = 0.8f
            };

            _loginButton.OnClick += OnLoginButtonClicked;

            _scene.Add(_inputField);
            _scene.Add(_inputFieldPassword);
            _scene.Add(_loginButton);
        }

        private void CreateMainMenuButtons()
        {
            var playButton = new Button(_spriteBatch, _textRenderer, 960, 540, 700, 120, "button", "Play", Color4.White)
            {
                Layer = 2,
                TextColor = Color4.White,
                TextOffset = new Vector2(-10f, -50f),
                TextScale = 0.7f,
                ScaleMultiply = 0.8f
            };

            var editButton = new Button(_spriteBatch, _textRenderer, 960, 640, 700, 120, "button", "Edit", Color4.White)
            {
                Layer = 2,
                TextColor = Color4.White,
                TextOffset = new Vector2(-10f, -50f),
                TextScale = 0.7f,
                ScaleMultiply = 0.8f
            };

            var optionButton = new Button(_spriteBatch, _textRenderer, 960, 740, 700, 120, "button", "Options", Color4.White)
            {
                Layer = 2,
                TextColor = Color4.White,
                TextOffset = new Vector2(-10f, -50f),
                TextScale = 0.7f,
                ScaleMultiply = 0.8f
            };

            var exitButton = new Button(_spriteBatch, _textRenderer, 960, 840, 700, 120, "button", "exit", Color4.White)
            {
                Layer = 2,
                TextColor = Color4.White,
                TextOffset = new Vector2(-10f, -50f),
                TextScale = 0.7f,
                ScaleMultiply = 0.8f
            };

            playButton.OnClick += StartGame;
            editButton.OnClick += GoEdit;
            exitButton.OnClick += ExitGame;

            _scene.Add(playButton);
            _scene.Add(editButton);
            _scene.Add(optionButton);
            _scene.Add(exitButton);
        }

        private void OnLoginButtonClicked()
        {
            if (_loginInProgress || _isLoggedIn) return;

            _loginInProgress = true;
            Console.WriteLine("=== Кнопка 'Войти' нажата ===");

            _ = PerformLoginAsync();
        }

        private async Task PerformLoginAsync()
        {
            try
            {
                Console.WriteLine("→ Вызываем Auth.Login()...");
                bool loginOk = await Auth.Login(_inputField.Text, _inputFieldPassword.Text).ConfigureAwait(false);
                Console.WriteLine($"   Auth.Login вернул: {loginOk}");

                if (!loginOk)
                {
                    Console.WriteLine("   Логин не прошёл.");
                    return;
                }

                Console.WriteLine("→ Вызываем User.FetchCurrentUser()...");
                bool fetchOk = await User.FetchCurrentUser().ConfigureAwait(false);
                Console.WriteLine($"   FetchCurrentUser вернул: {fetchOk}");

                if (fetchOk)
                {
                    _pendingUserName = User.UserName ?? "Unknown";
                    _pendingUserRating = User.Rating;
                    _pendingSwitchToLoggedIn = true;   // ← устанавливаем флаг
                    Console.WriteLine("   Данные пользователя сохранены, будет выполнен SwitchToLoggedInState");
                }
                else
                {
                    Console.WriteLine("   FetchCurrentUser вернул false — проверь токен или ответ сервера");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"!!! Исключение при логине: {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                _loginInProgress = false;
            }
        }

        private void StartAutoLogin()
        {
            Auth.LoadToken();
            if (string.IsNullOrEmpty(Auth.AuthToken) || Auth.IsTokenExpired())
            {
                Console.WriteLine("Автологин пропущен (нет валидного токена)");
                return;
            }

            Console.WriteLine("Запускаем автологин...");
            _ = PerformAutoLoginAsync();
        }

        private async Task PerformAutoLoginAsync()
        {
            try
            {
                bool ok = await User.FetchCurrentUser().ConfigureAwait(false);
                if (ok)
                {
                    _pendingUserName = User.UserName ?? "Unknown";
                    _pendingUserRating = User.Rating;
                    _pendingSwitchToLoggedIn = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка автологина: {ex.Message}");
            }
        }

        private void SwitchToLoggedInState()
        {
            if (_isLoggedIn) return;
            _isLoggedIn = true;

            Console.WriteLine("=== Выполняем SwitchToLoggedInState ===");

            // Удаляем элементы логина
            if (_inputField != null) _scene.Remove(_inputField);
            if (_inputFieldPassword != null) _scene.Remove(_inputFieldPassword);
            if (_loginButton != null) _scene.Remove(_loginButton);

            // Добавляем приветствие
            _welcomeText = new TextObject(_textRenderer,
                $"Игрок: {_userName} | Рейтинг: {_userRating}",
                15, 15, 0.3f)
            {
                Align = TextRender.TextAlign.Left,
                Color = Color4.Red,
                Layer = 3
            };

            _scene.Add(_welcomeText);

            Console.WriteLine($"=== УСПЕШНЫЙ ВХОД === {_userName} | Рейтинг: {_userRating}");
        }

        public void Update(double deltaTime)
        {
            // === ОБРАБОТКА ПЕНДИНГОВЫХ ДЕЙСТВИЙ ИЗ АСИНХРОННОГО КОДА ===
            if (_pendingSwitchToLoggedIn)
            {
                _pendingSwitchToLoggedIn = false;
                _userName = _pendingUserName;
                _userRating = _pendingUserRating;
                SwitchToLoggedInState();
            }

            // Обычное обновление сцены
            var mouse = _game.MouseState;
            _scene.Update(deltaTime, mouse, _game);
        }

        public void Render(Matrix4 projection)
        {
            _scene.Draw(projection);
        }

        public void OnExit()
        {
            _scene.Clear();
            Console.WriteLine("Мы вышли из главного меню");
        }

        public void HandleKeyDown(KeyboardKeyEventArgs e) { }

        private void StartGame() => _game.ChangeState(new SongSelectState(_game, _spriteBatch, _textRenderer, _audio));
        private void GoEdit() => _game.ChangeState(new EditState(_game, _spriteBatch, _textRenderer, _audio));
        private void ExitGame() => _game.Close();
    }
}