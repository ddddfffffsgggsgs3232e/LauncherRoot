using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LauncherRoot.Models;
using LauncherRoot.Services;

namespace LauncherRoot.ViewModels;

public partial class PlayerSetupViewModel : ViewModelBase, IDisposable
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

    public void Dispose()
    {
        _microsoftAuth?.Dispose();
        GC.SuppressFinalize(this);
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
            ErrorMessage = _localization["error.duplicate_user"];
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

        var cfg = await _config.LoadConfigAsync();
        if (!string.IsNullOrWhiteSpace(cfg.MicrosoftClientId))
            _microsoftAuth.SetClientId(cfg.MicrosoftClientId);

        var result = await _microsoftAuth.StartDeviceFlowAsync(
            showCodeCallback: (code, url) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    MicrosoftCode = code;
                    MicrosoftUrl = url;
                    ShowMicrosoftCode = true;
                });
                return Task.CompletedTask;
            },
            updateStatusCallback: (statusKey) =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    MicrosoftStatus = _localization[statusKey];
                });
                return Task.CompletedTask;
            });

        IsMicrosoftLoggingIn = false;

        if (result.Success)
        {
            if (Accounts.Any(a => a.Username == result.Username))
            {
                ErrorMessage = $"{_localization["error.duplicate_user"]}: {result.Username}";
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
            ErrorMessage = result.ErrorMessage ?? _localization["error.microsoft_failed"];
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
