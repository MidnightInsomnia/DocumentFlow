import type { Department, DocumentStatus, DocumentType } from './documents'

export interface DocumentKind {
  id: string
  name: string
  baseType: DocumentType
  isDraft: boolean
  isActive: boolean
  isSystem: boolean
  createdAutomatically: boolean
  departmentId?: string | null
}

export interface OrganizationDepartment {
  id: string
  name: string
  isActive: boolean
  isSystem: boolean
}

export interface Catalogs {
  kinds: DocumentKind[]
  departments: OrganizationDepartment[]
}

export interface KindInput {
  name: string
  baseType: DocumentType
  departmentId?: string | null
  isActive?: boolean
}

export interface DepartmentInput {
  name: string
  isActive?: boolean
}

export interface DocumentWorkspace {
  documentId: string
  kindId?: string | null
  departmentId?: string | null
  kindName?: string | null
  departmentName?: string | null
  resolution?: string | null
  resolutionReason?: string | null
  lastUpdatedAtUtc?: string | null
}

export type SearchMode = 'text' | 'semantic'

export interface SearchFilters {
  q: string
  mode: SearchMode
  type: string
  status: string
  departmentId: string
  kindId: string
  inn: string
  archive: 'active' | 'archived' | 'all'
}

export interface SearchDocumentItem {
  id: string
  fileName: string
  status: DocumentStatus
  type: DocumentType
  targetDepartment: Department
  createdAtUtc: string
  processedAtUtc: string | null
  isArchived: boolean
  number: string | null
  date: string | null
  inn: string | null
  customerInn?: string | null
  contractorInn?: string | null
  supplierInn?: string | null
  buyerInn?: string | null
  customerINN?: string | null
  contractorINN?: string | null
  supplierINN?: string | null
  buyerINN?: string | null
  amount: string | number | null
  kindName: string | null
  departmentName: string | null
  score?: number | null
  snippet: string | null
}

export interface DocumentSearchResult {
  page: number
  pageSize: number
  totalCount: number
  items: SearchDocumentItem[]
  semanticAvailable: boolean
  indexedCount: number
  message?: string | null
}

export interface CounterpartySearchItem {
  inn: string
  documentCount: number
}

export interface SearchIndexStatus {
  semanticAvailable?: boolean
  indexedCount?: number
  totalCount?: number
  model?: string | null
  message?: string | null
}
