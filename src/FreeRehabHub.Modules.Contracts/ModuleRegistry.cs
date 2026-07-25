using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Text.Json.Serialization;
using FreeRehabHub.Core;

namespace FreeRehabHub.Modules.Contracts;

public sealed class ModuleRegistry : IModuleRegistry
{
    private const string ManifestFileName = "manifest.json";
    private const string TestProjectSuffix = ".Tests.csproj";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    private readonly string _modulesRootPath;
    private readonly string _moduleAssemblyDirectory;

    public ModuleRegistry(string modulesRootPath, string moduleAssemblyDirectory)
    {
        _modulesRootPath = modulesRootPath;
        _moduleAssemblyDirectory = moduleAssemblyDirectory;
    }

    public IReadOnlyList<ModuleManifest> GetAvailableModules()
    {
        return ScanManifests().Select(entry => entry.Manifest).ToList();
    }

    public IReadOnlyList<ModuleManifest> GetModulesByDiscipline(Discipline discipline)
    {
        return GetAvailableModules().Where(manifest => manifest.Disciplines.Contains(discipline)).ToList();
    }

    public IModule CreateInstance(string moduleId)
    {
        var entry = ScanManifests().FirstOrDefault(candidate => candidate.Manifest.Id == moduleId)
            ?? throw new ModuleRegistrationException($"'{moduleId}' Id'li bir modül bulunamadı.");

        var assembly = LoadModuleAssembly(entry.ModuleDirectory);
        var entryType = assembly.GetType(entry.Manifest.EntryPointType)
            ?? throw new ModuleRegistrationException(
                $"'{moduleId}' modülünün derlenmiş assembly'sinde '{entry.Manifest.EntryPointType}' tipi bulunamadı.");

        return Activator.CreateInstance(entryType) as IModule
            ?? throw new ModuleRegistrationException(
                $"'{moduleId}' modülünün '{entry.Manifest.EntryPointType}' tipi IModule implementasyonu değil.");
    }

    private IEnumerable<ModuleManifestEntry> ScanManifests()
    {
        if (!Directory.Exists(_modulesRootPath))
        {
            yield break;
        }

        foreach (var manifestPath in Directory.EnumerateFiles(_modulesRootPath, ManifestFileName, SearchOption.AllDirectories))
        {
            ModuleManifest? manifest;
            try
            {
                manifest = JsonSerializer.Deserialize<ModuleManifest>(File.ReadAllText(manifestPath), SerializerOptions);
            }
            catch (JsonException exception)
            {
                throw new ModuleRegistrationException($"'{manifestPath}' JSON olarak ayrıştırılamadı.", exception);
            }

            if (manifest is null)
            {
                continue;
            }

            yield return new ModuleManifestEntry(manifest, Path.GetDirectoryName(manifestPath)!);
        }
    }

    private Assembly LoadModuleAssembly(string moduleDirectory)
    {
        var csprojPath = Directory.EnumerateFiles(moduleDirectory, "*.csproj", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(path => !path.EndsWith(TestProjectSuffix, StringComparison.OrdinalIgnoreCase));

        if (csprojPath is null)
        {
            throw new ModuleRegistrationException($"'{moduleDirectory}' klasöründe modül .csproj'u bulunamadı.");
        }

        var assemblyPath = Path.Combine(_moduleAssemblyDirectory, Path.GetFileNameWithoutExtension(csprojPath) + ".dll");
        if (!File.Exists(assemblyPath))
        {
            throw new ModuleRegistrationException(
                $"'{assemblyPath}' derlenmiş modül assembly'si bulunamadı. Proje derlenmiş mi kontrol edin.");
        }

        return new ModuleLoadContext().LoadFromAssemblyPath(assemblyPath);
    }

    private sealed record ModuleManifestEntry(ModuleManifest Manifest, string ModuleDirectory);

    // Godot, oyunun kendi assembly'lerini (FreeRehabHub.Modules.Contracts dahil) kendi özel
    // AssemblyLoadContext'ine yüklüyor — Assembly.LoadFrom bu context'i bilmediği için modül DLL'sinin
    // Modules.Contracts/Core bağımlılığını ayrı bir kopya olarak yükler ve "as IModule" cast'i sessizce
    // başarısız olur (aynı isimli ama farklı kimlikli iki IModule tipi). Bu context, o iki paylaşılan
    // sözleşme assembly'sini her zaman zaten yüklü olan (bu kodun kendisinin parçası olduğu) kopyaya
    // yönlendirerek tip kimliğini garanti ediyor.
    private sealed class ModuleLoadContext : AssemblyLoadContext
    {
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            if (assemblyName.Name == typeof(IModule).Assembly.GetName().Name)
            {
                return typeof(IModule).Assembly;
            }

            if (assemblyName.Name == typeof(Discipline).Assembly.GetName().Name)
            {
                return typeof(Discipline).Assembly;
            }

            return null;
        }
    }
}
