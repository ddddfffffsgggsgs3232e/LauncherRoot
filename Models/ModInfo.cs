namespace LauncherRoot.Models;

public class ModInfo
{
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string? IconUrl { get; set; }
    public string? DownloadUrl { get; set; }
    public string? FileName { get; set; }
    public bool Enabled { get; set; } = true;
    public bool Downloaded { get; set; }
}
