namespace DocumentFlow.Api.Requests.Documents;

public sealed class UploadDocumentRequest
{
    public IFormFile? File { get; init; }
}
