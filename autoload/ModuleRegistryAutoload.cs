using System;
using FreeRehabHub.Modules.Contracts;
using Godot;

namespace FreeRehabHub.App.Autoload;

public partial class ModuleRegistryAutoload : Node
{
    private const string ModulesResourcePath = "res://modules";

    public IModuleRegistry Registry { get; private set; } = null!;

    public override void _Ready()
    {
        var modulesRootPath = ProjectSettings.GlobalizePath(ModulesResourcePath);
        Registry = new ModuleRegistry(modulesRootPath, AppContext.BaseDirectory);
    }
}
