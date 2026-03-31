using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using TappiruCS.GameLogic;
using TappiruCS.Render;

namespace TappiruCS.State
{
    public class ScoreBoardState : IGameState
    {

        private readonly Game _game;
        private readonly SpriteBatch _spriteBatch;
        private readonly TextRender _textRenderer;
        private readonly AudioManager _audio;
        public GameSession _session;

        public ScoreBoardState(Game game, SpriteBatch spriteBatch, TextRender textRenderer, AudioManager audio,GameSession session)
        {
            _game = game;
            _spriteBatch = spriteBatch;
            _textRenderer = textRenderer;
            _audio = audio;
            _session = session;
        }

        public void OnEnter() 
        {
            Console.WriteLine("Открылся ScoarBoard");
        
            Console.WriteLine("===== РЕЗУЛЬТАТЫ КАРТЫ =====");
            //Console.WriteLine($"Название: {CurrentMap.title ?? "Не указано"}");
            //Console.WriteLine($"Сложность: {CurrentMap.difficulty ?? "Не указана"}");
            Console.WriteLine($"Итоговый счёт: {_session.TotalScore}");
            Console.WriteLine($"Максимальное комбо: {_session.MaxCombo}");
            Console.WriteLine($"Правильных нажатий: {_session.CorrectHits}");
            Console.WriteLine($"Ошибок (мисов): {_session.Misses}");
            Console.WriteLine($"Всего символов: {_session.TotalNotes}");
            Console.WriteLine($"Точность: {_session.Accuracy:F2}%");
            Console.WriteLine($"Успешно завершённых строк: {_session.CompletedPhases}");
            Console.WriteLine($"Проваленных строк: {_session.FailedPhases}");
            Console.WriteLine($"Оставшееся здоровье: {_session.Health:F1}");
            Console.WriteLine("============================");
        
        }
        public void OnExit() 
        {
            Console.WriteLine("Вы вышли из ScoarBoard");
        }
        public void Update(double currentTime) { }
        public void Render(Matrix4 projection) 
        {
            _textRenderer.DrawString("тут будет скорборд", Game.WindowWidth / 2, Game.WindowHeight / 2, 1f, 0.77f, 0, 0, 0, 1, projection);
        }
       
        public void HandleKeyDown(KeyboardKeyEventArgs e) 
        {
            if (e.Key == Keys.Escape)
            {
                _game.ChangeState(new SongSelectState(_game, _spriteBatch, _textRenderer, _audio));
            }
        }
      

    }
}
