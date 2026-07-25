using FreeRehabHub.Core;
using Xunit;

namespace FreeRehabHub.Modules.Contracts.Tests;

public sealed class ModuleRegistryTests
{
    private const string GeneralFunctionalCheckinId = "com.freerehabhub.general-functional-checkin";
    private const string FakeSceneModuleId = "com.freerehabhub.fake-scene-module";

    private static readonly string ModulesRootPath = Path.Combine(AppContext.BaseDirectory, "TestModulesRoot");
    private static readonly string ModuleAssemblyDirectory = AppContext.BaseDirectory;

    private readonly ModuleRegistry _registry = new(ModulesRootPath, ModuleAssemblyDirectory);

    [Fact]
    public void GetAvailableModules_ScansTestModuleDirectory_ReturnsBothFixtureManifests()
    {
        var modules = _registry.GetAvailableModules();

        Assert.Equal(2, modules.Count);
        var manifest = Assert.Single(modules, m => m.Id == GeneralFunctionalCheckinId);
        Assert.Equal(ModuleKind.Assessment, manifest.Kind);
        Assert.Contains(Discipline.Physiotherapy, manifest.Disciplines);
    }

    [Fact]
    public void GetModulesByDiscipline_MatchingDiscipline_ReturnsManifest()
    {
        var modules = _registry.GetModulesByDiscipline(Discipline.OccupationalTherapy);

        Assert.Single(modules);
    }

    [Fact]
    public void GetModulesByDiscipline_NonMatchingDiscipline_ReturnsEmpty()
    {
        var modules = _registry.GetModulesByDiscipline(Discipline.SpeechTherapy);

        Assert.Empty(modules);
    }

    [Fact]
    public void CreateInstance_KnownModuleId_ReturnsWorkingAssessmentModule()
    {
        var instance = _registry.CreateInstance(GeneralFunctionalCheckinId);

        var assessment = Assert.IsAssignableFrom<IAssessmentModule>(instance);
        Assert.Equal(GeneralFunctionalCheckinId, assessment.ModuleId);

        var context = new ModuleContext
        {
            PatientId = Guid.NewGuid(),
            SessionId = Guid.NewGuid(),
            CompletedAt = DateTime.UtcNow
        };
        var submission = new FormSubmission
        {
            FieldValues = new Dictionary<string, string> { ["painLevel"] = "2", ["functionalDifficulty"] = "2" }
        };

        var result = assessment.Score(submission, context);

        Assert.Equal(GeneralFunctionalCheckinId, result.ModuleId);
    }

    [Fact]
    public void CreateInstance_UnknownModuleId_ThrowsModuleRegistrationException()
    {
        Assert.Throws<ModuleRegistrationException>(() => _registry.CreateInstance("com.freerehabhub.does-not-exist"));
    }

    [Fact]
    public void CreateInstance_ModuleWithScenePath_ThrowsModuleRegistrationException()
    {
        // ScenePath'i olan modüller Godot katmanında PackedScene.Instantiate() ile kurulmalı,
        // reflection'la (CreateInstance) değil — bkz. ModuleRegistry.CreateInstance yorumu.
        Assert.Throws<ModuleRegistrationException>(() => _registry.CreateInstance(FakeSceneModuleId));
    }

    [Fact]
    public void GetAvailableModules_NonExistentModulesRoot_ReturnsEmpty()
    {
        var registry = new ModuleRegistry(Path.Combine(AppContext.BaseDirectory, "NoSuchFolder"), ModuleAssemblyDirectory);

        Assert.Empty(registry.GetAvailableModules());
    }
}
