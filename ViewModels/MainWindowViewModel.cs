using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using LauncherRoot.Models;
using LauncherRoot.Services;

namespace LauncherRoot.ViewModels;

public enum PageType
{
    PlayerSetup,
    ThemeSelection,
    Splash,
    MainMenu,
    ModManagement,
    Settings,
    InstanceCreate,
    InstanceEdit,
}

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ConfigService _config;
    private readonly IModrinthService _modrinth;
    private readonly IMinecraftService _minecraft;
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localization;
    private readonly IInstanceService _instances;

    [ObservableProperty]
    private object? _currentViewModel;

    [ObservableProperty]
    private string _windowTitle = "LauncherRoot";

    public ILocalizationService Localization => _localization;

    public event Action? Navigating;

    public Instance? EditingInstance { get; set; }

    public MainWindowViewModel(
        ConfigService config,
        IModrinthService modrinth,
        IMinecraftService minecraft,
        IThemeService themeService,
        ILocalizationService localization,
        IInstanceService instances)
    {
        _config = config;
        _modrinth = modrinth;
        _minecraft = minecraft;
        _themeService = themeService;
        _localization = localization;
        _instances = instances;

        _localization.PropertyChanged += OnLocalizationChanged;

        _ = InitializeAsync();
    }

    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == "Item" || e.PropertyName == "CurrentLanguage")
        {
            WindowTitle = _localization["app.title"];
        }
    }

    private async Task InitializeAsync()
    {
        var player = await _config.LoadPlayerAsync();
        if (string.IsNullOrWhiteSpace(player.Username))
        {
            NavigateTo(PageType.PlayerSetup);
            return;
        }

        NavigateTo(PageType.MainMenu);
    }

    public void NavigateTo(PageType page)
    {
        Navigating?.Invoke();

        switch (page)
        {
            case PageType.PlayerSetup:
                CurrentViewModel = new PlayerSetupViewModel(_config, this, _localization);
                break;
            case PageType.ThemeSelection:
                CurrentViewModel = new ThemeSelectionViewModel(_config, _modrinth, this, _localization);
                break;
            case PageType.Splash:
                CurrentViewModel = new SplashViewModel(this);
                break;
            case PageType.MainMenu:
                CurrentViewModel = new MainMenuViewModel(_config, _modrinth, _minecraft, this, _localization, _instances);
                break;
            case PageType.ModManagement:
                _ = NavigateToModManagementAsync();
                return;
            case PageType.Settings:
                CurrentViewModel = new SettingsViewModel(_config, this, _localization);
                break;
            case PageType.InstanceCreate:
                CurrentViewModel = new InstanceCreateViewModel(_config, _minecraft, _instances, this, _localization);
                break;
            case PageType.InstanceEdit:
                _ = NavigateToInstanceEditAsync();
                return;
        }
    }

    private async Task NavigateToModManagementAsync()
    {
        var cfg = await _config.LoadConfigAsync();
        if (string.IsNullOrEmpty(cfg.SelectedInstanceId))
        {
            NavigateTo(PageType.MainMenu);
            return;
        }

        var instance = await _instances.GetInstanceAsync(cfg.SelectedInstanceId);
        if (instance == null)
        {
            NavigateTo(PageType.MainMenu);
            return;
        }

        Navigating?.Invoke();
        CurrentViewModel = new ModManagementViewModel(
            _config, _modrinth, _instances, instance, this, _localization);
    }

    private Task NavigateToInstanceEditAsync()
    {
        var instance = EditingInstance;
        if (instance == null)
        {
            NavigateTo(PageType.MainMenu);
            return Task.CompletedTask;
        }

        Navigating?.Invoke();
        CurrentViewModel = new InstanceCreateViewModel(_config, _minecraft, _instances, this, _localization, instance);
        return Task.CompletedTask;
    }
}
