using System.Globalization;
using System.Text.RegularExpressions;
using DocumentFlow.Application.Abstractions.Processing;
using DocumentFlow.Application.Documents.Processing;
using DocumentFlow.Domain.Constants;
using DocumentFlow.Domain.Entities;
using DocumentFlow.Domain.Enums;

namespace DocumentFlow.Application.Documents.Processing.Validation;

public sealed class RuleBasedDocumentValidator : IDocumentValidator
{
    public const decimal MinimumFieldConfidence = 0.80m;

    public DocumentValidationResult Validate(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        if (document.Type == DocumentType.Unknown)
        {
            return Invalid(new DocumentValidationIssue(
                DocumentValidationIssueCodes.UnknownDocumentType,
                null,
                "Document type could not be determined automatically."));
        }

        var issues = new List<DocumentValidationIssue>();
        var requiredFieldNames = DocumentFieldSchema.GetRequiredFieldNames(document.Type);
        foreach (var fieldName in DocumentFieldSchema.GetSupportedFieldNames(document.Type))
        {
            var field = document.Fields.FirstOrDefault(item => string.Equals(
                item.Name,
                fieldName,
                StringComparison.OrdinalIgnoreCase));

            if (field is null && requiredFieldNames.Contains(fieldName))
            {
                issues.Add(new DocumentValidationIssue(
                    DocumentValidationIssueCodes.MissingRequiredField,
                    fieldName,
                    $"Заполните обязательное поле «{FieldLabel(fieldName)}»."));
                continue;
            }


            if (field is null || (!requiredFieldNames.Contains(fieldName) && string.IsNullOrWhiteSpace(field.Value)))
            {
                continue;
            }

            if (!field.WasManuallyEdited &&
                (field.Confidence is null or < MinimumFieldConfidence))
            {
                issues.Add(new DocumentValidationIssue(
                    DocumentValidationIssueCodes.LowFieldConfidence,
                    fieldName,
                    $"Проверьте поле «{FieldLabel(fieldName)}»: достоверность извлечения недостаточна."));
            }

            if (!IsValidValue(fieldName, field.Value))
            {
                issues.Add(new DocumentValidationIssue(
                    DocumentValidationIssueCodes.InvalidFieldValue,
                    fieldName,
                    InvalidValueMessage(fieldName)));
            }
        }

        return new DocumentValidationResult(issues.Count == 0, issues);
    }

    private static bool IsValidValue(string fieldName, string? value)
    {
        return fieldName switch
        {
            DocumentFieldNames.Number =>
                !string.IsNullOrWhiteSpace(value) && value.Length <= 50,
            DocumentFieldNames.Date =>
                DateOnly.TryParseExact(
                    value,
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out _),
            DocumentFieldNames.Inn or DocumentFieldNames.CustomerInn or
                DocumentFieldNames.ContractorInn or DocumentFieldNames.SupplierInn or
                DocumentFieldNames.BuyerInn =>
                value is not null &&
                value.Length is 10 or 12 &&
                value.All(char.IsAsciiDigit),
            DocumentFieldNames.Kpp =>
                value is not null &&
                value.Length == 9 &&
                value.All(char.IsAsciiDigit),
            DocumentFieldNames.Amount =>
                decimal.TryParse(
                    value is null ? null : DocumentFieldSchema.NormalizeAmount(value),
                    NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out var amount) &&
                amount > 0,
            DocumentFieldNames.RelatedContractNumber =>
                value is not null && Regex.IsMatch(
                    value,
                    @"^[\p{L}\p{Nd}/-]{1,50}$",
                    RegexOptions.CultureInvariant),
            _ => true
        };
    }

    private static string FieldLabel(string name) => name switch
    {
        DocumentFieldNames.Number => "Номер", DocumentFieldNames.Date => "Дата",
        DocumentFieldNames.Inn => "ИНН", DocumentFieldNames.Kpp => "КПП",
        DocumentFieldNames.CustomerInn => "ИНН заказчика",
        DocumentFieldNames.ContractorInn => "ИНН исполнителя",
        DocumentFieldNames.SupplierInn => "ИНН поставщика",
        DocumentFieldNames.BuyerInn => "ИНН покупателя",
        DocumentFieldNames.Amount => "Сумма", DocumentFieldNames.RelatedContractNumber => "Номер договора",
        _ => name
    };

    private static string InvalidValueMessage(string name) => name switch
    {
        DocumentFieldNames.Date => "Укажите существующую дату в формате ГГГГ-ММ-ДД.",
        DocumentFieldNames.Inn or DocumentFieldNames.CustomerInn or
            DocumentFieldNames.ContractorInn or DocumentFieldNames.SupplierInn or
            DocumentFieldNames.BuyerInn => "ИНН должен содержать 10 или 12 цифр без пробелов.",
        DocumentFieldNames.Kpp => "КПП должен содержать 9 цифр.",
        DocumentFieldNames.Amount => "Укажите положительную сумму числом (например, 1 234,56).",
        DocumentFieldNames.Number => "Номер обязателен и не должен превышать 50 символов.",
        DocumentFieldNames.RelatedContractNumber => "Номер договора: не более 50 букв, цифр, дефисов и косых черт.",
        _ => $"Проверьте значение поля «{FieldLabel(name)}»."
    };

    private static DocumentValidationResult Invalid(DocumentValidationIssue issue) =>
        new(false, [issue]);
}
