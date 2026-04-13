// TimingEvent.cs — УПРОЩЁННЫЙ ДЛЯ РЕДАКТОРА
namespace TappiruCS.GameLogic
{
    public class TimingEvent
    {
        public float startTime { get; set; }
        public float endTime { get; set; }
        public string text { get; set; } = string.Empty;

        public List<SliderTiming> sliders { get; set; } = new List<SliderTiming>();
    }

    public class SliderTiming
    {
        public int charIndex { get; set; }

        public float startTime { get; set; }
        public float endTime { get; set; }

    }
}