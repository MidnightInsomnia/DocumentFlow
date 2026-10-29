import { useCallback, useEffect, useMemo, useRef, useState } from 'react'
import { Link, useSearchParams } from 'react-router-dom'
import { getDocuments } from '../api/documentsApi'
import { exportDocuments } from '../api/reviewWorkApi'
import { Icon } from '../components/Icon'
import { InnRoles } from '../components/InnRoles'
import { StatusBadge } from '../components/StatusBadge'
import type { Department, DocumentStatus, DocumentType, PagedDocuments } from '../types/documents'
import { formatDate, getErrorMessage } from '../utils/formatters'
import { departmentLabels, statusLabels, typeLabels } from '../utils/labels'
import '../stitch-lists.css'

const pageSize = 20

function FileFormat({ name }: { name: string }) {
  const extension = name.includes('.') ? name.split('.').pop()?.toLowerCase() : ''
  return <span className="stitch-file-format" data-format={extension}>{extension?.toUpperCase() || 'ФАЙЛ'}</span>
}

export function DocumentsPage() {
  const [params, setParams] = useSearchParams()
  const pageParam = Number(params.get('page'))
  const page = Number.isSafeInteger(pageParam) && pageParam >= 1 && pageParam <= 1000000 ? pageParam : 1
  const statusValue = params.get('status') ?? ''
  const status = statusValue in statusLabels ? statusValue as DocumentStatus : ''
  const archiveValue = params.get('archive')
  const archive: 'active' | 'archived' | 'all' = archiveValue === 'archived' || archiveValue === 'all' ? archiveValue : 'active'
  const typeValue = params.get('type') ?? ''
  const type = typeValue in typeLabels ? typeValue as DocumentType : ''
  const departmentValue = params.get('department') ?? ''
  const department = departmentValue in departmentLabels ? departmentValue as Department : ''
  const search = params.get('search') ?? ''
  const createdFrom = params.get('createdFrom') ?? ''
  const createdTo = params.get('createdTo') ?? ''
  const sortValue = params.get('sortBy')
  const sortBy: 'createdAt' | 'fileName' | 'status' | 'type' = sortValue === 'fileName' || sortValue === 'status' || sortValue === 'type' ? sortValue : 'createdAt'
  const sortDirection: 'asc' | 'desc' = params.get('sortDirection') === 'asc' ? 'asc' : 'desc'
  const [searchDraft, setSearchDraft] = useState(search)
  useEffect(() => setSearchDraft(search), [search])
  const [data, setData] = useState<PagedDocuments | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const requestEpoch = useRef(0)
  const controllerRef = useRef<AbortController | null>(null)
  const [updatedAt, setUpdatedAt] = useState<Date | null>(null)
  const [exporting, setExporting] = useState(false)
  const [exportError, setExportError] = useState<string | null>(null)
  const dateError = createdFrom && createdTo && createdFrom > createdTo ? 'Начало периода должно быть не позже окончания.' : null

  const filters = useMemo(() => ({
    archive,
    status: status || undefined,
    type: type || undefined,
    department: department || undefined,
    search: search || undefined,
    createdFrom: createdFrom || undefined,
    createdTo: createdTo || undefined,
    sortBy,
    sortDirection,
  }), [archive, status, type, department, search, createdFrom, createdTo, sortBy, sortDirection])

  const load = useCallback(async (showLoading: boolean) => {
    if (dateError) { setLoading(false); return }
    controllerRef.current?.abort()
    const controller = new AbortController(); controllerRef.current = controller
    const epoch = ++requestEpoch.current
    if (showLoading) setLoading(true)
    try {
      const result = await getDocuments(page, pageSize, filters, controller.signal)
      if (epoch === requestEpoch.current && !controller.signal.aborted) { setData(result); setError(null); setUpdatedAt(new Date()) }
    } catch (requestError) {
      if (epoch === requestEpoch.current && !controller.signal.aborted) setError(getErrorMessage(requestError))
    } finally {
      if (epoch === requestEpoch.current && !controller.signal.aborted) setLoading(false)
    }
  }, [page, filters, dateError])

  useEffect(() => {
    void load(true)
    const timer = window.setInterval(() => void load(false), 5000)
    return () => { requestEpoch.current++; controllerRef.current?.abort(); window.clearInterval(timer) }
  }, [load])

  const totalPages = data ? Math.max(1, Math.ceil(data.totalCount / pageSize)) : 1
  useEffect(() => {
    if (!loading && data && page > totalPages) setParams(current => { const next = new URLSearchParams(current); next.set('page', String(totalPages)); return next }, { replace: true })
  }, [loading, data, page, totalPages, setParams])
  async function exportCsv() {
    setExporting(true); setExportError(null)
    try { await exportDocuments(new URLSearchParams(params)) }
    catch (cause) { setExportError(getErrorMessage(cause)) }
    finally { setExporting(false) }
  }
  const returnTo = `/documents${params.size ? `?${params}` : ''}`
  const hasFilters = Boolean(status || type || department || search || createdFrom || createdTo || archive !== 'active')
  const setFilter = (key: string, value: string, resetPage = true) => setParams((current) => {
    const next = new URLSearchParams(current)
    if (value && !(key === 'archive' && value === 'active') && !(key === 'sortBy' && value === 'createdAt') && !(key === 'sortDirection' && value === 'desc') && !(key === 'page' && value === '1')) next.set(key, value)
    else next.delete(key)
    if (resetPage) next.delete('page')
    return next
  })
  const applySearch = (event: React.FormEvent) => {
    event.preventDefault()
    setFilter('search', searchDraft.trim())
  }
  const resetFilters = () => {
    setSearchDraft('')
    setParams(new URLSearchParams())
  }

  return (
    <div className="stitch-list-page stitch-registry">
      <div className="page-heading stitch-list-heading">
        <div><h1>Документы</h1><p>Контроль обработки и маршрутизации документов.</p></div>
        <Link className="button" to="/upload"><Icon name="upload" size={15} />Загрузить документы</Link>
      </div>
      <section className="stitch-registry-controls" aria-label="Фильтры документов">
        <div className="stitch-status-switches" role="group" aria-label="Статус документа">
          <button type="button" aria-pressed={!status} onClick={() => setFilter('status', '')}>Все документы</button>
          {Object.entries(statusLabels).map(([value, label]) => <button key={value} type="button" data-status={value} aria-pressed={status === value} onClick={() => setFilter('status', value)}><span className="stitch-status-dot" aria-hidden="true" />{value === 'NeedsReview' ? 'Требуют проверки' : label}</button>)}
        </div>
        <div className="stitch-registry-filters">
          <form className="stitch-registry-query" onSubmit={applySearch}>
            <label>Поиск по всем данным документа
              <input type="search" value={searchDraft} maxLength={100} onChange={(e) => setSearchDraft(e.target.value)} placeholder="Имя, номер, ИНН любой стороны, сумма или текст..." />
            </label>
            <button className="button button-secondary" type="submit"><Icon name="search" size={14} />Найти</button>
          </form>
          <label>Тип<select value={type} onChange={(e) => setFilter('type', e.target.value)}>
            <option value="">Все</option>{Object.entries(typeLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
          </select></label>
          <label>Отдел<select value={department} onChange={(e) => setFilter('department', e.target.value)}>
            <option value="">Все</option>{Object.entries(departmentLabels).map(([value, label]) => <option key={value} value={value}>{label}</option>)}
          </select></label>
          <label>Архив<select value={archive} onChange={(e) => setFilter('archive', e.target.value)}>
            <option value="active">Активные</option><option value="archived">Архивные</option><option value="all">Все</option>
          </select></label>
          <label>Загружен с<input type="date" value={createdFrom} aria-invalid={Boolean(dateError)} aria-describedby={dateError ? 'registry-date-error' : undefined} onChange={(e) => setFilter('createdFrom', e.target.value)} /></label>
          <label>Загружен по<input type="date" value={createdTo} aria-invalid={Boolean(dateError)} aria-describedby={dateError ? 'registry-date-error' : undefined} onChange={(e) => setFilter('createdTo', e.target.value)} /></label>
          <label>Сортировать по<select value={sortBy} onChange={(e) => setFilter('sortBy', e.target.value)}>
            <option value="createdAt">Дате создания</option><option value="fileName">Имени файла</option><option value="status">Статусу</option><option value="type">Типу</option>
          </select></label>
          <label>Направление<select value={sortDirection} onChange={(e) => setFilter('sortDirection', e.target.value)}>
            <option value="desc">По убыванию</option><option value="asc">По возрастанию</option>
          </select></label>
          <button className="button button-secondary stitch-reset" type="button" onClick={resetFilters}><Icon name="refresh-cw" size={13} />Сбросить</button>
        </div>
      </section>
      {dateError && <p id="registry-date-error" className="alert alert-error" role="alert">{dateError}</p>}
      {exportError && <p className="alert alert-error" role="alert">Не удалось выгрузить CSV: {exportError}</p>}
      <div className="stitch-table-toolbar">
        <p className="stitch-help">CSV содержит только подтверждённые документы из выбранной выборки.</p>
        <button type="button" className="button button-secondary" disabled={exporting || !!dateError} onClick={() => void exportCsv()}><Icon name="download" size={14} />{exporting ? 'Выгрузка...' : 'CSV подтверждённых'}</button>
      </div>
      <section className="stitch-results-region" aria-label="Реестр документов" aria-busy={loading}>
        {loading && <div className="state-panel" role="status">Загрузка...</div>}
        {error && <div className="alert alert-error" role="alert"><p>Не удалось загрузить документы: {error}</p><button className="button button-secondary" type="button" onClick={() => void load(true)}>Повторить</button></div>}
        {!loading && !dateError && !error && data?.items.length === 0 && <div className="state-panel">{hasFilters ? <><p>По выбранным фильтрам ничего не найдено</p><button className="button button-secondary" type="button" onClick={resetFilters}>Сбросить фильтры</button></> : 'Документов пока нет'}</div>}
        {!loading && !dateError && data && data.items.length > 0 && <div className="stitch-table-scroll" tabIndex={0} role="region" aria-label="Таблица документов, прокручивается по горизонтали">
          <table className="stitch-data-table stitch-registry-table">
            <thead><tr><th scope="col">Статус</th><th scope="col">Файл и формат</th><th scope="col">Тип</th><th scope="col">Номер / дата</th><th scope="col">ИНН сторон</th><th scope="col" className="stitch-amount">Сумма</th><th scope="col">Отдел</th><th scope="col">Загружен / обработан</th><th scope="col">Действие</th></tr></thead>
            <tbody>{data.items.map((document) => <tr className={document.status === 'NeedsReview' && !document.isArchived ? 'stitch-row-attention' : undefined} key={document.id}>
              <td><StatusBadge status={document.status} />{document.isArchived && <span className="stitch-archive-label"><Icon name="archive" size={12} />В архиве</span>}</td>
              <td><div className="stitch-file-cell"><FileFormat name={document.fileName} /><strong className="stitch-file-name">{document.fileName}</strong></div>{search && document.matchReason && <div className="stitch-help stitch-match-reason">{document.matchReason}</div>}</td>
              <td>{typeLabels[document.type]}</td>
              <td className="stitch-mono"><strong>{document.number || '—'}</strong><span className="stitch-cell-note">{document.date || '—'}</span></td>
              <td className="stitch-mono"><InnRoles type={document.type} values={document} compact /></td>
              <td className="stitch-mono stitch-amount">{document.amount || '—'}</td>
              <td><span className="stitch-department">{departmentLabels[document.targetDepartment]}</span></td>
              <td><span className="stitch-cell-note stitch-date-line"><span>Загружен</span><time dateTime={document.createdAtUtc}>{formatDate(document.createdAtUtc)}</time></span><span className="stitch-cell-note stitch-date-line"><span>Обработан</span>{document.processedAtUtc ? <time dateTime={document.processedAtUtc}>{formatDate(document.processedAtUtc)}</time> : '—'}</span></td>
              <td>{document.status === 'NeedsReview' && !document.isArchived
                ? <Link className="button stitch-row-action" aria-label={`Проверить ${document.fileName}`} state={{ returnTo }} to={`/documents/${document.id}#manual-review`}>Проверить<Icon name="chevron-right" size={13} /></Link>
                : <Link className="button button-secondary stitch-row-action" aria-label={`Открыть ${document.fileName}`} state={{ returnTo }} to={`/documents/${document.id}`}>Открыть<Icon name="chevron-right" size={13} /></Link>}</td>
            </tr>)}</tbody>
          </table>
        </div>}
      </section>
      <footer className="stitch-list-footer">
        <span className="stitch-help">{updatedAt ? <>Обновлено: <time className="stitch-mono" dateTime={updatedAt.toISOString()}>{updatedAt.toLocaleTimeString('ru-RU')}</time></> : 'Ожидание обновления реестра'}</span>
        {data && <nav className="stitch-pagination" aria-label="Страницы реестра">
          <span>Страница <strong>{page}</strong> / {totalPages} · Всего документов: <strong>{data.totalCount}</strong></span>
          <div className="stitch-inline-actions"><button type="button" className="button button-secondary" disabled={page <= 1} onClick={() => setFilter('page', String(page - 1), false)}><Icon name="chevron-left" size={13} />Назад</button>
          <button type="button" className="button button-secondary" disabled={page >= totalPages} onClick={() => setFilter('page', String(page + 1), false)}>Вперёд<Icon name="chevron-right" size={13} /></button></div>
        </nav>}
      </footer>
    </div>
  )
}
