namespace FreeRehabHub.Modules.Contracts;

public sealed class FormSchemaValidationException : Exception
{
    public FormSchemaValidationException(string message)
        : base(message)
    {
    }

    public FormSchemaValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
