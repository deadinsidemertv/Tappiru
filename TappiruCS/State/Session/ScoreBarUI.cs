// UI/ScoreBarUI.cs
using OpenTK.Mathematics;
using TappiruCS.Core.GameObject;
using TappiruCS.GameLogic.Logic;
using TappiruCS.UI.TextAbstract;
using TappiruCS.Render.Text;

namespace TappiruCS.State.Session
{
    public class ScoreBarUI
    {

        private readonly TextObject _scoreText;
        private readonly TextObject _accuracyText;
        private readonly TextObject _comboText;
        private readonly TextObject _comboXText;

        private float _displayedScore;
        private float _displayedAccuracy;

        // Конструктор без session!
        public ScoreBarUI()
        {

            _scoreText = new TextObject("0000000", 1890, 140, 144f)
            {
                Color = Color4.White,
                Align = TextAlign.Right,
                FontKey = "GameOverlay",
                ScaleMultiply = 0.23f,
                HasShadow = true,
            };

            _accuracyText = new TextObject("100.00%", 1785, 235, 28f)
            {
                Color = Color4.White,
                Align = TextAlign.Center,
                FontKey = "GameOverlay",
                HasShadow = true
            };

            _comboText = new TextObject("0", 1795, 330, 28f)
            {
                Align = TextAlign.Left,
                FontKey = "GameOverlay",
                HasShadow = true
            };

            _comboXText = new TextObject("x", 55, 1040, 48f) 
            {
                HasShadow = true
            };
        }

        // Метод обновления — принимает session каждый кадр
        public void Update(GameSession session, double deltaTime)
        {
            if (session == null) return;

            const float lerpSpeed = 25.0f;

            _displayedScore = MathHelper.Lerp(_displayedScore, session.TotalScore, lerpSpeed * (float)deltaTime);
            _displayedAccuracy = MathHelper.Lerp(_displayedAccuracy, session.Accuracy, lerpSpeed * (float)deltaTime);

            _scoreText.Text = ((int)Math.Round(_displayedScore)).ToString("D7");
            _accuracyText.Text = (Math.Round(_displayedAccuracy * 100f) / 100f).ToString("F2") + "%";
            _comboText.Text = session.Combo.ToString();

            // Обновляем позицию "x"
            _comboXText.WorldPosition = new Vector2(_comboText.WorldPosition.X - 10, _comboText.WorldPosition.Y-2 );
        }

        public void AddToScene(Scene scene)
        {
            if (scene == null) return;

            scene.Add(_scoreText);
            scene.Add(_accuracyText);
            scene.Add(_comboText);
            scene.Add(_comboXText);
        }

        // Если захочешь скрыть/показать всю группу
        public void SetActive(bool active)
        {
            _scoreText.Active = active;
            _accuracyText.Active = active;
            _comboText.Active = active;
            _comboXText.Active = active;
        }
    }
}