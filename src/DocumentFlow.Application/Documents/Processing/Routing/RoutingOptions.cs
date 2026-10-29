using DocumentFlow.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace DocumentFlow.Application.Documents.Processing.Routing;

public sealed class RoutingOptions
{
    public const string SectionName = "Routing";
    private IReadOnlyDictionary<DocumentType, Department> _departments =
        new Dictionary<DocumentType, Department>();

    public static RoutingOptions EmbeddedDefaults() => FromConfiguration(EmbeddedDefaultsConfiguration());

    internal static IConfiguration EmbeddedDefaultsConfiguration()
    {
        using var stream = typeof(RoutingOptions).Assembly.GetManifestResourceStream(
            "DocumentFlow.Application.Documents.Processing.Routing.routing.defaults.json")
            ?? throw new InvalidOperationException("Embedded routing defaults were not found.");
        return new ConfigurationBuilder().AddJsonStream(stream).Build();
    }

    public static RoutingOptions FromConfiguration(IConfiguration configuration)
    {
        var section = configuration.GetSection(SectionName);
        var unknown = section[nameof(DocumentType.Unknown)];
        if (unknown is not null && !string.Equals(unknown.Trim(), nameof(Department.Unassigned), StringComparison.OrdinalIgnoreCase))
            throw Invalid("Routing configuration for DocumentType 'Unknown' must be absent or 'Unassigned'.");

        var departments = new Dictionary<DocumentType, Department>();
        foreach (var type in Enum.GetValues<DocumentType>().Where(type => type != DocumentType.Unknown))
        {
            var raw = section[type.ToString()];
            if (string.IsNullOrWhiteSpace(raw))
                throw Invalid($"Routing configuration for DocumentType '{type}' is missing.");

            var value = raw.Trim();
            if (!Enum.GetNames<Department>().Any(name => string.Equals(name, value, StringComparison.OrdinalIgnoreCase)) ||
                !Enum.TryParse<Department>(value, true, out var department) || department == Department.Unassigned)
                throw Invalid($"Department '{value}' is not valid for DocumentType '{type}'.");

            departments.Add(type, department);
        }
        return new RoutingOptions { _departments = departments };
    }

    internal void Load(IConfiguration configuration) =>
        _departments = FromConfiguration(configuration)._departments;

    public Department Route(DocumentType type)
    {
        if (type == DocumentType.Unknown) return Department.Unassigned;
        if (_departments.TryGetValue(type, out var department)) return department;
        throw new ArgumentOutOfRangeException(nameof(type), type, "Unsupported or unconfigured document type.");
    }

    private static OptionsValidationException Invalid(string error) =>
        new(SectionName, typeof(RoutingOptions), [error]);
}
