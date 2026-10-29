import { useCallback, useEffect, useMemo, useRef, useState, type ChangeEvent, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { getDocument, uploadDocument } from '../api/documentsApi'
import { useNavigationGuard } from '../hooks/useNavigationGuard'
import { isUploadSessionCurrent, takeUploadFiles, useUploadInbox } from '../hooks/useUploadInbox'
import { getSession } from '../api/session'
import { formatFileSize, getErrorMessage } from '../utils/formatters'
import { Icon } from '../components/Icon'
import { StatusBadge } from '../components/StatusBadge'
import { uploadBatch, validateBatch, validateFile, type UploadItem, type UploadItemStatus } from '../utils/uploadBatch'
import '../stitch-lists.css'

const statusLabels: Record<UploadItemStatus, string> = {
  waiting: 'Ожидает', uploading: 'Загружается', queued: 'Файл принят', duplicate: 'Дубликат', error: 'Ошибка',
}

type SelectionMode = 'append' | 'replace'
let selectionSequence = 0

function createUploadItems(files: readonly File[]): UploadItem[] {
  const selection = ++selectionSequence
  return files.map((file, index) => {
    const error = validateFile(file)
    return {
      key: globalThis.crypto?.randomUUID?.() ?? `upload-${selection}-${index}`,
      file,
      status: error ? 'error' : 'waiting',
      error,
    }
  })
}

export function UploadDocumentPage() {
  const inbox = useUploadInbox()
  const [items, setItems] = useState<UploadItem[]>([])
  const mounted = useRef(true)
  const [uploading, setUploading] = useState(false)
  const [completed, setCompleted] = useState(false)
  const [inputKey, setInputKey] = useState(0)
  const [selectionMode, setSelectionMode] = useState<SelectionMode>('append')
  const historyKey = `documentflow.uploads.${getSession()?.user.id ?? 'anonymous'}`
  const [recentIds, setRecentIds] = useState<string[]>(() => {
    try { const raw: unknown = JSON.parse(sessionStorage.getItem(historyKey) || '[]'); return Array.isArray(raw) ? raw.filter((id): id is string => typeof id === 'string' && /^[0-9a-f-]{36}$/i.test(id)).slice(0, 100) : [] } catch { return [] }
  })
  useNavigationGuard(uploading || items.some(item => item.status === 'waiting' || item.status === 'error' && !item.documentId), 'Есть неотправленные файлы. Они не сохранятся при выходе. Покинуть страницу?')
  useEffect(() => {
    const ids = items.flatMap(item => item.documentId || item.existingDocumentId || [])
    if (!ids.length) return
    setRecentIds(current => {
      const next = Array.from(new Set([...ids, ...current])).slice(0, 100)
      try { sessionStorage.setItem(historyKey, JSON.stringify(next)) } catch { /* Upload itself does not depend on browser storage. */ }
      return next
    })
  }, [items, historyKey])

  const counts = useMemo(() => ({
    waiting: items.filter((item) => item.status === 'waiting').length,
    uploading: items.filter((item) => item.status === 'uploading').length,
    queued: items.filter((item) => item.status === 'queued').length,
    duplicate: items.filter((item) => item.status === 'duplicate').length,
    error: items.filter((item) => item.status === 'error').length,
  }), [items])
  const batchError = useMemo(
    () => validateBatch(items.map((item) => item.file)) ?? null,
    [items],
  )

  const addFiles = useCallback((files: readonly File[], mode: SelectionMode) => {
    const selected = createUploadItems(files)
    setItems(current => mode === 'append' ? [...current, ...selected] : selected)
    setCompleted(false)
    setInputKey(value => value + 1)
  }, [])

  useEffect(() => {
    mounted.current = true
    return () => { mounted.current = false }
  }, [])

  useEffect(() => {
    if (uploading || !inbox.files.length) return
    // A synchronous claim is idempotent under StrictMode. Drops always append;
    // replace is an explicit file-picker action, never a destructive global drop.
    const files = takeUploadFiles(inbox.sessionVersion)
    if (files.length) addFiles(files, 'append')
  }, [inbox.files, inbox.sessionVersion, uploading, addFiles])

  function receiveFiles(files: File[]) {
    if (uploading || !files.length) return false
    if (selectionMode === 'replace' && items.length && !window.confirm('Заменить текущий список файлов? Уже загруженные документы останутся в реестре.')) return false
    addFiles(files, selectionMode)
    return true
  }

  function selectFiles(event: ChangeEvent<HTMLInputElement>) {
    if (!receiveFiles(Array.from(event.target.files ?? []))) event.target.value = ''
  }

  function updateItem(key: string, value: Partial<UploadItem>) {
    if (!mounted.current || !isUploadSessionCurrent(inbox.sessionVersion)) return
    setItems((current) => current.map((item) => item.key === key ? { ...item, ...value } : item))
  }

  function uploadInCurrentSession(file: File) {
    if (!mounted.current || !isUploadSessionCurrent(inbox.sessionVersion)) return Promise.reject(new Error('Страница загрузки закрыта или сеанс изменился.'))
    return uploadDocument(file)
  }

  const pollingKey = items.filter((item) => item.status === 'queued' && item.documentId &&
    item.processingStatus !== 'Completed' && item.processingStatus !== 'NeedsReview' && item.processingStatus !== 'Failed')
    .map((item) => `${item.key}:${item.documentId}`).join('|')

  async function refreshProcessing(key: string, id: string, isCancelled: () => boolean = () => false) {
    try {
      const result = await getDocument(id)
      if (!isCancelled()) updateItem(key, { processingStatus: result.status, pollError: undefined })
    } catch (requestError) {
      if (!isCancelled()) updateItem(key, { pollError: `Не удалось обновить статус: ${getErrorMessage(requestError)}` })
    }
  }

  useEffect(() => {
    if (!pollingKey) return
    let cancelled = false
    let timer: number
    const candidates = pollingKey.split('|').map((entry) => {
      const index = entry.indexOf(':')
      return { key: entry.slice(0, index), id: entry.slice(index + 1) }
    })
    async function poll() {
      await Promise.all(candidates.map(({ key, id }) => refreshProcessing(key, id, () => cancelled)))
      if (!cancelled) timer = window.setTimeout(() => void poll(), 3000)
    }
    timer = window.setTimeout(() => void poll(), 3000)
    return () => { cancelled = true; window.clearTimeout(timer) }
  }, [pollingKey])

  async function submit(event: FormEvent) {
    event.preventDefault()
    if (uploading || batchError || !items.some((item) => item.status === 'waiting')) return
    setUploading(true)
    setCompleted(false)
    try {
      await uploadBatch(items, uploadInCurrentSession, updateItem, getErrorMessage)
      setCompleted(true)
    } finally {
      setUploading(false)
    }
  }

  function reset() {
    setItems([])
    setCompleted(false)
    setInputKey((value) => value + 1)
  }

  async function retryUpload(item: UploadItem) {
    if (uploading || item.status !== 'error' || validateFile(item.file) || batchError) return
    setUploading(true)
    setCompleted(false)
    updateItem(item.key, { status: 'waiting', error: undefined })
    try {
      await uploadBatch([{ ...item, status: 'waiting' }], uploadInCurrentSession, updateItem, getErrorMessage)
      setCompleted(true)
    } finally {
      setUploading(false)
    }
  }

  const finishedCount = counts.queued + counts.duplicate + counts.error
  const uploadNoun = counts.waiting === 1 ? 'документ' : counts.waiting < 5 ? 'документа' : 'документов'

  return <div className="stitch-list-page stitch-upload">
    <div className="page-heading stitch-list-heading"><div><h1>Загрузить документы</h1><p id="upload-limits">До 20 PDF, DOCX, JPG, JPEG, PNG, TIF или TIFF; каждый файл — до 20 МиБ, общий размер партии — до 100 МиБ.</p></div><Link className="button button-secondary" to="/documents"><Icon name="chevron-left" size={14} />К реестру</Link></div>
    <section className="stitch-upload-surface" aria-label="Загрузка документов"><form onSubmit={submit}>
      <div className="stitch-upload-selection"><label>Выбор на диске<select disabled={uploading} value={selectionMode} onChange={event => setSelectionMode(event.target.value as typeof selectionMode)}><option value="append">Добавить к текущему списку</option><option value="replace">Заменить текущий список</option></select></label><p className="stitch-help">Перетаскивание всегда добавляет файлы к списку. Замена применяется к выбору на диске; принятые документы остаются в реестре.</p></div>
      <div className={`stitch-dropzone${uploading ? ' is-disabled' : ''}`}>
        <Icon name="upload" size={28} />
        <label htmlFor="upload-files"><strong>Перетащите файлы в любое место окна или выберите на диске</strong><span className="stitch-help">Выберите файлы · PDF, DOCX, JPG, JPEG, PNG, TIF, TIFF</span>
          <input id="upload-files" key={inputKey} accept=".pdf,.docx,.jpg,.jpeg,.png,.tif,.tiff" disabled={uploading} multiple type="file" onChange={selectFiles} aria-describedby="upload-limits" />
        </label>
      </div>
      {inbox.files.length > 0 && <p className="alert alert-info" role="status">Новых файлов ожидает добавления: {inbox.files.length}. Они появятся в списке после текущей загрузки; для их отправки потребуется снова нажать «Загрузить документы».</p>}
      {batchError && <div className="alert alert-error" role="alert">{batchError}</div>}
      {items.length === 0 && <p className="stitch-upload-empty">Файлы ещё не выбраны. Добавьте документы, чтобы проверить список перед загрузкой.</p>}
      {items.length > 0 && <>
        <div className="stitch-upload-summary" aria-live="polite">
          <span>Всего: <strong>{items.length}</strong></span><span>Ожидают: <strong>{counts.waiting}</strong></span>
          <span>Загружаются: <strong>{counts.uploading}</strong></span><span>Принято: <strong>{counts.queued}</strong></span>
          <span>Дубликаты: <strong>{counts.duplicate}</strong></span><span>Ошибки: <strong>{counts.error}</strong></span>
        </div>
        <div className="stitch-table-scroll" tabIndex={0} role="region" aria-label="Файлы в пакете, прокручивается по горизонтали"><table className="stitch-data-table stitch-upload-table">
          <thead><tr><th scope="col">Файл и формат</th><th scope="col">Размер</th><th scope="col">Статус и обработка</th><th scope="col">Действия</th></tr></thead>
          <tbody>{items.map((item) => {
            const extension = item.file.name.includes('.') ? item.file.name.split('.').pop()?.toLowerCase() : ''
            return <tr className={`stitch-upload-row-${item.status}`} key={item.key}>
              <td><div className="stitch-file-cell"><span className="stitch-file-format" data-format={extension}>{extension?.toUpperCase() || 'ФАЙЛ'}</span><strong className="stitch-file-name">{item.file.name}</strong></div></td>
              <td className="stitch-mono">{formatFileSize(item.file.size)}</td>
              <td><span className={`stitch-upload-status stitch-upload-status-${item.status}`}><Icon name={item.status === 'queued' ? 'circle-check' : item.status === 'error' || item.status === 'duplicate' ? 'triangle-alert' : item.status === 'uploading' ? 'refresh-cw' : 'file-text'} size={12} />{statusLabels[item.status]}</span>
                {item.status === 'queued' && item.processingStatus && <small className="stitch-processing-status">Обработка: <StatusBadge status={item.processingStatus} /></small>}
                {item.processingStatus === 'Failed' && <small className="stitch-item-error">Ошибка обработки. Откройте документ для повторной обработки.</small>}
                {item.pollError && <small className="stitch-item-error" role="alert">{item.pollError}</small>}
                {item.status === 'duplicate' && <><small className="stitch-cell-note">{item.existingIsArchived ? 'Этот файл уже загружен и находится в архиве.' : 'Этот файл уже загружен.'}</small><small className="stitch-cell-note">Существующий документ: {item.existingFileName}</small></>}
                {item.error && <small className="stitch-item-error">{item.error}</small>}
              </td>
              <td><div className="stitch-row-actions">
                {!uploading && !completed && <button className="button button-secondary stitch-remove-file" type="button" aria-label={`Удалить ${item.file.name} из списка`} onClick={() => setItems((current) => current.filter((candidate) => candidate.key !== item.key))}><Icon name="trash-2" size={13} />Удалить</button>}
                {item.status === 'error' && !validateFile(item.file) && !batchError && <button className="button button-secondary" disabled={uploading} type="button" onClick={() => void retryUpload(item)}><Icon name="refresh-cw" size={13} />Повторить</button>}
                {item.pollError && item.documentId && <button className="button button-secondary" type="button" onClick={() => void refreshProcessing(item.key, item.documentId!)}>Обновить статус</button>}
                {item.documentId && <Link className={`button${item.processingStatus === 'NeedsReview' ? '' : ' button-secondary'}`} to={item.processingStatus === 'NeedsReview' ? `/documents/${item.documentId}#manual-review` : `/documents/${item.documentId}`}>{item.processingStatus === 'NeedsReview' ? 'Проверить' : 'Открыть'}<Icon name="chevron-right" size={13} /></Link>}
                {item.existingDocumentId && <Link className="button button-secondary" to={`/documents/${item.existingDocumentId}`}>{item.existingIsArchived ? 'Открыть документ' : 'Открыть существующий документ'}</Link>}
              </div></td>
            </tr>
          })}</tbody>
        </table></div>
        {uploading && <p className="stitch-help stitch-upload-notice" role="status">Не закрывайте страницу до завершения загрузки. Завершено: {finishedCount} из {items.length}.</p>}
        {completed && <div className={`alert ${counts.error > 0 || counts.duplicate > 0 ? 'alert-info' : 'alert-success'}`} role="status"><strong>Результаты загрузки.</strong><p>Успешно: {counts.queued}. Дубликаты: {counts.duplicate}. Ошибки: {counts.error}.</p><p>Принятые файлы ещё могут ожидать обработки.</p></div>}
      </>}
      <footer className="stitch-upload-footer">
        <div className="stitch-upload-progress"><span>{items.length > 0 ? `Завершено: ${finishedCount} из ${items.length}` : 'Добавьте файлы для загрузки'}</span>{items.length > 0 && <progress value={finishedCount} max={items.length} aria-label="Файлы с итоговым статусом загрузки" />}</div>
        <div className="stitch-inline-actions">
          {!completed && <><button className="button button-secondary" disabled={uploading || items.length === 0} type="button" onClick={reset}>Очистить</button><button className="button" disabled={uploading || Boolean(batchError) || counts.waiting === 0} type="submit"><Icon name="upload" size={14} />{uploading ? `Загрузка: ${finishedCount} из ${items.length}` : counts.waiting === 0 ? 'Загрузить документы' : `Загрузить ${counts.waiting} ${uploadNoun}`}</button></>}
          {completed && <><button className="button button-secondary" type="button" onClick={reset}>Новая загрузка</button><Link className="button" to="/documents">Перейти к документам<Icon name="chevron-right" size={14} /></Link></>}
        </div>
      </footer>
    </form></section>
    {recentIds.length > 0 && <section className="stitch-upload-history" aria-labelledby="upload-history-heading"><div className="stitch-table-toolbar"><h2 id="upload-history-heading">Недавно принятые в этой вкладке</h2><button type="button" className="button button-secondary" onClick={() => { setRecentIds([]); sessionStorage.removeItem(historyKey) }}>Очистить историю</button></div><p className="stitch-help">Сохранены только ссылки на документы. Содержимое файлов в браузере не сохраняется; история исчезнет при закрытии вкладки.</p><ul>{recentIds.map((id, index) => <li key={id}><Link className="text-link" to={`/documents/${id}`}><Icon name="file-text" size={13} />Документ {index + 1} · <span className="stitch-mono">{id.slice(0, 8)}</span></Link></li>)}</ul></section>}
  </div>
}
