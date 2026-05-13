using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core;
using TappiruCS.Core.GameObject;
using TappiruCS.GameLogic;
using TappiruCS.Render;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;
using TappiruCS.Render.Text;
using TappiruCS.State.SongSelector;

namespace TappiruCS.State
{
    public class ScoreBoardState : IGameState
    {
        private readonly RenderContext _context;
        private readonly PlayerScore _playerScore;
        private readonly MapData _mapData;

        private readonly Scene _scene = new Scene();

        private SpriteObject _scoreList;
        private SpriteObject _topBlack;

        // Тексты статистики (дети панели)
        private TextObject _scoreText;
        private TextObject _accuraciText;
        private TextObject _maxCombo;
        private TextObject _maxComboX;
        private TextObject _completePhase;   // 300
        private TextObject _completeChar;    // 100
        private TextObject _failChar;        // miss

        // Хиты — тоже дети панели, с простыми локальными смещениями
        private SpriteObject _hit300Sprite;
        private SpriteObject _hit100Sprite;
        private SpriteObject _hit0Sprite;

        private SpriteObject _gradeSprite;

        // Тексты сверху (вне панели)
        private TextObject _title;
        private TextObject _creator;
        private TextObject _dateText;

        public ScoreBoardState(RenderContext context, PlayerScore playerScore, MapData mapData)
        {
            _context = context;
            _playerScore = playerScore;
            _mapData = mapData;
        }

        public void OnEnter()
        {
            _scene.Initialize(_context);

            // Фон
            var background = new Background(TextureLoader.Load(_mapData.backGroundPath)) { ParalaxEffect = true };
            var backgroundOpacity = new Background(0) { Opacity = 0.5f };

            _scoreList = new SpriteObject(TextureManager.GetTexture("ranking-panel"), 980, 600, 1400, 667)
            {
                ScaleMultiply = 1.4f
            };

            _topBlack = new SpriteObject(0, 960, 45, 1920, 210)
            {
                Color = new Color4(0f, 0f, 0f, 0.7f),
                Opacity = 0.6f
            };

            CreateAllTexts();
            CreateHitSprites();
            CreateGradeSprite();

            // Добавляем в сцену только главные объекты
            _scene.Add(background);
            _scene.Add(backgroundOpacity);
            _scene.Add(_topBlack);
            _scene.Add(_scoreList);

            _scene.Add(_title);
            _scene.Add(_creator);
            _scene.Add(_dateText);

            // Добавляем всё как детей панели
            AddChildrenToScoreList();
        }

        private void CreateAllTexts()
        {
            _scoreText = new TextObject(_playerScore._score.ToString("00000000000"), -500, -360, 96f)
            {
                Align = TextAlign.Center
            };
            
            _accuraciText = new TextObject(_playerScore._accuraci.ToString("F2") + "%", -550, 180, 64f)
            {
                Align = TextAlign.Left
            };

            _maxCombo = new TextObject(_playerScore._maxCobmo.ToString(), -880, 180, 64f)
            {
                Align = TextAlign.Right
            };

            _maxComboX = new TextObject("x", 15, 5, 64f)          // небольшое смещение от цифры комбо
            {
                Align = TextAlign.Left
            };

            _completePhase = new TextObject(_playerScore._completePhase.ToString(), -745, -230, 64f)
            {
                Align = TextAlign.Left
            };

            _completeChar = new TextObject(_playerScore._completeChar.ToString(), -745, -90, 64f)
            {
                Align = TextAlign.Left
            };

            _failChar = new TextObject(_playerScore._failChar.ToString(), -745, 45, 64f)
            {
                Align = TextAlign.Left
            };

            _title = new TextObject(_mapData.title + $" - [{_mapData.artist}]", 0, 30, 72f)
            {
                Align = TextAlign.Left
            };

            _creator = new TextObject("Автор: " + _mapData.creator, 5, 90, 48f)
            {
                Align = TextAlign.Left
            };

            _dateText = new TextObject($"Played at {_playerScore.PlayerName} {_playerScore.PlayedAt}", 5, 135, 36f)
            {
                Align = TextAlign.Left
            };
        }

        private void CreateHitSprites()
        {
            // Простые локальные смещения относительно панели _scoreList
            // Хиты будут стоять слева от своих цифр
            _hit300Sprite = new SpriteObject(TextureManager.GetTexture("hit300"), -890, -240, 75, 75);
            _hit100Sprite = new SpriteObject(TextureManager.GetTexture("hit100"), -890, -110, 75, 75);
            _hit0Sprite = new SpriteObject(TextureManager.GetTexture("hit0"), -890, 20, 80, 80);
        }

        private void CreateGradeSprite()
        {
            int[] gradetx = new int[6];
            for (int i = 0; i < 6; i++)
                gradetx[i] = TextureManager.GetTexture("grade" + i);

            float acc = _playerScore._accuraci ;
            int fails = _playerScore._failChar;

            int rank;

            if (acc >= 100f)
                rank = 5;  // SS
            else if (acc >= 90f)
                rank = 4;  // A (я сделал >= 90, а не >90)
            else if (acc >= 80f)
                rank = 3;  // B
            else if (acc >= 70f)
                rank = 2;  // C
            else if (acc >= 60f)
                rank = 1;  // D
            else
                rank = 0;  // F

            _gradeSprite = new SpriteObject(gradetx[rank], 1600, 440, 80, 100)
            {
                ScaleMultiply = 8f
            };

            // ========== ОТЛАДКА ==========
            Console.WriteLine($"[GRADE] Accuracy: {acc:F2}% | Fails: {fails} | Rank: {rank} | Texture: grade{rank}");
        }

        private void AddChildrenToScoreList()
        {
            _scoreList.AddChild(_scoreText);
            _scoreList.AddChild(_accuraciText);
            _scoreList.AddChild(_maxCombo);
            _scoreList.AddChild(_maxComboX);
            _scoreList.AddChild(_completePhase);
            _scoreList.AddChild(_completeChar);
            _scoreList.AddChild(_failChar);

            // Хиты как дети панели с фиксированными локальными смещениями
            _scoreList.AddChild(_hit300Sprite);
            _scoreList.AddChild(_hit100Sprite);
            _scoreList.AddChild(_hit0Sprite);

            // Грейд оставляем отдельно (он обычно в стороне от панели)
            _scene.Add(_gradeSprite);


            _scene.Add(_scoreList);
            
        }

        public void OnExit()
        {
            _scene.Clear();
            Console.WriteLine("Вы вышли из ScoreBoard");
        }

        public void Update(double currentTime)
        {
            var mouseState = _context.Game.MouseState;
            _scene.Update(currentTime, mouseState, _context.Game);
        }

        public void Render(Matrix4 projection)
        {
            _scene.Draw(projection);
        }

        public void HandleKeyDown(KeyboardKeyEventArgs e)
        {
            if (e.Key == Keys.Escape)
            {
                _context.Game.ChangeState(new SongSelectState(_context));
            }
        }
    }
}