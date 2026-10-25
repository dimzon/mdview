using System.IO;
using System.Text;
using Microsoft.Win32;

namespace MdView;

public static class Settings
{
    private static readonly string FilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MdView", "settings.txt");

    /// <summary>Сохранённая тема; null — авто (следуем теме Windows).</summary>
    public static string? Theme { get; private set; }

    /// <summary>Сохранённый язык ("en"/"ru"); null — берём язык системы.</summary>
    public static string? Language { get; private set; }

    public static void Load()
    {
        try
        {
            if (!File.Exists(FilePath))
                return;
            foreach (var raw in File.ReadAllLines(FilePath))
            {
                var line = raw.Trim();
                var eq = line.IndexOf('=');
                var key = eq >= 0 ? line[..eq].Trim() : "theme"; // старый формат: одна строка с темой
                var value = eq >= 0 ? line[(eq + 1)..].Trim() : line;
                if (key == "theme" && value is "dark" or "light")
                    Theme = value;
                if (key == "lang" && value is "en" or "ru" or "auto")
                    Language = value == "auto" ? null : value;
            }
        }
        catch { /* настройки не критичны */ }
    }

    public static void SetTheme(string? theme)
    {
        Theme = theme;
        Save();
    }

    public static void SetLanguage(string? language)
    {
        Language = language;
        Save();
    }

    private static void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            var sb = new StringBuilder();
            sb.Append("theme=").Append(Theme ?? "auto").AppendLine();
            sb.Append("lang=").Append(Language ?? "auto").AppendLine();
            File.WriteAllText(FilePath, sb.ToString());
        }
        catch { /* настройки не критичны */ }
    }

    /// <summary>Тёмная ли сейчас тема приложений в Windows.</summary>
    public static bool IsSystemDark()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is 0;
        }
        catch
        {
            return false;
        }
    }
}
