using System.Text.Json;
using DocumentFlow.Application.Documents.Processing;
using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Infrastructure.Processing.Llm;

public static class OllamaJsonSchemas
{
    public static JsonElement Classification { get; } = JsonSerializer.SerializeToElement(new
    {
        type = "object",
        properties = new
        {
            type = new { type = "string", @enum = new[] { "Invoice", "Contract", "Act", "Unknown" } },
            confidence = new { type = "number", minimum = 0, maximum = 1 }
        },
        required = new[] { "type", "confidence" },
        additionalProperties = false
    });

    public static JsonElement Fields(DocumentType type)
    {
        if (type is DocumentType.Unknown || !Enum.IsDefined(type))
            throw new ArgumentOutOfRangeException(nameof(type));
        return JsonSerializer.SerializeToElement(new
        {
            type = "object",
            properties = new
            {
                fields = new
                {
                    type = "array",
                    items = new
                    {
                        type = "object",
                        properties = new
                        {
                            name = new { type = "string", @enum = DocumentFieldSchema.GetSupportedFieldNames(type) },
                            value = new { type = "string" },
                            confidence = new { type = "number", minimum = 0, maximum = 1 }
                        },
                        required = new[] { "name", "value", "confidence" },
                        additionalProperties = false
                    }
                }
            },
            required = new[] { "fields" },
            additionalProperties = false
        });
    }
}
