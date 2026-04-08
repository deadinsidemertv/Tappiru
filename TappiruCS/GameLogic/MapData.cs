using OpenTK.Mathematics;
using System;
using System.Collections.Generic;
using System.Text;

namespace TappiruCS.GameLogic
{
    public class MapData
    {
        public string title { get; set; }
        public string creator { get; set; }
        public List<TimingEvent> Events { get; set; } = new List<TimingEvent>();
        public string audioPath { get; set; }
        public string backGroundPath { get; set; }
        public string dataPath { get; set; }
        public string artist { get; set; }
        public double endTime { get; set; }

        public float tappedR { get; set; }
        public float tappedG{ get; set; }
        public float tappedB{ get; set; }

        public float needR { get; set; }
        public float needG{ get; set; }
        public float needB{ get; set; }

        public float completeR { get; set; }
        public float completeG{ get; set; }
        public float completeB{ get; set; }


    }
    public class JsonMap 
    {
        public List<TimingEvent> events { get; set; }
        public string title { get; set; }
        public string artist { get; set; }
        public double previewTime { get; set; }
        public string creator { get; set; }
        public string difficulty { get; set; }

        public double endTime { get; set; }

        public float tappedR { get; set; }
        public float tappedG{ get; set; }
        public float tappedB{ get; set; }

        public float needR { get; set; }
        public float needG { get; set; }
        public float needB { get; set; }

        public float completeR { get; set; }
        public float completeG { get; set; }
        public float completeB { get; set; }

    }
}
