using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LauncherRoot.Models;

namespace LauncherRoot.Services;

public interface IModrinthService
{
    IProgress<double>? Progress { get; set; }
    Task<List<ModInfo>> DownloadThemeModsAsync(List<ThemeMod> mods, string modsPath, string gameVersion, string loader);
    Task<List<ModrinthHit>> SearchModsAsync(string query, string loader = "fabric", int limit = 20);
    Task<ModInfo?> GetModInfoAsync(string slug, string gameVersion, string loader);
}
