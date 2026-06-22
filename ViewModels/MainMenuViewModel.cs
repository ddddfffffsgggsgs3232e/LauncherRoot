using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LauncherRoot.Models;
using LauncherRoot.Services;

namespace LauncherRoot.ViewModels;

public partial class MainMenuViewModel : ViewModelBase
{
    private readonly ConfigService _config;
    private readonly IModrinthService _modrinth;
    private readonly IMinecraftService _minecraft;
    private readonly MainWindowViewModel _main;
    private readonly ILocalizationService _localization;
    private readonly IInstanceService _instances;

    [ObservableProperty]
    private string _playerUsername = "";

    [ObservableProperty]
    private string _playerInitial = "";

    [ObservableProperty]
    private bool _showStatusBar;

    [ObservableProperty]
    private string _statusText = "";

    [ObservableProperty]
    private double _statusProgress;

    [ObservableProperty]
    private ObservableCollection<Instance> _instanceList = [];

    [ObservableProperty]
    private Instance? _selectedInstance;

    [ObservableProperty]
    private bool _hasInstances;

    public IBrush BackgroundBrush { get; }

    public ILocalizationService Localization => _localization;

    public MainMenuViewModel(
        ConfigService config,
        IModrinthService modrinth,
        IMinecraftService minecraft,
        MainWindowViewModel main,
        ILocalizationService localization,
        IInstanceService instances)
    {
        _config = config;
        _modrinth = modrinth;
        _minecraft = minecraft;
        _main = main;
        _localization = localization;
        _instances = instances;

        BackgroundBrush = new LinearGradientBrush
        {
            StartPoint = RelativePoint.TopLeft,
            EndPoint = RelativePoint.BottomRight,
            GradientStops =
            {
                new GradientStop(Color.Parse("#0D1117"), 0),
                new GradientStop(Color.Parse("#1a2332"), 0.5),
                new GradientStop(Color.Parse("#0f1923"), 1),
            }
        };

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var player = await _config.LoadPlayerAsync();
        PlayerUsername = player.Username;
        PlayerInitial = !string.IsNullOrEmpty(player.Username) ? player.Username[..1].ToUpperInvariant() : "?";

        var allInstances = await _instances.LoadInstancesAsync();
        InstanceList = new ObservableCollection<Instance>(allInstances);
        HasInstances = InstanceList.Count > 0;

        var cfg = await _config.LoadConfigAsync();
        if (cfg.SelectedInstanceId != null)
            SelectedInstance = InstanceList.FirstOrDefault(i => i.Id == cfg.SelectedInstanceId);
        if (SelectedInstance == null && InstanceList.Count > 0)
            SelectedInstance = InstanceList[0];
    }

    partial void OnSelectedInstanceChanged(Instance? value)
    {
        if (value != null)
        {
            _ = SelectInstanceAsync(value);
        }
    }

    private async Task SelectInstanceAsync(Instance instance)
    {
        var cfg = await _config.LoadConfigAsync();
        cfg.SelectedInstanceId = instance.Id;
        await _config.SaveConfigAsync(cfg);
    }

    [RelayCommand]
    private void SelectInstance(Instance? instance)
    {
        if (instance == null) return;
        SelectedInstance = instance;
    }

    [RelayCommand]
    private void AddInstance()
    {
        _main.NavigateTo(PageType.InstanceCreate);
    }

    [RelayCommand]
    private void OpenModManagement()
    {
        if (SelectedInstance == null)
        {
            _main.NavigateTo(PageType.InstanceCreate);
            return;
        }

        _main.NavigateTo(PageType.ModManagement);
    }

    [RelayCommand]
    private void OpenPlayerSetup()
    {
        _main.NavigateTo(PageType.PlayerSetup);
    }

    [RelayCommand]
    private void OpenSettings()
    {
        _main.NavigateTo(PageType.Settings);
    }

