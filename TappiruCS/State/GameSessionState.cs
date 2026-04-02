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
        private string _songPath;

        public int background;

        private readonly Scene _scene = new Scene();
        public Background bg;
        public Background Fade;
        

        public TextObject score;
        public TextObject Accuraci;

        public TextObject combo;
        public TextObject comboApof;

        public GameSessionState(Game game, SpriteBatch spriteBatch, TextRender textRenderer, AudioManager audio,string songPath)
        {
            _game = game;
            _spriteBatch = spriteBatch;
            _textRenderer = textRenderer;
            _audio = audio;
            _songPath = songPath;
        }

        public MapData MapLoad(string mapFolderPath)
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
            Console.WriteLine(mapdata.endTime + " endTime");
            return mapdata;
        }
        public void OnEnter()
        {
            Console.WriteLine("Запуск уровня");
            _mapData = MapLoad(_songPath);

            session = new GameSession(_mapData);
            Console.WriteLine(_mapData.audioPath.ToString());
            if(_audio == null)
            {
                Console.WriteLine("_audio РОВНО NULL CYKA");
            }

            background = TextureLoader.Load(_mapData.backGroundPath);
            bg = new Background(_spriteBatch, background, _game);

            Fade = new Background(_spriteBatch, 0, _game) { Opacity = 0.5f };


            _audio.LoadMusic(_mapData.audioPath);
            _audio.Play();


            score = new TextObject(_textRenderer, session.TotalScore.ToString(), 1900, 20, 0.7f)
            {
                Color = Color4.White,
                Align = TextAlign.Right
            };
            Accuraci = new TextObject(_textRenderer, session.Accuracy.ToString(), 1900, score.Position.Y+50, 0.4f)
            {
                Color = Color4.White,
                Align = TextAlign.Right
            };
            combo = new TextObject(_textRenderer, session.Combo.ToString(), 70, 990, 0.7f) { Align = TextAlign.Left};
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
                    _game.ChangeState(new ScoreBoardState(_game, _spriteBatch, _textRenderer, _audio, playerscore));
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
            
            InputCharDraw(session, projection, 960, 540);
        }
       
        public void HandleKeyDown(KeyboardKeyEventArgs e) 
        {
            session.HandleInput(KeyToChar(e));
        }

      


       

       

        private void InputCharDraw(GameSession session, Matrix4 projection, float centerX, float y)
        {
            if (session.CurrentPhaseChars == null) return;
            
            char[] chars = session.CurrentPhaseChars;
            float scaleX = 0.6f;
            float scaleY = 0.6f;
            if (chars.Length < 10)
            {
                scaleX = 1.4f;
                scaleY = 1.4f;
            }else if (chars.Length > 10 && chars.Length < 20)
            {
                scaleX = 0.6f;
                scaleY = 0.6f;
            }
            
            float charWidth = this._textRenderer.charWidth * scaleX;
            float spacing = 0.77f;         // межсимвольный интервал (например, 0.3)
            float step = charWidth * spacing;
            // Общая ширина строки: (кол-во символов - 1) * шаг + ширина последнего символа
            float totalWidth = (chars.Length - 1) * step + charWidth;
            float startX = centerX - totalWidth / 2;

            float currentX = startX;
            for (int i = 0; i < chars.Length; i++)
            {
                
                // Определяем цвет
                float r, g, b;
                
                if (session.PhaseComplete)
                {
                        (r, g, b) = (0.6f, 1.0f, 0.6f);
                }
                else
                {
                    if (i < session.CurrentCharIndex)
                        (r, g, b) = (0.6f, 0.8f, 1.0f);   // синий – уже набранные
                    else if (i == session.CurrentCharIndex)
                        (r, g, b) = (1.0f, 0.9f, 0.6f);   // жёлтый – текущий символ
                    else
                        (r, g, b) = (1, 1, 1);   // чёрный – ещё не набранные
                }
                _textRenderer.DrawString(chars[i].ToString(), currentX*_scene.CanvasScale.X, y * _scene.CanvasScale.Y, scaleX * _scene.CanvasScale.X, scaleY * _scene.CanvasScale.Y, r, g, b, 1, projection);
                currentX += step;
            }
            
        }

        public char KeyToChar(KeyboardKeyEventArgs e)
        {
            char ch = '\0';
            switch (e.Key)
            {
                case Keys.A: return 'ф';
                case Keys.B: return 'и';
                case Keys.C: return 'с';
                case Keys.D: return 'в';
                case Keys.E: return 'у';
                case Keys.F: return 'а';
                case Keys.G: return 'п';
                case Keys.H: return 'р';
                case Keys.I: return 'ш';
                case Keys.J: return 'о';
                case Keys.K: return 'л';
                case Keys.L: return 'д';
                case Keys.M: return 'ь';
                case Keys.N: return 'т';
                case Keys.O: return 'щ';
                case Keys.P: return 'з';
                case Keys.Q: return 'й';
                case Keys.R: return 'к';
                case Keys.S: return 'ы';
                case Keys.T: return 'е';
                case Keys.U: return 'г';
                case Keys.V: return 'м';
                case Keys.W: return 'ц';
                case Keys.X: return 'ч';
                case Keys.Y: return 'н';
                case Keys.Z: return 'я';

                // Дополнительные буквы
                case Keys.LeftBracket: return 'х';      // [
                case Keys.RightBracket: return 'ъ';     // ]
                case Keys.Semicolon: return 'ж';        // ;
                case Keys.Apostrophe: return 'э';            // '
                case Keys.Comma: return 'б';            // ,
                case Keys.Period: return 'ю';           // .
                case Keys.GraveAccent: return 'ё';      // ` (клавиша с ё)



                case Keys.Space: return ' ';

                default: return '\0';

            }
        }

    }
}
