namespace FreeRehabHub.Modules.Contracts;

public interface IExerciseModule : IModule, IDisposable
{
    Task InitializeAsync(ModuleContext context, CancellationToken cancellationToken = default);

    void OnActivated();
    void OnPaused();
    void OnResumed();
    void OnDeactivated();

    event EventHandler<ModuleResult> Completed;
}
