using FreeRehabHub.Core;

namespace FreeRehabHub.Modules.Contracts;

public interface IModuleRegistry
{
    IReadOnlyList<ModuleManifest> GetAvailableModules();
    IReadOnlyList<ModuleManifest> GetModulesByDiscipline(Discipline discipline);
    IModule CreateInstance(string moduleId);
}
