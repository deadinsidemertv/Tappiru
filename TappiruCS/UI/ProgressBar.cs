using OpenTK.Mathematics;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;

namespace TappiruCS.UI
{
    public class ProgressBar : GameObject
    {
        // Публичные свойства (приведены к общепринятому стилю C#)
        public float MinValue { get; set; } = 0f;
        public float MaxValue { get; set; } = 100f;

        /// <summary>
        /// Реальное (фактическое) значение здоровья / прогресса
        /// </summary>
        public float Value { get; set; } = 100f;

        private readonly SpriteObject _background;
        private readonly SpriteObject _fillLine;

        private float _originalFillWidth;

        // Плавно отображаемое значение (для анимации)
        private float _displayedValue = 100f;

        /// <summary>
        /// Скорость анимации lerp (чем выше — тем быстрее реагирует бар)
        /// Рекомендуемые значения: 8–15
        /// </summary>
        public float LerpSpeed { get; set; } = 12f;

        public ProgressBar(float x, float y, float scaleX, float scaleY)
        {
            _background = new SpriteObject(TextureManager.GetTexture("white"), x, y, scaleX, scaleY)
            {
                Parent = this,
                Color = new Color4(0.25f, 0.25f, 0.25f, 1f), // тёмно-серый
                Pivot = new Vector2(0, 0.5f),
                Layer = 5
            };

            // Заполняющая линия
            _fillLine = new SpriteObject(TextureManager.GetTexture("white"), x + 1, y, scaleX - 2, scaleY - 2)
            {
                Parent = this,
                Color = new Color4(0f, 1f, 0f, 1f), // приятный зелёный по умолчанию
                Pivot = new Vector2(0, 0.5f),
                EnableGlow = true,
                Layer = 5
            };

            AddChild(_background);
            AddChild(_fillLine);

            _originalFillWidth = scaleX - 2f;
            _displayedValue = Value;
        }

        public override void Update(double currentTime) // ← добавили deltaTime
        {
            base.Update(currentTime);

            // Плавное приближение _displayedValue к реальному Value
            _displayedValue = MathHelper.Lerp(_displayedValue, Value, LerpSpeed * (float)currentTime);

            // Вычисляем прогресс на основе плавного значения
            float progress = Math.Clamp(
                (_displayedValue - MinValue) / (MaxValue - MinValue),
                0f, 1f);

            // Устанавливаем ширину заливки
            _fillLine.Scale = new Vector2(
                _originalFillWidth * progress,
                _fillLine.Scale.Y
            );

            // Красивое изменение цвета в зависимости от уровня
            UpdateColor(progress);
        }

        private void UpdateColor(float progress)
        {
            if (progress > 0.65f)
            {
                // Зелёный → светло-зелёный
                _fillLine.Color = new Color4(0.25f, 1f, 0.25f, 1f);
            }
            else if (progress > 0.35f)
            {
                // Жёлто-оранжевый
                _fillLine.Color = new Color4(1f, 0.85f, 0.15f, 1f);
            }
            else
            {
                // Красный (опасная зона)
                _fillLine.Color = new Color4(1f, 0.25f, 0.2f, 1f);
            }
        }

        /// <summary>
        /// Удобный метод для мгновенной установки значения
        /// </summary>
        public void SetValue(float newValue)
        {
            Value = Math.Clamp(newValue, MinValue, MaxValue);
        }

        /// <summary>
        /// Мгновенно установить значение без анимации (например при старте)
        /// </summary>
        public void SetValueInstant(float newValue)
        {
            Value = _displayedValue = Math.Clamp(newValue, MinValue, MaxValue);
        }
    }
}