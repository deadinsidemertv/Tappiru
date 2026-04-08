using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using TappiruCS.Core;
using TappiruCS.GameLogic;
using TappiruCS.Render;
using TappiruCS.State;
using TappiruCS.UI;
using static System.Collections.Specialized.BitVector32;
using static TappiruCS.Render.TextRender;

namespace TappiruCS
{

    public class GameSessionState : IGameState
    {
        private readonly Game _game;
        private readonly SpriteBatch _spriteBatch;
        private readonly TextRender _textRenderer;
        private readonly AudioManager _audio;

        

        public GameSession session;
        public MapData _mapData;

        public int background;

        private readonly Scene _scene = new Scene();
        public Background bg;
        public Background Fade;
        

        public TextObject score;
        public TextObject Accuraci;

        public TextObject combo;
        public TextObject comboApof;

        public GameSessionState(Game game, SpriteBatch spriteBatch, TextRender textRenderer, AudioManager audio,MapData mapdata)
        {
            _game = game;
            _spriteBatch = spriteBatch;
            _textRenderer = textRenderer;
            _audio = audio;
            _mapData = mapdata;
        }

        public static MapData MapLoad(string mapFolderPath)
        {

            MapData mapdata = new MapData();
            string[] bgP = Directory.GetFiles(mapFolderPath, "*.jpg");
            mapdata.backGroundPath = bgP[0];
            string[] audioP = Directory.GetFiles(mapFolderPath, "*.mp3");
            mapdata.audioPath = audioP[0];
            string[] dataP = Directory.GetFiles(mapFolderPath, "*.tapp");
            mapdata.dataPath = dataP[0];

            string json = File.ReadAllText(mapdata.dataPath);
            JsonMap tmp = JsonSerializer.Deserialize<JsonMap>(json);
            mapdata.Events = tmp.events;
            mapdata.endTime = tmp.endTime;
            mapdata.title = tmp.title;
            mapdata.creator = tmp.creator;
            mapdata.artist = tmp.artist;

            mapdata.tappedR = tmp.tappedR;
            mapdata.tappedG = tmp.tappedG;
            mapdata.tappedB = tmp.tappedB;

            mapdata.needR = tmp.needR;
            mapdata.needG = tmp.needG; 
            mapdata.needB = tmp.needB;

            mapdata.completeR = tmp.completeR;
            mapdata.completeG = tmp.completeG;
            mapdata.completeB = tmp.completeB;

            foreach (var ev in mapdata.Events)
    ev.text = ev.text.ToLowerInvariant();
            Console.WriteLine(mapdata.endTime + " endTime");
            return mapdata;
        }
        public void OnEnter()
        {
            Console.WriteLine("Запуск уровня");
            
            //_mapData = MapLoad(_songPath);

            session = new GameSession(_mapData);
            Console.WriteLine(_mapData.audioPath.ToString());
            if(_audio == null)
            {
                Console.WriteLine("_audio РОВНО NULL CYKA");
            }

            background = TextureLoader.Load(_mapData.backGroundPath);
            bg = new Background(_spriteBatch, background, _game);

            Fade = new Background(_spriteBatch, 0, _game) { Opacity = 0.7f };


            _audio.LoadMusic(_mapData.audioPath);
            _audio.Play();


            score = new TextObject(_textRenderer, session.TotalScore.ToString(), 1900, 20, 0.7f)
            {
                Color = Color4.White,
                Align = TextAlign.Right
            };
            Accuraci = new TextObject(_textRenderer, session.Accuracy.ToString(), 1900, score.Position.Y+100, 0.35f)
            {
                Color = Color4.White,
                Align = TextAlign.Right
            };
            combo = new TextObject(_textRenderer, session.Combo.ToString(), 70, 900, 0.7f) { Align = TextAlign.Left};
            comboApof = new TextObject(_textRenderer, "x", combo.Position.X-15, combo.Position.Y+15, 0.4f);

            _scene.Add(bg);
            _scene.Add(Fade);

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
        public void Update(double currentTime)
        {
            
            var mouse = _game.MouseState;
            _scene.Update(currentTime, mouse, _game);
            if (session != null)
            {
                float cTime = _audio?.GetCurrentTime() ?? 0f;
                session?.Update(cTime);        // ← передаём время в логику игры

                if (cTime >= session.endTime) 
                {
                    PlayerScore playerscore = new PlayerScore();
                    playerscore._accuraci = session.Accuracy;
                    playerscore._score = session.TotalScore;
                    playerscore._completeChar = session.CorrectHits;
                    playerscore._failChar = session.Misses;
                    playerscore._completePhase = session.CompletedPhases;
                    playerscore._failPhase = session.FailedPhases;
                    playerscore._maxCobmo = session.MaxCombo;
                    playerscore.textureBG = background;
                    playerscore.PlayedAt = DateTime.Now;
                    _audio.Stop();
                    
                    _game.ChangeState(new ScoreBoardState(_game, _spriteBatch, _textRenderer, _audio, playerscore,_mapData));
                }
                   



            }
            comboApof.Position = new Vector2(combo.Position.X - 15, combo.Position.Y + 15);

            score.Text = session.TotalScore.ToString("D9");
            
            Accuraci.Text = (Math.Round(session.Accuracy*100f)/100f).ToString()+"%";
            combo.Text = session.Combo.ToString();


        }
        public void Render(Matrix4 projection)
        {
            string phrase = session.CurrentPhaseText;
            int typed = session.CurrentCharIndex;

           
            bg = new Background(_spriteBatch, background, _game);

            bg = new Background(_spriteBatch, 0, _game) { Opacity = 0.5f};





            
            
            //ScoreDraw(session, projection, 1270, 20);
            _scene.Draw(projection);
            
            InputCharDraw(session, projection, 960, 440);
        }

        public void HandleKeyDown(KeyboardKeyEventArgs e)
        {
            if (session == null || !session._isActivePhase) return;

            Keys key = e.Key;
            if (!KeyToCharsMap.TryGetValue(key, out char[] possibleChars))
                return; // клавиша не участвует в игре (например, F1)

            int currentIndex = session.CurrentCharIndex;
            if (currentIndex >= session.CurrentPhaseChars.Length) return;

            char expectedChar = session.CurrentPhaseChars[currentIndex];

            // Если ожидаемый символ есть среди допустимых для этой клавиши
            if (Array.IndexOf(possibleChars, expectedChar) >= 0)
            {
                session.HandleInput(expectedChar); // правильное нажатие
            }
            else
            {
                // Неправильное нажатие – передаём любой символ, который не совпадёт
                session.HandleInput('\0');
            }
        }








        private void InputCharDraw(GameSession session, Matrix4 projection, float centerX, float y)
        {
            if (session.CurrentPhaseChars == null || session.CurrentPhaseChars.Length == 0)
                return;

            string text = new string(session.CurrentPhaseChars);

            float maxPixelWidth = _game.ClientSize.X * 0.85f;

            // Автоскейл
            float bestScale = 1.8f;
            for (float testScale = 1.8f; testScale >= 0.5f; testScale -= 0.02f)
            {
                float estimatedWidth = _textRenderer.CalculateTextWidth(text, testScale * _scene.CanvasScale.X); // используем текущий CanvasScale
                if (estimatedWidth <= maxPixelWidth)
                {
                    bestScale = testScale;
                    break;
                }
            }

            // Цвета
            Color4[] colors = new Color4[text.Length];
            for (int i = 0; i < text.Length; i++)
            {
                if (session.PhaseComplete)
                    colors[i] = new Color4(_mapData.completeR, _mapData.completeG, _mapData.completeB, 1f);      // тёмный фиолет (завершённая строка)
                else if (i < session.CurrentCharIndex)
                    colors[i] = new Color4(_mapData.tappedR, _mapData.tappedG, _mapData.tappedB, 1f);      // тусклый фиолетово-синий (набранные)
                else if (i == session.CurrentCharIndex)
                    colors[i] = new Color4(_mapData.needR, _mapData.needG, _mapData.needB, 1f);      // бледно-фиолетовый (текущий символ)
                else
                    colors[i] = new Color4(1f, 1f, 1f, 1f);      
            }

            // Главный вызов — передаём актуальный CanvasScale
            _textRenderer.DrawStringWithCharColorsScaled(
                text,
                centerX,
                y,
                _scene.CanvasScale,      // ← вот сюда передаём текущее значение
                bestScale,
                1.0f,                    // ScaleMultiply
                colors,
                projection,
                TextAlign.Center
            );
        }

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
            // при необходимости добавьте цифры, знаки препинания и т.д.
        };

    }
}
