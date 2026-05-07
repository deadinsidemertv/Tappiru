// ColorPreviewPanel.cs — UI-панель с RGB-слайдерами и превью цветов
using OpenTK.Mathematics;
using TappiruCS.Core.GameObject;
using TappiruCS.GameLogic;
using TappiruCS.Render.Text;
using TappiruCS.UI;
using TappiruCS.UI.TextAbstract;

namespace TappiruCS.State.Edit
{
    /// <summary>
    /// Создаёт три группы RGB-слайдеров (Tapped / Need / Complete)
    /// и обновляет цвет демо-текстов в реальном времени.
    /// Отвечает только за UI цветов — EditState делегирует ей эту ответственность.
    /// </summary>
    internal class ColorPreviewPanel
    {
        // ── Цветовые группы ──────────────────────────────────────────────────────
        internal record ColorGroup(string Name, Slider R, Slider G, Slider B);

        private readonly List<ColorGroup> _groups = new();

        // Демо-тексты
        private TextObject _demoNewT = null!;
        private TextObject _demoE = null!;
        private TextObject _demoXt = null!;
        private TextObject _demoComplete = null!;

        // ── Инициализация ────────────────────────────────────────────────────────
        public void Build(Scene scene)
        {
            _groups.Clear();

            const float startX = 1620f;
            const float startY = 130f;
            const float groupSpacingX = 200f;
            const float groupSpacingY = 200f;

            AddGroup(scene, "Tapped", startX, startY, 0.4f, 0.3f, 0.6f);
            AddGroup(scene, "Need", startX + groupSpacingX, startY, 0.7f, 0.3f, 0.8f);
            AddGroup(scene, "Complete", startX + groupSpacingX / 2, startY + groupSpacingY, 0.2f, 0.1f, 0.4f);

            BuildDemoTexts(scene, startX, startY, groupSpacingX);
        }

        // ── Синхронизация с JsonMap ──────────────────────────────────────────────
        public void LoadFrom(JsonMap map)
        {
            if (_groups.Count < 3) return;

            _groups[0].R.SetValue(map.tappedR); _groups[0].G.SetValue(map.tappedG); _groups[0].B.SetValue(map.tappedB);
            _groups[1].R.SetValue(map.needR); _groups[1].G.SetValue(map.needG); _groups[1].B.SetValue(map.needB);
            _groups[2].R.SetValue(map.completeR); _groups[2].G.SetValue(map.completeG); _groups[2].B.SetValue(map.completeB);
        }

        public void SaveTo(JsonMap map)
        {
            if (_groups.Count < 3) return;

            map.tappedR = _groups[0].R.Value; map.tappedG = _groups[0].G.Value; map.tappedB = _groups[0].B.Value;
            map.needR = _groups[1].R.Value; map.needG = _groups[1].G.Value; map.needB = _groups[1].B.Value;
            map.completeR = _groups[2].R.Value; map.completeG = _groups[2].G.Value; map.completeB = _groups[2].B.Value;
        }

        // ── Обновление цветов (каждый кадр) ─────────────────────────────────────
        public void Tick()
        {
            if (_groups.Count < 3) return;

            _demoNewT.Color = GroupColor(_groups[0]);
            _demoE.Color = GroupColor(_groups[1]);
            _demoXt.Color = Color4.White;
            _demoComplete.Color = GroupColor(_groups[2]);
        }

        // ── Вспомогательные ─────────────────────────────────────────────────────
        private void AddGroup(Scene scene, string name, float x, float y,
                              float r, float g, float b)
        {
            const float spacing = 45f;
            const float width = 255f;
            const float scale = 0.72f;

            var sliderR = MakeSlider(scene, x, y, r, scale);
            var sliderG = MakeSlider(scene, x, y + spacing, g, scale);
            var sliderB = MakeSlider(scene, x, y + spacing * 2, b, scale);

            _groups.Add(new ColorGroup(name, sliderR, sliderG, sliderB));
        }

        private static Slider MakeSlider(Scene scene, float x, float y, float value, float scale)
        {
            var s = new Slider(0f, 1f, x, y, 255f) { ScaleMultiply = scale, AllowHover = true };
            s.SetValue(value);
            scene.Add(s);
            return s;
        }

        private void BuildDemoTexts(Scene scene, float startX, float startY, float spacingX)
        {
            float cx = startX + spacingX / 2;

            _demoNewT = MakeText(scene, "new t", cx - 28, startY - 90, 0.6f);
            _demoE = MakeText(scene, " e", cx + 45, startY - 90, 0.6f);
            _demoXt = MakeText(scene, "xt", cx + 90, startY - 90, 0.6f);
            _demoComplete = MakeText(scene, "new text", cx, startY + 90, 0.5f);
        }

        private static TextObject MakeText(Scene scene, string text, float x, float y, float scale)
        {
            var t = new TextObject(text, x, y, 86f)
            {
                ScaleMultiply = scale,
                Align = TextAlign.Center,
                Color = Color4.White
            };
            scene.Add(t);
            return t;
        }

        private static Color4 GroupColor(ColorGroup g) =>
            new Color4(g.R.Value, g.G.Value, g.B.Value, 1f);
    }
}