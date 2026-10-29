export type DocumentStatus =
  | 'PendingProcessing'
  | 'Processing'
  | 'NeedsReview'
  | 'Completed'
  | 'Failed'

export type DocumentType = 'Unknown' | 'Invoice' | 'Contract' | 'Act'
export type ReviewableDocumentType = Exclude<DocumentType, 'Unknown'>
export type Department = 'Unassigned' | 'Accounting' | 'Legal'

export interface DocumentListItem {
  number?: string | null
  date?: string | null
  inn?: string | null
  customerInn?: string | null
  contractorInn?: string | null
  supplierInn?: string | null
  buyerInn?: string | null
  customerINN?: string | null
  contractorINN?: string | null
  supplierINN?: string | null
  buyerINN?: string | null
  amount?: string | null
  id: string
  isArchived: boolean
  fileName: string
  status: DocumentStatus
  type: DocumentType
  classificationConfidence: number | null
  targetDepartment: Department
  createdAtUtc: string
  processedAtUtc: string | null
  matchReason?: string | null
}

export interface PagedDocuments {
  page: number
  pageSize: number
  totalCount: number
  items: DocumentListItem[]
}

export interface DocumentField {
  id: string
  name: string
  value: string | null
  confidence: number | null
  wasManuallyEdited: boolean
  createdAtUtc: string
  updatedAtUtc: string | null
}

export interface DocumentEvent {
  type: string
  message: string | null
  createdAtUtc: string
  actorUserId?: string | null
  actorDisplayName?: string | null
}

export interface DocumentDetails {
  version: string
  id: string
  isArchived: boolean
  archivedAtUtc: string | null
  archivedByDisplayName: string | null
  fileName: string
  contentType: string
  fileSizeBytes: number
  status: DocumentStatus
  type: DocumentType
  classificationConfidence: number | null
  targetDepartment: Department
  extractedText: string | null
  processingError: string | null
  createdAtUtc: string
  processingStartedAtUtc: string | null
  processedAtUtc: string | null
  fields: DocumentField[]
  events: DocumentEvent[]
}

export interface RelatedDocumentItem {
  id: string
  fileName: string
  type: DocumentType
  status: DocumentStatus
  number: string | null
  date: string | null
  amount: string | null
  isArchived: boolean
}

export interface RelatedDocuments {
  documentId: string
  type: DocumentType
  referenceNumber: string | null
  items: RelatedDocumentItem[]
}

export interface PotentialDuplicateItem {
  id: string
  fileName: string
  type: DocumentType
  status: DocumentStatus
  number: string | null
  date: string | null
  inn: string | null
  amount: string | null
  isArchived: boolean
  createdAtUtc: string
  amountDiffers: boolean | null
}

export interface PotentialDuplicates {
  checkStatus?: string
  sourceDocumentId: string
  supported: boolean
  matchKey: { inn: string; number: string; date: string } | null
  reason: string
  sourceAmount: string | null
  items: PotentialDuplicateItem[]
}

export interface ConsistencyWarning {
  code: 'RelatedContractNotFound' | 'RelatedContractAmbiguous' |
    'ActDateBeforeContractDate' | 'ActAmountExceedsContractAmount'
  message: string
  relatedDocumentId: string | null
  actValue: string | null
  contractValue: string | null
}

export interface ConsistencyChecks {
  checkStatus?: string
  reason?: string | null
  checkedRules?: string[]
  documentId: string
  warnings: ConsistencyWarning[]
}

export interface CounterpartyDocumentItem {
  id: string
  fileName: string
  type: DocumentType
  status: DocumentStatus
  number: string | null
  kpp: string | null
  amount: string | null
  createdAtUtc: string
  isArchived: boolean
}

export interface CounterpartyCard {
  inn: string
  knownKpps: string[]
  documentCount: number
  countsByType: Record<DocumentType, number>
  countsByStatus: Record<DocumentStatus, number>
  needsReviewCount: number
  knownDocumentAmountTotal: number
  amountDocumentCount: number
  latestDocuments: CounterpartyDocumentItem[]
  documents: CounterpartyDocumentItem[]
  page: number
  pageSize: number
}

export interface DocumentFilters {
  archive?: 'active' | 'archived' | 'all'
  status?: DocumentStatus
  type?: DocumentType
  department?: Department
  search?: string
  createdFrom?: string
  createdTo?: string
  sortBy: 'createdAt' | 'fileName' | 'status' | 'type'
  sortDirection: 'asc' | 'desc'
}

export interface UploadDocumentResponse {
  id: string
  fileName: string
  status: DocumentStatus
  createdAtUtc: string
}

export interface ReviewDocumentResponse {
  id: string
  status: DocumentStatus
  type: DocumentType
  targetDepartment: Department
}

export interface RetryDocumentResponse {
  id: string
  status: DocumentStatus
}

export interface ReprocessDocumentResponse {
  id: string
  status: DocumentStatus
}

export interface ReviewDocumentFieldRequest {
  name: string
  value: string
}

export interface ReviewDocumentRequest {
  expectedVersion: string
  type: ReviewableDocumentType
  fields: ReviewDocumentFieldRequest[]
}

export interface ValidationIssue {
  code: string
  fieldName: string | null
  message: string
}

export interface ReviewContextIssue {
  code: string
  fieldName: string | null
  message: string
}

export interface ReviewContext {
  id: string
  type: DocumentType
  classificationConfidence: number | null
  issues: ReviewContextIssue[]
}

export interface AiReviewSuggestedField {
  name: string
  value: string
  confidence: number
}

export interface AiReviewSuggestion {
  suggestedType: DocumentType
  classificationConfidence: number
  fields: AiReviewSuggestedField[]
}
