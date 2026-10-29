import type { Department, DocumentStatus, DocumentType } from '../types/documents'

export const statusLabels: Record<DocumentStatus, string> = {
  PendingProcessing: 'Ожидает обработки',
  Processing: 'Обрабатывается',
  NeedsReview: 'Требует проверки',
  Completed: 'Завершён',
  Failed: 'Ошибка',
}

export const typeLabels: Record<DocumentType, string> = {
  Unknown: 'Не определён',
  Invoice: 'Счёт',
  Contract: 'Договор',
  Act: 'Акт',
}

export const departmentLabels: Record<Department, string> = {
  Unassigned: 'Не назначен',
  Accounting: 'Бухгалтерия',
  Legal: 'Юридический отдел',
}

export const fieldLabels: Record<string, string> = {
  Number: 'Номер документа',
  Date: 'Дата',
  INN: 'ИНН (старая запись, сторона не указана)',
  CustomerINN: 'ИНН заказчика',
  ContractorINN: 'ИНН исполнителя',
  SupplierINN: 'ИНН поставщика',
  BuyerINN: 'ИНН покупателя',
  KPP: 'КПП',
  Amount: 'Сумма',
  RelatedContractNumber: 'Номер связанного договора',
}

export const eventLabels: Record<string, string> = {
  DocumentUploaded: 'Документ загружен',
  ProcessingStarted: 'Начата обработка',
  TextExtracted: 'Текст извлечён',
  DocumentClassified: 'Тип документа определён',
  FieldsExtracted: 'Поля извлечены',
  ManualReviewRequired: 'Требуется ручная проверка',
  ManuallyReviewed: 'Проверено вручную',
  DocumentRouted: 'Документ маршрутизирован',
  ProcessingCompleted: 'Обработка завершена',
  ProcessingFailed: 'Ошибка обработки',
  ProcessingRetryRequested: 'Запрошена повторная обработка',
  DocumentArchived: 'Документ архивирован',
  DocumentRestored: 'Документ восстановлен',
  DocumentReopened: 'Возвращён на проверку',
  DocumentResolved: 'Разбор завершён с решением',
  AiReviewSuggestionRequested: 'Запрошена AI-подсказка',
  ProcessingReprocessRequested: 'Запрошена повторная обработка по текущим правилам',
}
