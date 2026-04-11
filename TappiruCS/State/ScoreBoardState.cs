using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
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
        private readonly MapData _mapData;

        private readonly Scene _scene = new Scene();

        private int _scoreListTexture;
        private int _blackTexture;

        private SpriteObject _scoreList;
        private SpriteObject _topBlack;
        


        private TextObject _scoreText;
        private TextObject _dateText;
        private TextObject _accuraciText;
        private TextObject _maxCombo;
        private TextObject _maxComboX; //крестик у комбо
        private TextObject _completeChar;
        private TextObject _completePhase;
        private TextObject _failChar;
        private TextObject title;
        private TextObject creator;



        public ScoreBoardState(Game game, SpriteBatch spriteBatch, TextRender textRenderer, AudioManager audio, PlayerScore playerscore,MapData mapdata)
        {
            _game = game;
            _spriteBatch = spriteBatch;
            _textRenderer = textRenderer;
            _audio = audio;
            _playerScore = playerscore;
            _mapData = mapdata;
        }

        public void OnEnter()
        {
            Console.WriteLine("Открылся ScoreBoard");
            PrintResultsToConsole();

            _scoreListTexture = TextureManager.GetTexture("ranking-panel");
            _blackTexture = TextureManager.GetTexture("black");
            var background = new Background(_spriteBatch, TextureLoader.Load(_mapData.backGroundPath), _game) {ParalaxEffect = true };
            var backgroundopacity = new Background(_spriteBatch, 0, _game) { Opacity = 0.5f };
            _scoreList = new SpriteObject(_spriteBatch, _scoreListTexture, 980, 600, 1400, 667) { ScaleMultiply = 1.4f};
            _topBlack = new SpriteObject(_spriteBatch, 0, 960, 45, 1920, 210) { Color = new Color4(0f, 0f, 0f, 0.7f),
                Opacity = 0.6f
            };


            _scoreText = new TextObject(_textRenderer, _playerScore._score.ToString("00000000000"), _scoreList.Position.X -500, _scoreList.Position.Y-450, 0.6f) 
            { Align =TextAlign.Center};
            _accuraciText = new TextObject(_textRenderer, _playerScore._accuraci.ToString("F2")+"%", _scoreList.Position.X  -550, _scoreList.Position.Y+120 , 0.5f)
            { Align = TextAlign.Left };

            title = new TextObject(_textRenderer, _mapData.title+$" - [{_mapData.artist}]", 0, 0, 0.5f) { Align = TextAlign.Left};

            creator = new TextObject(_textRenderer, "Автор: "+_mapData.creator, 5, 60, 0.35f) { Align = TextAlign.Left };

            _dateText = new TextObject(_textRenderer,"Played at "+_playerScore.PlayerName+" "+_playerScore.PlayedAt.ToString(), 5, 105, 0.2f){ Align = TextAlign.Left };


            _maxCombo = new TextObject(_textRenderer, _playerScore._maxCobmo.ToString(), _scoreList.Position.X -880, _scoreList.Position.Y + 120, 0.5f)
            { Align = TextAlign.Right };
            _maxComboX = new TextObject(_textRenderer, "x", _maxCombo.Position.X +10, _maxCombo.Position.Y+10 , 0.3f)
            { Align = TextAlign.Left };

            _completeChar = new TextObject(_textRenderer, _playerScore._completeChar.ToString(), _scoreList.Position.X -745, _scoreList.Position.Y -160, 0.5f)
            { Align = TextAlign.Left };
            int _hit100tx = TextureManager.GetTexture("hit100");
            var hit100 = new SpriteObject(_spriteBatch, _hit100tx, _completeChar.Position.X - 150, _completeChar.Position.Y +50, 75, 75);

            _completePhase = new TextObject(_textRenderer, _playerScore._completePhase.ToString(), _scoreList.Position.X -745 , _scoreList.Position.Y + -295, 0.5f)
            { Align = TextAlign.Left };
            int _hit300tx = TextureManager.GetTexture("hit300");
            var hit300 = new SpriteObject(_spriteBatch, _hit300tx, _completePhase.Position.X - 150, _completePhase.Position.Y + 50, 75, 75);

            _failChar = new TextObject(_textRenderer, _playerScore._failChar.ToString(), _scoreList.Position.X -745, _scoreList.Position.Y - 25, 0.5f)
            { Align = TextAlign.Left };
            int _hit0tx = TextureManager.GetTexture("hit0");
            var hit0 = new SpriteObject(_spriteBatch, _hit0tx, _failChar.Position.X - 145, _failChar.Position.Y+45, 100, 100);

            int[] gradetx = new int[6];
            for (int i = 0; i < 6; i++)
            {
                gradetx[i] = TextureManager.GetTexture("grade" + i);
            }

            float acc = _playerScore._accuraci;
            int failChars = _playerScore._failChar;
            int rank;

            if (acc == 100f && failChars == 0)          // 100% accuracy, ни одного промаха
                rank = 5;                                 // SS
            else if (acc > 90.0f && failChars == 0)       // >90% и нет промахов
                rank = 4;                                 // S
            else if ((acc > 80.0f && failChars == 0) || (acc > 90.0f))   // (>80% без промахов) ИЛИ (>90% с любыми промахами)
                rank = 3;                                 // A
            else if ((acc > 70.0f && failChars == 0) || (acc > 80.0f))   // (>70% без промахов) ИЛИ (>80% с любыми промахами)
                rank = 2;                                 // B
            else if (acc > 60.0f)                         // >60% (с любыми промахами)
                rank = 1;                                 // C
            else
                rank = 0;                                 // D (всё остальное)


            var _grade = new SpriteObject(_spriteBatch, gradetx[rank], 1600, 440, 80, 100) { ScaleMultiply = 8f};

            _scene.Add(background);
            _scene.Add(backgroundopacity);

            
            _scene.Add(_topBlack);

            _scene.Add(_scoreList);

            _scene.Add(_scoreText);
            _scene.Add(_accuraciText);
            _scene.Add(_maxCombo);
            _scene.Add(_maxComboX);
            _scene.Add(_completeChar);
            _scene.Add(_completePhase);
            _scene.Add(_failChar);
            _scene.Add(hit0);
            _scene.Add(hit300);
            _scene.Add(hit100);
            _scene.Add(_grade);

            _scene.Add(title);
            _scene.Add(creator);
            _scene.Add(_dateText);
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