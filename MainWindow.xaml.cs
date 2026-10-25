using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Packaging;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Documents;
using System.Windows.Xps.Packaging;
using Microsoft.Win32;

namespace MdView;

public partial class MainWindow : Window
{
    public const string RepoUrl = "https://github.com/dimzon/mdview";

    public static readonly RoutedCommand ToggleTocCommand = new("ToggleToc", typeof(MainWindow));
    public static readonly RoutedCommand ZoomInCommand = new("ZoomIn", typeof(MainWindow));
    public static readonly RoutedCommand ZoomOutCommand = new("ZoomOut", typeof(MainWindow));
    public static readonly RoutedCommand ZoomResetCommand = new("ZoomReset", typeof(MainWindow));
    public static readonly RoutedCommand PrintCommand = new("PrintDocument", typeof(MainWindow));
    public static readonly RoutedCommand SaveXpsCommand = new("SaveXps", typeof(MainWindow));

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);

    private string? _currentPath;
    private bool _isDark;
    private string _language = "en";

    public MainWindow(string[] args)
    {
        InitializeComponent();

        // по умолчанию — тема и язык Windows; явный выбор в меню сохраняется и имеет приоритет
        _isDark = Settings.Theme is not null ? Settings.Theme == "dark" : Settings.IsSystemDark();
        _language = ResolveLanguage();
        DarkThemeItem.IsChecked = _isDark;
        SyncLanguageMenu();
        ApplyTheme(_isDark);
        ApplyLanguage(_language);

        SourceInitialized += (_, _) => ApplyTitleBarTheme(this, _isDark);

        AllowDrop = true;
        AddHandler(DragDrop.DragOverEvent, new DragEventHandler(OnDragOver), true);
        AddHandler(DragDrop.DropEvent, new DragEventHandler(OnDrop), true);

        ShowWelcome();

        var file = args.FirstOrDefault(File.Exists);
        if (file is not null)
            OpenFile(file);
    }

    // ---------- Язык ----------

    private string ResolveLanguage()
    {
        if (Settings.Language is not null)
            return Settings.Language;
        return CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ru" ? "ru" : "en";
    }

    private void ApplyLanguage(string language)
    {
        _language = language;
        var dictionaries = Application.Current.Resources.MergedDictionaries;
        if (dictionaries.Count > 1)
            dictionaries[1].Source = new Uri($"Themes/Strings.{language}.xaml", UriKind.Relative);
        if (_currentPath is null)
            ShowWelcome(); // перерисовать приветствие на выбранном языке
    }

    private void SyncLanguageMenu()
    {
        SystemLangItem.IsChecked = Settings.Language is null;
        EnglishItem.IsChecked = Settings.Language == "en";
        RussianItem.IsChecked = Settings.Language == "ru";
    }

    private void OnLanguageClick(object sender, RoutedEventArgs e)
    {
        var language = sender == EnglishItem ? "en"
            : sender == RussianItem ? "ru"
            : null; // «как в системе»
        Settings.SetLanguage(language);
        SyncLanguageMenu();
        ApplyLanguage(ResolveLanguage());
    }

    // ---------- Открытие файлов ----------

    private void OpenFile(string path)
    {
        try
        {
            var markdown = File.ReadAllText(path);
            var result = MarkdownConverter.Convert(
                markdown,
                Path.GetDirectoryName(Path.GetFullPath(path))!,
                OpenFile); // относительные ссылки .md открываются в этом же окне

            Viewer.Document = result.Document;
            _currentPath = path;
            Title = Path.GetFileName(path) + " — mdview";
            TocList.ItemsSource = result.Toc;
            Viewer.Document?.Blocks.FirstBlock?.BringIntoView();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, L10n.L("Msg.OpenError") + "\n" + ex.Message,
                "mdview", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenViaDialog()
    {
        var dialog = new OpenFileDialog
        {
            Title = "mdview",
            Filter = string.Join("|",
                L10n.L("Msg.FilterMd"),
                L10n.L("Msg.FilterTxt"),
                L10n.L("Msg.FilterAll")),
        };
        if (dialog.ShowDialog(this) == true)
            OpenFile(dialog.FileName);
    }

    private void ShowWelcome()
    {
        var doc = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 14.5,
            PagePadding = new Thickness(35, 15, 45, 30),
            Background = Brushes.Transparent,
        };
        doc.SetResourceReference(TextElement.ForegroundProperty, "FgBrush");

        doc.Blocks.Add(new Paragraph(new Run("mdview"))
        {
            FontSize = 30,
            FontWeight = FontWeights.Bold,
            Margin = new Thickness(0, 40, 0, 10),
        });
        doc.Blocks.Add(new Paragraph(new Run(L10n.L("Welcome.Lead"))));

        var list = new List { Margin = new Thickness(0, 0, 0, 10), Padding = new Thickness(24, 0, 0, 0) };
        AddHint(list, L10n.L("Welcome.HintDialog"));
        AddHint(list, L10n.L("Welcome.HintDrop"));
        AddHint(list, L10n.L("Welcome.HintCli"));
        doc.Blocks.Add(list);

        var hint = new Paragraph(new Run(L10n.L("Welcome.Hints")))
        {
            Margin = new Thickness(0, 10, 0, 0),
        };
        hint.SetResourceReference(TextElement.ForegroundProperty, "MutedBrush");
        doc.Blocks.Add(hint);

        Viewer.Document = doc;
        Title = "mdview";
        TocList.ItemsSource = null;
        _currentPath = null;
    }

    private static void AddHint(List list, string text)
    {
        list.ListItems.Add(new ListItem(
            new Paragraph(new Run(text)) { Margin = new Thickness(0, 0, 0, 3) }));
    }

    // ---------- Тема ----------

    private void ApplyTheme(bool dark)
    {
        var dictionaries = Application.Current.Resources.MergedDictionaries;
        if (dictionaries.Count == 0)
            return;
        dictionaries[0].Source = new Uri($"Themes/{(dark ? "Dark" : "Light")}.xaml", UriKind.Relative);
        ApplyTitleBarTheme(this, dark);
    }

    /// <summary>Тёмный заголовок окна через DWM (Win10/11).</summary>
    private static void ApplyTitleBarTheme(Window window, bool dark)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
            return;
        var useDark = dark ? 1 : 0;
        // 20 = DWMWA_USE_IMMERSIVE_DARK_MODE; на ранних сборках Win10 атрибут имеет номер 19
        if (DwmSetWindowAttribute(handle, 20, ref useDark, sizeof(int)) != 0)
            DwmSetWindowAttribute(handle, 19, ref useDark, sizeof(int));
    }

    // ---------- Печать и XPS ----------

    private void OnPrintExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (Viewer.Document is null)
            return;
        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
            return;
        try
        {
            var paginator = ((IDocumentPaginatorSource)Viewer.Document).DocumentPaginator;
            var capabilities = dialog.PrintQueue.GetPrintCapabilities(dialog.PrintTicket);
            var area = capabilities.PageImageableArea;
            if (area is not null)
                paginator.PageSize = new Size(area.ExtentWidth, area.ExtentHeight);
            dialog.PrintDocument(paginator, Title);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, L10n.L("Msg.PrintError") + "\n" + ex.Message,
                "mdview", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OnSaveXpsExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (Viewer.Document is null)
            return;
        var dialog = new SaveFileDialog
        {
            Title = L10n.L("Msg.XpsTitle"),
            Filter = L10n.L("Msg.XpsFilter"),
            FileName = (string.IsNullOrEmpty(_currentPath)
                ? L10n.L("Msg.DefaultDocName")
                : Path.GetFileNameWithoutExtension(_currentPath)) + ".xps",
        };
        if (dialog.ShowDialog(this) != true)
            return;
        try
        {
            using var package = Package.Open(dialog.FileName, FileMode.Create);
            using var xpsDocument = new XpsDocument(package, CompressionOption.Maximum);
            var paginator = ((IDocumentPaginatorSource)Viewer.Document).DocumentPaginator;
            paginator.PageSize = new Size(793.7, 1122.5); // A4 при 96 dpi
            XpsDocument.CreateXpsDocumentWriter(xpsDocument).Write(paginator);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, L10n.L("Msg.XpsError") + "\n" + ex.Message,
                "mdview", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ---------- Справка ----------

    private void OnOpenRepoClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(RepoUrl) { UseShellExecute = true });
        }
        catch { /* браузера нет — молча */ }
    }

    private void OnAboutClick(object sender, RoutedEventArgs e)
    {
        var version = typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "?";

        var panel = new StackPanel { Margin = new Thickness(28, 22, 28, 22) };
        panel.SetResourceReference(TextElement.ForegroundProperty, "FgBrush");
        panel.Children.Add(new TextBlock
        {
            Text = "mdview",
            FontSize = 24,
            FontWeight = FontWeights.Bold,
        });
        panel.Children.Add(new TextBlock
        {
            Text = L10n.L("About.Tagline"),
            Margin = new Thickness(0, 6, 0, 0),
            TextWrapping = TextWrapping.Wrap,
        });
        panel.Children.Add(new TextBlock
        {
            Text = $"{L10n.L("About.Version")}: {version}",
            Margin = new Thickness(0, 4, 0, 14),
        });

        var linkText = new TextBlock();
        var link = new Hyperlink { NavigateUri = new Uri(RepoUrl), ToolTip = RepoUrl };
        link.SetResourceReference(TextElement.ForegroundProperty, "AccentBrush");
        link.Inlines.Add(L10n.L("About.Repo"));
        link.RequestNavigate += (_, ev) =>
        {
            ev.Handled = true;
            OnOpenRepoClick(sender, e);
        };
        linkText.Inlines.Add(link);
        panel.Children.Add(linkText);

        var about = new Window
        {
            Title = L10n.L("About.Title"),
            SizeToContent = SizeToContent.WidthAndHeight,
            ResizeMode = ResizeMode.NoResize,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ShowInTaskbar = false,
            Background = TryFindResource("BgBrush") as Brush ?? Brushes.White,
            Content = panel,
        };
        // заголовок About согласуется с темой приложения, а не с темой Windows
        about.SourceInitialized += (_, _) => ApplyTitleBarTheme(about, _isDark);
        try
        {
            about.ShowDialog();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, "About: " + ex.Message,
                "mdview", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // ---------- Обработчики меню ----------

    private void OnOpenExecuted(object sender, ExecutedRoutedEventArgs e) => OpenViaDialog();

    private void OnReloadExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        if (_currentPath is not null)
            OpenFile(_currentPath);
    }

    private void OnExitClick(object sender, RoutedEventArgs e) => Close();

    private void OnToggleTheme(object sender, RoutedEventArgs e)
    {
        _isDark = DarkThemeItem.IsChecked;
        ApplyTheme(_isDark);
        Settings.SetTheme(_isDark ? "dark" : "light");
    }

    private void OnToggleTocExecuted(object sender, ExecutedRoutedEventArgs e)
    {
        TocPanel.Visibility = TocPanel.Visibility == Visibility.Visible
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void OnZoomIn(object sender, ExecutedRoutedEventArgs e) => Viewer.IncreaseZoom();

    private void OnZoomOut(object sender, ExecutedRoutedEventArgs e) => Viewer.DecreaseZoom();

    private void OnZoomReset(object sender, ExecutedRoutedEventArgs e) => Viewer.Zoom = 100;

    private void OnTocItemClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: TocEntry entry })
            entry.Block.BringIntoView();
    }

    // ---------- Drag & drop ----------

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetData(DataFormats.FileDrop) is not string[] { Length: > 0 } files)
            return;

        var file = files[0];
        var ext = Path.GetExtension(file).ToLowerInvariant();
        if (ext is ".md" or ".markdown" or ".mdown" or ".mkd" or ".txt")
        {
            OpenFile(file);
        }
        else
        {
            MessageBox.Show(this, L10n.L("Msg.DropUnsupported"),
                "mdview", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
