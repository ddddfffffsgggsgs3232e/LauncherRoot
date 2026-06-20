using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace LauncherRoot.Services;

public class MinecraftService : IMinecraftService
{
    private readonly HttpClient _http;
    private readonly ConfigService _config;

    public IProgress<double>? Progress { get; set; }

    public MinecraftService(ConfigService config)
    {
        _config = config;
        _http = new HttpClient();
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("LauncherRoot/1.0");
        _http.Timeout = TimeSpan.FromSeconds(60);
    }

    public async Task<bool> EnsureFabricInstalledAsync(string minecraftDir)
    {
        try
        {
            var launcherConfig = await _config.LoadConfigAsync();
            if (launcherConfig.FabricInstalled) return true;

            Progress?.Report(0.1);
            var metaUrl = "https://meta.fabricmc.net/v2/versions/loader/1.21.4";
            var loaders = await _http.GetFromJsonAsync<List<JsonElement>>(metaUrl);
            if (loaders == null || loaders.Count == 0)
            {
                _config.Log("Fabric loader meta alınamadı");
                return false;
            }

            var loader = loaders[0];
            var loaderVersion = loader.GetProperty("loader").GetProperty("version").GetString() ?? "0.16.9";
            var intermediaryVersion = loader.GetProperty("intermediary").GetProperty("version").GetString() ?? "1.21.4";

            var profileUrl = $"https://meta.fabricmc.net/v2/versions/loader/1.21.4/{loaderVersion}/profile/json";
            var profileJson = await _http.GetStringAsync(profileUrl);

            var versionDir = Path.Combine(minecraftDir, "versions", $"fabric-loader-1.21.4-{loaderVersion}");
            Directory.CreateDirectory(versionDir);

            var profilePath = Path.Combine(versionDir, $"fabric-loader-1.21.4-{loaderVersion}.json");
            await File.WriteAllTextAsync(profilePath, profileJson);

            Progress?.Report(0.3);
            if (!await DownloadMinecraftClientAsync(minecraftDir))
            {
                _config.Log("Minecraft client indirilemedi");
                return false;
            }

            Progress?.Report(0.6);
            var profile = JsonSerializer.Deserialize<JsonElement>(profileJson);
            if (profile.TryGetProperty("libraries", out var libraries))
            {
                await DownloadFabricLibrariesAsync(libraries, minecraftDir);
            }

            Progress?.Report(0.9);
            launcherConfig.FabricInstalled = true;
            await _config.SaveConfigAsync(launcherConfig);
            _config.Log($"Fabric kuruldu: {loaderVersion}");
            Progress?.Report(1.0);
            return true;
        }
        catch (Exception ex)
        {
            _config.Log($"Fabric kurulum hatası: {ex.Message}");
            return false;
        }
    }

    private async Task<bool> DownloadMinecraftClientAsync(string minecraftDir)
    {
        try
        {
            var manifestUrl = "https://piston-meta.mojang.com/mc/game/version_manifest_v2.json";
            var manifest = await _http.GetFromJsonAsync<JsonElement>(manifestUrl);
            foreach (var v in manifest.GetProperty("versions").EnumerateArray())
            {
                if (v.GetProperty("id").GetString() != "1.21.4") continue;

                var versionData = await _http.GetFromJsonAsync<JsonElement>(v.GetProperty("url").GetString()!);

                var clientUrl = versionData.GetProperty("downloads").GetProperty("client").GetProperty("url").GetString()!;
                var assetIndexUrl = versionData.GetProperty("assetIndex").GetProperty("url").GetString()!;

                var clientDir = Path.Combine(minecraftDir, "versions", "1.21.4");
                Directory.CreateDirectory(clientDir);
                var clientJar = Path.Combine(clientDir, "1.21.4.jar");
                if (!File.Exists(clientJar))
                {
                    var response = await _http.GetAsync(clientUrl);
                    response.EnsureSuccessStatusCode();
                    await using var fs = File.Create(clientJar);
                    await response.Content.CopyToAsync(fs);
                }

                var assetsDir = Path.Combine(minecraftDir, "assets");
                var indexDir = Path.Combine(assetsDir, "indexes");
                Directory.CreateDirectory(indexDir);
                var indexPath = Path.Combine(indexDir, "1.21.4.json");
                if (!File.Exists(indexPath))
                {
                    var indexData = await _http.GetStringAsync(assetIndexUrl);
                    await File.WriteAllTextAsync(indexPath, indexData);
                }

                await DownloadAssetsAsync(assetsDir);

                if (versionData.TryGetProperty("libraries", out var mcLibs))
                {
                    await DownloadMinecraftLibrariesAsync(mcLibs, minecraftDir);
                }

                return true;
            }
            return false;
        }
        catch (Exception ex)
        {
            _config.Log($"Minecraft client hatası: {ex.Message}");
            return false;
        }
    }

