using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LauncherRoot.Models;
using LauncherRoot.Services;

namespace LauncherRoot.ViewModels;

public partial class PlayerSetupViewModel : ViewModelBase
{
    private readonly ConfigService _config;
    private readonly MainWindowViewModel _main;
    private readonly ILocalizationService _localization;

    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private string _errorMessage = "";

    public ILocalizationService Localization => _localization;

    public PlayerSetupViewModel(ConfigService config, MainWindowViewModel main, ILocalizationService localization)
    {
        _config = config;
        _main = main;
        _localization = localization;
    }

    [RelayCommand]
    private async Task Continue()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = _localization["error.noplayer"];
            return;
        }

        await _config.SavePlayerAsync(new PlayerConfig { Username = Username.Trim() });
        _config.Log($"Kullanıcı kaydedildi: {Username}");
        _main.NavigateTo(PageType.ThemeSelection);
    }
}
