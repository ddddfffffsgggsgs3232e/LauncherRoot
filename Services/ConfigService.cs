using System.Text.Json;
using System.Threading.Tasks;
using LauncherRoot.Models;

namespace LauncherRoot.Services;

public class ConfigService : IConfigService
{
    public string RootPath { get; }

    public ConfigService()
    {
        RootPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".LauncherRoot");
        Directory.CreateDirectory(RootPath);
        Directory.CreateDirectory(GetModsPath());
        Directory.CreateDirectory(GetMinecraftPath());
        Directory.CreateDirectory(GetLogsPath());
    }

    public string GetModsPath() => Path.Combine(RootPath, "mods");
    public string GetMinecraftPath() => Path.Combine(RootPath, "minecraft");
    public string GetLogsPath() => Path.Combine(RootPath, "logs");
    public string GetAssetsPath() => Path.Combine(GetMinecraftPath(), "assets");

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true
    };

    public async Task<PlayerConfig> LoadPlayerAsync()
    {
        var path = Path.Combine(RootPath, "player.json");
        if (File.Exists(path))
        {
            await using var fs = File.OpenRead(path);
            var old = await JsonSerializer.DeserializeAsync<PlayerConfig>(fs, JsonOpts);
            if (old != null && !string.IsNullOrEmpty(old.Username))
            {
                // Migrate old single player to accounts list
                var cfg = await LoadConfigAsync();
                if (cfg.Accounts.Count == 0)
                {
                    old.Id = "active";
                    cfg.Accounts.Add(old);
                    cfg.ActiveAccountId = "active";
                    await SaveConfigAsync(cfg);
                }
                File.Delete(path);
                return old;
            }
        }

        var config = await LoadConfigAsync();
        var active = config.Accounts.Find(a => a.Id == config.ActiveAccountId);
        return active ?? config.Accounts.FirstOrDefault() ?? new PlayerConfig();
    }

    public async Task SavePlayerAsync(PlayerConfig player)
    {
        var cfg = await LoadConfigAsync();
        var idx = cfg.Accounts.FindIndex(a => a.Id == player.Id);
        if (idx >= 0)
            cfg.Accounts[idx] = player;
        else
            cfg.Accounts.Add(player);
        cfg.ActiveAccountId = player.Id;
        await SaveConfigAsync(cfg);
    }

    public async Task DeleteAccountAsync(string accountId)
    {
        var cfg = await LoadConfigAsync();
        cfg.Accounts.RemoveAll(a => a.Id == accountId);
        if (cfg.ActiveAccountId == accountId)
            cfg.ActiveAccountId = cfg.Accounts.FirstOrDefault()?.Id ?? "";
        await SaveConfigAsync(cfg);
    }

    public async Task<List<PlayerConfig>> LoadAccountsAsync()
    {
        var cfg = await LoadConfigAsync();
        return cfg.Accounts;
    }

    public async Task SwitchAccountAsync(string accountId)
    {
        var cfg = await LoadConfigAsync();
        cfg.ActiveAccountId = accountId;
        await SaveConfigAsync(cfg);
    }

    public async Task<LauncherConfig> LoadConfigAsync()
    {
        var path = Path.Combine(RootPath, "config.json");
        if (!File.Exists(path)) return new LauncherConfig();
        await using var fs = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<LauncherConfig>(fs, JsonOpts) ?? new LauncherConfig();
    }

    public async Task SaveConfigAsync(LauncherConfig config)
    {
        var path = Path.Combine(RootPath, "config.json");
        await using var fs = File.Create(path);
        await JsonSerializer.SerializeAsync(fs, config, JsonOpts);
    }

    public async Task<ModState> LoadModStateAsync()
    {
        var path = Path.Combine(RootPath, "modstate.json");
        if (!File.Exists(path)) return new ModState();
        await using var fs = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<ModState>(fs, JsonOpts) ?? new ModState();
    }

    public async Task SaveModStateAsync(ModState state)
    {
        var path = Path.Combine(RootPath, "modstate.json");
        await using var fs = File.Create(path);
        await JsonSerializer.SerializeAsync(fs, state, JsonOpts);
    }

    public void Log(string message)
    {
        try
        {
            var logPath = Path.Combine(GetLogsPath(), $"launcher-{DateTime.Now:yyyy-MM-dd}.log");
            File.AppendAllText(logPath, $"[{DateTime.Now:HH:mm:ss}] {message}{Environment.NewLine}");
        }
        catch { }
    }

    public void ResetAll()
    {
        if (Directory.Exists(RootPath))
            Directory.Delete(RootPath, true);

        Directory.CreateDirectory(RootPath);
        Directory.CreateDirectory(GetModsPath());
        Directory.CreateDirectory(GetMinecraftPath());
        Directory.CreateDirectory(GetLogsPath());
        Directory.CreateDirectory(Path.Combine(RootPath, "instances"));
    }
}
