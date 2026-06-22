using System.Collections.ObjectModel;
using System.Linq;
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
    private readonly MicrosoftAuthService _microsoftAuth;

    [ObservableProperty]
    private string _username = "";

    [ObservableProperty]
    private string _errorMessage = "";

    [ObservableProperty]
    private bool _isMicrosoftLoggingIn;

    [ObservableProperty]
    private bool _showMicrosoftCode;

    [ObservableProperty]
    private string _microsoftCode = "";

    [ObservableProperty]
    private string _microsoftUrl = "";

    [ObservableProperty]
    private string _microsoftStatus = "";

    [ObservableProperty]
    private ObservableCollection<PlayerConfig> _accounts = [];

    [ObservableProperty]
    private PlayerConfig? _selectedAccount;

    public ILocalizationService Localization => _localization;

    public PlayerSetupViewModel(ConfigService config, MainWindowViewModel main, ILocalizationService localization)
    {
        _config = config;
        _main = main;
        _localization = localization;
        _microsoftAuth = new MicrosoftAuthService(config);
        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var allAccounts = await _config.LoadAccountsAsync();
        Accounts = new ObservableCollection<PlayerConfig>(allAccounts);
        var active = await _config.LoadPlayerAsync();
        if (active != null && !string.IsNullOrEmpty(active.Username))
            SelectedAccount = Accounts.FirstOrDefault(a => a.Id == active.Id) ?? Accounts.FirstOrDefault();
        else
            SelectedAccount = Accounts.FirstOrDefault();
    }

    partial void OnSelectedAccountChanged(PlayerConfig? value)
    {
        if (value != null)
            _ = _config.SwitchAccountAsync(value.Id);
    }

    [RelayCommand]
    private async Task Continue()
    {
        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = _localization["error.noplayer"];
            return;
        }

        var trimmed = Username.Trim();
        if (Accounts.Any(a => a.Username == trimmed))
        {
            ErrorMessage = "Bu kullanıcı adı zaten kayıtlı.";
            return;
        }

        var player = new PlayerConfig { Username = trimmed };
        await _config.SavePlayerAsync(player);
        _config.Log($"Kullanıcı kaydedildi: {Username}");

        Accounts.Add(player);
        SelectedAccount = player;

        _main.NavigateTo(PageType.MainMenu);
    }

    [RelayCommand]
    private async Task MicrosoftLogin()
    {
        IsMicrosoftLoggingIn = true;
        ErrorMessage = "";
        ShowMicrosoftCode = false;

        var result = await _microsoftAuth.StartDeviceFlowAsync(
            showCodeCallback: (code, url) =>
            {
                MicrosoftCode = code;
                MicrosoftUrl = url;
                ShowMicrosoftCode = true;
                return Task.CompletedTask;
            },
            updateStatusCallback: (status) =>
            {
                MicrosoftStatus = status;
                return Task.CompletedTask;
            });

        IsMicrosoftLoggingIn = false;

        if (result != null)
        {
            if (Accounts.Any(a => a.Username == result.Username))
            {
                ErrorMessage = $"\"{result.Username}\" kullanıcı adı zaten kayıtlı.";
                return;
            }

            var player = new PlayerConfig
            {
                Username = result.Username,
                Uuid = result.Uuid,
                AccessToken = result.AccessToken,
                AuthType = result.AuthType
            };
            await _config.SavePlayerAsync(player);
            Accounts.Add(player);
            SelectedAccount = player;
            _main.NavigateTo(PageType.MainMenu);
        }
        else
        {
            ShowMicrosoftCode = false;
            ErrorMessage = "Microsoft girişi başarısız. Lütfen tekrar deneyin.";
        }
    }

    [RelayCommand]
    private async Task SelectAccount(PlayerConfig? account)
    {
        if (account == null) return;
        await _config.SwitchAccountAsync(account.Id);
        _main.NavigateTo(PageType.MainMenu);
    }

    [RelayCommand]
    private void CancelMicrosoft()
    {
        ShowMicrosoftCode = false;
        IsMicrosoftLoggingIn = false;
    }

    [RelayCommand]
    private async Task DeleteAccount(PlayerConfig? account)
    {
        if (account == null) return;
        await _config.DeleteAccountAsync(account.Id);
        Accounts.Remove(account);
        if (SelectedAccount == account)
            SelectedAccount = Accounts.FirstOrDefault();
    }
}
