using FreeRehabHub.Core;

namespace FreeRehabHub.Modules.Contracts;

public interface IModuleRegistry
{
    IReadOnlyList<ModuleManifest> GetAvailableModules();
    IReadOnlyList<ModuleManifest> GetModulesByDiscipline(Discipline discipline);

    // Sadece ScenePath'i olmayan modüller için (ör. IAssessmentModule) — ScenePath'i olan modüller
    // Godot katmanında PackedScene.Instantiate() ile kurulmalı, bkz. ModuleRegistry.CreateInstance.
    IModule CreateInstance(string moduleId);
}
