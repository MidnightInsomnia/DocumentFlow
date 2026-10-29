namespace DocumentFlow.Api.Requests.Documents;

public sealed class UploadDocumentBatchRequest
{
    public List<IFormFile> Files { get; init; } = [];
}
