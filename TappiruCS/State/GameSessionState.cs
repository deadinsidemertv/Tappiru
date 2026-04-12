using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System.Text.Json;
using TappiruCS.Core;
using TappiruCS.GameLogic;
using TappiruCS.Render;
using TappiruCS.Server.Player;
using TappiruCS.UI;
using static TappiruCS.Render.TextRender;

namespace TappiruCS.State
{
    public class GameSessionState : IGameState
    {
        #region Injected Dependencies

        private readonly Game _game;
        private readonly SpriteBatch _spriteBatch;
        private readonly TextRender _textRenderer;
        private readonly AudioManager _audio;

        #endregion

        #region Game Data

        public GameSession session;
        public MapData _mapData;

        #endregion

        #region Textures and Visuals

        public int background;
        public int scoreBarBGTex;

        private readonly Scene _scene = new Scene();
        public Background bg;
        public Background Fade;
        public SpriteObject scorebarBG;

        #endregion

        #region UI Text Elements

        public TextObject score;
        public TextObject Accuraci;
        public TextObject combo;
        public TextObject comboApof;

        #endregion

        #region Display State

        private float _displayedScore;
        private float _displayedAccuraci;

        private readonly HashSet<Keys> _pressedKeys = new HashSet<Keys>();

        #endregion

        #region Static Input Mapping

        private static readonly Dictionary<Keys, char[]> KeyToCharsMap = new Dictionary<Keys, char[]>
        {
            { Keys.A,      new char[] { 'a', 'ф' } },
            { Keys.B,      new char[] { 'b', 'и' } },
            { Keys.C,      new char[] { 'c', 'с' } },
            { Keys.D,      new char[] { 'd', 'в' } },
            { Keys.E,      new char[] { 'e', 'у' } },
            { Keys.F,      new char[] { 'f', 'а' } },
            { Keys.G,      new char[] { 'g', 'п' } },
            { Keys.H,      new char[] { 'h', 'р' } },
            { Keys.I,      new char[] { 'i', 'ш' } },
            { Keys.J,      new char[] { 'j', 'о' } },
            { Keys.K,      new char[] { 'k', 'л' } },
            { Keys.L,      new char[] { 'l', 'д' } },
            { Keys.M,      new char[] { 'm', 'ь' } },
            { Keys.N,      new char[] { 'n', 'т' } },
            { Keys.O,      new char[] { 'o', 'щ' } },
            { Keys.P,      new char[] { 'p', 'з' } },
            { Keys.Q,      new char[] { 'q', 'й' } },
            { Keys.R,      new char[] { 'r', 'к' } },
            { Keys.S,      new char[] { 's', 'ы' } },
            { Keys.T,      new char[] { 't', 'е' } },
            { Keys.U,      new char[] { 'u', 'г' } },
            { Keys.V,      new char[] { 'v', 'м' } },
            { Keys.W,      new char[] { 'w', 'ц' } },
            { Keys.X,      new char[] { 'x', 'ч' } },
            { Keys.Y,      new char[] { 'y', 'н' } },
            { Keys.Z,      new char[] { 'z', 'я' } },
            { Keys.LeftBracket,  new char[] { '[', 'х' } },
            { Keys.RightBracket, new char[] { ']', 'ъ' } },
            { Keys.Semicolon,    new char[] { ';', 'ж' } },
            { Keys.Apostrophe,   new char[] { '\'', 'э' } },
            { Keys.Comma,        new char[] { ',', 'б' } },
            { Keys.Period,       new char[] { '.', 'ю' } },
            { Keys.GraveAccent,  new char[] { '`', 'ё' } },
            { Keys.Space,        new char[] { ' ' } },
        };

        #endregion

        #region Constructor

        public GameSessionState(Game game, SpriteBatch spriteBatch, TextRender textRenderer, AudioManager audio, MapData mapdata)
        {
            _game = game;
            _spriteBatch = spriteBatch;
            _textRenderer = textRenderer;
            _audio = audio;
            _mapData = mapdata;
        }

        #endregion

        #region State Lifecycle

