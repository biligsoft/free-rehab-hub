namespace FreeRehabHub.Modules.Contracts;

public interface IAssessmentModule : IModule
{
    ModuleResult Score(FormSubmission submission, ModuleContext context);
}
