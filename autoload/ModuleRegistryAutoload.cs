using System;
using FreeRehabHub.Modules.Contracts;
using Godot;

namespace FreeRehabHub.App.Autoload;

public partial class ModuleRegistryAutoload : Node
{
    private const string ModulesRelativePath = "modules";

    public IModuleRegistry Registry { get; private set; } = null!;

    public override void _Ready()
    {
        var modulesRootPath = AppContentRoot.Resolve(ModulesRelativePath);
        Registry = new ModuleRegistry(modulesRootPath, AppContext.BaseDirectory);
    }
}
