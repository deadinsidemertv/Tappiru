// UI/ScoreBarUI.cs
using OpenTK.Mathematics;
using TappiruCS.Core.GameObject;
using TappiruCS.GameLogic;
using TappiruCS.Render;
using static TappiruCS.Render.TextRender;

namespace TappiruCS.UI
{
    public class ScoreBarUI
    {
        private readonly TextRender _textRenderer;

        private readonly TextObject _scoreText;
        private readonly TextObject _accuracyText;
        private readonly TextObject _comboText;
        private readonly TextObject _comboXText;

        private float _displayedScore;
        private float _displayedAccuracy;

        // Конструктор без session!
        public ScoreBarUI(TextRender textRenderer)
        {
            _textRenderer = textRenderer;

            _scoreText = new TextObject("000000000", 1900, 0, 0.35f)
            {
                Color = Color4.White,
                Align = TextAlign.Right
            };

            _accuracyText = new TextObject("100.00%", 1840, 40, 0.3f)
            {
                Color = Color4.White,
                Align = TextAlign.Center
            };

            _comboText = new TextObject("0", 35, 990, 0.5f)
            {
                Align = TextAlign.Left
            };

            _comboXText = new TextObject("x", 55, 915, 0.3f);
        }

        // Метод обновления — принимает session каждый кадр
        public void Update(GameSession session, double deltaTime)
        {
            if (session == null) return;

            const float lerpSpeed = 25.0f;

            _displayedScore = MathHelper.Lerp(_displayedScore, session.TotalScore, lerpSpeed * (float)deltaTime);
            _displayedAccuracy = MathHelper.Lerp(_displayedAccuracy, session.Accuracy, lerpSpeed * (float)deltaTime);

            _scoreText.Text = ((int)Math.Round(_displayedScore)).ToString("D9");
            _accuracyText.Text = (Math.Round(_displayedAccuracy * 100f) / 100f).ToString("F2") + "%";
            _comboText.Text = session.Combo.ToString();

            // Обновляем позицию "x"
            _comboXText.Position = new Vector2(_comboText.Position.X - 15, _comboText.Position.Y + 15);
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