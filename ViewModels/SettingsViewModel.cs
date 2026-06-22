using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LauncherRoot;
using LauncherRoot.Models;
using LauncherRoot.Services;

namespace LauncherRoot.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    private readonly ConfigService _config;
    private readonly MainWindowViewModel _main;
    private readonly ILocalizationService _localization;

    [ObservableProperty]
    private int _ramGB = 4;

    [ObservableProperty]
    private int _selectedFpsIndex;

    [ObservableProperty]
    private bool _isDarkTheme = true;

    [ObservableProperty]
    private bool _isTurkish = true;

    [ObservableProperty]
    private string _jvmArgs = "";

    [ObservableProperty]
    private string _preLaunchCommand = "";

    [ObservableProperty]
    private string _wrapperCommand = "";

    [ObservableProperty]
    private string _postExitCommand = "";

    [ObservableProperty]
    private string _javaPath = "";

    [ObservableProperty]
    private int _windowWidth = 854;

    [ObservableProperty]
    private int _windowHeight = 480;

    public ILocalizationService Localization => _localization;

    public ObservableCollection<string> FpsOptions { get; } =
    [
        "30", "60", "120", "144", "240", "Sınırsız"
    ];

    public SettingsViewModel(ConfigService config, MainWindowViewModel main, ILocalizationService localization)
    {
        _config = config;
        _main = main;
        _localization = localization;
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var cfg = await _config.LoadConfigAsync();
        RamGB = cfg.RamGB;
        IsDarkTheme = cfg.DarkTheme;
        IsTurkish = cfg.Language == "tr";
        JvmArgs = cfg.JvmArgs;
        if (string.IsNullOrWhiteSpace(JvmArgs))
            JvmArgs = "-XX:+UseG1GC -XX:+UnlockExperimentalVMOptions -XX:MaxGCPauseMillis=50 -XX:+DisableExplicitGC";

        PreLaunchCommand = cfg.PreLaunchCommand ?? "";
        WrapperCommand = cfg.WrapperCommand ?? "";
        PostExitCommand = cfg.PostExitCommand ?? "";

        JavaPath = cfg.JavaPath ?? "";
        WindowWidth = cfg.WindowWidth;
        WindowHeight = cfg.WindowHeight;

        App.SetTheme(cfg.DarkTheme);

        SelectedFpsIndex = cfg.FpsLimit switch
        {
            30 => 0,
            60 => 1,
            120 => 2,
            144 => 3,
            240 => 4,
            _ => 5,
        };
    }

    partial void OnRamGBChanged(int value)
    {
        _ = SaveRamAsync(value);
    }

    partial void OnSelectedFpsIndexChanged(int value)
    {
        _ = SaveFpsAsync(value);
    }

    partial void OnIsDarkThemeChanged(bool value)
    {
        _ = SaveThemeAsync(value);
    }

    partial void OnIsTurkishChanged(bool value)
    {
        var lang = value ? "tr" : "en";
        _ = SaveLanguageAsync(lang);
    }

    partial void OnJvmArgsChanged(string value)
    {
        _ = SaveJvmArgsAsync(value);
    }

    partial void OnPreLaunchCommandChanged(string value)
    {
        _ = SaveStringAsync(nameof(LauncherConfig.PreLaunchCommand), value);
    }

    partial void OnWrapperCommandChanged(string value)
    {
        _ = SaveStringAsync(nameof(LauncherConfig.WrapperCommand), value);
    }

    partial void OnPostExitCommandChanged(string value)
    {
        _ = SaveStringAsync(nameof(LauncherConfig.PostExitCommand), value);
    }

    partial void OnJavaPathChanged(string value)
    {
        _ = SaveStringAsync(nameof(LauncherConfig.JavaPath), value);
    }

    partial void OnWindowWidthChanged(int value)
    {
        _ = SaveIntAsync(nameof(LauncherConfig.WindowWidth), value);
    }

    partial void OnWindowHeightChanged(int value)
    {
        _ = SaveIntAsync(nameof(LauncherConfig.WindowHeight), value);
    }

    private async Task SaveStringAsync(string property, string value)
    {
        try
        {
            var cfg = await _config.LoadConfigAsync();
            var prop = typeof(LauncherConfig).GetProperty(property);
            if (prop != null)
            {
                prop.SetValue(cfg, value);
                await _config.SaveConfigAsync(cfg);
            }
        }
        catch (Exception ex)
        {
            _config.Log($"{property} kaydetme hatası: {ex.Message}");
        }
    }

    private async Task SaveIntAsync(string property, int value)
    {
        try
        {
            var cfg = await _config.LoadConfigAsync();
            var prop = typeof(LauncherConfig).GetProperty(property);
            if (prop != null)
            {
                prop.SetValue(cfg, value);
                await _config.SaveConfigAsync(cfg);
            }
        }
        catch (Exception ex)
        {
            _config.Log($"{property} kaydetme hatası: {ex.Message}");
        }
    }

    private async Task SaveRamAsync(int value)
    {
        try
        {
            var cfg = await _config.LoadConfigAsync();
            cfg.RamGB = value;
            await _config.SaveConfigAsync(cfg);
        }
        catch (Exception ex)
        {
            _config.Log($"RAM kaydetme hatası: {ex.Message}");
        }
    }

    private async Task SaveFpsAsync(int index)
    {
        try
        {
            var cfg = await _config.LoadConfigAsync();
            cfg.FpsLimit = index switch
            {
                0 => 30,
                1 => 60,
                2 => 120,
                3 => 144,
                4 => 240,
                _ => 0,
            };
            await _config.SaveConfigAsync(cfg);
        }
        catch (Exception ex)
        {
            _config.Log($"FPS kaydetme hatası: {ex.Message}");
        }
    }

    private async Task SaveThemeAsync(bool dark)
    {
        try
        {
            var cfg = await _config.LoadConfigAsync();
            cfg.DarkTheme = dark;
            await _config.SaveConfigAsync(cfg);
            App.SetTheme(dark);
        }
        catch (Exception ex)
        {
            _config.Log($"Tema kaydetme hatası: {ex.Message}");
        }
    }

    private async Task SaveLanguageAsync(string lang)
    {
        try
        {
            var cfg = await _config.LoadConfigAsync();
            cfg.Language = lang;
            await _config.SaveConfigAsync(cfg);
            await _localization.SetLanguageAsync(lang);
        }
        catch (Exception ex)
        {
            _config.Log($"Dil kaydetme hatası: {ex.Message}");
        }
    }

    private async Task SaveJvmArgsAsync(string value)
    {
        try
        {
            var cfg = await _config.LoadConfigAsync();
            cfg.JvmArgs = value;
            await _config.SaveConfigAsync(cfg);
        }
        catch (Exception ex)
        {
            _config.Log($"JVM argüman kaydetme hatası: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Back()
    {
        _main.NavigateTo(PageType.MainMenu);
    }

    [RelayCommand]
    private async Task ResetLauncher()
    {
        try
        {
            _config.ResetAll();

            App.SetTheme(true);
            await _localization.SetLanguageAsync("tr");

            _main.NavigateTo(PageType.PlayerSetup);
        }
        catch (Exception ex)
        {
            _config.Log($"Sıfırlama hatası: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task BrowseJava()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.MainWindow;
            if (window == null) return;
            var files = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Java yürütülebilir dosyasını seçin",
                AllowMultiple = false,
            });
            if (files.Count > 0)
                JavaPath = files[0].Path.LocalPath;
        }
    }
}
