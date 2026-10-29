import { useCallback, useEffect, useRef, useState } from 'react'
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom'
import { ApiError, archiveDocument, getConsistencyChecks, getDocument, getDocuments, getDocumentReviewContext, getPotentialDuplicates, getRelatedDocuments, reprocessDocument, restoreDocument, retryDocument } from '../api/documentsApi'
import { DocumentEvents } from '../components/DocumentEvents'
import { DocumentFieldsTable } from '../components/DocumentFieldsTable'
import { innEntries } from '../components/InnRoles'
import { DocumentReviewForm } from '../components/DocumentReviewForm'
import { DocumentPreview } from '../components/DocumentPreview'
import { ExtractedTextPanel } from '../components/ExtractedTextPanel'
import { StatusBadge } from '../components/StatusBadge'
import { Icon } from '../components/Icon'
import '../stitch-inspection.css'
import { DocumentAssignment } from '../components/DocumentAssignment'
import { reopenDocument, resolveDocument } from '../api/reviewWorkApi'
import { searchDocuments } from '../api/workspaceApi'
import '../document-workspace.css'
import type { ConsistencyChecks, DocumentDetails, PotentialDuplicates, RelatedDocuments, ReviewContext } from '../types/documents'
import { formatConfidence, formatDate, formatDocumentFormat, formatFileSize, getErrorMessage } from '../utils/formatters'
import { departmentLabels, typeLabels } from '../utils/labels'

