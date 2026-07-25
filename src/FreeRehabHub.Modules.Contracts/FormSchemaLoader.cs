using System.Text.Json;
using System.Text.Json.Serialization;
using FreeRehabHub.Core;

namespace FreeRehabHub.Modules.Contracts;

public sealed class FormSchemaLoader
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public FormSchema LoadFromFile(string filePath)
    {
        return Parse(File.ReadAllText(filePath));
    }

    public FormSchema Parse(string json)
    {
        FormSchema? schema;
        try
        {
            schema = JsonSerializer.Deserialize<FormSchema>(json, SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new FormSchemaValidationException("Form şeması JSON olarak ayrıştırılamadı.", exception);
        }

        if (schema is null)
        {
            throw new FormSchemaValidationException("Form şeması boş.");
        }

        Validate(schema);
        return schema;
    }

    private static void Validate(FormSchema schema)
    {
        if (string.IsNullOrWhiteSpace(schema.Id))
        {
            throw new FormSchemaValidationException("Form şemasının Id alanı zorunlu.");
        }

        if (!HasBothLanguages(schema.Title))
        {
            throw new FormSchemaValidationException($"'{schema.Id}' şemasının başlığı TR ve EN olarak dolu olmalı.");
        }

        if (schema.Fields.Count == 0)
        {
            throw new FormSchemaValidationException($"'{schema.Id}' şemasında en az bir alan olmalı.");
        }

        var seenFieldIds = new HashSet<string>();
        foreach (var field in schema.Fields)
        {
            ValidateField(schema.Id, field, seenFieldIds);
        }
    }

    private static void ValidateField(string schemaId, FormField field, HashSet<string> seenFieldIds)
    {
        if (string.IsNullOrWhiteSpace(field.Id))
        {
            throw new FormSchemaValidationException($"'{schemaId}' şemasında Id'siz bir alan var.");
        }

        if (!seenFieldIds.Add(field.Id))
        {
            throw new FormSchemaValidationException($"'{schemaId}' şemasında '{field.Id}' Id'si birden fazla kez kullanılmış.");
        }

        if (!HasBothLanguages(field.Label))
        {
            throw new FormSchemaValidationException($"'{field.Id}' alanının etiketi TR ve EN olarak dolu olmalı.");
        }

        var isChoiceField = field.Type is FormFieldType.SingleChoice or FormFieldType.MultiChoice;
        if (isChoiceField && field.Options.Count == 0)
        {
            throw new FormSchemaValidationException($"'{field.Id}' alanı seçim tipinde ama hiç seçeneği yok.");
        }

        foreach (var option in field.Options)
        {
            if (string.IsNullOrWhiteSpace(option.Value))
            {
                throw new FormSchemaValidationException($"'{field.Id}' alanının değeri boş bir seçeneği var.");
            }

            if (!HasBothLanguages(option.Label))
            {
                throw new FormSchemaValidationException(
                    $"'{field.Id}' alanının '{option.Value}' seçeneğinin etiketi TR ve EN olarak dolu olmalı.");
            }
        }

        if (field.MinValue.HasValue && field.MaxValue.HasValue && field.MinValue > field.MaxValue)
        {
            throw new FormSchemaValidationException($"'{field.Id}' alanında MinValue, MaxValue'dan büyük olamaz.");
        }
    }

    private static bool HasBothLanguages(LocalizedText text)
    {
        return !string.IsNullOrWhiteSpace(text.Tr) && !string.IsNullOrWhiteSpace(text.En);
    }
}
