using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace TappiruCS.GameLogic
{
    public static class LoadMap
    {
        public static MapData MapLoad(string mapFolderPath)
        {

            MapData mapdata = new MapData();
            string[] bgP = Directory.GetFiles(mapFolderPath, "*.jpg");
            mapdata.backGroundPath = bgP[0];
            string[] audioP = Directory.GetFiles(mapFolderPath, "*.mp3");
            mapdata.audioPath = audioP[0];
            string[] dataP = Directory.GetFiles(mapFolderPath, "*.tapp");
            mapdata.dataPath = dataP[0];

            string json = File.ReadAllText(mapdata.dataPath);
            JsonMap tmp = JsonSerializer.Deserialize<JsonMap>(json);
            mapdata.Events = tmp.events;
            mapdata.endTime = tmp.endTime;

            mapdata.title = tmp.title;
            mapdata.creator = tmp.creator;
            mapdata.artist = tmp.artist;

            mapdata.MapHash = tmp.MapHash;

            mapdata.tappedR = tmp.tappedR;
            mapdata.tappedG = tmp.tappedG;
            mapdata.tappedB = tmp.tappedB;

            mapdata.needR = tmp.needR;
            mapdata.needG = tmp.needG;
            mapdata.needB = tmp.needB;

            mapdata.completeR = tmp.completeR;
            mapdata.completeG = tmp.completeG;
            mapdata.completeB = tmp.completeB;

            foreach (var ev in mapdata.Events)
                ev.text = ev.text.ToLowerInvariant();
            Console.WriteLine(mapdata.endTime + " endTime");
            return mapdata;
        }
    }
}
