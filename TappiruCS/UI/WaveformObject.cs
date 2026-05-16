// UI/WaveformObject.cs
using OpenTK.Mathematics;
using OpenTK.Windowing.GraphicsLibraryFramework;
using TappiruCS.Core.GameObject;
using TappiruCS.Render;
using TappiruCS.UI.Sprite;

namespace TappiruCS.UI
{
    public class WaveformObject : GameObject
    {
        private readonly AudioManager _audio;

        public float Width { get; set; } = 1300f;
        public float Height { get; set; } = 180f;
        public int BarCount { get; set; } = 256;         // адекватное кол-во как на скрине

        // Доля ширины слота, занятая баром (0.6 = 60% бар, 40% зазор)
        public float BarFill { get; set; } = 0.6f;

        public UIColor Color { get; set; } = "#cd3a68";

        private float[] _smoothedHeights;

        private const float RiseSpeed = 12f;
        private const float FallSpeed = 5f;

        public WaveformObject(float x, float y)
        {
            _audio = AudioManager.Instance;
            LocalPosition = new Vector2(x, y);
            Layer = 3;

            _smoothedHeights = new float[BarCount];

            CreateBars();
            _audio.StartSpectrumCapture();
        }

        public void CreateBars()
        {
            foreach (var child in Children.ToList())
                RemoveChild(child);

            for (int i = 0; i < BarCount; i++)
            {
                var bar = new SpriteObject(TextureManager.GetTexture("white"), 0, 0, 1, 1)
                {
                    Color = Color,
                    EnableGlow = true,
                    GlowIntensity = 0.2f,
                    GlowSpread = 9f,
                    GlowSteps = 5,
                    Pivot = new Vector2(0.5f, 1f),
                    AutoScale = true,
                    ScaleMultiply = 1f,
                    AllowHover = false,
                    Active = false
                };

                AddChild(bar);
            }
        }

        public override void Update(double deltaTime, MouseState mouse)
        {
            base.Update(deltaTime, mouse);

            float dt = (float)deltaTime;

            if (_audio == null || _audio.Spectrum == null)
            {
                for (int i = 0; i < _smoothedHeights.Length; i++)
                    _smoothedHeights[i] = Lerp(_smoothedHeights[i], 2f, FallSpeed * dt);

                foreach (var child in Children) child.Active = false;
                return;
            }

            var spectrum = _audio.Spectrum;
            int usable = Math.Min(BarCount, spectrum.Length);
            float slotWidth = Width / usable;       // ширина слота (бар + зазор)
            float barWidth = slotWidth * BarFill;  // фактическая ширина бара

            // === Симметричный массив амплитуд (центр = бас) ===
            int half = usable / 2;
            float[] targetAmps = new float[usable];

            for (int i = 0; i < half; i++)
            {
                float amp = spectrum[i];
                amp = MathF.Pow(amp * 6f, 0.6f) * Height;

                targetAmps[half - 1 - i] = amp;
                targetAmps[half + i] = amp;
            }

            if (usable % 2 != 0)
            {
                float amp = MathF.Pow(spectrum[0] * 6f, 0.6f) * Height;
                targetAmps[half] = amp;
            }

            // === Lerp: подъём быстрый, спуск медленный ===
            for (int i = 0; i < usable; i++)
            {
                float target = Math.Max(targetAmps[i], 1f);
                float speed = target > _smoothedHeights[i] ? RiseSpeed : FallSpeed;
                _smoothedHeights[i] = Lerp(_smoothedHeights[i], target, speed * dt);
            }

            // === Применяем к барам ===
            for (int i = 0; i < usable; i++)
            {
                var bar = (SpriteObject)Children[i];
                bar.Active = true;

                // Центр слота
                float x = i * slotWidth - Width / 2f + slotWidth / 2f;
                bar.LocalPosition = new Vector2(x, 0);
                bar.Scale = new Vector2(barWidth, _smoothedHeights[i]);
            }

            for (int i = usable; i < Children.Count; i++)
                Children[i].Active = false;
        }

        private static float Lerp(float a, float b, float t) =>
            a + (b - a) * Math.Clamp(t, 0f, 1f);

        public override void Draw(Matrix4 projection)
        {
            base.Draw(projection);
        }
    }
}