using System;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LauncherRoot.Services;

namespace LauncherRoot.ViewModels;

public partial class MainMenuViewModel : ViewModelBase
{
    private readonly ConfigService _config;
    private readonly IModrinthService _modrinth;
    private readonly IMinecraftService _minecraft;
    private readonly MainWindowViewModel _main;
    private readonly ILocalizationService _localization;

    [ObservableProperty]
    private string _playerUsername = "";

    [ObservableProperty]
    private bool _showStatusBar;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private double _statusProgress;

    public ILocalizationService Localization => _localization;

    public MainMenuViewModel(
        ConfigService config,
        IModrinthService modrinth,
        IMinecraftService minecraft,
        MainWindowViewModel main,
        ILocalizationService localization)
    {
        _config = config;
        _modrinth = modrinth;
        _minecraft = minecraft;
        _main = main;
        _localization = localization;
        _ = LoadPlayerAsync();
    }

    private async Task LoadPlayerAsync()
    {
        var player = await _config.LoadPlayerAsync();
        PlayerUsername = player.Username;
    }

    [RelayCommand]
    private void OpenModManagement()
    {
        _main.NavigateTo(PageType.ModManagement);
    }

    [RelayCommand]
    private void OpenSettings()
    {
        _main.NavigateTo(PageType.Settings);
    }

    [RelayCommand]
    private async Task Play()
    {
        var launcherConfig = await _config.LoadConfigAsync();
        var player = await _config.LoadPlayerAsync();

        if (string.IsNullOrWhiteSpace(player.Username))
        {
            _main.NavigateTo(PageType.PlayerSetup);
            return;
        }

        var minecraftDir = _config.GetMinecraftPath();
        var modsDir = _config.GetModsPath();

        ShowStatusBar = true;

        if (!launcherConfig.FabricInstalled)
        {
            var progress = new Progress<double>(p =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusProgress = p;
                    if (p < 0.3)
                        StatusText = _localization["status.downloading.fabric"];
                    else if (p < 0.6)
                        StatusText = _localization["status.downloading.minecraft"];
                    else if (p < 0.9)
                        StatusText = _localization["status.downloading.libraries"];
                    else
                        StatusText = _localization["status.preparing"];
                });
            });

            _minecraft.Progress = progress;
            StatusProgress = 0;
            StatusText = _localization["status.downloading.fabric"];

            var fabricOk = await _minecraft.EnsureFabricInstalledAsync(minecraftDir);
            _minecraft.Progress = null;

            if (!fabricOk)
            {
                ShowStatusBar = false;
                return;
            }
        }
        else
        {
            var progress = new Progress<double>(p =>
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    StatusProgress = p;
                    StatusText = _localization["status.downloading.minecraft"];
                });
            });

            _minecraft.Progress = progress;
            StatusProgress = 0;
            StatusText = _localization["status.downloading.minecraft"];

            await _minecraft.EnsureAssetsDownloadedAsync(minecraftDir);
            _minecraft.Progress = null;
        }

        ShowStatusBar = true;
        StatusProgress = 0;
        StatusText = _localization["status.launching"];

        var launched = await _minecraft.LaunchMinecraftAsync(
            player.Username, launcherConfig.RamGB, minecraftDir, modsDir);

        ShowStatusBar = false;

        if (launched)
        {
            Environment.Exit(0);
        }
        else
        {
            StatusText = _localization["error.java"];
            ShowStatusBar = true;
            await Task.Delay(3000);
            ShowStatusBar = false;
        }
    }
}
