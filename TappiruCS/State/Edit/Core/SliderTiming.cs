using System;
using System.Collections.Generic;
using System.Text;

namespace TappiruCS.State.Edit.Core
{
    public class SliderTiming : ITimelineSelectable
    {
        public int charIndex { get; set; }
        public float startTime { get; set; }
        public float endTime { get; set; }

        float ITimelineSelectable.StartTime
        {
            get => startTime;
            set => startTime = value;
        }

        float ITimelineSelectable.EndTime
        {
            get => endTime;
            set => endTime = value;
        }

        string ITimelineSelectable.GetDisplayName() => $"Слайдер #{charIndex}";
        string ITimelineSelectable.GetTypeName() => "Слайдер";
    }
}
