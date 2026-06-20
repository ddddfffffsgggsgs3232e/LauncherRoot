using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LauncherRoot;
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

    private async Task SaveRamAsync(int value)
    {
        var cfg = await _config.LoadConfigAsync();
        cfg.RamGB = value;
        await _config.SaveConfigAsync(cfg);
    }

    private async Task SaveFpsAsync(int index)
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

    private async Task SaveThemeAsync(bool dark)
    {
        var cfg = await _config.LoadConfigAsync();
        cfg.DarkTheme = dark;
        await _config.SaveConfigAsync(cfg);
        App.SetTheme(dark);
    }

    private async Task SaveLanguageAsync(string lang)
    {
        var cfg = await _config.LoadConfigAsync();
        cfg.Language = lang;
        await _config.SaveConfigAsync(cfg);
        await _localization.SetLanguageAsync(lang);
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
}
