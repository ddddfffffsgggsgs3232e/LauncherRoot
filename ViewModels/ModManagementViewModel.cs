using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LauncherRoot.Models;
using LauncherRoot.Services;

namespace LauncherRoot.ViewModels;

public partial class ModManagementViewModel : ViewModelBase
{
    private readonly ConfigService _config;
    private readonly IModrinthService _modrinth;
    private readonly IInstanceService _instances;
    private readonly MainWindowViewModel _main;
    private readonly ILocalizationService _localization;
    private readonly Instance _instance;
    private readonly string _modsPath;

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

    [ObservableProperty]
    private string _instanceName = "";

    public ILocalizationService Localization => _localization;

    public ModManagementViewModel(
        ConfigService config,
        IModrinthService modrinth,
        IInstanceService instances,
        Instance instance,
        MainWindowViewModel main,
        ILocalizationService localization)
    {
        _config = config;
        _modrinth = modrinth;
        _instances = instances;
        _instance = instance;
        _main = main;
        _localization = localization;
        _modsPath = _instances.GetInstanceModsPath(instance);
        InstanceName = instance.DisplayName;
        Directory.CreateDirectory(_modsPath);
        _ = LoadInstalledModsAsync();
    }

    private async Task LoadInstalledModsAsync()
    {
        var modState = await _instances.LoadModStateAsync(_instance);
        var mods = new ObservableCollection<ModInfo>();

        if (Directory.Exists(_modsPath))
        {
            foreach (var file in Directory.GetFiles(_modsPath, "*.jar"))
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

            foreach (var file in Directory.GetFiles(_modsPath, "*.jar.disabled"))
            {
                var fileName = Path.GetFileName(file);
                var baseName = fileName[..^".disabled".Length];
                var slug = Path.GetFileNameWithoutExtension(baseName);
                mods.Add(new ModInfo
                {
                    Slug = slug,
                    Name = slug,
                    FileName = baseName,
                    Downloaded = true,
                    Enabled = false
                });
            }
        }

        InstalledMods = mods;
    }

    [RelayCommand]
    private async Task ToggleMod(ModInfo? mod)
    {
        if (mod == null || string.IsNullOrEmpty(mod.FileName)) return;

        // Binding has already updated mod.Enabled via ToggleSwitch
        var enabledPath = Path.Combine(_modsPath, mod.FileName);
        var disabledPath = enabledPath + ".disabled";

        try
        {
            if (mod.Enabled)
            {
                if (File.Exists(disabledPath))
                    File.Move(disabledPath, enabledPath);
            }
            else if (File.Exists(enabledPath))
            {
                File.Move(enabledPath, disabledPath);
            }

            var modState = await _instances.LoadModStateAsync(_instance);
            modState.ModEnabled[mod.Slug] = mod.Enabled;
            await _instances.SaveModStateAsync(_instance, modState);
        }
        catch (Exception ex)
        {
            _config.Log($"Mod durumu değiştirilemedi ({mod.Slug}): {ex.Message}");
        }
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task Search()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;

        IsSearching = true;
        SearchStatus = _localization["modmng.searching"];

        var results = await _modrinth.SearchModsAsync(SearchQuery, _instance.Loader);

        SearchResults.Clear();
        foreach (var hit in results)
            SearchResults.Add(hit);

        _ = LoadIconsAsync(results);

        SearchStatus = results.Count > 0
            ? $"{results.Count} {_localization["modmng.results"]}"
            : _localization["modmng.noresults"];
        IsSearching = false;
    }

    private static async Task LoadIconsAsync(List<ModrinthHit> hits)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("LauncherRoot/1.0");

        foreach (var hit in hits)
        {
            if (string.IsNullOrWhiteSpace(hit.IconUrl)) continue;
            try
            {
                var data = await client.GetByteArrayAsync(hit.IconUrl);
                using var ms = new MemoryStream(data);
                hit.Icon = new Bitmap(ms);
            }
            catch { }
        }
    }

    [RelayCommand(AllowConcurrentExecutions = true)]
    private async Task InstallMod(ModrinthHit? hit)
    {
        if (hit == null) return;

        try
        {
            var modInfo = await _modrinth.GetModInfoAsync(hit.Slug, _instance.Version, _instance.Loader);
            if (modInfo?.DownloadUrl == null) return;

            var fileName = modInfo.FileName ?? $"{hit.Slug}.jar";
            var filePath = Path.Combine(_modsPath, fileName);

            if (!File.Exists(filePath))
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
                client.DefaultRequestHeaders.UserAgent.ParseAdd("LauncherRoot/1.0");
                var response = await client.GetAsync(modInfo.DownloadUrl);
                response.EnsureSuccessStatusCode();
                await using var fs = File.Create(filePath);
                await response.Content.CopyToAsync(fs);
            }

            if (InstalledMods.Any(m => m.Slug == hit.Slug)) return;

            InstalledMods.Add(new ModInfo
            {
                Slug = hit.Slug,
                Name = hit.Title,
                FileName = fileName,
                Downloaded = true,
                Enabled = true
            });

            var modState = await _instances.LoadModStateAsync(_instance);
            modState.ModEnabled[hit.Slug] = true;
            await _instances.SaveModStateAsync(_instance, modState);
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
