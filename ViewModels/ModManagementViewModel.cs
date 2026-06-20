using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LauncherRoot.Models;
using LauncherRoot.Services;

namespace LauncherRoot.ViewModels;

public partial class ModManagementViewModel : ViewModelBase
{
    private readonly ConfigService _config;
    private readonly IModrinthService _modrinth;
    private readonly MainWindowViewModel _main;
    private readonly ILocalizationService _localization;

    [ObservableProperty]
    private ObservableCollection<ModInfo> _installedMods = [];

    [ObservableProperty]
    private ObservableCollection<ModrinthHit> _searchResults = [];

    [ObservableProperty]
    private string _searchQuery = "";

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private string _searchStatus = "";

    public ILocalizationService Localization => _localization;

    public ModManagementViewModel(
        ConfigService config,
        IModrinthService modrinth,
        MainWindowViewModel main,
        ILocalizationService localization)
    {
        _config = config;
        _modrinth = modrinth;
        _main = main;
        _localization = localization;
        _ = LoadInstalledModsAsync();
    }

    private async Task LoadInstalledModsAsync()
    {
        var modsPath = _config.GetModsPath();
        var modState = await _config.LoadModStateAsync();
        var mods = new ObservableCollection<ModInfo>();

        if (Directory.Exists(modsPath))
        {
            foreach (var file in Directory.GetFiles(modsPath, "*.jar"))
            {
                var fileName = Path.GetFileName(file);
                var slug = Path.GetFileNameWithoutExtension(file);
                mods.Add(new ModInfo
                {
                    Slug = slug,
                    Name = slug,
                    FileName = fileName,
                    Downloaded = true,
                    Enabled = modState.ModEnabled.GetValueOrDefault(slug, true)
                });
            }
        }

        InstalledMods = mods;
    }

    [RelayCommand]
    private async Task ToggleMod(ModInfo? mod)
    {
        if (mod == null) return;

        mod.Enabled = !mod.Enabled;

        var modState = await _config.LoadModStateAsync();
        modState.ModEnabled[mod.Slug] = mod.Enabled;
        await _config.SaveModStateAsync(modState);
    }

    [RelayCommand]
    private async Task Search()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;

        IsSearching = true;
        SearchStatus = _localization["modmng.searching"];

        var results = await _modrinth.SearchModsAsync(SearchQuery);
        SearchResults = new ObservableCollection<ModrinthHit>(results);
        SearchStatus = results.Count > 0 ? $"{results.Count} {_localization["modmng.results"]}" : _localization["modmng.noresults"];
        IsSearching = false;
    }

    [RelayCommand]
    private async Task InstallMod(ModrinthHit? hit)
    {
        if (hit == null) return;

        try
        {
            var modInfo = await _modrinth.GetModInfoAsync(hit.Slug);
            if (modInfo?.DownloadUrl == null) return;

            var modsPath = _config.GetModsPath();
            var fileName = modInfo.FileName ?? $"{hit.Slug}.jar";
            var filePath = Path.Combine(modsPath, fileName);

            if (!File.Exists(filePath))
            {
                using var client = new HttpClient();
                var response = await client.GetAsync(modInfo.DownloadUrl);
                response.EnsureSuccessStatusCode();
                await using var fs = File.Create(filePath);
                await response.Content.CopyToAsync(fs);
            }

            InstalledMods.Add(new ModInfo
            {
                Slug = hit.Slug,
                Name = hit.Title,
                FileName = fileName,
                Downloaded = true,
                Enabled = true
            });
        }
        catch (Exception ex)
        {
            _config.Log($"Mod yükleme hatası ({hit.Slug}): {ex.Message}");
        }
    }

    [RelayCommand]
    private void Back()
    {
        _main.NavigateTo(PageType.MainMenu);
    }
}
