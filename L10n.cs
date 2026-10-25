using System.Windows;

namespace MdView;

/// <summary>Доступ к локализованным строкам из словарей Themes/Strings.*.xaml.</summary>
public static class L10n
{
    public static string L(string key) =>
        Application.Current?.TryFindResource(key) as string ?? key;
}
