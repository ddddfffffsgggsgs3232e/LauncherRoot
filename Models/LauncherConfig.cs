namespace LauncherRoot.Models;

public class LauncherConfig
{
    public int RamGB { get; set; } = 4;
    public int FpsLimit { get; set; } = 60;
    public bool DarkTheme { get; set; } = true;
    public string Language { get; set; } = "tr";
    public string? SelectedTheme { get; set; }
    public bool FabricInstalled { get; set; }
}
