namespace FreeRehabHub.Modules.Contracts;

public sealed class ModuleRegistrationException : Exception
{
    public ModuleRegistrationException(string message)
        : base(message)
    {
    }

    public ModuleRegistrationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
