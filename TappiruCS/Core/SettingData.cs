using OpenTK.Mathematics;
using System.IO;
using System.Text.Json;
using OpenTK.Windowing.Common;

namespace TappiruCS.Core
{
    public class SettingsData
    {
        public float MasterVolume { get; set; } = 0.5f;

        public int ScreenWidth { get; set; } = 1920;
        public int ScreenHeight { get; set; } = 1080;
        public WindowState WindowState { get; set; } = WindowState.Fullscreen;
        
    }
}
