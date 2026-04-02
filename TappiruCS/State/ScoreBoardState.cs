using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Reflection;
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

        private int _scoreListTexture;
        private int _blackTexture;


        private SpriteObject _scoreList;
        private SpriteObject _black;

        private TextObject _scoreText;
        private TextObject _dateText;
        private TextObject _accuraciText;
        private TextObject _maxCombo;
        private TextObject _maxComboX; //крестик у комбо
        private TextObject _completeChar;
        private TextObject _completePhase;
        private TextObject _failChar;


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

            _scoreListTexture = TextureManager.GetTexture("scoreList");
            _blackTexture = TextureManager.GetTexture("black");
            var background = new SpriteObject(_spriteBatch, _playerScore.textureBG, 0, 0, Game.WindowWidth, Game.WindowHeight);
            _scoreList = new SpriteObject(_spriteBatch, _scoreListTexture, 0, 120, 640f, 500f) { ScaleMultiply = 1.1f};
            _black = new SpriteObject(_spriteBatch, _blackTexture, -100, -20, Game.WindowWidth+200, 120);

            _scoreText = new TextObject(_textRenderer, _playerScore._score.ToString("00000000000"), _scoreList.Position.X + 20, _scoreList.Position.Y + 20, 0.7f) 
            { Align =TextAlign.Left};
            _accuraciText = new TextObject(_textRenderer, _playerScore._accuraci.ToString("F2")+"%", _scoreList.Position.X + 440, _scoreList.Position.Y + 515, 0.5f)
            { Align = TextAlign.Left };
            _dateText = new TextObject(_textRenderer, _playerScore.PlayedAt.ToString(), 20, 20, 0.7f)
            { Align = TextAlign.Left };
            _maxCombo = new TextObject(_textRenderer, _playerScore._maxCobmo.ToString(), _scoreList.Position.X + 190, _scoreList.Position.Y + 515, 0.5f)
            { Align = TextAlign.Right };
            _maxComboX = new TextObject(_textRenderer, "x", _maxCombo.Position.X +40, _maxCombo.Position.Y+10 , 0.3f)
            { Align = TextAlign.Left };
            _completeChar = new TextObject(_textRenderer, _playerScore._completeChar.ToString(), _scoreList.Position.X + 100, _scoreList.Position.Y + 160, 0.5f)
            { Align = TextAlign.Left };
            _completePhase = new TextObject(_textRenderer, _playerScore._completePhase.ToString(), _scoreList.Position.X + 400, _scoreList.Position.Y + 160, 0.5f)
            { Align = TextAlign.Left };
            _failChar = new TextObject(_textRenderer, _playerScore._failChar.ToString(), _scoreList.Position.X + 100, _scoreList.Position.Y + 260, 0.5f)
            { Align = TextAlign.Left };


            _scene.Add(background);
            _scene.Add(_scoreList);
            _scene.Add(_black);

            _scene.Add(_scoreText);
            _scene.Add(_dateText);
            _scene.Add(_accuraciText);
            _scene.Add(_maxCombo);
            _scene.Add(_maxComboX);
            _scene.Add(_completeChar);
            _scene.Add(_completePhase);
            _scene.Add(_failChar);

        }



        public void OnExit()
        {
            _scene.Clear();
            Console.WriteLine("Вы вышли из ScoreBoard");
        }

        public void Update(double currentTime)
        {
            

            var mouseState = _game.MouseState;
            _scene.Update(currentTime, mouseState, _game);

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