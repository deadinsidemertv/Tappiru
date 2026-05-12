using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;

namespace TappiruCS.UI
{
    public class ProgressBar : GameObject
    {
        public float MinValue { get; set; } = 0f;
        public float MaxValue { get; set; } = 100f;
        public bool AllowOverMax { get; set; } = false;
        public float Value { get; private set; } = 100f;
        public float LerpSpeed { get; set; } = 12f;

        // Цвета — обычные строки, обёртка UIColor подхватит автоматически
        public string ColorLow { get; set; } = "#e4143b";
        public string ColorMedium { get; set; } = "#a32395";
        public string ColorHigh { get; set; } = "#ff548a";
        public string ColorOverMax { get; set; } = "#ff0000";

        public bool UseEqualMode { get; set; } = false;
        public string ColorEqual { get; set; } = "#00ff00";   // зелёный при Value == MaxValue
        public string ColorNotEqual { get; set; } = "#ffa500"; // оранжевый при Value < MaxValue

        private readonly SpriteObject _background;
        public readonly SpriteObject _fillLine;
        private float _originalFillWidth;
        private float _displayedValue;

        public ProgressBar(float x, float y, float scaleX, float scaleY)
        {
            _background = new SpriteObject(TextureManager.GetTexture("white"), x, y, scaleX, scaleY)
            {
                Parent = this,
                Color = new Color4(0.25f, 0.25f, 0.25f, 1f),
                Pivot = new Vector2(0, 0.5f),
                Layer = 5
            };

            _fillLine = new SpriteObject(TextureManager.GetTexture("white"), x + 1, y, scaleX - 2, scaleY - 2)
            {
                Parent = this,
                Color = ColorHigh,    // строка -> UIColor -> Color4
                Pivot = new Vector2(0, 0.5f),
                EnableGlow = true,
                Layer = 5
            };

            AddChild(_fillLine);
            _originalFillWidth = scaleX - 2f;
            _displayedValue = Value;
        }

        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime, mouse);
            _displayedValue = MathHelper.Lerp(_displayedValue, Value, LerpSpeed * (float)deltaTime);

            float range = MaxValue - MinValue;
            float progress = range > 0.0001f ? Math.Clamp((_displayedValue - MinValue) / range, 0f, 1f) : 0f;

            _fillLine.Scale = new Vector2(_originalFillWidth * progress, _fillLine.Scale.Y);
            UpdateColor(progress);
        }

        private void UpdateColor(float progress)
        {
            if (UseEqualMode)
            {
                // Специальная логика для маппинга
                if (AllowOverMax && Value > MaxValue)
                    _fillLine.Color = ColorOverMax;      // красный
                else if (Value >= MaxValue)
                    _fillLine.Color = ColorEqual;        // зелёный (достигнут максимум)
                else
                    _fillLine.Color = ColorNotEqual;     // оранжевый (не хватает)
                return;
            }

            // Старая логика (для HP и прочих)
            if (progress > 0.65f)
                _fillLine.Color = ColorHigh;
            else if (progress > 0.35f)
                _fillLine.Color = ColorMedium;
            else
                _fillLine.Color = ColorLow;
        }

        public void SetValue(float newValue)
        {
            if (!AllowOverMax)
                newValue = Math.Clamp(newValue, MinValue, MaxValue);
            Value = newValue;
        }

        public void SetValueInstant(float newValue)
        {
            if (!AllowOverMax)
                newValue = Math.Clamp(newValue, MinValue, MaxValue);
            Value = newValue;
            _displayedValue = Value;
        }
    }
}