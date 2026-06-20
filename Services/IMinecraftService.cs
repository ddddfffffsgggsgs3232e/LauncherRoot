using System;
using System.Threading.Tasks;

namespace LauncherRoot.Services;

public interface IMinecraftService
{
    IProgress<double>? Progress { get; set; }
    Task<bool> EnsureFabricInstalledAsync(string minecraftDir);
    Task EnsureAssetsDownloadedAsync(string minecraftDir);
    Task<bool> LaunchMinecraftAsync(string username, int ramGB, string minecraftDir, string modsDir);
}
