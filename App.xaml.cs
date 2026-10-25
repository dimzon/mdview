using System.IO;
using System.Windows;

namespace MdView;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            try
            {
                var logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MdView", "error.log");
                Directory.CreateDirectory(Path.GetDirectoryName(logPath)!);
                File.WriteAllText(logPath, args.Exception.ToString());
            }
            catch { /* лог не критичен */ }

            MessageBox.Show(L10n.L("Msg.UnexpectedError") + "\n" + args.Exception.Message,
                "mdview", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        Settings.Load();
        new MainWindow(e.Args).Show();
    }
}
