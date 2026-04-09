using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using StbImageSharp;
using System;
using System.IO;
using System.Net.Http;
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

        // UI элементы
        private InputField _loginInput;
        private InputField _passwordInput;
        private Button _loginButton;
        private TextObject _welcomeText;
        private SpriteObject _avatarSprite;

        // Состояние пользователя
        private bool _isLoggedIn;
        private string _userName = "";
        private int _userRating;

        private bool _loginInProgress;

        // Аватарка (асинхронная загрузка)
        private bool _avatarLoadRequested;
        private Task<AvatarLoadResult>? _avatarLoadTask;
        private string? _avatarUrl;

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

            AddBackgroundAndDecorations();

            StartAutoLogin();
        }

        private void AddBackgroundAndDecorations()
        {
            var bg = new Background(_spriteBatch, TextureManager.GetTexture("menubg"), _game) { ParalaxEffect = true };
            var logo = new SpriteObject(_spriteBatch, TextureManager.GetTexture("logo"), 960, 300, 606, 256)
            {
                ScaleMultiply = 1.1f
            };

            int blackTex = TextureManager.GetTexture("black");
            var blackTop = new SpriteObject(_spriteBatch, blackTex, 960, 0, 2000, _game.ClientSize.Y / 3)
            {
                Color = new Color4(0f, 0f, 0f, 0.5f),
                AutoScale = true
            };
            var blackBottom = new SpriteObject(_spriteBatch, blackTex, 960, 1080, 2000, _game.ClientSize.Y / 3)
            {
                Color = new Color4(0f, 0f, 0f, 0.5f),
                AutoScale = true
            };

            _scene.Add(bg);
            _scene.Add(logo);
            _scene.Add(blackTop);
            _scene.Add(blackBottom);
        }

        private void CreateLoginUI()
        {
            _loginInput = new InputField(_game, _spriteBatch, _textRenderer, 350, 535, 500, 70)
            {
                PlaceHolderText = "login",
                Layer = 4
            };

            _passwordInput = new InputField(_game, _spriteBatch, _textRenderer, 350, 615, 500, 70)
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

            _scene.Add(_loginInput);
            _scene.Add(_passwordInput);
            _scene.Add(_loginButton);
        }

        private void CreateMainMenuButtons()
        {
            var playBtn = CreateMenuButton(540, "Play", StartGame);
            var editBtn = CreateMenuButton(640, "Edit", GoEdit);
            var optionsBtn = CreateMenuButton(740, "Options", null); // добавить обработчик при необходимости
            var exitBtn = CreateMenuButton(840, "exit", ExitGame);

            _scene.Add(playBtn);
            _scene.Add(editBtn);
            _scene.Add(optionsBtn);
            _scene.Add(exitBtn);
        }

        private Button CreateMenuButton(int y, string text, Action? onClick)
        {
            var btn = new Button(_spriteBatch, _textRenderer, 960, y, 700, 120, "button", text, Color4.White)
            {
                Layer = 2,
                TextColor = Color4.White,
                TextOffset = new Vector2(-10f, -50f),
                TextScale = 0.7f,
                ScaleMultiply = 0.8f
            };

            if (onClick != null)
                btn.OnClick += onClick;

            return btn;
        }

        private void OnLoginButtonClicked()
        {
            if (_loginInProgress || _isLoggedIn) return;

            _loginInProgress = true;
            _ = PerformLoginAsync();
        }

        private async Task PerformLoginAsync()
        {
            try
            {
                bool loginOk = await Auth.Login(_loginInput.Text, _passwordInput.Text).ConfigureAwait(false);
                if (!loginOk) return;

                bool fetchOk = await User.FetchCurrentUser().ConfigureAwait(false);
                if (fetchOk)
                {
                    SwitchToLoggedInState(User.UserName ?? "Unknown", User.Rating);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка логина: {ex.GetType().Name}: {ex.Message}");
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

            _ = PerformAutoLoginAsync();
        }

        private async Task PerformAutoLoginAsync()
        {
            try
            {
                if (await User.FetchCurrentUser().ConfigureAwait(false))
                {
                    SwitchToLoggedInState(User.UserName ?? "Unknown", User.Rating);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка автологина: {ex.Message}");
            }
        }

        private void SwitchToLoggedInState(string userName, int rating)
        {
            if (_isLoggedIn) return;
            _isLoggedIn = true;

            _userName = userName;
            _userRating = rating;

            Console.WriteLine($"=== УСПЕШНЫЙ ВХОД === {_userName} (рейтинг: {_userRating})");

            // Удаляем форму логина
            _scene.Remove(_loginInput);
            _scene.Remove(_passwordInput);
            _scene.Remove(_loginButton);

            // Приветствие
            _welcomeText = new TextObject(_textRenderer,
                $"{_userName}",
                250, 460, 0.5f)
            {
                Align = TextRender.TextAlign.Left,
                Color = Color4.White,
                Layer = 3
            };
            var ranting = new TextObject(_textRenderer, $"Ranking MMR: {_userRating}", 250, 540, 0.35f)
            {
                Align = TextRender.TextAlign.Left,
                Color = Color4.White,
                Layer = 3
            };
            _scene.Add(_welcomeText);
            _scene.Add(ranting);

            // Запуск загрузки аватарки
            if (!string.IsNullOrEmpty(User.AvatarPath))
            {
                _avatarUrl = User.AvatarPath;
                StartAvatarLoading();
            }
        }

        private void StartAvatarLoading()
        {
            if (_avatarLoadTask != null || string.IsNullOrEmpty(_avatarUrl)) return;

            _avatarLoadRequested = true;
            _avatarLoadTask = LoadAvatarDataAsync(_avatarUrl);

            Console.WriteLine($"Запущена фоновая загрузка аватарки: {_avatarUrl}");
        }

        // Только скачивание + декодирование (background thread)
        private static async Task<AvatarLoadResult> LoadAvatarDataAsync(string relativeUrl)
        {
            try
            {
                string baseUrl = "https://localhost:7068/"; // вынеси в конфиг/Auth если нужно
                string fullUrl = baseUrl.TrimEnd('/') + relativeUrl;

                using var client = new HttpClient();
                byte[] imageBytes = await client.GetByteArrayAsync(fullUrl).ConfigureAwait(false);

                ImageResult image = ImageResult.FromMemory(imageBytes, ColorComponents.RedGreenBlueAlpha);

                if (image == null)
                    return AvatarLoadResult.Failed("Не удалось декодировать изображение");

                return new AvatarLoadResult(image.Data, image.Width, image.Height);
            }
            catch (Exception ex)
            {
                return AvatarLoadResult.Failed($"Ошибка загрузки аватарки: {ex.Message}");
            }
        }

        // Создание текстуры ТОЛЬКО на главном потоке
        private int CreateTextureFromImageData(byte[] data, int width, int height)
        {
            int textureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, textureId);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba,
                          width, height, 0,
                          PixelFormat.Rgba, PixelType.UnsignedByte, data);

            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.LinearMipmapLinear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

            GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
            GL.BindTexture(TextureTarget.Texture2D, 0);

            Console.WriteLine($"Текстура аватарки создана ({width}x{height}), ID = {textureId}");
            return textureId;
        }

        public void Update(double deltaTime)
        {
            // Переключение в залогиненное состояние (из асинхронного кода)
            // (если используешь pending-флаги — оставь, но в моём варианте SwitchToLoggedInState вызывается напрямую)

            // Обработка завершившейся загрузки аватарки
            if (_avatarLoadTask?.IsCompleted == true)
            {
                var result = _avatarLoadTask.Result;
                _avatarLoadTask = null;

                if (result.Success && result.Data != null)
                {
                    int texId = CreateTextureFromImageData(result.Data, result.Width, result.Height);
                    var accBg = new SpriteObject(_spriteBatch, 0, 360, 540, 520, 150) { Color = new Color4(1f,1f,1f,0.5f)};
                    _avatarSprite = new SpriteObject(_spriteBatch, texId, 180, 540, 130, 130)
                    {
                        Layer = 3
                    };

                    _scene.Add(accBg);
                    _scene.Add(_avatarSprite);
                    Console.WriteLine("✅ Аватарка успешно добавлена на экран!");
                }
                else
                {
                    Console.WriteLine(result.ErrorMessage ?? "Неизвестная ошибка загрузки аватарки");
                }
            }

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
            Console.WriteLine("Выход из MenuState");
        }

        public void HandleKeyDown(KeyboardKeyEventArgs e) { }

        private void StartGame() => _game.ChangeState(new SongSelectState(_game, _spriteBatch, _textRenderer, _audio));
        private void GoEdit() => _game.ChangeState(new EditState(_game, _spriteBatch, _textRenderer, _audio));
        private void ExitGame() => _game.Close();

        // Вспомогательный record для результата загрузки аватарки
        private record AvatarLoadResult
        {
            public bool Success { get; }
            public byte[]? Data { get; }
            public int Width { get; }
            public int Height { get; }
            public string? ErrorMessage { get; }

            public AvatarLoadResult(byte[] data, int width, int height)
            {
                Success = true;
                Data = data;
                Width = width;
                Height = height;
            }

            private AvatarLoadResult(string error)
            {
                Success = false;
                ErrorMessage = error;
            }

            public static AvatarLoadResult Failed(string message) => new(message);
        }
    }
}