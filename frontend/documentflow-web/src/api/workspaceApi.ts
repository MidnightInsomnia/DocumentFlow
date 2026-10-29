import { ApiError, apiFetch, apiRequest } from './apiClient'
import { getErrorMessage } from '../utils/formatters'
import type {
  Catalogs, CounterpartySearchItem, DepartmentInput, DocumentKind,
  DocumentSearchResult, DocumentWorkspace, KindInput, OrganizationDepartment,
  SearchFilters, SearchIndexStatus,
} from '../types/workspace'

export function workspaceErrorMessage(error: unknown): string {
  if (error instanceof ApiError && error.status === 401) return 'Сеанс завершён. Войдите снова, чтобы продолжить.'
  if (error instanceof ApiError && error.status === 403) return 'Недостаточно прав для этого действия.'
  return getErrorMessage(error)
}

export function getCatalogs(signal?: AbortSignal): Promise<Catalogs> {
  return apiRequest<Catalogs>('/api/catalogs', { signal })
}

async function list<T>(path: string, signal?: AbortSignal): Promise<T[]> {
  const result = await apiRequest<{ items: T[] } | T[]>(path, { signal })
  return Array.isArray(result) ? result : result.items
}

export function getAdminKinds(signal?: AbortSignal): Promise<DocumentKind[]> {
  return list<DocumentKind>('/api/admin/catalogs/kinds', signal)
}

export function getAdminDepartments(signal?: AbortSignal): Promise<OrganizationDepartment[]> {
  return list<OrganizationDepartment>('/api/admin/catalogs/departments', signal)
}

async function write(path: string, method: 'POST' | 'PUT' | 'DELETE', body?: unknown): Promise<void> {
  await apiFetch(path, {
    method,
    ...(body === undefined ? {} : { headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) }),
  })
}

export function createKind(body: KindInput): Promise<void> {
  return write('/api/admin/catalogs/kinds', 'POST', body)
}
export function updateKind(id: string, body: KindInput): Promise<void> {
  return write(`/api/admin/catalogs/kinds/${encodeURIComponent(id)}`, 'PUT', body)
}
export function deleteKind(id: string): Promise<void> {
  return write(`/api/admin/catalogs/kinds/${encodeURIComponent(id)}`, 'DELETE')
}
export function approveKind(id: string, body: KindInput): Promise<void> {
  return write(`/api/admin/catalogs/kinds/${encodeURIComponent(id)}/approve`, 'POST', body)
}
export function createDepartment(body: DepartmentInput): Promise<void> {
  return write('/api/admin/catalogs/departments', 'POST', body)
}
export function updateDepartment(id: string, body: DepartmentInput): Promise<void> {
  return write(`/api/admin/catalogs/departments/${encodeURIComponent(id)}`, 'PUT', body)
}
export function deleteDepartment(id: string): Promise<void> {
  return write(`/api/admin/catalogs/departments/${encodeURIComponent(id)}`, 'DELETE')
}

export function getDocumentWorkspace(id: string, signal?: AbortSignal): Promise<DocumentWorkspace> {
  return apiRequest<DocumentWorkspace>(`/api/workspace/documents/${encodeURIComponent(id)}`, { signal })
}
export async function updateDocumentWorkspace(id: string, body: { kindId?: string | null; departmentId?: string | null }): Promise<DocumentWorkspace> {
  await write(`/api/workspace/documents/${encodeURIComponent(id)}`, 'PUT', body)
  return getDocumentWorkspace(id)
}

export function searchDocuments(filters: SearchFilters, page: number, signal?: AbortSignal): Promise<DocumentSearchResult> {
  const params = new URLSearchParams({ page: String(page), pageSize: '20' })
  for (const [key, value] of Object.entries(filters)) if (value) params.set(key, value)
  return apiRequest<DocumentSearchResult>(`/api/search/documents?${params}`, { signal })
}
export function searchCounterparties(q: string, signal?: AbortSignal): Promise<{ items: CounterpartySearchItem[] }> {
  return apiRequest<{ items: CounterpartySearchItem[] }>(`/api/search/counterparties?${new URLSearchParams({ q })}`, { signal })
}
export function getSearchIndexStatus(signal?: AbortSignal): Promise<SearchIndexStatus> {
  return apiRequest<SearchIndexStatus>('/api/admin/search/status', { signal })
}
export async function reindexSearch(): Promise<SearchIndexStatus> {
  await write('/api/admin/search/reindex?rebuild=true', 'POST', {})
  return getSearchIndexStatus()
}
