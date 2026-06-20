using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using LauncherRoot.Models;

namespace LauncherRoot.Services;

public class ModrinthService : IModrinthService
{
    private readonly HttpClient _http;
    private const string BaseUrl = "https://api.modrinth.com/v2";
    private readonly ConfigService _config;

    public IProgress<double>? Progress { get; set; }

    public ModrinthService(ConfigService config)
    {
        _config = config;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("LauncherRoot/1.0");
    }

    public async Task<List<ModInfo>> DownloadThemeModsAsync(List<ThemeMod> mods, string modsPath)
    {
        var downloaded = new List<ModInfo>();
        var total = mods.Count;
        var completed = 0;

        foreach (var themeMod in mods)
        {
            try
            {
                var slug = themeMod.Slug;

                var versionUrl = $"{BaseUrl}/project/{slug}/version";
                var versions = await _http.GetFromJsonAsync<List<ModrinthVersion>>(versionUrl);

                if (versions == null || versions.Count == 0)
                {
                    _config.Log($"Mod {slug}: Versiyon bulunamadı");
                    completed++;
                    Progress?.Report((double)completed / total);
                    continue;
                }

                var best = versions
                    .Where(v => v.GameVersions.Contains("1.21.4") && v.Loaders.Contains("fabric"))
                    .OrderByDescending(v => v.DatePublished)
                    .FirstOrDefault();

                if (best == null)
                {
                    _config.Log($"Mod {slug}: 1.21.4/Fabric sürümü bulunamadı");
                    completed++;
                    Progress?.Report((double)completed / total);
                    continue;
                }

                var primaryFile = best.Files.FirstOrDefault(f => f.Primary) ?? best.Files.FirstOrDefault();
                if (primaryFile == null)
                {
                    completed++;
                    Progress?.Report((double)completed / total);
                    continue;
                }

                var filePath = Path.Combine(modsPath, primaryFile.Filename);
                if (!File.Exists(filePath))
                {
                    var response = await _http.GetAsync(primaryFile.Url);
                    response.EnsureSuccessStatusCode();
                    await using var fs = File.Create(filePath);
                    await response.Content.CopyToAsync(fs);
                }

                downloaded.Add(new ModInfo
                {
                    Slug = slug,
                    Name = themeMod.Name,
                    FileName = primaryFile.Filename,
                    Downloaded = true,
                    Enabled = true
                });

                _config.Log($"Mod indirildi: {slug} -> {primaryFile.Filename}");
            }
            catch (HttpRequestException ex)
            {
                _config.Log($"Mod indirme hatası ({themeMod.Slug}): {ex.Message}");
            }
            catch (Exception ex)
            {
                _config.Log($"Beklenmeyen hata ({themeMod.Slug}): {ex.Message}");
            }

            completed++;
            Progress?.Report((double)completed / total);
        }

        return downloaded;
    }

    public async Task<List<ModrinthHit>> SearchModsAsync(string query, int limit = 20)
    {
        try
        {
            var url = $"{BaseUrl}/search?query={Uri.EscapeDataString(query)}&facets=[[\"project_type:mod\"],[\"categories:fabric\"]]&limit={limit}";
            var result = await _http.GetFromJsonAsync<ModrinthSearchResult>(url);
            return result?.Hits ?? [];
        }
        catch
        {
            return [];
        }
    }

    public async Task<ModInfo?> GetModInfoAsync(string slug)
    {
        try
        {
            var project = await _http.GetFromJsonAsync<ModrinthProject>($"{BaseUrl}/project/{slug}");
            if (project == null) return null;

            var versionUrl = $"{BaseUrl}/project/{slug}/version";
            var versions = await _http.GetFromJsonAsync<List<ModrinthVersion>>(versionUrl);
            var best = versions?
                .Where(v => v.GameVersions.Contains("1.21.4") && v.Loaders.Contains("fabric"))
                .OrderByDescending(v => v.DatePublished)
                .FirstOrDefault();

            return new ModInfo
            {
                Slug = project.Slug,
                Name = project.Title,
                Description = project.Description,
                IconUrl = project.IconUrl,
                DownloadUrl = best?.Files.FirstOrDefault()?.Url,
                FileName = best?.Files.FirstOrDefault()?.Filename,
                Downloaded = false,
                Enabled = true
            };
        }
        catch
        {
            return null;
        }
    }
}
