using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using LauncherRoot.Models;

namespace LauncherRoot.Services;

public class InstanceService : IInstanceService
{
    private readonly string _instancesDir;
    private readonly string _instancesFilePath;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
    };

    public InstanceService()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            ".LauncherRoot");
        _instancesDir = Path.Combine(root, "instances");
        _instancesFilePath = Path.Combine(root, "instances.json");
        Directory.CreateDirectory(_instancesDir);
    }

    public async Task<List<Instance>> LoadInstancesAsync()
    {
        if (!File.Exists(_instancesFilePath)) return [];
        await using var fs = File.OpenRead(_instancesFilePath);
        return await JsonSerializer.DeserializeAsync<List<Instance>>(fs, JsonOpts) ?? [];
    }

    public async Task SaveInstancesAsync(List<Instance> instances)
    {
        await using var fs = File.Create(_instancesFilePath);
        await JsonSerializer.SerializeAsync(fs, instances, JsonOpts);
    }

    public async Task<Instance?> GetInstanceAsync(string id)
    {
        var instances = await LoadInstancesAsync();
        return instances.Find(i => i.Id == id);
    }

    public async Task AddInstanceAsync(Instance instance)
    {
        var instances = await LoadInstancesAsync();
        instances.Add(instance);
        await SaveInstancesAsync(instances);

        var instanceDir = Path.Combine(_instancesDir, instance.InstanceDir);
        Directory.CreateDirectory(Path.Combine(instanceDir, "minecraft", "versions"));
        Directory.CreateDirectory(Path.Combine(instanceDir, "minecraft", "libraries"));
        Directory.CreateDirectory(Path.Combine(instanceDir, "mods"));
    }

    public async Task DeleteInstanceAsync(string id)
    {
        var instances = await LoadInstancesAsync();
        var instance = instances.Find(i => i.Id == id);
        if (instance != null)
        {
            instances.Remove(instance);
            await SaveInstancesAsync(instances);

            var instanceDir = Path.Combine(_instancesDir, instance.InstanceDir);
            if (Directory.Exists(instanceDir))
                Directory.Delete(instanceDir, true);
        }
    }

    public async Task UpdateInstanceAsync(Instance instance)
    {
        var instances = await LoadInstancesAsync();
        var index = instances.FindIndex(i => i.Id == instance.Id);
        if (index < 0) return;

        instances[index] = instance;
        await SaveInstancesAsync(instances);
    }

    public async Task<Instance> DuplicateInstanceAsync(Instance source, string newName)
    {
        var instances = await LoadInstancesAsync();

        var clone = new Instance
        {
            Name = newName,
            Version = source.Version,
            Loader = source.Loader,
            LoaderVersion = source.LoaderVersion,
        };

        // Copy instance directory if it exists
        var srcDir = Path.Combine(_instancesDir, source.InstanceDir);
        var dstDir = Path.Combine(_instancesDir, clone.InstanceDir);
        if (Directory.Exists(srcDir))
            CopyDirectory(srcDir, dstDir);

        instances.Add(clone);
        await SaveInstancesAsync(instances);
        return clone;
    }

    private static void CopyDirectory(string src, string dst)
    {
        Directory.CreateDirectory(dst);
        foreach (var file in Directory.GetFiles(src, "*", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(src, file);
            var dest = Path.Combine(dst, rel);
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            File.Copy(file, dest, true);
        }
    }

    public string GetInstanceMinecraftPath(Instance instance)
    {
        return Path.Combine(_instancesDir, instance.InstanceDir, "minecraft");
    }

    public string GetInstanceModsPath(Instance instance)
    {
        return Path.Combine(_instancesDir, instance.InstanceDir, "mods");
    }

    public string GetInstanceGamePath(Instance instance)
    {
        return Path.Combine(_instancesDir, instance.InstanceDir);
    }

    public async Task<ModState> LoadModStateAsync(Instance instance)
    {
        var path = Path.Combine(_instancesDir, instance.InstanceDir, "modstate.json");
        if (!File.Exists(path)) return new ModState();
        await using var fs = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<ModState>(fs, JsonOpts) ?? new ModState();
    }

    public async Task SaveModStateAsync(Instance instance, ModState state)
    {
        var dir = Path.Combine(_instancesDir, instance.InstanceDir);
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "modstate.json");
        await using var fs = File.Create(path);
        await JsonSerializer.SerializeAsync(fs, state, JsonOpts);
    }
}
