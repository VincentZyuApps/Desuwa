using System.Diagnostics;
using System.Runtime.InteropServices;
using Microsoft.UI.Xaml;

namespace Desuwa;

public partial class App : Application
{
    private const string SingleInstanceMutexName = @"Local\Desuwa.SingleInstance";
    private static Mutex? _singleInstanceMutex;
    private MainWindow? _mainWindow;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern int MessageBox(IntPtr hWnd, string text, string caption, uint type);

    public App()
    {
        AppLogger.Log("[App] Constructor start.");

        bool createdNew;
        try
        {
            _singleInstanceMutex = new Mutex(true, SingleInstanceMutexName, out createdNew);
        }
        catch (AbandonedMutexException)
        {
            createdNew = true;
        }

        AppLogger.Log($"[App] createdNew = {createdNew}");

        if (!createdNew)
        {
            AppLogger.Log("[App] Another instance is already running. Exiting.");
            MessageBox(IntPtr.Zero, "Desuwa 已经在运行。请先退出系统托盘中的现有实例。", "Desuwa", 0x00000040);
            Process.GetCurrentProcess().Kill();
            return;
        }

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            AppLogger.Log($"[AppDomainUnhandledException] {e.ExceptionObject}");
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            AppLogger.Log($"[UnobservedTaskException] {e.Exception}");
        };

        AppDomain.CurrentDomain.ProcessExit += (s, e) =>
        {
            AppLogger.Log($"[App] ProcessExit fired.");
        };

        UnhandledException += (s, e) =>
        {
            AppLogger.Log($"[XamlUnhandledException] {e.Message}\n{e.Exception}\nStackTrace:\n{e.Exception?.StackTrace}");
        };

        InitializeComponent();
        AppLogger.Log("[App] InitializeComponent completed.");
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        AppLogger.Log("[App] OnLaunched start.");
        try
        {
            _mainWindow = new MainWindow();
            _mainWindow.Activate();
            AppLogger.Log("[App] MainWindow created and activated successfully.");
        }
        catch (Exception ex)
        {
            AppLogger.Log($"[OnLaunchedException] {ex}");
            throw;
        }
    }
}
