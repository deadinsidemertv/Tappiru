using System.Text.Json;
using TappiruCS.Core;

public static class OptionFile
{
    private static SettingsData _data = new();

    private static readonly string ConfigPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "Tappiru",           // название вашей игры
        "settings.json"
    );

    static OptionFile()
    {
        Load();
    }

    public static void Load()
    {
        if (!File.Exists(ConfigPath))
        {
            Save();
            return;
        }
        try
        {
            string json = File.ReadAllText(ConfigPath);
            var loaded = JsonSerializer.Deserialize<SettingsData>(json);
            if (loaded != null) _data = loaded;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка загрузки настроек: {ex.Message}");
        }
    }

    public static void Save()
    {
        try
        {
            string dir = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            string json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigPath, json);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка сохранения настроек: {ex.Message}");
        }
    }

    public static float MasterVolume
    {
        get => _data.MasterVolume;
        set { _data.MasterVolume = value; Save(); }
    }
}