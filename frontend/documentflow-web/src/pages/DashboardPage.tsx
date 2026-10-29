import { useCallback, useEffect, useRef, useState } from 'react'
import { Link } from 'react-router-dom'
import { getDashboardHealth, getDashboardSummary } from '../api/dashboardApi'
import { useAuth } from '../components/AuthProvider'
import type { DashboardHealth, OperationsSummary } from '../types/dashboard'
import { getErrorMessage } from '../utils/formatters'

const reviewUrl = '/documents?status=NeedsReview'
const failedUrl = '/documents?status=Failed'
const statusText: Record<string, string> = {
  Healthy: 'Работает', Critical: 'Критично', Degraded: 'Требует внимания',
  Unknown: 'Нет данных', Unavailable: 'Недоступен', Stale: 'Устарел',
  Disabled: 'Отключено', Available: 'Доступен', ModelUnavailable: 'Модель недоступна',
}

function safeReason(reason: string) {
  const singleLine = reason.trim()
  // Treat diagnostic payloads as untrusted, even if the API normally sanitizes them.
  if (!singleLine || /[\r\n<>]|(?:[A-Za-z]:\\|\/(?:home|var|etc)\/)|(?:stack trace|exception at| at \w+\.|password\s*=|username\s*=|connectionstring|select\s+.+\s+from\s+)/i.test(singleLine)) {
    return 'Причина недоступна'
  }
  return singleLine.length > 160 ? `${singleLine.slice(0, 160)}…` : singleLine
}

function timestamp(value: string) {
  const date = new Date(value)
  return Number.isNaN(date.getTime()) ? 'Время неизвестно' :
    new Intl.DateTimeFormat('ru-RU', { dateStyle: 'short', timeStyle: 'short', timeZone: 'UTC' }).format(date) + ' UTC'
}

function Metric({ label, count, to, tone }: { label: string; count: number; to?: string; tone?: string }) {
  const content = <><span>{label}</span><strong>{count}</strong></>
  return to ? <Link className={`metric-card metric-link ${tone ?? ''}`} to={to}>{content}</Link> :
    <article className={`metric-card ${tone ?? ''}`}>{content}</article>
}

