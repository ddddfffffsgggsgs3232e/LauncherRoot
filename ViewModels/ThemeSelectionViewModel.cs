using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LauncherRoot.Models;
using LauncherRoot.Services;

namespace LauncherRoot.ViewModels;

public partial class ThemeSelectionViewModel : ViewModelBase
{
    private readonly ConfigService _config;
    private readonly IModrinthService _modrinth;
    private readonly MainWindowViewModel _main;
    private readonly ILocalizationService _localization;

    [ObservableProperty]
    private ObservableCollection<Theme> _themes = [];

    [ObservableProperty]
    private bool _isDownloading;

    [ObservableProperty]
    private double _downloadProgress;

    [ObservableProperty]
    private string _progressText = "";

    public ILocalizationService Localization => _localization;

    public ThemeSelectionViewModel(ConfigService config, IModrinthService modrinth, MainWindowViewModel main, ILocalizationService localization)
    {
        _config = config;
        _modrinth = modrinth;
        _main = main;
        _localization = localization;
        LoadThemes();
    }

    private void LoadThemes()
    {
        Theme.CurrentLanguage = _localization.CurrentLanguage;
        var themeService = new ThemeService(_config);
        Themes = new ObservableCollection<Theme>(themeService.GetThemes());
    }

    [RelayCommand]
    private async Task SelectTheme(Theme? theme)
    {
        if (theme == null || IsDownloading) return;

        IsDownloading = true;
        DownloadProgress = 0;

        var launcherConfig = await _config.LoadConfigAsync();
        launcherConfig.SelectedTheme = theme.Id;
        await _config.SaveConfigAsync(launcherConfig);

        var modsPath = _config.GetModsPath();

        var progress = new Progress<double>(p =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                DownloadProgress = p;
                ProgressText = $"%{p * 100:F0}";
            });
        });

        _modrinth.Progress = progress;

        try
        {
            await _modrinth.DownloadThemeModsAsync(theme.Mods, modsPath, "1.21.4", "fabric");
            _config.Log($"Tema indirildi: {theme.Id}");
        }
        catch (Exception ex)
        {
            _config.Log($"Tema indirme hatası: {ex.Message}");
        }

        IsDownloading = false;
        _main.NavigateTo(PageType.Splash);
    }
}
