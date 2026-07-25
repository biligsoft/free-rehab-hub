namespace FreeRehabHub.Modules.Contracts;

public interface IModule
{
    string ModuleId { get; }
    ModuleManifest Manifest { get; }
}