export function DashboardPage() {
  const { user } = useAuth()
  const [summary, setSummary] = useState<OperationsSummary | null>(null)
  const [health, setHealth] = useState<DashboardHealth | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const sequence = useRef(0)
  const controller = useRef<AbortController | null>(null)
  const active = useRef(false)

  const load = useCallback(async () => {
    controller.current?.abort()
    const abort = new AbortController()
    controller.current = abort
    const request = ++sequence.current
    try {
      const [nextSummary, nextHealth] = await Promise.all([
        getDashboardSummary(abort.signal), getDashboardHealth(abort.signal),
      ])
      if (!active.current || request !== sequence.current) return
      setSummary(nextSummary)
      setHealth(nextHealth)
      setError(null)
    } catch (requestError) {
      if (active.current && request === sequence.current && !abort.signal.aborted) setError(getErrorMessage(requestError))
    } finally {
      if (active.current && request === sequence.current) setLoading(false)
    }
  }, [])

  useEffect(() => {
    active.current = true
    void load()
    const timer = window.setInterval(() => void load(), 25000)
    return () => { active.current = false; ++sequence.current; controller.current?.abort(); window.clearInterval(timer) }
  }, [load])

  return <div>
    <div className="page-heading"><div><h1>Обзор обработки</h1><p>Очередь документов и состояние обработки. «Сегодня» — по UTC.</p></div>
      <button className="button button-secondary" type="button" onClick={() => void load()}>Обновить</button></div>
    {error && <div className="alert alert-error" role="alert">{error} <button className="button button-secondary" type="button" onClick={() => void load()}>Повторить</button></div>}
    {loading && !summary && <div className="state-panel">Загрузка...</div>}
    {summary && <>
      <section className="dashboard-cards" aria-label="Основные показатели">
        <Metric label="На проверке" count={summary.needsReview} to={reviewUrl} tone="metric-warning" />
        <Metric label="Ошибки" count={summary.failed} to={failedUrl} tone="metric-danger" />
        <Metric label="Обрабатываются" count={summary.processing} />
        <Metric label="Ожидают обработки" count={summary.pendingProcessing} />
        <Metric label="Завершено сегодня (UTC)" count={summary.completedToday} tone="metric-success" />
      </section>
      <div className="dashboard-totals"><span>Активных документов: <strong>{summary.totalActive}</strong></span><span>В архиве <strong>{summary.archivedTotal}</strong></span></div>
      <nav className="dashboard-actions" aria-label="Быстрые действия">
        <Link className="button" to="/upload">Загрузить документы</Link>
        <Link className="button button-secondary" to={reviewUrl}>Требуют проверки</Link>
        <Link className="button button-secondary" to={failedUrl}>Ошибки обработки</Link>
      </nav>
      {summary.needsReview === 0 && summary.failed === 0 && summary.processing === 0 &&
        <p className="card dashboard-empty">Документов, требующих внимания, нет.</p>}
      {summary.recentNeedsReview.length > 0 && <section className="card dashboard-section">
        <div className="section-heading"><h2>Последние на проверке</h2><Link to={reviewUrl}>Показать все</Link></div>
        <ul className="dashboard-recent">{summary.recentNeedsReview.map(item => <li key={item.id}>
          <div><strong>{item.fileName}</strong><small>{item.type} · <time dateTime={item.createdAtUtc}>{timestamp(item.createdAtUtc)}</time></small></div>
          <Link to={`/documents/${encodeURIComponent(item.id)}#manual-review`}>Проверить</Link>
        </li>)}</ul>
      </section>}
      {summary.failed > 0 && summary.recentFailed.length > 0 && <section className="card dashboard-section">
        <div className="section-heading"><h2>Последние ошибки</h2><Link to={failedUrl}>Показать все ошибки</Link></div>
        <ul className="dashboard-recent">{summary.recentFailed.map(item => <li key={item.id}>
          <div><strong>{item.fileName}</strong><small><time dateTime={item.createdAtUtc}>{timestamp(item.createdAtUtc)}</time> · {safeReason(item.reason)}</small></div>
          <Link to={`/documents/${encodeURIComponent(item.id)}`}>Открыть</Link>
        </li>)}</ul>
      </section>}
    </>}
    {health && <section className="card dashboard-section" aria-label="Состояние системы">
      <h2>Состояние системы</h2>
      <p className="dashboard-overall" role="status">{statusText[health.overall]}</p>
      {health.ai?.status === 'Unavailable' || health.ai?.status === 'ModelUnavailable'
        ? <p>{health.overall === 'Healthy' ? 'Основная обработка работает, AI-подсказки недоступны.' : 'AI-подсказки недоступны.'}</p> : null}
      {user?.role === 'Admin' && <dl className="dashboard-health">
        <div><dt>API</dt><dd>{statusText[health.api ?? 'Unknown']}</dd></div>
        <div><dt>PostgreSQL</dt><dd>{statusText[health.postgresql ?? 'Unknown']}</dd></div>
        <div><dt>Worker</dt><dd>{statusText[health.worker?.status ?? 'Unknown']} · {health.worker?.healthyInstances ?? 0} работают / {health.worker?.staleInstances ?? 0} устарел</dd></div>
        <div><dt>OCR</dt><dd>{statusText[health.ocr?.status ?? 'Unknown']} · {health.ocr?.languages?.length ? health.ocr.languages.join(', ') : 'Нет данных'}</dd></div>
        <div><dt>AI</dt><dd>{statusText[health.ai?.status ?? 'Unknown']}</dd></div>
        <div><dt>Версия</dt><dd>{health.version ?? 'Нет данных'}</dd></div>
        {health.runtime && <div><dt>Runtime</dt><dd>{health.runtime}</dd></div>}
      </dl>}
    </section>}
  </div>
}
