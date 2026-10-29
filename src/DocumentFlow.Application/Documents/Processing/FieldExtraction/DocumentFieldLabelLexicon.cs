namespace DocumentFlow.Application.Documents.Processing.FieldExtraction;

public static class DocumentFieldLabelLexicon
{
    public static readonly string[] Date = ["дата оформления", "дата подписания", "оформлен", "дата"];

    public static readonly string[] InvoiceNumber =
    [
        "счет на оплату", "счет поставщика", "платежный счет", "документ к оплате",
        "оплата по счету", "расчетный документ", "счет для оплаты", "счет"
    ];

    public static readonly string[] ContractNumber = ["регистрационный номер", "договор", "контракт"];
    public static readonly string[] ActNumber = ["номер акта", "номер документа", "акт выполненных работ", "акт оказанных услуг", "акт"];

    public static readonly string[] InvoiceAmount =
        ["итого к оплате", "всего к оплате", "сумма к оплате", "к перечислению"];

    public static readonly string[] ContractAmount =
        ["цена договора", "стоимость договора", "сумма договора", "общая стоимость по договору", "общая стоимость соглашения"];

    public static readonly string[] ActAmount =
        ["стоимость принятых работ", "стоимость работ", "итого", "всего", "сумма"];

    public static readonly string[] RelatedContractNumber =
        ["к договору", "по договору", "основание — договор", "основание: договор", "основание договор"];
}
