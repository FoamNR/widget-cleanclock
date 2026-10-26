using System;
using System.IO;
using System.Text.Json;

namespace widget;

public class AppSettings
{
    public double WindowLeft { get; set; } = -1;
    public double WindowTop { get; set; } = -1;

    // Window Layer Mode: "Wallpaper" (หลังสุดติด Desktop), "Topmost" (ลอยบนสุด), "Normal" (ปกติ)
    public string WindowMode { get; set; } = "Wallpaper";

    public bool ShowSeconds { get; set; } = false;
    public bool Is24Hour { get; set; } = true;
    public string Language { get; set; } = "en"; // "en" (English) or "th"

    // Colors
    public string TextColorHex { get; set; } = "#FFFFFF";
    public string DateColorHex { get; set; } = "#E2E8F0";

    // Typography & Styling
    public double Scale { get; set; } = 1.0;
    public string FontWeight { get; set; } = "Light"; // "Light", "Normal", "SemiBold"
    public bool ShowShadow { get; set; } = true;

    // Click-Through & Auto Start
    public bool ClickThrough { get; set; } = false;
    public bool StartWithWindows { get; set; } = false;

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DesktopOverlayClock",
        "settings.json"
    );

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                string json = File.ReadAllText(SettingsPath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null) return settings;
            }
        }
        catch
        {
            // Fallback to default
        }
        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            string dir = Path.GetDirectoryName(SettingsPath)!;
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
        }
        catch
        {
            // Ignore error on save
        }
    }
}
