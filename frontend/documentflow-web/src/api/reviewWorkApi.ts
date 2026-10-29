import { ApiError, apiFetch, jsonRequest } from './apiClient'
import type { ReviewableDocumentType } from '../types/documents'

export interface ReviewDraft {
  type: ReviewableDocumentType | ''
  fields: { name: string; value: string }[]
  baseVersion: string
  updatedAtUtc?: string
}

function parseDraft(value: unknown): ReviewDraft | null {
  if (value === null) return null
  if (typeof value !== 'object' || Array.isArray(value)) throw new Error('Сервер вернул некорректный черновик. Повторите загрузку.')
  const draft = value as Record<string, unknown>
  if (typeof draft.type !== 'string' || !['', 'Invoice', 'Contract', 'Act'].includes(draft.type) || typeof draft.baseVersion !== 'string' || !draft.baseVersion ||
      !Array.isArray(draft.fields) || draft.fields.length > 20 ||
      draft.updatedAtUtc !== undefined && (typeof draft.updatedAtUtc !== 'string' || !Number.isFinite(Date.parse(draft.updatedAtUtc)))) {
    throw new Error('Сервер вернул некорректный черновик. Повторите загрузку.')
  }
  const seen = new Set<string>()
  const fields = draft.fields.map((item: unknown) => {
    if (typeof item !== 'object' || item === null || Array.isArray(item)) throw new Error('Не удалось прочитать поля черновика.')
    const field = item as Record<string, unknown>
    if (typeof field.name !== 'string' || !['Number', 'Date', 'INN', 'KPP', 'Amount', 'RelatedContractNumber', 'CustomerINN', 'ContractorINN', 'SupplierINN', 'BuyerINN'].includes(field.name) ||
        seen.has(field.name) || (field.value !== null && typeof field.value !== 'string') || (typeof field.value === 'string' && field.value.length > 4000)) throw new Error('Не удалось прочитать поля черновика.')
    seen.add(field.name)
    return { name: field.name, value: field.value as string | null ?? '' }
  })
  return { type: draft.type as ReviewDraft['type'], fields, baseVersion: draft.baseVersion, updatedAtUtc: draft.updatedAtUtc as string | undefined }
}

export async function getReviewDraft(id: string, signal?: AbortSignal): Promise<ReviewDraft | null> {
  try {
    const response = await apiFetch(`/api/documents/${encodeURIComponent(id)}/review/draft`, { signal })
    return response.status === 204 ? null : parseDraft(await response.json())
  } catch (error) {
    if (error instanceof ApiError && error.status === 404) return null
    throw error
  }
}

export async function saveReviewDraft(id: string, draft: ReviewDraft): Promise<ReviewDraft | null> {
  const response = await apiFetch(`/api/documents/${encodeURIComponent(id)}/review/draft`, {
    method: 'PUT', headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ type: draft.type, fields: draft.fields, baseVersion: draft.baseVersion, expectedDraftUpdatedAtUtc: draft.updatedAtUtc ?? null }),
  })
  // Older servers can acknowledge a successful save without a JSON body.
  return response.status === 204 ? null : parseDraft(await response.json())
}
export async function reopenDocument(id: string, reason: string, expectedVersion: string): Promise<void> {
  await jsonRequest(`/api/documents/${encodeURIComponent(id)}/reopen`, 'POST', { reason, expectedVersion })
}
export async function resolveDocument(id: string, resolution: 'notSupported' | 'irrelevant', reason: string, expectedVersion: string): Promise<void> {
  await jsonRequest(`/api/documents/${encodeURIComponent(id)}/resolve`, 'POST', { resolution, reason, expectedVersion })
}
export async function exportDocuments(params: URLSearchParams): Promise<void> {
  const response = await apiFetch(`/api/documents/export?${params}`)
  const url = URL.createObjectURL(await response.blob())
  const anchor = document.createElement('a')
  anchor.href = url; anchor.download = 'documentflow-requisites.csv'
  document.body.appendChild(anchor); anchor.click(); anchor.remove()
  window.setTimeout(() => URL.revokeObjectURL(url), 1000)
}
