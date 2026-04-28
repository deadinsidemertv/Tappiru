using System.IO;
using System.Text.Json;
// или Newtonsoft.Json, но лучше System.Text.Json
namespace TappiruCS.Core
{
    public class SettingsData
    {
        public float MasterVolume { get; set; } = 0.5f;
        
    }
}
