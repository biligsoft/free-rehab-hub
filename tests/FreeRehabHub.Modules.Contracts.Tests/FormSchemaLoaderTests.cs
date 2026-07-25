using Xunit;

namespace FreeRehabHub.Modules.Contracts.Tests;

public sealed class FormSchemaLoaderTests
{
    private readonly FormSchemaLoader _loader = new();

    private const string ValidSchemaJson = """
        {
          "id": "com.freerehabhub.sample-assessment",
          "title": { "tr": "Örnek Değerlendirme", "en": "Sample Assessment" },
          "fields": [
            {
              "id": "pain-level",
              "type": "scale",
              "label": { "tr": "Ağrı seviyesi", "en": "Pain level" },
              "required": true,
              "minValue": 0,
              "maxValue": 10
            },
            {
              "id": "dominant-side",
              "type": "singleChoice",
              "label": { "tr": "Baskın taraf", "en": "Dominant side" },
              "required": true,
              "options": [
                { "value": "left", "label": { "tr": "Sol", "en": "Left" } },
                { "value": "right", "label": { "tr": "Sağ", "en": "Right" } }
              ]
            }
          ]
        }
        """;

    [Fact]
    public void Parse_ValidSchema_ReturnsPopulatedFormSchema()
    {
        var schema = _loader.Parse(ValidSchemaJson);

        Assert.Equal("com.freerehabhub.sample-assessment", schema.Id);
        Assert.Equal("Örnek Değerlendirme", schema.Title.Tr);
        Assert.Equal("Sample Assessment", schema.Title.En);
        Assert.Equal(2, schema.Fields.Count);

        var painField = schema.Fields[0];
        Assert.Equal(FormFieldType.Scale, painField.Type);
        Assert.Equal(0, painField.MinValue);
        Assert.Equal(10, painField.MaxValue);

        var sideField = schema.Fields[1];
        Assert.Equal(FormFieldType.SingleChoice, sideField.Type);
        Assert.Equal(2, sideField.Options.Count);
        Assert.Equal("left", sideField.Options[0].Value);
    }

    [Fact]
    public void Parse_MalformedJson_ThrowsFormSchemaValidationException()
    {
        Assert.Throws<FormSchemaValidationException>(() => _loader.Parse("{ not valid json"));
    }

    [Fact]
    public void Parse_MissingSchemaId_ThrowsFormSchemaValidationException()
    {
        const string json = """
            {
              "title": { "tr": "Başlık", "en": "Title" },
              "fields": [
                { "id": "f1", "type": "text", "label": { "tr": "Ad", "en": "Name" } }
              ]
            }
            """;

        Assert.Throws<FormSchemaValidationException>(() => _loader.Parse(json));
    }

    [Fact]
    public void Parse_MissingEnglishTitle_ThrowsFormSchemaValidationException()
    {
        const string json = """
            {
              "id": "com.freerehabhub.sample",
              "title": { "tr": "Başlık" },
              "fields": [
                { "id": "f1", "type": "text", "label": { "tr": "Ad", "en": "Name" } }
              ]
            }
            """;

        Assert.Throws<FormSchemaValidationException>(() => _loader.Parse(json));
    }

    [Fact]
    public void Parse_NoFields_ThrowsFormSchemaValidationException()
    {
        const string json = """
            {
              "id": "com.freerehabhub.sample",
              "title": { "tr": "Başlık", "en": "Title" },
              "fields": []
            }
            """;

        Assert.Throws<FormSchemaValidationException>(() => _loader.Parse(json));
    }

    [Fact]
    public void Parse_DuplicateFieldId_ThrowsFormSchemaValidationException()
    {
        const string json = """
            {
              "id": "com.freerehabhub.sample",
              "title": { "tr": "Başlık", "en": "Title" },
              "fields": [
                { "id": "f1", "type": "text", "label": { "tr": "Ad", "en": "Name" } },
                { "id": "f1", "type": "text", "label": { "tr": "Soyad", "en": "Surname" } }
              ]
            }
            """;

        Assert.Throws<FormSchemaValidationException>(() => _loader.Parse(json));
    }

    [Fact]
    public void Parse_ChoiceFieldWithoutOptions_ThrowsFormSchemaValidationException()
    {
        const string json = """
            {
              "id": "com.freerehabhub.sample",
              "title": { "tr": "Başlık", "en": "Title" },
              "fields": [
                { "id": "f1", "type": "singleChoice", "label": { "tr": "Seçim", "en": "Choice" }, "options": [] }
              ]
            }
            """;

        Assert.Throws<FormSchemaValidationException>(() => _loader.Parse(json));
    }

    [Fact]
    public void Parse_MinValueGreaterThanMaxValue_ThrowsFormSchemaValidationException()
    {
        const string json = """
            {
              "id": "com.freerehabhub.sample",
              "title": { "tr": "Başlık", "en": "Title" },
              "fields": [
                {
                  "id": "f1", "type": "scale", "label": { "tr": "Skor", "en": "Score" },
                  "minValue": 10, "maxValue": 0
                }
              ]
            }
            """;

        Assert.Throws<FormSchemaValidationException>(() => _loader.Parse(json));
    }
}
