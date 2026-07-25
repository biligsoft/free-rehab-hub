using FreeRehabHub.Modules.Contracts;
using Xunit;

namespace FreeRehabHub.Modules.GeneralFunctionalCheckin.Tests;

public sealed class FormSchemaContentPackTests
{
    [Fact]
    public void GeneralFunctionalCheckinSchema_LoadsSuccessfully_AndMatchesExpectedFieldIds()
    {
        var loader = new FormSchemaLoader();
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "TestData", "general-functional-checkin.json");

        var schema = loader.LoadFromFile(schemaPath);

        Assert.Equal("com.freerehabhub.general-functional-checkin", schema.Id);
        Assert.Equal(
            new[] { "painLevel", "functionalDifficulty", "affectedSide", "symptoms", "notes" },
            schema.Fields.Select(field => field.Id));
    }
}
