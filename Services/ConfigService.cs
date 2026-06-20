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

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true
    };

    public async Task<PlayerConfig> LoadPlayerAsync()
    {
        var path = Path.Combine(RootPath, "player.json");
        if (!File.Exists(path)) return new PlayerConfig();
        await using var fs = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<PlayerConfig>(fs, JsonOpts) ?? new PlayerConfig();
    }

    public async Task SavePlayerAsync(PlayerConfig config)
    {
        var path = Path.Combine(RootPath, "player.json");
        await using var fs = File.Create(path);
        await JsonSerializer.SerializeAsync(fs, config, JsonOpts);
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
    }
}
