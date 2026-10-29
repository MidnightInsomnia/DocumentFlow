import { useCallback, useEffect, useRef, useState } from 'react'
import { apiFetch, apiRequest } from '../api/apiClient'
import type { DocumentDetails } from '../types/documents'
import { formatDate, getErrorMessage } from '../utils/formatters'
import { useAuth } from './AuthProvider'

interface Discovery {
  documentId: string
  status: string
  message: string
  canRetry: boolean
  lastAttemptAtUtc: string | null
  nextRetryAtUtc: string | null
  kindId: string | null
  kindName: string | null
  errorCode: string | null
}

export function KindDiscoveryStatus({ document, onAssignmentChanged }: { document: DocumentDetails; onAssignmentChanged: () => void }) {
  const { user } = useAuth()
  const [result, setResult] = useState<Discovery | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const current = useRef(onAssignmentChanged)
  current.current = onAssignmentChanged
  const lastKind = useRef<string | null | undefined>(undefined)
  const endpoint = `/api/workspace/documents/${document.id}/kind-discovery`
  const refresh = useCallback(async (signal?: AbortSignal) => {
    const fresh = await apiRequest<Discovery>(endpoint, { signal })
    if (signal?.aborted) return
    setResult(fresh); setError(null)
    if (lastKind.current !== fresh.kindId) {
      lastKind.current = fresh.kindId
      current.current()
    }
  }, [endpoint])

  useEffect(() => {
    const controller = new AbortController()
    lastKind.current = undefined
    setResult(null); setError(null)
    const load = () => { if (!globalThis.document.hidden) void refresh(controller.signal).catch(cause => { if (!controller.signal.aborted) setError(getErrorMessage(cause)) }) }
    load()
    const timer = window.setInterval(load, 5000)
    return () => { controller.abort(); window.clearInterval(timer) }
  }, [refresh, document.status, document.version])

  async function retry() {
    if (busy) return
    setBusy(true); setError(null)
    try {
      await apiFetch(`${endpoint}/retry`, { method: 'POST' })
      await refresh()
    } catch (cause) { setError(getErrorMessage(cause)) }
    finally { setBusy(false) }
  }

  if (result?.status === 'Assigned' || result?.status === 'Suggested' || result?.status === 'NotApplicable') return null
  return <div className="kind-discovery" aria-label="Автоматическое определение вида">
    <strong>Автоматическое определение</strong>
    {!result && !error && <p role="status">Проверка состояния…</p>}
    {result && <p role="status">{result.message}</p>}
    {result?.nextRetryAtUtc && <small>Следующая автоматическая попытка — не ранее {formatDate(result.nextRetryAtUtc)}.</small>}
    {error && <p className="assignment-field-error" role="alert">{error} <button className="text-button" type="button" onClick={() => void refresh().catch(cause => setError(getErrorMessage(cause)))}>Обновить статус</button></p>}
    {user?.role === 'Admin' && result && ['Pending', 'Discovering', 'RetryScheduled', 'NoProposal'].includes(result.status) && <button className="button button-secondary" type="button" disabled={busy || !result.canRetry} onClick={() => void retry()}>{busy ? 'Постановка в очередь…' : result.status === 'Discovering' ? 'Определение выполняется…' : 'Повторить определение'}</button>}
  </div>
}
