using System.ComponentModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using LauncherRoot.Services;

namespace LauncherRoot.ViewModels;

public enum PageType
{
    PlayerSetup,
    ThemeSelection,
    Splash,
    MainMenu,
    ModManagement,
    Settings
}

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ConfigService _config;
    private readonly IModrinthService _modrinth;
    private readonly IMinecraftService _minecraft;
    private readonly IThemeService _themeService;
    private readonly ILocalizationService _localization;

    [ObservableProperty]
    private object? _currentViewModel;

    [ObservableProperty]
    private string _windowTitle = "LauncherRoot";

    public ILocalizationService Localization => _localization;

    public MainWindowViewModel(
        ConfigService config,
        IModrinthService modrinth,
        IMinecraftService minecraft,
        IThemeService themeService,
        ILocalizationService localization)
    {
        _config = config;
        _modrinth = modrinth;
        _minecraft = minecraft;
        _themeService = themeService;
        _localization = localization;

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

        var cfg = await _config.LoadConfigAsync();
        if (string.IsNullOrEmpty(cfg.SelectedTheme))
            NavigateTo(PageType.ThemeSelection);
        else
            NavigateTo(PageType.MainMenu);
    }

    public void NavigateTo(PageType page)
    {
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
                CurrentViewModel = new MainMenuViewModel(_config, _modrinth, _minecraft, this, _localization);
                break;
            case PageType.ModManagement:
                CurrentViewModel = new ModManagementViewModel(_config, _modrinth, this, _localization);
                break;
            case PageType.Settings:
                CurrentViewModel = new SettingsViewModel(_config, this, _localization);
                break;
        }
    }
}