    [RelayCommand]
    private void EditInstance(Instance? instance)
    {
        if (instance == null) return;
        _main.EditingInstance = instance;
        _main.NavigateTo(PageType.InstanceEdit);
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task DeleteInstance(Instance? instance)
    {
        if (instance == null) return;

        await _instances.DeleteInstanceAsync(instance.Id);
        InstanceList.Remove(instance);

        HasInstances = InstanceList.Count > 0;

        if (SelectedInstance == instance)
        {
            SelectedInstance = InstanceList.FirstOrDefault();
            if (SelectedInstance == null)
            {
                var cfg = await _config.LoadConfigAsync();
                cfg.SelectedInstanceId = null;
                await _config.SaveConfigAsync(cfg);
            }
        }
    }

    [RelayCommand]
    private void OpenLogsFolder(Instance? instance)
    {
        if (instance == null) return;
        var logsPath = Path.Combine(_instances.GetInstanceGamePath(instance), "minecraft", "logs");
        if (!Directory.Exists(logsPath))
            logsPath = _config.GetLogsPath();
        if (Directory.Exists(logsPath))
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = logsPath,
                UseShellExecute = true,
            };
            System.Diagnostics.Process.Start(psi);
        }
    }

    [RelayCommand]
    private async Task DuplicateInstance(Instance? instance)
    {
        if (instance == null) return;
        var clone = await _instances.DuplicateInstanceAsync(instance, $"{instance.Name} (kopya)");
        InstanceList.Add(clone);
    }

    [RelayCommand]
    private void OpenInstanceFolder(Instance? instance)
    {
        if (instance == null) return;
        var path = _instances.GetInstanceGamePath(instance);
        if (Directory.Exists(path))
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true,
            };
            System.Diagnostics.Process.Start(psi);
        }
    }

    [RelayCommand]
    private void OpenLogViewer()
    {
        var window = new Views.LogViewerWindow();
        window.Show();
    }

    [RelayCommand]
    private void OpenScreenshotsFolder(Instance? instance)
    {
        if (instance == null) return;
        var path = Path.Combine(_instances.GetInstanceGamePath(instance), "minecraft", "screenshots");
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
        };
        System.Diagnostics.Process.Start(psi);
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task Play()
    {
        var launcherConfig = await _config.LoadConfigAsync();
        var player = await _config.LoadPlayerAsync();

        if (string.IsNullOrWhiteSpace(player.Username))
        {
            _main.NavigateTo(PageType.PlayerSetup);
            return;
        }

        var instance = SelectedInstance;
        if (instance == null)
        {
            _main.NavigateTo(PageType.InstanceCreate);
            return;
        }

        ShowStatusBar = true;
        StatusText = _localization["status.preparing"];

        var progress = new Progress<double>(p =>
        {
            Dispatcher.UIThread.InvokeAsync(() =>
            {
                StatusProgress = p;
                StatusText = p switch
                {
                    < 0.3 => _localization["status.preparing"],
                    < 0.6 => _localization["status.downloading.minecraft"],
                    < 0.9 => _localization["status.downloading.libraries"],
                    _ => _localization["status.preparing"],
                };
            });
        });

        _minecraft.Progress = progress;
        StatusProgress = 0;
        StatusText = _localization["status.preparing"];

        var assetsDir = _config.GetAssetsPath();
        var ready = await _minecraft.EnsureInstanceReadyAsync(instance, assetsDir);
        _minecraft.Progress = null;

        if (!ready)
        {
            StatusText = _minecraft.LastError ?? _localization["error.download"];
            await Task.Delay(5000);
            ShowStatusBar = false;
            return;
        }

        StatusProgress = 0;
        StatusText = _localization["status.launching"];

        var launched = await _minecraft.LaunchInstanceAsync(
            instance, player.Username, launcherConfig.RamGB, _config.GetAssetsPath(),
            player.AccessToken, player.Uuid);

        if (launched)
        {
            Environment.Exit(0);
        }
        else
        {
            StatusText = _minecraft.LastError ?? _localization["error.java"];
            await Task.Delay(5000);
            ShowStatusBar = false;
        }
    }
}
