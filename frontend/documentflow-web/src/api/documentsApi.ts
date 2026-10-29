import { API_BASE_URL } from '../config'
import { apiFetch, apiRequest, ApiError } from './apiClient'
import type {
  DocumentDetails,
  DocumentFilters,
  PagedDocuments,
  ReviewDocumentRequest,
  ReviewDocumentResponse,
  RetryDocumentResponse,
  ReprocessDocumentResponse,
  ReviewContext,
  UploadDocumentResponse,
  AiReviewSuggestion,
  RelatedDocuments,
  CounterpartyCard,
  PotentialDuplicates,
  ConsistencyChecks,
} from '../types/documents'

export { ApiError }
const request = apiRequest

export async function getDocuments(
  page: number,
  pageSize: number,
  filters: DocumentFilters,
  signal?: AbortSignal,
): Promise<PagedDocuments> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  params.set('archive', filters.archive ?? 'active')
  if (filters.status) params.set('status', filters.status)
  if (filters.type) params.set('type', filters.type)
  if (filters.department) params.set('department', filters.department)
  if (filters.search) params.set('search', filters.search)
  if (filters.createdFrom) params.set('createdFrom', filters.createdFrom)
  if (filters.createdTo) params.set('createdTo', filters.createdTo)
  params.set('sortBy', filters.sortBy)
  params.set('sortDirection', filters.sortDirection)
  return request<PagedDocuments>(`/api/documents?${params.toString()}`, { signal })
}

export function getDocument(id: string, signal?: AbortSignal): Promise<DocumentDetails> {
  return request<DocumentDetails>(`/api/documents/${encodeURIComponent(id)}`, { signal })
}

export function getRelatedDocuments(id: string): Promise<RelatedDocuments> {
  return request<RelatedDocuments>(`/api/documents/${encodeURIComponent(id)}/related`)
}

export function getPotentialDuplicates(id: string): Promise<PotentialDuplicates> {
  return request<PotentialDuplicates>(`/api/documents/${encodeURIComponent(id)}/potential-duplicates`)
}

export function getConsistencyChecks(id: string): Promise<ConsistencyChecks> {
  return request<ConsistencyChecks>(`/api/documents/${encodeURIComponent(id)}/consistency-checks`)
}

export function getCounterparty(inn: string, page = 1, pageSize = 20): Promise<CounterpartyCard> {
  const params = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  return request<CounterpartyCard>(`/api/counterparties/${encodeURIComponent(inn)}?${params}`)
}

export async function archiveDocument(id: string): Promise<void> {
  await apiFetch(`/api/documents/${encodeURIComponent(id)}/archive`, { method: 'POST' })
}

export async function restoreDocument(id: string): Promise<void> {
  await apiFetch(`/api/documents/${encodeURIComponent(id)}/restore`, { method: 'POST' })
}

export function getDocumentReviewContext(id: string): Promise<ReviewContext> {
  return request<ReviewContext>(`/api/documents/${encodeURIComponent(id)}/review-context`)
}

export function getDocumentFileUrl(id: string): string {
  return `${API_BASE_URL}/api/documents/${encodeURIComponent(id)}/file`
}

export function getDocumentDownloadUrl(id: string): string {
  return `${getDocumentFileUrl(id)}/download`
}

export function getDocumentPreviewPageUrl(id: string, page: number): string {
  return `${API_BASE_URL}/api/documents/${encodeURIComponent(id)}/preview/pages/${page}`
}

export function getDocumentPreviewPages(id: string, signal?: AbortSignal): Promise<{ pageCount: number }> {
  return request<{ pageCount: number }>(`/api/documents/${encodeURIComponent(id)}/preview/pages`, { signal })
}

export function uploadDocument(file: File): Promise<UploadDocumentResponse> {
  const formData = new FormData()
  formData.append('File', file)
  return request<UploadDocumentResponse>('/api/documents', {
    method: 'POST',
    body: formData,
  })
}

export function reviewDocument(id: string, review: ReviewDocumentRequest): Promise<ReviewDocumentResponse> {
  return request<ReviewDocumentResponse>(`/api/documents/${encodeURIComponent(id)}/review`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify(review),
  })
}

export function getAiReviewSuggestion(id: string): Promise<AiReviewSuggestion> {
  return request<AiReviewSuggestion>(`/api/documents/${encodeURIComponent(id)}/review/ai-suggestion`, {
    method: 'POST',
  })
}

export function reprocessDocument(id: string): Promise<ReprocessDocumentResponse> {
  return request<ReprocessDocumentResponse>(`/api/documents/${encodeURIComponent(id)}/reprocess`, {
    method: 'POST',
  })
}

export function retryDocument(id: string): Promise<RetryDocumentResponse> {
  return request<RetryDocumentResponse>(`/api/documents/${encodeURIComponent(id)}/retry`, {
    method: 'POST',
  })
}
