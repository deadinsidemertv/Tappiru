using System;
using System.Collections.Generic;
using System.Text;

namespace TappiruCS.GameLogic
{
    public class MapData
    {
        public List<TimingEvent> Events { get; set; } = new List<TimingEvent>();
        public string audioPath { get; set; }
        public string backGroundPath { get; set; }
        public string dataPath { get; set; }

        
    }
    public class JsonMap 
    {
        public List<TimingEvent> events { get; set; }
        public string title { get; set; }
        public string artist { get; set; }
        public double previewTime { get; set; }
        public string creator { get; set; }
        public string difficulty { get; set; }

    }
}
