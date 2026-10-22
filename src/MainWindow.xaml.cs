using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using WinUIEx;

namespace Desuwa;

public sealed partial class MainWindow : WindowEx
{
    private static readonly VirtualKey[] SupportedKeys = CreateSupportedKeys();

    private readonly SettingsStore _settingsStore;
    private readonly ClipboardSuffixInjector _clipboardInjector;
    private readonly SimulatedTypingSuffixInjector _simulatedTypingInjector;
    private readonly MessageWindow _messageWindow;
    private readonly KeyboardHookService _keyboardHookService;
    private readonly GlobalHotkeyService _hotkeyService;

    private AppSettings _settings;
    private bool _isEnabled = true;
    private bool _isExiting;
    private MenuFlyoutItem? _trayToggleItem;
    private MenuFlyoutItem? _trayModeItem;

    public ICommand ShowSettingsCommand { get; }
    public ICommand ToggleEnabledCommand { get; }
    public ICommand ToggleModeCommand { get; }
    public ICommand ExitAppCommand { get; }

    public MainWindow()
    {
        AppLogger.Log("[MainWindow] Constructor start.");

        InitializeComponent();

        ShowSettingsCommand = new RelayCommand(ShowSettings);
        ToggleEnabledCommand = new RelayCommand(ToggleEnabled);
        ToggleModeCommand = new RelayCommand(ToggleMode);
        ExitAppCommand = new RelayCommand(ExitApp);

        _settingsStore = new SettingsStore();
        _settings = _settingsStore.Load();
        _isEnabled = true;

        _clipboardInjector = new ClipboardSuffixInjector();
        _simulatedTypingInjector = new SimulatedTypingSuffixInjector();
        _messageWindow = new MessageWindow();

        _keyboardHookService = new KeyboardHookService(
            () => _isEnabled,
            () => _settings.Suffix,
            GetCurrentInjector,
            ex =>
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    ShowStatus(InfoBarSeverity.Error, "键盘钩子错误", ex.Message);
                });
            });

        _hotkeyService = new GlobalHotkeyService(_messageWindow, HandleHotkeyAction);

        InitKeyComboBox(ToggleEnabledKeyCombo);
        InitKeyComboBox(ToggleModeKeyCombo);

        InitTrayMenu();

        LoadSettingsToUi(_settings);

        var hotkeyErrors = _hotkeyService.Reload(_settings);
        if (hotkeyErrors.Count > 0)
        {
            ShowStatus(InfoBarSeverity.Warning, "快捷键注册异常", string.Join("；", hotkeyErrors));
        }

        AppWindow.Closing += AppWindow_Closing;
        Closed += (_, _) => AppLogger.Log("[MainWindow] Closed event fired!");

        AppLogger.Log("[MainWindow] Constructor end.");
    }

    private void RootGrid_Loaded(object sender, RoutedEventArgs e)
    {
        AppLogger.Log("[RootGrid_Loaded] start.");

        var shouldShow = Environment.GetCommandLineArgs().Any(a =>
            string.Equals(a, "--show", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(a, "--settings", StringComparison.OrdinalIgnoreCase));

        if (!shouldShow)
        {
            AppWindow.Hide();
            AppLogger.Log("[RootGrid_Loaded] AppWindow.Hide() called for silent tray start.");
        }

        try
        {
            AppLogger.Log("[RootGrid_Loaded] Calling UpdateTrayPresentation()...");
            UpdateTrayPresentation();
            AppLogger.Log("[RootGrid_Loaded] Calling TrayIcon.ForceCreate()...");
            TrayIcon.ForceCreate();
            AppLogger.Log("[RootGrid_Loaded] Finished successfully.");
        }
        catch (Exception ex)
        {
            AppLogger.Log($"[RootGrid_Loaded] UpdateTrayPresentation exception: {ex}");
        }
    }

    private ISuffixInjector GetCurrentInjector()
    {
        return _settings.InjectionMode == SuffixInjectionMode.ClipboardPaste
            ? _clipboardInjector
            : _simulatedTypingInjector;
    }

    private void HandleHotkeyAction(HotkeyAction action)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            switch (action)
            {
                case HotkeyAction.ToggleEnabled:
                    _isEnabled = !_isEnabled;
                    UpdateTrayPresentation();
                    break;

                case HotkeyAction.ToggleMode:
                    _settings.InjectionMode = _settings.InjectionMode == SuffixInjectionMode.ClipboardPaste
                        ? SuffixInjectionMode.SimulatedTyping
                        : SuffixInjectionMode.ClipboardPaste;
                    _settingsStore.Save(_settings);
                    UpdateTrayPresentation();
                    LoadSettingsToUi(_settings);
                    break;
            }
        });
    }

    private void ToggleEnabled()
    {
        _isEnabled = !_isEnabled;
        UpdateTrayPresentation();
    }

    private void ToggleMode()
    {
        _settings.InjectionMode = _settings.InjectionMode == SuffixInjectionMode.ClipboardPaste
            ? SuffixInjectionMode.SimulatedTyping
            : SuffixInjectionMode.ClipboardPaste;
        _settingsStore.Save(_settings);
        UpdateTrayPresentation();
        LoadSettingsToUi(_settings);
    }

    private void InitTrayMenu()
    {
        var menu = new MenuFlyout();

        _trayToggleItem = new MenuFlyoutItem
        {
            Text = _isEnabled ? "已启用 (点击切换)" : "已禁用 (点击切换)",
            Command = ToggleEnabledCommand,
            Icon = new SymbolIcon(_isEnabled ? Symbol.Accept : Symbol.Cancel)
        };

        _trayModeItem = new MenuFlyoutItem
        {
            Text = $"当前模式: {(_settings.InjectionMode == SuffixInjectionMode.ClipboardPaste ? "剪贴板" : "模拟输入")} (点击切换)",
            Command = ToggleModeCommand,
            Icon = new SymbolIcon(Symbol.Sync)
        };

        var settingsItem = new MenuFlyoutItem
        {
            Text = "设置...",
            Command = ShowSettingsCommand,
            Icon = new SymbolIcon(Symbol.Setting)
        };

        var exitItem = new MenuFlyoutItem
        {
            Text = "退出",
            Command = ExitAppCommand,
            Icon = new SymbolIcon(Symbol.Cancel)
        };

        menu.Items.Add(_trayToggleItem);
        menu.Items.Add(_trayModeItem);
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(settingsItem);
        menu.Items.Add(new MenuFlyoutSeparator());
        menu.Items.Add(exitItem);

        TrayIcon.ContextFlyout = menu;
    }

    private void UpdateTrayPresentation()
    {
        var iconFile = _isEnabled ? "enabled.ico" : "disabled.ico";
        var iconPath = Path.Combine(AppContext.BaseDirectory, "Assets", iconFile);
        if (File.Exists(iconPath))
        {
            TrayIcon.Icon = new System.Drawing.Icon(iconPath);
        }

        var modeText = _settings.InjectionMode == SuffixInjectionMode.ClipboardPaste ? "剪贴板" : "模拟输入";
        TrayIcon.ToolTipText = _isEnabled
            ? $"Desuwa (已启用 - [{modeText}] {_settings.Suffix})"
            : "Desuwa (已禁用)";

        if (_trayToggleItem != null)
        {
            _trayToggleItem.Text = _isEnabled ? "已启用 (点击切换)" : "已禁用 (点击切换)";
            _trayToggleItem.Icon = new SymbolIcon(_isEnabled ? Symbol.Accept : Symbol.Cancel);
        }

        if (_trayModeItem != null)
        {
            _trayModeItem.Text = $"当前模式: {modeText} (点击切换)";
        }
    }

    private void ShowSettings()
    {
        AppWindow.Show();
        this.BringToFront();
    }

    private void AppWindow_Closing(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        AppLogger.Log($"[AppWindow_Closing] fired. isExiting: {_isExiting}");
        if (!_isExiting)
        {
            args.Cancel = true;
            AppWindow.Hide();
            AppLogger.Log("[AppWindow_Closing] args.Cancel set to true, AppWindow.Hide() called.");
        }
    }

    private void ExitApp()
    {
        _isExiting = true;
        TrayIcon.Dispose();
        _keyboardHookService.Dispose();
        _hotkeyService.Dispose();
        _messageWindow.Dispose();
        Close();
    }

    private void PresetButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Content is string text)
        {
            SuffixTextBox.Text = text;
        }
    }

    private void HideToTray_Click(object sender, RoutedEventArgs e)
    {
        AppWindow.Hide();
    }

    private void ResetDefaults_Click(object sender, RoutedEventArgs e)
    {
        var defaultSettings = new AppSettings();
        LoadSettingsToUi(defaultSettings);
        ShowStatus(InfoBarSeverity.Informational, "已重置为默认值", "请点击“保存设置”以使其生效。");
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        var toggleEnabledHotkey = ReadHotkey(
            ToggleEnabledEnableCheck,
            ToggleEnabledCtrlCheck,
            ToggleEnabledAltCheck,
            ToggleEnabledShiftCheck,
            ToggleEnabledKeyCombo);

        var toggleModeHotkey = ReadHotkey(
            ToggleModeEnableCheck,
            ToggleModeCtrlCheck,
            ToggleModeAltCheck,
            ToggleModeShiftCheck,
            ToggleModeKeyCombo);

        if (!toggleEnabledHotkey.IsValid())
        {
            ShowStatus(InfoBarSeverity.Error, "快捷键错误", "“切换启用状态”快捷键至少需要勾选一个修饰键（Ctrl/Alt/Shift）。");
            return;
        }

        if (!toggleModeHotkey.IsValid())
        {
            ShowStatus(InfoBarSeverity.Error, "快捷键错误", "“切换输入方式”快捷键至少需要勾选一个修饰键（Ctrl/Alt/Shift）。");
            return;
        }

        if (toggleEnabledHotkey.ConflictsWith(toggleModeHotkey))
        {
            ShowStatus(InfoBarSeverity.Error, "快捷键冲突", "两个快捷键不能设置为相同的按键组合。");
            return;
        }

        _settings.Suffix = string.IsNullOrEmpty(SuffixTextBox.Text) ? "desuwa" : SuffixTextBox.Text;
        _settings.InjectionMode = ClipboardModeRadio.IsChecked == true
            ? SuffixInjectionMode.ClipboardPaste
            : SuffixInjectionMode.SimulatedTyping;
        _settings.ToggleEnabledHotkey = toggleEnabledHotkey;
        _settings.ToggleModeHotkey = toggleModeHotkey;

        _settingsStore.Save(_settings);

        var hotkeyErrors = _hotkeyService.Reload(_settings);
        UpdateTrayPresentation();

        if (hotkeyErrors.Count > 0)
        {
            ShowStatus(InfoBarSeverity.Warning, "设置已保存，部分快捷键注册失败", string.Join("；", hotkeyErrors));
        }
        else
        {
            ShowStatus(InfoBarSeverity.Success, "设置已保存", "新的配置与全局快捷键已立即生效。");
        }
    }

    private void ShowStatus(InfoBarSeverity severity, string title, string message)
    {
        StatusInfoBar.Severity = severity;
        StatusInfoBar.Title = title;
        StatusInfoBar.Message = message;
        StatusInfoBar.IsOpen = true;
    }

    private void LoadSettingsToUi(AppSettings settings)
    {
        SuffixTextBox.Text = settings.Suffix;

        if (settings.InjectionMode == SuffixInjectionMode.ClipboardPaste)
        {
            ClipboardModeRadio.IsChecked = true;
        }
        else
        {
            SimulatedModeRadio.IsChecked = true;
        }

        LoadHotkey(
            settings.ToggleEnabledHotkey,
            ToggleEnabledEnableCheck,
            ToggleEnabledCtrlCheck,
            ToggleEnabledAltCheck,
            ToggleEnabledShiftCheck,
            ToggleEnabledKeyCombo);

        LoadHotkey(
            settings.ToggleModeHotkey,
            ToggleModeEnableCheck,
            ToggleModeCtrlCheck,
            ToggleModeAltCheck,
            ToggleModeShiftCheck,
            ToggleModeKeyCombo);
    }

    private static void LoadHotkey(
        HotkeyGesture gesture,
        CheckBox enableCheck,
        CheckBox ctrlCheck,
        CheckBox altCheck,
        CheckBox shiftCheck,
        ComboBox keyCombo)
    {
        enableCheck.IsChecked = gesture.Enabled;
        var modifiers = gesture.Enabled ? gesture.NormalizedModifiers : KeyModifiers.None;

        ctrlCheck.IsChecked = (modifiers & KeyModifiers.Control) != 0;
        altCheck.IsChecked = (modifiers & KeyModifiers.Alt) != 0;
        shiftCheck.IsChecked = (modifiers & KeyModifiers.Shift) != 0;

        var selectedKey = gesture.Enabled ? gesture.Key : VirtualKey.None;
        var index = Array.IndexOf(SupportedKeys, selectedKey);
        keyCombo.SelectedIndex = index >= 0 ? index : 0;
    }

    private static HotkeyGesture ReadHotkey(
        CheckBox enableCheck,
        CheckBox ctrlCheck,
        CheckBox altCheck,
        CheckBox shiftCheck,
        ComboBox keyCombo)
    {
        if (enableCheck.IsChecked != true)
        {
            return HotkeyGesture.Disabled();
        }

        var key = keyCombo.SelectedIndex >= 0 && keyCombo.SelectedIndex < SupportedKeys.Length
            ? SupportedKeys[keyCombo.SelectedIndex]
            : VirtualKey.None;

        if (key == VirtualKey.None)
        {
            return HotkeyGesture.Disabled();
        }

        var modifiers = KeyModifiers.None;
        if (ctrlCheck.IsChecked == true) modifiers |= KeyModifiers.Control;
        if (altCheck.IsChecked == true) modifiers |= KeyModifiers.Alt;
        if (shiftCheck.IsChecked == true) modifiers |= KeyModifiers.Shift;

        return new HotkeyGesture
        {
            Enabled = true,
            Key = key,
            Modifiers = modifiers
        };
    }

    private static void InitKeyComboBox(ComboBox comboBox)
    {
        comboBox.Items.Clear();
        foreach (var key in SupportedKeys)
        {
            comboBox.Items.Add(GetKeyDisplayName(key));
        }
    }

    private static string GetKeyDisplayName(VirtualKey key)
    {
        if (key == VirtualKey.None) return "(未设置)";
        if (key is >= VirtualKey.D0 and <= VirtualKey.D9) return ((char)('0' + (key - VirtualKey.D0))).ToString();
        return key.ToString();
    }

    private static VirtualKey[] CreateSupportedKeys()
    {
        var list = new List<VirtualKey> { VirtualKey.None };

        for (var k = VirtualKey.F1; k <= VirtualKey.F12; k++) list.Add(k);
        for (var k = VirtualKey.A; k <= VirtualKey.Z; k++) list.Add(k);
        for (var k = VirtualKey.D0; k <= VirtualKey.D9; k++) list.Add(k);

        return list.ToArray();
    }
}