    private async Task DownloadMinecraftLibrariesAsync(JsonElement libraries, string minecraftDir)
    {
        var libDir = Path.Combine(minecraftDir, "libraries");
        foreach (var lib in libraries.EnumerateArray())
        {
            try
            {
                if (lib.TryGetProperty("rules", out _))
                {
                    var os = Environment.OSVersion.Platform switch
                    {
                        PlatformID.Win32NT => "windows",
                        PlatformID.MacOSX => "osx",
                        _ => "linux"
                    };
                    var allow = false;
                    foreach (var rule in lib.GetProperty("rules").EnumerateArray())
                    {
                        var action = rule.GetProperty("action").GetString();
                        if (rule.TryGetProperty("os", out var osRule))
                        {
                            var name = osRule.GetProperty("name").GetString();
                            if (name == os) allow = action == "allow";
                        }
                        else
                        {
                            allow = action == "allow";
                        }
                    }
                    if (!allow) continue;
                }

                if (!lib.TryGetProperty("downloads", out var downloads) ||
                    !downloads.TryGetProperty("artifact", out var artifact)) continue;

                var libPath = artifact.GetProperty("path").GetString()!;
                var libUrl = artifact.GetProperty("url").GetString()!;
                var libFilePath = Path.Combine(libDir, libPath);

                if (!File.Exists(libFilePath))
                {
                    var resp = await _http.GetAsync(libUrl);
                    if (resp.IsSuccessStatusCode)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(libFilePath)!);
                        await using var fs = File.Create(libFilePath);
                        await resp.Content.CopyToAsync(fs);
                    }
                }
            }
            catch { }
        }
    }

    private async Task DownloadFabricLibrariesAsync(JsonElement libraries, string minecraftDir)
    {
        var libDir = Path.Combine(minecraftDir, "libraries");
        foreach (var lib in libraries.EnumerateArray())
        {
            try
            {
                var name = lib.GetProperty("name").GetString()!;
                var parts = name.Split(':');
                if (parts.Length < 3) continue;

                var groupId = parts[0].Replace('.', '/');
                var artifactId = parts[1];
                var version = parts[2];
                var jarName = $"{artifactId}-{version}.jar";
                var libFilePath = Path.Combine(libDir, groupId, artifactId, version, jarName);

                if (File.Exists(libFilePath)) continue;

                var baseUrl = lib.TryGetProperty("url", out var urlProp)
                    ? urlProp.GetString()!
                    : "https://maven.fabricmc.net/";

                var downloadUrl = $"{baseUrl.TrimEnd('/')}/{groupId}/{artifactId}/{version}/{jarName}";

                var resp = await _http.GetAsync(downloadUrl);
                if (resp.IsSuccessStatusCode)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(libFilePath)!);
                    await using var fs = File.Create(libFilePath);
                    await resp.Content.CopyToAsync(fs);
                }
                else
                {
                    _config.Log($"Kütüphane indirilemedi: {downloadUrl}");
                }
            }
            catch (Exception ex)
            {
                _config.Log($"Fabric lib hatası: {ex.Message}");
            }
        }
    }

    public async Task EnsureAssetsDownloadedAsync(string minecraftDir)
    {
        var assetsDir = Path.Combine(minecraftDir, "assets");
        var indexDir = Path.Combine(assetsDir, "indexes");
        Directory.CreateDirectory(indexDir);
        var indexPath = Path.Combine(indexDir, "1.21.4.json");

        if (!File.Exists(indexPath))
        {
            var manifestUrl = "https://piston-meta.mojang.com/mc/game/version_manifest_v2.json";
            var manifest = await _http.GetFromJsonAsync<JsonElement>(manifestUrl);
            foreach (var v in manifest.GetProperty("versions").EnumerateArray())
            {
                if (v.GetProperty("id").GetString() != "1.21.4") continue;
                var versionData = await _http.GetFromJsonAsync<JsonElement>(v.GetProperty("url").GetString()!);
                var assetIndexUrl = versionData.GetProperty("assetIndex").GetProperty("url").GetString()!;
                var indexData = await _http.GetStringAsync(assetIndexUrl);
                await File.WriteAllTextAsync(indexPath, indexData);
                break;
            }
        }

        await DownloadAssetsAsync(assetsDir);
    }

    private async Task DownloadAssetsAsync(string assetsDir)
    {
        var indexPath = Path.Combine(assetsDir, "indexes", "1.21.4.json");
        if (!File.Exists(indexPath)) return;

        var objectsDir = Path.Combine(assetsDir, "objects");
        var indexJson = JsonSerializer.Deserialize<JsonElement>(await File.ReadAllTextAsync(indexPath));
        if (!indexJson.TryGetProperty("objects", out var objects)) return;

        var total = objects.EnumerateObject().Count();
        var done = 0;
        var assetBaseUrl = "https://resources.download.minecraft.net";
        foreach (var asset in objects.EnumerateObject())
        {
            var hash = asset.Value.GetProperty("hash").GetString()!;
            var prefix = hash[..2];
            var objDir = Path.Combine(objectsDir, prefix);
            var objPath = Path.Combine(objDir, hash);
            if (!File.Exists(objPath))
            {
                Directory.CreateDirectory(objDir);
                try
                {
                    var resp = await _http.GetAsync($"{assetBaseUrl}/{prefix}/{hash}");
                    if (resp.IsSuccessStatusCode)
                    {
                        await using var fs = File.Create(objPath);
                        await resp.Content.CopyToAsync(fs);
                    }
                }
                catch { }
            }
            done++;
            if (done % 100 == 0 || done == total)
                Progress?.Report((double)done / total);
        }
    }

    public async Task<bool> LaunchMinecraftAsync(string username, int ramGB, string minecraftDir, string modsDir)
    {
        try
        {
            var javaPath = FindJava();
            if (javaPath == null)
            {
                _config.Log("Java bulunamadı");
                return false;
            }

            var clientJar = Path.Combine(minecraftDir, "versions", "1.21.4", "1.21.4.jar");

            var libDir = Path.Combine(minecraftDir, "libraries");
            var allJars = new List<string>();
            if (File.Exists(clientJar))
                allJars.Add(clientJar);
            allJars.AddRange(Directory.GetFiles(libDir, "*.jar", SearchOption.AllDirectories));

            if (Directory.Exists(modsDir))
                allJars.AddRange(Directory.GetFiles(modsDir, "*.jar"));

            // Aynı library artifact'inin farklı versiyonlarını filtrele (en yenisini tut)
            var byArtifact = new Dictionary<string, (string path, Version? version)>();
            foreach (var jar in allJars)
            {
                if (jar.StartsWith(libDir + Path.DirectorySeparatorChar))
                {
                    var rel = Path.GetRelativePath(libDir, jar);
                    var parts = rel.Split('/');
                    var fileName = parts[^1];
                    var fileNameNoExt = Path.GetFileNameWithoutExtension(fileName);
                    // Maven: groupId/artifactId/version/artifactId-version[-classifier].jar
                    if (parts.Length >= 4)
                    {
                        var groupId = string.Join(".", parts.Take(parts.Length - 3));
                        var artifactId = parts[^3];
                        var versionStr = parts[^2];
                        // classifier'ı ayır (örn: "jtracy-1.0.29-natives-linux" -> "1.0.29", "natives-linux")
                        var classifier = "";
                        var baseName = fileNameNoExt;
                        if (baseName.StartsWith(artifactId + "-"))
                            baseName = baseName[(artifactId.Length + 1)..];
                        if (baseName.StartsWith(versionStr + "-"))
                            classifier = baseName[(versionStr.Length + 1)..];
                        var key = $"{groupId}:{artifactId}:{classifier}";
                        Version? v = null;
                        var clean = versionStr.Split('-')[0];
                        if (Version.TryParse(clean, out var parsed))
                            v = parsed;
                        if (!byArtifact.TryGetValue(key, out var existing) ||
                            (v != null && (existing.version == null || v > existing.version)))
                            byArtifact[key] = (jar, v);
                        continue;
                    }
                }
                // client.jar, mods ve standart dışı yapıdaki jar'lar olduğu gibi eklenir
                byArtifact[jar] = (jar, null);
            }

            var classpath = byArtifact.Values.Select(x => x.path).ToList();
            var cp = string.Join(Path.PathSeparator, classpath);
            var assetsDir = Path.Combine(minecraftDir, "assets");

            var args = $"-Xmx{ramGB}G -Xms{Math.Max(1, ramGB / 2)}G -cp \"{cp}\" " +
                       $"net.fabricmc.loader.impl.launch.knot.KnotClient " +
                       $"--gameDir \"{minecraftDir}\" " +
                       $"--assetsDir \"{assetsDir}\" " +
                       $"--assetIndex \"1.21.4\" " +
                       $"--username \"{username}\" " +
                       $"--version \"1.21.4\" " +
                       $"--width 854 --height 480";

            _config.Log($"Başlatılıyor: {javaPath}");

            var psi = new ProcessStartInfo
            {
                FileName = javaPath,
                Arguments = args,
                UseShellExecute = false,
                WorkingDirectory = minecraftDir,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            var process = Process.Start(psi);
            if (process == null)
            {
                _config.Log("Minecraft işlemi başlatılamadı");
                return false;
            }

            // Kısa bir süre bekle, process hemen çökerse yakala
            var exited = process.WaitForExit(3000);
            if (exited)
            {
                var stderr = await process.StandardError.ReadToEndAsync();
                var stdout = await process.StandardOutput.ReadToEndAsync();
                _config.Log($"Minecraft erken çıktı (kod: {process.ExitCode})");
                if (!string.IsNullOrWhiteSpace(stderr))
                    _config.Log($"Hata: {stderr[..Math.Min(stderr.Length, 1000)]}");
                if (!string.IsNullOrWhiteSpace(stdout))
                    _config.Log($"Çıktı: {stdout[..Math.Min(stdout.Length, 500)]}");
                return false;
            }

            // Process çalışıyor, çıktıları arka planda oku
            _ = Task.Run(async () =>
            {
                try
                {
                    var output = await process.StandardOutput.ReadToEndAsync();
                    var error = await process.StandardError.ReadToEndAsync();
                    if (!string.IsNullOrWhiteSpace(output))
                        _config.Log($"MC çıktı: {output[..Math.Min(output.Length, 10000)]}");
                    if (!string.IsNullOrWhiteSpace(error))
                        _config.Log($"MC hata: {error[..Math.Min(error.Length, 10000)]}");
                    var exitCode = process.ExitCode;
                    _config.Log($"Minecraft çıkış kodu: {exitCode}");
                }
                catch { }
            });

            _config.Log($"Minecraft başlatıldı: {username}, RAM: {ramGB}GB");
            return true;
        }
        catch (Exception ex)
        {
            _config.Log($"Başlatma hatası: {ex.Message}");
            return false;
        }
    }

    private static string? FindJava()
    {
        var javaHome = Environment.GetEnvironmentVariable("JAVA_HOME");
        if (javaHome != null)
        {
            var javas = new[] { Path.Combine(javaHome, "bin", "java"),
                                Path.Combine(javaHome, "bin", "java.exe") };
            foreach (var j in javas)
                if (File.Exists(j)) return j;
        }

        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? "";
        foreach (var dir in pathEnv.Split(Path.PathSeparator))
        {
            try
            {
                var javaPath = Path.Combine(dir.Trim(), "java");
                if (File.Exists(javaPath)) return javaPath;
                var javaExe = Path.Combine(dir.Trim(), "java.exe");
                if (File.Exists(javaExe)) return javaExe;
            }
            catch { }
        }

        // Linux'da yaygın JDK kurulum yolları
        var extraPaths = new[]
        {
            "/usr/bin/java",
            "/usr/lib/jvm/default-java/bin/java",
            "/usr/lib/jvm/default/bin/java",
            "/usr/local/lib/jvm/java/bin/java",
        };
        foreach (var p in extraPaths)
            if (File.Exists(p)) return p;

        var jvmDirs = new[] { "/usr/lib/jvm", "/usr/local/lib/jvm", "/opt/java" };
        foreach (var jvmDir in jvmDirs)
        {
            if (!Directory.Exists(jvmDir)) continue;
            try
            {
                foreach (var dir in Directory.GetDirectories(jvmDir))
                {
                    var javaPath = Path.Combine(dir, "bin", "java");
                    if (File.Exists(javaPath)) return javaPath;
                }
            }
            catch { }
        }

        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "java",
                Arguments = "-version",
                UseShellExecute = false,
                RedirectStandardError = true,
            };
            var proc = Process.Start(psi);
            if (proc != null)
            {
                proc.WaitForExit(2000);
                return "java";
            }
        }
        catch { }

        return null;
    }
}
