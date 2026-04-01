using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using TappiruCS.Core;
using TappiruCS.GameLogic;
using TappiruCS.Render;
using TappiruCS.UI;
using static TappiruCS.Render.TextRender;

namespace TappiruCS.State
{
    public class ScoreBoardState : IGameState
    {
        private readonly Game _game;
        private readonly SpriteBatch _spriteBatch;
        private readonly TextRender _textRenderer;
        private readonly AudioManager _audio;
        private readonly PlayerScore _playerScore;

        private readonly Scene _scene = new Scene();
        private TextObject _rankText;
        private TextObject _exitButton;
        private TextObject _replayButton;
        private string _grade;

        public ScoreBoardState(Game game, SpriteBatch spriteBatch, TextRender textRenderer, AudioManager audio, PlayerScore playerscore)
        {
            _game = game;
            _spriteBatch = spriteBatch;
            _textRenderer = textRenderer;
            _audio = audio;
            _playerScore = playerscore;
        }

        public void OnEnter()
        {
            Console.WriteLine("Открылся ScoreBoard");
            PrintResultsToConsole();


        }

       

        public void OnExit()
        {
            _scene.Clear();
            Console.WriteLine("Вы вышли из ScoreBoard");
        }

        public void Update(double currentTime)
        {
            _scene.Update(currentTime);

            var mouseState = _game.MouseState;
            
        }

       

        public void Render(Matrix4 projection)
        {
            
            
            _scene.Draw(projection);
        }

        public void HandleKeyDown(KeyboardKeyEventArgs e)
        {
            if (e.Key == Keys.Escape)
            {
                _game.ChangeState(new SongSelectState(_game, _spriteBatch, _textRenderer, _audio));
            }
        }

       

        private void PrintResultsToConsole()
        {
            Console.WriteLine("===== РЕЗУЛЬТАТЫ КАРТЫ =====");
            Console.WriteLine($"Score: {_playerScore._score}");
            Console.WriteLine($"Max Combo: {_playerScore._maxCobmo}");
            Console.WriteLine($"Correct: {_playerScore._completeChar}");
            Console.WriteLine($"Misses: {_playerScore._failChar}");
            Console.WriteLine($"Accuracy: {_playerScore._accuraci:F2}%");
            Console.WriteLine($"Completed Phases: {_playerScore._completePhase}");
            Console.WriteLine($"Failed Phases: {_playerScore._failPhase}");
            Console.WriteLine("============================");
        }
    }
}