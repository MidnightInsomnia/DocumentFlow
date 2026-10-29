import { API_BASE_URL } from '../config'
import { clearSession, getSession } from './session'

export class ApiError extends Error {
  constructor(message: string, public readonly status: number, public readonly issues: { code: string; fieldName: string | null; message: string }[] = [], public readonly code?: string) {
    super(message)
    this.name = 'ApiError'
  }
}

export class DuplicateDocumentApiError extends ApiError {
  override readonly code = 'document_duplicate' as const

  constructor(public readonly existingDocumentId: string, public readonly existingFileName: string, public readonly existingIsArchived = false) {
    super('Этот файл уже был загружен.', 409)
    this.name = 'DuplicateDocumentApiError'
  }
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === 'object' && value !== null && !Array.isArray(value)
}

export async function apiFetch(path: string, init?: RequestInit): Promise<Response> {
  const headers = new Headers(init?.headers)
  const token = getSession()?.accessToken
  if (token) headers.set('Authorization', `Bearer ${token}`)
  const response = await fetch(`${API_BASE_URL}${path}`, { ...init, headers })
  if (!response.ok) {
    if (response.status === 401 && token) clearSession()
    let body: unknown = {}
    try { body = await response.json() } catch { /* Non-JSON errors are possible. */ }
    const problem = isRecord(body) ? body : {}
    const existing = problem.existingDocument
    if (response.status === 409 && problem.code === 'document_duplicate' &&
      isRecord(existing) && typeof existing.id === 'string' && existing.id.trim() &&
      typeof existing.fileName === 'string' && existing.fileName.trim()) {
      throw new DuplicateDocumentApiError(existing.id, existing.fileName, existing.isArchived === true)
    }
    const message = [problem.detail, problem.message, problem.title].find((value): value is string => typeof value === 'string')
    const issues = Array.isArray(problem.issues) ? problem.issues as ApiError['issues'] : []
    throw new ApiError(response.status === 403 ? 'Недостаточно прав' : message ?? `Ошибка API (${response.status})`, response.status, issues,
      typeof problem.code === 'string' ? problem.code : undefined)
  }
  return response
}

export async function apiRequest<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await apiFetch(path, init)
  if (response.status === 204 || response.headers.get('content-length') === '0') return undefined as T
  return await response.json() as T
}

export function jsonRequest<T>(path: string, method: string, body: unknown): Promise<T> {
  return apiRequest<T>(path, { method, headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) })
}
