using DocumentFlow.Application.Abstractions.Processing;
using DocumentFlow.Domain.Enums;
using Microsoft.Extensions.Options;

namespace DocumentFlow.Application.Documents.Processing.Routing;

public sealed class RuleBasedDocumentRouter(IOptions<RoutingOptions> options) : IDocumentRouter
{
    // Existing evaluation/test callers use the same defaults from JSON, not C# routing branches.
    public RuleBasedDocumentRouter() : this(Options.Create(RoutingOptions.EmbeddedDefaults())) { }

    public Department Route(DocumentType documentType) => options.Value.Route(documentType);
}

