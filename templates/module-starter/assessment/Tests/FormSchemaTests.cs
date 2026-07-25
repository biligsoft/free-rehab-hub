using FreeRehabHub.Modules.Contracts;
using Xunit;

namespace FreeRehabHub.Modules.TemplateAssessment.Tests;

public sealed class FormSchemaTests
{
    [Fact]
    public void ExampleFormSchema_LoadsSuccessfully_AndMatchesExpectedFieldIds()
    {
        var loader = new FormSchemaLoader();
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "TestData", "form-schema.json");

        var schema = loader.LoadFromFile(schemaPath);

        Assert.Equal("com.yourorg.template-assessment", schema.Id);
        Assert.Equal(new[] { "score", "notes" }, schema.Fields.Select(field => field.Id));
    }
}
