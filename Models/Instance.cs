namespace LauncherRoot.Models;

public class Instance
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; set; } = "";
    public string Version { get; set; } = "1.21.4";
    public string Loader { get; set; } = "vanilla";
    public string? LoaderVersion { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string DisplayName => string.IsNullOrEmpty(Name)
        ? $"{Version} ({Loader})"
        : Name;

    public string Icon => Loader switch
    {
        "fabric" => "🔧",
        "forge" => "⚒️",
        "neoforge" => "🔶",
        "quilt" => "🪡",
        _ => "🟩",
    };

    public string LoaderLabel => Loader switch
    {
        "fabric" => "Fabric",
        "forge" => "Forge",
        "neoforge" => "NeoForge",
        "quilt" => "Quilt",
        _ => "Vanilla",
    };

    public string InstanceDir => $"{Version}-{Loader}-{Id}";
}
