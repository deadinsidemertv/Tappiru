namespace TappiruCS.GameLogic
{
    public class TimingEvent
    {
        public double time { get; set; }
        public string text { get; set; }
        public List<SliderTiming> sliders { get; set; } = new List<SliderTiming>();
    }

    public class SliderTiming
    {
        public int charIndex { get; set; }           // какая буква является слайдером
        public double startTime { get; set; }        // когда нужно нажать
        public double endTime { get; set; }          // когда нужно отпустить
        public double perfectStartWindow { get; set; } = 0.08;
        public double goodStartWindow { get; set; } = 0.15;
        public double perfectEndWindow { get; set; } = 0.08;
        public double goodEndWindow { get; set; } = 0.20;
    }
}