        public void OnEnter()
        {
            Console.WriteLine("Запуск уровня");
            GameSession.InitCharToKeyMap(KeyToCharsMap);

            session = new GameSession(_mapData);
            Console.WriteLine(_mapData.audioPath.ToString());

            if (_audio == null)
                Console.WriteLine("_audio РОВНО NULL CYKA");

            background = TextureLoader.Load(_mapData.backGroundPath);
            scoreBarBGTex = TextureManager.GetTexture("scorebg");

            bg = new Background(_spriteBatch, background, _game);
            Fade = new Background(_spriteBatch, 0, _game) { Opacity = 0.7f };
            scorebarBG = new SpriteObject(_spriteBatch, scoreBarBGTex, 960, 540, 1920, 1080) { AllowHover = false };

            _audio.LoadMusic(_mapData.audioPath);
            _audio.Play();

            InitializeUIElements();

            _scene.Add(bg);
            _scene.Add(Fade);
            _scene.Add(scorebarBG);
            _scene.Add(Accuraci);
            _scene.Add(score);
            _scene.Add(combo);
            _scene.Add(comboApof);
        }

        public void OnExit()
        {
            _audio.Stop();
            _scene.Clear();
            Console.WriteLine("Вы вышли с мапы");
        }

        #endregion

        #region Update and Render

        public void Update(double currentTime)
        {
            var mouse = _game.MouseState;
            _scene.Update(currentTime, mouse, _game);

            if (session == null)
                return;

            float cTime = _audio?.GetCurrentTime() ?? 0f;
            session.Update(cTime, _game.KeyboardState);

            CheckForMapCompletion(cTime);
            UpdateComboPosition();
            UpdateDisplayedValues(currentTime);
            UpdateUIText();
        }

        public void Render(Matrix4 projection)
        {
            _scene.Draw(projection);
            InputCharDraw(session, projection, 960, 440);
        }

        #endregion

        #region Input Handling

        public void HandleKeyDown(KeyboardKeyEventArgs e)
        {
            if (session == null || !session.IsActivePhase)
                return;

            Keys key = e.Key;

            if (_pressedKeys.Contains(key))
                return;                    // ← это важно

            if (!KeyToCharsMap.TryGetValue(key, out char[] possibleChars))
                return;

            int currentIndex = session.CurrentCharIndex;
            if (currentIndex >= session.CurrentPhaseChars.Length)
                return;

            char expectedChar = session.CurrentPhaseChars[currentIndex];

            if (Array.IndexOf(possibleChars, expectedChar) >= 0)
                session.HandleInput(expectedChar, _audio.GetCurrentTime());
            else
                session.HandleInput('\0', _audio.GetCurrentTime());

            _pressedKeys.Add(key);
        }

        public void HandleKeyUp(KeyboardKeyEventArgs e)
        {
            _pressedKeys.Remove(e.Key);
        }

        #endregion

        #region Private Helper Methods

        private void InitializeUIElements()
        {
            score = new TextObject(_textRenderer, session.TotalScore.ToString(), 1900, 0, 0.35f)
            {
                Color = Color4.White,
                Align = TextAlign.Right
            };

            Accuraci = new TextObject(_textRenderer, session.Accuracy.ToString(), 1840, score.Position.Y + 40, 0.3f)
            {
                Color = Color4.White,
                Align = TextAlign.Center
            };

            combo = new TextObject(_textRenderer, session.Combo.ToString(), 70, 900, 0.7f)
            {
                Align = TextAlign.Left
            };

            comboApof = new TextObject(_textRenderer, "x", combo.Position.X - 15, combo.Position.Y + 15, 0.4f);
        }

        private void CheckForMapCompletion(float currentAudioTime)
        {
            if (currentAudioTime < session.endTime)
                return;

            var newScore = new PlayerScore
            {
                MapHash = session.CurrentMap.MapHash,
                _score = session.TotalScore,
                _accuraci = session.Accuracy,
                _maxCobmo = session.MaxCombo,
                _completePhase = session.CompletedPhases,
                _failPhase = session.FailedPhases,
                _completeChar = session.CorrectHits,
                _failChar = session.Misses,
                PlayedAt = DateTime.Now,
            };

            newScore.PlayerName = PlayerProfile.Instance.IsLoggedIn
                ? " " + PlayerProfile.Instance.UserName
                : "offline-mode";

            ScoreManager.AddScore(newScore);
            _audio.Stop();

            _game.ChangeState(new ScoreBoardState(_game, _spriteBatch, _textRenderer, _audio, newScore, _mapData));
        }