export function DocumentDetailsPage() {
  const { id } = useParams()
  const location = useLocation()
  const navigate = useNavigate()
  const requestedReturn = (location.state as { returnTo?: string } | null)?.returnTo
  const returnTo = requestedReturn && /^\/(documents|search)(?:[?#]|$)/.test(requestedReturn) ? requestedReturn : '/documents'
  const [theater, setTheater] = useState(false)
  const theaterRef = useRef<HTMLDivElement>(null)
  const theaterTrigger = useRef<HTMLElement | null>(null)
  const [lifecycleAction, setLifecycleAction] = useState<'reopen' | 'resolve' | null>(null)
  const [actionReason, setActionReason] = useState('')
  const [resolution, setResolution] = useState<'notSupported' | 'irrelevant'>('notSupported')
  const [lifecyclePending, setLifecyclePending] = useState(false)
  const requestEpoch = useRef(0)
  const requestController = useRef<AbortController | null>(null)
  const notice = (location.state as { notice?: string } | null)?.notice
  const [document, setDocument] = useState<DocumentDetails | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [actionNotice, setActionNotice] = useState<string | null>(notice ?? null)
  const [retryError, setRetryError] = useState<string | null>(null)
  const [retrying, setRetrying] = useState(false)
  const [reprocessing, setReprocessing] = useState(false)
  const [archivePending, setArchivePending] = useState(false)
  const [reviewContext, setReviewContext] = useState<ReviewContext | null>(null)
  const [reviewContextLoading, setReviewContextLoading] = useState(false)
  const [reviewContextAttempt, setReviewContextAttempt] = useState(0)
  const [reviewContextError, setReviewContextError] = useState<string | null>(null)
  const [related, setRelated] = useState<RelatedDocuments | null>(null)
  const [relatedError, setRelatedError] = useState<string | null>(null)
  const [relatedLoading, setRelatedLoading] = useState(false)
  const [duplicates, setDuplicates] = useState<PotentialDuplicates | null>(null)
  const [duplicatesError, setDuplicatesError] = useState<string | null>(null)
  const [duplicatesLoading, setDuplicatesLoading] = useState(false)
  const [consistency, setConsistency] = useState<ConsistencyChecks | null>(null)
  const [consistencyError, setConsistencyError] = useState<string | null>(null)
  const [consistencyLoading, setConsistencyLoading] = useState(false)

  const load = useCallback(async (showLoading = false) => {
    if (!id) return
    requestController.current?.abort()
    const controller = new AbortController()
    requestController.current = controller
    const epoch = ++requestEpoch.current
    if (showLoading) setLoading(true)
    try {
      const result = await getDocument(id, controller.signal)
      if (epoch !== requestEpoch.current || controller.signal.aborted) return
      setDocument(result)
      setError(null)
    } catch (requestError) {
      if (epoch === requestEpoch.current && !controller.signal.aborted) setError(getErrorMessage(requestError))
    } finally {
      if (epoch === requestEpoch.current && !controller.signal.aborted) setLoading(false)
    }
  }, [id])

  useEffect(() => {
    void load(true)
    setLifecycleAction(null)
    return () => { ++requestEpoch.current; requestController.current?.abort() }
  }, [load])

  function toggleTheater() {
    if (!theater) theaterTrigger.current = globalThis.document.activeElement as HTMLElement | null
    setTheater(value => !value)
  }

  useEffect(() => {
    if (!theater) return
    const previousOverflow = globalThis.document.body.style.overflow
    globalThis.document.body.style.overflow = 'hidden'
    const frame = window.requestAnimationFrame(() => theaterRef.current?.querySelector<HTMLButtonElement>('[data-theater-close]')?.focus())
    const keydown = (event: KeyboardEvent) => {
      if (event.key === 'Escape') { event.preventDefault(); setTheater(false) }
      if (event.key !== 'Tab') return
      const focusable = Array.from(theaterRef.current?.querySelectorAll<HTMLElement>('button:not(:disabled), a[href], input:not(:disabled), select:not(:disabled), textarea:not(:disabled), iframe, [tabindex="0"]') ?? []).filter(node => node.getClientRects().length > 0)
      const first = focusable[0], last = focusable[focusable.length - 1]
      if (event.shiftKey && globalThis.document.activeElement === first) { event.preventDefault(); last?.focus() }
      else if (!event.shiftKey && globalThis.document.activeElement === last) { event.preventDefault(); first?.focus() }
    }
    globalThis.document.addEventListener('keydown', keydown)
    return () => {
      window.cancelAnimationFrame(frame)
      globalThis.document.body.style.overflow = previousOverflow
      globalThis.document.removeEventListener('keydown', keydown)
      theaterTrigger.current?.focus()
    }
  }, [theater])

  async function nextReview() {
    try {
      const query = new URLSearchParams(returnTo.split('?')[1] ?? '')
      const previous = (location.state as { reviewVisited?: unknown } | null)?.reviewVisited
      const visited = new Set(Array.isArray(previous) ? previous.filter((value): value is string => typeof value === 'string') : [])
      if (id) visited.add(id)
      for (let page = 1; ; page++) {
        const result = returnTo.startsWith('/search')
          ? await searchDocuments({ q: query.get('q') || '', mode: query.get('mode') === 'semantic' ? 'semantic' : 'text', type: query.get('type') || '', status: 'NeedsReview', archive: 'active', departmentId: query.get('departmentId') || '', kindId: query.get('kindId') || '', inn: query.get('inn') || '' }, page)
          : await getDocuments(page, 100, { status: 'NeedsReview', archive: 'active', type: (query.get('type') || undefined) as DocumentDetails['type'] | undefined, department: (query.get('department') || undefined) as DocumentDetails['targetDepartment'] | undefined, search: query.get('search') || undefined, createdFrom: query.get('createdFrom') || undefined, createdTo: query.get('createdTo') || undefined, sortBy: 'createdAt', sortDirection: 'asc' })
        const next = result.items.find(item => !visited.has(item.id))
        if (next) {
          navigate(`/documents/${next.id}#manual-review`, { state: { returnTo, reviewVisited: [...visited] } })
          return
        }
        if (!result.items.length || page * result.pageSize >= result.totalCount) break
      }
      setActionNotice('В этом проходе больше нет непросмотренных документов по выборке. Пропущенные документы остаются на проверке; новый проход можно начать из реестра.')
    } catch (cause) { setRetryError(getErrorMessage(cause)) }
  }

  async function finishReview(next = false) {
    setActionNotice('Реквизиты сохранены. Проверка документа завершена.')
    await load()
    if (next) await nextReview()
  }

  async function applyLifecycle(event: React.FormEvent) {
    event.preventDefault()
    if (!document || !lifecycleAction || !actionReason.trim() || lifecyclePending) return
    if (lifecycleAction === 'resolve' && !window.confirm('Завершить разбор и убрать документ из активной очереди? Сначала сохраните нужные правки черновиком. Оригинал останется в архиве.')) return
    setLifecyclePending(true); setRetryError(null)
    try {
      if (lifecycleAction === 'reopen') await reopenDocument(document.id, actionReason.trim(), document.version)
      else await resolveDocument(document.id, resolution, actionReason.trim(), document.version)
      setLifecycleAction(null); setActionReason('')
      setActionNotice(lifecycleAction === 'reopen' ? 'Документ возвращён на проверку. Предыдущий результат сохранён в истории.' : 'Разбор завершён с указанной причиной. Оригинал сохранён в архиве.')
      await load()
    } catch (cause) { setRetryError(getErrorMessage(cause)) }
    finally { setLifecyclePending(false) }
  }

  useEffect(() => {
    if (!document || document.isArchived || document.status !== 'NeedsReview') {
      setReviewContext(null)
      setReviewContextError(null)
      setReviewContextLoading(false)
      return
    }

    let cancelled = false
    setReviewContextLoading(true)
    setReviewContextError(null)
    void getDocumentReviewContext(document.id)
      .then((context) => { if (!cancelled) setReviewContext(context) })
      .catch((requestError) => { if (!cancelled) setReviewContextError(getErrorMessage(requestError)) })
      .finally(() => { if (!cancelled) setReviewContextLoading(false) })
    return () => { cancelled = true }
  }, [document?.id, document?.status, document?.isArchived, reviewContextAttempt])

  useEffect(() => {
    if (location.hash === '#manual-review' && !loading && !reviewContextLoading && reviewContext && document?.status === 'NeedsReview') {
      globalThis.document.getElementById('manual-review')?.scrollIntoView({ block: 'start' })
    }
  }, [location.hash, loading, reviewContextLoading, reviewContext, document?.id, document?.status])

  useEffect(() => {
    if (document?.status !== 'PendingProcessing' && document?.status !== 'Processing') return
    const timer = window.setInterval(() => void load(), 3000)
    return () => window.clearInterval(timer)
  }, [document?.status, load])

  useEffect(() => {
    if (!document || (document.type !== 'Act' && document.type !== 'Contract')) {
      setRelated(null)
      setRelatedError(null)
      setRelatedLoading(false)
      return
    }
    let cancelled = false
    setRelated(null)
    setRelatedError(null)
    setRelatedLoading(true)
    void getRelatedDocuments(document.id)
      .then((value) => { if (!cancelled) setRelated(value) })
      .catch((cause) => { if (!cancelled) setRelatedError(getErrorMessage(cause)) })
      .finally(() => { if (!cancelled) setRelatedLoading(false) })
    return () => { cancelled = true }
  }, [document])

  useEffect(() => {
    if (document?.type !== 'Invoice') {
      setDuplicates(null)
      setDuplicatesError(null)
      setDuplicatesLoading(false)
      return
    }
    let cancelled = false
    setDuplicates(null)
    setDuplicatesError(null)
    setDuplicatesLoading(true)
    void getPotentialDuplicates(document.id)
      .then((value) => { if (!cancelled) setDuplicates(value) })
      .catch((cause) => { if (!cancelled) setDuplicatesError(getErrorMessage(cause)) })
      .finally(() => { if (!cancelled) setDuplicatesLoading(false) })
    return () => { cancelled = true }
  }, [document])

  useEffect(() => {
    if (document?.type !== 'Act') {
      setConsistency(null)
      setConsistencyError(null)
      setConsistencyLoading(false)
      return
    }
    let cancelled = false
    setConsistency(null)
    setConsistencyError(null)
    setConsistencyLoading(true)
    void getConsistencyChecks(document.id)
      .then((value) => { if (!cancelled) setConsistency(value) })
      .catch((cause) => { if (!cancelled) setConsistencyError(getErrorMessage(cause)) })
      .finally(() => { if (!cancelled) setConsistencyLoading(false) })
    return () => { cancelled = true }
  }, [document])

  async function retry() {
    if (!document || document.isArchived || retrying) return
    setRetrying(true)
    setRetryError(null)
    setActionNotice(null)
    try {
      await retryDocument(document.id)
      setActionNotice('Документ повторно поставлен в очередь обработки.')
      await load()
    } catch (requestError) {
      if (requestError instanceof ApiError && requestError.status === 409) {
        setRetryError('Документ уже не находится в статусе Failed.')
        await load()
      } else {
        setRetryError(getErrorMessage(requestError))
      }
    } finally {
      setRetrying(false)
    }
  }

  async function reprocess() {
    if (!document || document.isArchived || reprocessing || retrying || archivePending ||
        document.status === 'PendingProcessing' || document.status === 'Processing') return
    if (!window.confirm('Документ будет повторно обработан текущей версией правил. Автоматические результаты будут заменены, а несохранённые изменения формы потеряны. Продолжить?')) return
    setReprocessing(true)
    setRetryError(null)
    setActionNotice(null)
    try {
      await reprocessDocument(document.id)
      setActionNotice('Документ поставлен в очередь повторной обработки.')
      await load()
    } catch (requestError) {
      if (requestError instanceof ApiError && requestError.status === 409) {
        const messages: Record<string, string> = {
          document_reprocess_manual_edits: 'Документ содержит ручные изменения. Сначала сохраните ваши правки отдельно.',
          document_reprocess_archived: 'Сначала восстановите документ из архива.',
          document_reprocess_in_progress: 'Документ уже обрабатывается.',
          document_reprocess_source_missing: 'Исходный файл документа отсутствует в хранилище.',
        }
        setRetryError(messages[requestError.code ?? ''] ?? getErrorMessage(requestError))
      } else {
        setRetryError(getErrorMessage(requestError))
      }
      // Do not reload on conflict: the manual-review form may contain unsaved edits.
    } finally {
      setReprocessing(false)
    }
  }

  async function changeArchive() {
    if (!document || archivePending || (!document.isArchived && (document.status === 'PendingProcessing' || document.status === 'Processing'))) return
    if (!document.isArchived && !window.confirm('Переместить документ в архив? Сначала сохраните черновик: несохранённые изменения формы будут скрыты.')) return
    setArchivePending(true)
    setRetryError(null)
    setActionNotice(null)
    try {
      if (document.isArchived) await restoreDocument(document.id)
      else await archiveDocument(document.id)
      await load()
      setActionNotice(document.isArchived ? 'Документ восстановлен.' : 'Документ перемещён в архив.')
    } catch (requestError) {
      setRetryError(getErrorMessage(requestError))
    } finally {
      setArchivePending(false)
    }
  }

  if (loading) return <div className="state-panel">Загрузка...</div>
  if (error && !document) return <div className="alert alert-error" role="alert"><p>{error}</p><button className="button button-secondary" onClick={() => void load(true)}>Повторить</button></div>
  if (!document) return <div className="state-panel">Документ не найден.</div>

  return (
    <div className="stitch-inspection">
      <header className="inspection-context">
        <div className="inspection-document-heading">
          <Link className="back-link" to={returnTo}><Icon name="chevron-left" size={14} /> К результатам</Link>
          <span className="inspection-context-divider" aria-hidden="true">/</span>
          <h1><Icon name="file-text" size={16} /><span>{document.fileName}</span></h1>
          <StatusBadge status={document.status} />
          {document.isArchived && <span className="status-badge">В архиве</span>}
        </div>
        <div className="inspection-context-actions">
          {(document.isArchived || (document.status !== 'PendingProcessing' && document.status !== 'Processing')) && <>
            <button className="button button-secondary" type="button" disabled={archivePending || reprocessing} onClick={() => void changeArchive()}><Icon name="archive" size={14} />{archivePending ? 'Сохранение...' : document.isArchived ? 'Восстановить' : 'В архив'}</button>
            {!document.isArchived && <button className="button button-secondary" type="button" title="Документ будет повторно обработан текущей версией правил." disabled={reprocessing || archivePending || retrying} onClick={() => void reprocess()}>{reprocessing ? 'Постановка в очередь...' : 'Обработать заново'}</button>}
          </>}
          {!document.isArchived && document.status === 'Completed' && <button type="button" className="button button-secondary" onClick={() => { setLifecycleAction('reopen'); setActionReason('') }}><Icon name="pencil" size={14} />Исправить реквизиты</button>}
          {!document.isArchived && (document.status === 'NeedsReview' || document.status === 'Failed') && <>
            <button type="button" className="button button-secondary inspection-resolve" onClick={() => { setLifecycleAction('resolve'); setActionReason('') }}>Завершить разбор без подтверждения</button>
            <button type="button" className="button button-secondary" onClick={() => void nextReview()}>Пропустить и открыть следующий<Icon name="chevron-right" size={14} /></button>
          </>}
        </div>
      </header>
      {error && <div className="alert alert-error" role="alert">Не удалось обновить данные: {error}. Показан последний успешный результат. <button type="button" className="text-button" onClick={() => void load()}>Повторить</button></div>}
      {lifecycleAction && <form className="card lifecycle-form" onSubmit={event => void applyLifecycle(event)}>
        <h2>{lifecycleAction === 'reopen' ? 'Вернуть документ на проверку' : 'Завершить разбор'}</h2>
        {lifecycleAction === 'resolve' && <label>Решение<select value={resolution} onChange={event => setResolution(event.target.value as typeof resolution)}><option value="notSupported">Вид документа не поддерживается</option><option value="irrelevant">Не относится к рабочим документам</option></select></label>}
        <label>Причина<textarea required maxLength={1000} value={actionReason} onChange={event => setActionReason(event.target.value)} /></label>
        <div className="workspace-actions"><button className="button" disabled={lifecyclePending} type="submit">{lifecyclePending ? 'Сохранение...' : 'Сохранить решение'}</button><button className="button button-secondary" type="button" disabled={lifecyclePending} onClick={() => setLifecycleAction(null)}>Отмена</button></div>
      </form>}
      {actionNotice && <div className="alert alert-success" role="status">{actionNotice}</div>}
      {document.status === 'Failed' && <div className="alert alert-error">
        <strong>Ошибка обработки</strong><p>{document.processingError ?? 'Причина ошибки не указана.'}</p>
        {!document.isArchived && <button className="button" disabled={retrying} type="button" onClick={() => void retry()}>
          {retrying ? 'Повторная постановка...' : 'Повторить обработку'}
        </button>}
      </div>}
      {retryError && <div className="alert alert-error" role="alert">{retryError}</div>}
      <section className="card details-grid review-summary-strip">
        <Detail label="Статус" value={<StatusBadge status={document.status} />} />
        <Detail label="Тип" value={typeLabels[document.type]} />
        <Detail label="Отдел по правилу" value={departmentLabels[document.targetDepartment]} />
        <Detail label="Загружен" value={formatDate(document.createdAtUtc)} />
        <Detail label="Обработка завершена" value={formatDate(document.processedAtUtc)} />
        {document.isArchived && <Detail label="Архивирован" value={formatDate(document.archivedAtUtc)} />}
        {document.isArchived && <Detail label="Кем архивирован" value={document.archivedByDisplayName || '—'} />}
      </section>
      <details className="review-extra-meta"><summary>Дополнительные сведения</summary>
        <div>Оценка правила распознавания: {formatConfidence(document.classificationConfidence)} (не вероятность правильности) · Размер: {formatFileSize(document.fileSizeBytes)} · Формат: {formatDocumentFormat(document.contentType)} · Обработка начата: {formatDate(document.processingStartedAtUtc)}</div>
      </details>
      <div ref={theaterRef} className={`review-workspace${theater ? ' document-theater' : ''}`} role={theater ? 'dialog' : undefined} aria-modal={theater ? true : undefined} aria-label={theater ? 'Кинотеатр: оригинал и проверка документа' : undefined}>
        {theater && <div className="theater-toolbar"><div><Icon name="file-text" size={16} /><strong>{document.fileName}</strong><span>Оригинал и проверка</span></div><button className="button button-secondary" type="button" data-theater-close onClick={() => setTheater(false)}><Icon name="minimize" size={14} />Выйти из кинотеатра · Esc</button></div>}
        <div className="review-source"><DocumentPreview document={document} theater={theater} onToggleTheater={toggleTheater} /><div hidden={theater}><ExtractedTextPanel text={document.extractedText} /></div></div>
        <div className="review-form-column">
          {document.status === 'NeedsReview' && !document.isArchived ? <>
            {reviewContextLoading && <div className="state-panel">Загрузка данных для проверки...</div>}
            {!reviewContextLoading && reviewContextError && <div className="alert alert-error" role="alert">Не удалось загрузить данные для проверки: {reviewContextError} <button className="text-button" type="button" onClick={() => setReviewContextAttempt(value => value + 1)}>Повторить</button></div>}
            {!reviewContextLoading && reviewContext && <DocumentReviewForm key={document.id} document={document} reviewContext={reviewContext} onReviewed={finishReview} />}
          </> : <><DocumentFieldsTable fields={document.fields} type={document.type} />{document.status === 'Completed' && !document.isArchived && <button type="button" className="button button-secondary" onClick={() => { setTheater(false); setLifecycleAction('reopen'); setActionReason(''); window.scrollTo(0, 0) }}>Исправить реквизиты</button>}</>}
          <DocumentAssignment key={document.id} document={document} />
        </div>
      </div>
      {(() => {
        const value = (name: string) => document.fields.find(field => field.name === name)?.value
        const entries = innEntries(document.type, { inn: value('INN'), customerInn: value('CustomerINN'), contractorInn: value('ContractorINN'), supplierInn: value('SupplierINN'), buyerInn: value('BuyerINN') })
        return entries.length ? <section className="card"><h2>Стороны документа</h2><ul>{entries.map(entry =>
          <li key={entry.key}>{entry.label}: <strong>{entry.value}</strong>{entry.roleSpecific && /^(?:[0-9]{10}|[0-9]{12})$/.test(entry.value) && <> · <Link to={`/counterparties/${encodeURIComponent(entry.value)}`}>Открыть контрагента ({entry.label.toLowerCase()})</Link></>}</li>
        )}</ul></section> : null
      })()}
      {document.type === 'Invoice' && <section className="card">
        <h2>Возможные повторы</h2>
        <p className="muted">Сравнение по ИНН, номеру и дате. Возможное совпадение не означает, что это один и тот же файл.</p>
        {duplicatesLoading && <p>Поиск возможных повторов...</p>}
        {duplicatesError && <p className="alert alert-error" role="alert">Не удалось проверить возможные повторы: {duplicatesError}</p>}
        {duplicates && !duplicates.matchKey && <p className="alert alert-info">Проверка не выполнена: нужны корректные ИНН, номер и дата документа.</p>}
        {duplicates?.matchKey && duplicates.items.length === 0 && <p>Проверка выполнена. Возможные повторы по ИНН, номеру и дате не обнаружены.</p>}
        {duplicates && duplicates.items.length > 0 && <>
          <p>Обнаружен документ с теми же ИНН, номером и датой. Это не подтверждение ошибки.</p>
          <ul>{duplicates.items.map((item) => <li key={item.id}>
            <Link to={`/documents/${item.id}`}>{item.fileName} · № {item.number ?? '—'} · {item.date ?? '—'} · Открыть</Link>
            {' · '}{item.amount ?? 'Сумма не указана'} · <StatusBadge status={item.status} />
            {item.isArchived && <> · <span className="status-badge">В архиве</span></>}
            {item.amountDiffers === true && duplicates.sourceAmount && item.amount &&
              <p className="muted">Сумма отличается: {duplicates.sourceAmount} / {item.amount}</p>}
          </li>)}</ul>
        </>}
      </section>}
      {(document.type === 'Act' || document.type === 'Contract') && <section className="card">
        <h2>{document.type === 'Act' ? 'Связанный договор' : 'Связанные акты'}</h2>
        <p className="muted">Кандидаты на связь по номеру договора. Совпадение номера не подтверждает совпадение сторон; сверьте оригиналы.</p>
        {relatedLoading && <p>Поиск связанных документов...</p>}
        {relatedError && <p className="alert alert-error" role="alert">Не удалось загрузить связи: {relatedError}</p>}
        {related && <>
          {document.type === 'Act' && related.items.length > 1 &&
            <p>Найдено несколько договоров с номером {related.referenceNumber}</p>}
          {related.items.length === 0 && <p>{document.type === 'Act' ? 'Связанный договор не найден' : 'Связанные акты не найдены'}</p>}
          {related.items.length > 0 && <ul>{related.items.map((item) =>
            <li key={item.id}><Link to={`/documents/${item.id}`}>{item.fileName} · {typeLabels[item.type]} {item.number && `№ ${item.number}`} · Открыть</Link> · <StatusBadge status={item.status} /> {item.isArchived && <span className="status-badge">В архиве</span>}</li>
          )}</ul>}
        </>}
      </section>}
      {document.type === 'Act' && <section className="card">
        <h2>Проверки документа</h2>
        <p className="muted">Диагностические сигналы по связанному договору, не юридическое или бухгалтерское заключение.</p>
        {consistencyLoading && <p>Проверка связанных документов...</p>}
        {consistencyError && <p className="alert alert-error" role="alert">Не удалось выполнить проверки документа: {consistencyError}</p>}
        {consistency && (!consistency.checkStatus || consistency.checkStatus.toLowerCase() === 'checked') && consistency.warnings.length === 0 &&
          <p>Расхождений по реализованным проверкам не найдено.</p>}
        {consistency?.checkStatus && consistency.checkStatus.toLowerCase() !== 'checked' && <p className="alert alert-info">{consistency.reason || 'Проверка не выполнена: недостаточно данных или правило неприменимо.'}</p>}
        {consistency && consistency.warnings.length > 0 && <ul>{consistency.warnings.map((warning) =>
          <li className="diagnostic-warning" key={warning.code}>
            {warning.message}
            {warning.actValue && <span> {warning.code === 'RelatedContractNotFound' || warning.code === 'RelatedContractAmbiguous'
              ? `Номер: ${warning.actValue}.` : `Акт: ${warning.actValue}; договор: ${warning.contractValue ?? '—'}.`}</span>}
            {warning.relatedDocumentId && <> <Link to={`/documents/${warning.relatedDocumentId}`}>Открыть договор</Link></>}
          </li>
        )}</ul>}
      </section>}
      <DocumentEvents events={document.events} />
    </div>
  )
}

function Detail({ label, value }: { label: string; value: React.ReactNode }) {
  return <div className="detail-item"><span>{label}</span><strong>{value}</strong></div>
}
