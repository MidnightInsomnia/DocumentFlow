namespace DocumentFlow.Application.Documents.Processing.Validation;

public static class DocumentValidationIssueCodes
{
    public const string UnknownDocumentType = "UnknownDocumentType";
    public const string MissingRequiredField = "MissingRequiredField";
    public const string InvalidFieldValue = "InvalidFieldValue";
    public const string LowFieldConfidence = "LowFieldConfidence";
}
