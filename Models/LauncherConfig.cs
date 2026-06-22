using System.Collections.Generic;

namespace LauncherRoot.Models;

public class LauncherConfig
{
    public int RamGB { get; set; } = 4;
    public int FpsLimit { get; set; } = 60;
    public bool DarkTheme { get; set; } = true;
    public string Language { get; set; } = "tr";
    public string? SelectedTheme { get; set; }
    public bool FabricInstalled { get; set; }
    public string? SelectedInstanceId { get; set; }
    public string JvmArgs { get; set; } = "-XX:+UseG1GC -XX:+UnlockExperimentalVMOptions -XX:MaxGCPauseMillis=50 -XX:+DisableExplicitGC";
    public string PreLaunchCommand { get; set; } = "";
    public string WrapperCommand { get; set; } = "";
    public string PostExitCommand { get; set; } = "";
    public string JavaPath { get; set; } = "";
    public int WindowWidth { get; set; } = 854;
    public int WindowHeight { get; set; } = 480;
    public List<PlayerConfig> Accounts { get; set; } = [];
    public string ActiveAccountId { get; set; } = "";
}
