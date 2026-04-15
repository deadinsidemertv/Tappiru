// State/GameSessionState.cs
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core;
using TappiruCS.GameLogic;
using TappiruCS.Render;
using TappiruCS.Server.Player;
using TappiruCS.UI;

namespace TappiruCS.State
{
    public class GameSessionState : IGameState
    {
        // === Зависимости ===
        private readonly Game _game;
        private readonly SpriteBatch _spriteBatch;
        private readonly TextRender _textRenderer;
        private readonly AudioManager _audio;

        // === Данные игры ===
        public GameSession session;
        public MapData _mapData;

        // === Визуальные объекты ===
        private readonly Scene _scene = new Scene();
        public Background bg;
        public Background Fade;
        public SpriteObject scorebarBG;

        private readonly PhraseDisplayRenderer _phraseRenderer;     // ← вынесен
        private ScoreBarUI _scoreBarUI;                    // ← вынесен (по желанию)

        // === Состояние ввода ===
        private readonly HashSet<Keys> _pressedKeys = new HashSet<Keys>();

        public GameSessionState(Game game, SpriteBatch spriteBatch, TextRender textRenderer, AudioManager audio, MapData mapdata)
        {
            _game = game;
            _spriteBatch = spriteBatch;
            _textRenderer = textRenderer;
            _audio = audio;
            _mapData = mapdata;

            _phraseRenderer = new PhraseDisplayRenderer(spriteBatch, textRenderer, game, mapdata, audio);
            _scoreBarUI = new ScoreBarUI(textRenderer); // session передадим позже
        }

        public void OnEnter()
        {
            Console.WriteLine("Запуск уровня");
            InputMapping.Initialize();

            session = new GameSession(_mapData);

            // Загрузка текстур и создание фона
            int backgroundTex = TextureLoader.Load(_mapData.backGroundPath);
            int scoreBarTex = TextureManager.GetTexture("scorebg");

            bg = new Background(_spriteBatch, backgroundTex, _game)
            {
                AutoBreathingParallax = true,
                BreathingSpeed = 0.65f,
                BreathingStrength = 12f
            };

            Fade = new Background(_spriteBatch, 0, _game) { Opacity = 0.7f };
            scorebarBG = new SpriteObject(_spriteBatch, scoreBarTex, 960, 540, 1920, 1080) { AllowHover = false };

            _scoreBarUI = new ScoreBarUI(_textRenderer);   // передаём session
            _scoreBarUI.AddToScene(_scene);

            _scene.Add(bg);
            _scene.Add(Fade);
            _scene.Add(scorebarBG);

            _audio.LoadMusic(_mapData.audioPath);
            _audio.Play();
        }

        public void OnExit()
        {
            _audio.Stop();
            _scene.Clear();
        }

        public void Update(double currentTime)
        {
            _scene.Update(currentTime, _game.MouseState, _game);

            if (session == null) return;

            float audioTime = _audio?.GetCurrentTime() ?? 0f;
            session.Update(audioTime, _game.KeyboardState);

            CheckForMapCompletion(audioTime);
            _scoreBarUI.Update(session, currentTime);
        }

        public void Render(Matrix4 projection)
        {
            _scene.Draw(projection);
            _phraseRenderer.Draw(session, projection, 960, 440);
        }

        public void HandleKeyDown(KeyboardKeyEventArgs e)
        {
            if (session == null) return;

            double currentTime = _audio?.GetCurrentTime() ?? 0.0;

            if (!session.IsInputAllowed(currentTime)) return;
            if (_pressedKeys.Contains(e.Key)) return;

            if (!InputMapping.KeyToCharsMap.TryGetValue(e.Key, out char[] possibleChars))
                return;

            int idx = session.CurrentCharIndex;
            if (idx >= session.CurrentPhaseChars.Length) return;

            char expected = session.CurrentPhaseChars[idx];

            if (Array.IndexOf(possibleChars, expected) >= 0)
                session.HandleInput(expected, currentTime);
            else
                session.HandleInput('\0', currentTime);

            _pressedKeys.Add(e.Key);
        }

        public void HandleKeyUp(KeyboardKeyEventArgs e)
        {
            _pressedKeys.Remove(e.Key);
        }

        private void CheckForMapCompletion(float currentAudioTime)
        {
            if (currentAudioTime < session.endTime) return;

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
                PlayerName = PlayerProfile.Instance.IsLoggedIn ? " " + PlayerProfile.Instance.UserName : "offline-mode"
            };

            ScoreManager.AddScore(newScore);
            _audio.Stop();

            _game.ChangeState(new ScoreBoardState(_game, _spriteBatch, _textRenderer, _audio, newScore, _mapData));
        }
    }
}