        private void UpdateComboPosition()
        {
            comboApof.Position = new Vector2(combo.Position.X - 15, combo.Position.Y + 15);
        }

        private void UpdateDisplayedValues(double currentTime)
        {
            const float lerpSpeed = 25.0f;

            _displayedScore = MathHelper.Lerp(_displayedScore, session.TotalScore, lerpSpeed * (float)currentTime);
            _displayedAccuraci = MathHelper.Lerp(_displayedAccuraci, session.Accuracy, lerpSpeed * (float)currentTime);
        }

        private void UpdateUIText()
        {
            int intScore = (int)Math.Round(_displayedScore);
            score.Text = intScore.ToString("D9");

            Accuraci.Text = (Math.Round(_displayedAccuraci * 100f) / 100f).ToString() + "%";
            combo.Text = session.Combo.ToString();
        }

        private void InputCharDraw(GameSession session, Matrix4 projection, float centerX, float y)
        {
            if (session.CurrentPhaseChars == null || session.CurrentPhaseChars.Length == 0)
                return;

            string text = new string(session.CurrentPhaseChars);
            float maxPixelWidth = _game.ClientSize.X * 0.85f;

            float bestScale = 1.8f;
            for (float testScale = 1.8f; testScale >= 0.5f; testScale -= 0.02f)
            {
                float estimatedWidth = _textRenderer.CalculateTextWidth(text, testScale * Scene.CanvasScale.X);
                if (estimatedWidth <= maxPixelWidth)
                {
                    bestScale = testScale;
                    break;
                }
            }

            Color4[] colors = new Color4[text.Length];
            double currentTime = _audio?.GetCurrentTime() ?? 0.0;

            for (int i = 0; i < text.Length; i++)
            {
                bool isSlider = session.CurrentSliders?.ContainsKey(i) ?? false;
                bool isHoldingThis = session.IsHoldingSlider && session.CurrentSliderCharIndex == i;
                bool isSuccessfullyHeld = session.SuccessfullyHeldSliders.Contains(i);      // зелёный после endTime
                bool isSuccessfullyCompleted = session.SuccessfullyCompletedSliders.Contains(i);

                if (session.PhaseComplete || isSuccessfullyCompleted)
                {
                    // Фраза полностью завершена — голубой
                    colors[i] = new Color4(_mapData.completeR, _mapData.completeG, _mapData.completeB, 1f);
                }
                else if (isSuccessfullyHeld)
                {
                    // Слайдер прошёл по времени (endTime достигнут) — ЗЕЛЁНЫЙ
                    // Даже если клавиша ещё зажата
                    colors[i] = new Color4(0.1f, 1.0f, 0.3f, 1f);
                }
                else if (isHoldingThis)
                {
                    // Сейчас держим слайдер, но endTime ещё не наступил — синий/голубой holding
                    colors[i] = new Color4(0.2f, 0.85f, 1.0f, 1f);
                }
                else if (i < session.CurrentCharIndex)
                {
                    // Уже успешно оттапано (обычный тап)
                    colors[i] = new Color4(_mapData.tappedR, _mapData.tappedG, _mapData.tappedB, 1f);
                }
                else if (isSlider)
                {
                    // Слайдер ещё не начат — показываем активное окно
                    if (session.CurrentSliders.TryGetValue(i, out var slider))
                    {
                        bool isTimeForSlider = currentTime >= slider.startTime - 0.5 && currentTime <= slider.endTime + 0.3;
                        colors[i] = isTimeForSlider
                            ? new Color4(1.0f, 0.4f, 0.0f, 1f)   // оранжевый/красный — пора жать
                            : new Color4(1.0f, 1.0f, 1.0f, 1f);
                    }
                }
                else if (i == session.CurrentCharIndex)
                {
                    // Обычный текущий символ
                    colors[i] = new Color4(_mapData.needR, _mapData.needG, _mapData.needB, 1f);
                }
                else
                {
                    colors[i] = new Color4(1f, 1f, 1f, 1f);
                }
            }

            _textRenderer.DrawStringWithCharColorsScaled(
                text, centerX, y, Scene.CanvasScale, bestScale, 1.0f, colors, projection, TextAlign.Center
            );
        }

        #endregion
    }
}