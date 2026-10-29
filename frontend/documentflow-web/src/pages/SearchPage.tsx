import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, useLocation, useSearchParams } from 'react-router-dom'
import { getCatalogs, searchCounterparties, searchDocuments, workspaceErrorMessage } from '../api/workspaceApi'
import { StatusBadge } from '../components/StatusBadge'
import { Icon } from '../components/Icon'
import { InnRoles } from '../components/InnRoles'
import { departmentLabels, statusLabels, typeLabels } from '../utils/labels'
import { formatDate } from '../utils/formatters'
import type { Catalogs, CounterpartySearchItem, DocumentSearchResult, SearchFilters } from '../types/workspace'
import '../workspace.css'
import '../stitch-lists.css'

function readFilters(params: URLSearchParams): SearchFilters {
  const archive = params.get('archive')
  return {
    q: params.get('q') ?? '', mode: params.get('mode') === 'semantic' ? 'semantic' : 'text',
    type: params.get('type') ?? '', status: params.get('status') ?? '',
    departmentId: params.get('departmentId') ?? '', kindId: params.get('kindId') ?? '',
    inn: params.get('inn') ?? '', archive: archive === 'all' || archive === 'archived' ? archive : 'active',
  }
}

export function SearchPage() {
  const [params, setParams] = useSearchParams()
  const location = useLocation()
  const queryKey = params.toString()
  const applied = readFilters(params)
  const requestedPage = Number(params.get('page') ?? 1)
  const page = Number.isSafeInteger(requestedPage) && requestedPage > 0 ? requestedPage : 1
  const [draft, setDraft] = useState<SearchFilters>(() => readFilters(params))
  const [catalogs, setCatalogs] = useState<Catalogs>({ kinds: [], departments: [] })
  const [catalogError, setCatalogError] = useState<string | null>(null)
  const [catalogLoading, setCatalogLoading] = useState(true)
  const [catalogRevision, setCatalogRevision] = useState(0)
  const [result, setResult] = useState<DocumentSearchResult | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [revision, setRevision] = useState(0)
  const [counterpartyQuery, setCounterpartyQuery] = useState('')
  const [counterparties, setCounterparties] = useState<CounterpartySearchItem[] | null>(null)
  const [counterpartyLoading, setCounterpartyLoading] = useState(false)
  const [counterpartyError, setCounterpartyError] = useState<string | null>(null)
  const counterpartyController = useRef<AbortController | null>(null)
  const innInput = useRef<HTMLInputElement>(null)

  useEffect(() => { setDraft(readFilters(new URLSearchParams(queryKey))) }, [queryKey])
  useEffect(() => {
    const controller = new AbortController()
    setCatalogLoading(true); setCatalogError(null)
    getCatalogs(controller.signal).then(data => { if (!controller.signal.aborted) setCatalogs(data) })
      .catch(cause => { if (!controller.signal.aborted) setCatalogError(workspaceErrorMessage(cause)) })
      .finally(() => { if (!controller.signal.aborted) setCatalogLoading(false) })
    return () => controller.abort()
  }, [catalogRevision])
  useEffect(() => {
    const controller = new AbortController()
    setLoading(true); setError(null); setResult(null)
    searchDocuments(readFilters(new URLSearchParams(queryKey)), page, controller.signal).then(data => {
      if (controller.signal.aborted) return
      const last = Math.max(1, Math.ceil(data.totalCount / Math.max(1, data.pageSize)))
      if (page > last && !(applied.mode === 'semantic' && !data.semanticAvailable)) {
        const next = new URLSearchParams(queryKey); next.set('page', String(last)); setParams(next, { replace: true }); return
      }
      setResult(data)
    }).catch(cause => { if (!controller.signal.aborted) setError(workspaceErrorMessage(cause)) })
      .finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [queryKey, page, revision, setParams, applied.mode])
  useEffect(() => () => counterpartyController.current?.abort(), [])

  function apply(filters: SearchFilters, nextPage = 1) {
    const next = new URLSearchParams(params)
    for (const [key, value] of Object.entries(filters)) {
      if (value) next.set(key, value)
      else next.delete(key)
    }
    next.set('page', String(nextPage)); next.set('pageSize', '20')
    setParams(next)
    if (next.toString() === queryKey) setRevision(value => value + 1)
  }
  function submit(event: FormEvent) {
    event.preventDefault()
    apply({ ...draft, q: draft.q.trim(), inn: draft.inn.trim() })
  }
  async function findCounterparties(event?: FormEvent) {
    event?.preventDefault()
    counterpartyController.current?.abort()
    const controller = new AbortController(); counterpartyController.current = controller
    setCounterpartyLoading(true); setCounterpartyError(null); setCounterparties(null)
    try {
      const data = await searchCounterparties(counterpartyQuery.trim(), controller.signal)
      if (!controller.signal.aborted) setCounterparties(data.items)
    } catch (cause) { if (!controller.signal.aborted) setCounterpartyError(workspaceErrorMessage(cause)) }
    finally { if (!controller.signal.aborted) setCounterpartyLoading(false) }
  }
  const semanticUnavailable = applied.mode === 'semantic' && result?.semanticAvailable === false
  const lastPage = result ? Math.max(1, Math.ceil(result.totalCount / Math.max(1, result.pageSize))) : 1
  const returnTo = location.pathname + location.search
  const showInactiveKind = draft.kindId && !catalogs.kinds.some(item => item.id === draft.kindId)
  const showInactiveDepartment = draft.departmentId && !catalogs.departments.some(item => item.id === draft.departmentId)

  return <div className="workspace-page stitch-list-page stitch-search">
    <div className="page-heading stitch-list-heading"><div><h1>Поиск</h1><p>Найдите документ по тексту, смыслу или реквизитам.</p></div><Link className="button button-secondary" to="/documents"><Icon name="file-text" size={14} />К реестру</Link></div>
    <section className="stitch-search-controls" aria-labelledby="search-form-heading">
      <h2 id="search-form-heading">Поиск документов</h2>
      <form onSubmit={submit} className="workspace-form">
        <fieldset className="workspace-mode"><legend>Режим поиска</legend>
          <label><input type="radio" name="search-mode" value="text" checked={draft.mode === 'text'} onChange={() => setDraft({ ...draft, mode: 'text' })} />По тексту</label>
          <label><input type="radio" name="search-mode" value="semantic" checked={draft.mode === 'semantic'} onChange={() => setDraft({ ...draft, mode: 'semantic' })} />По смыслу</label>
        </fieldset>
        <div className="workspace-search-line"><label htmlFor="document-search">{draft.mode === 'semantic' ? 'Опишите содержание документа' : 'Текст или реквизиты'}<input id="document-search" type="search" value={draft.q} required={draft.mode === 'semantic'} onChange={event => setDraft({ ...draft, q: event.target.value })} aria-describedby="search-mode-hint" /></label><button className="button" type="submit" disabled={loading}><Icon name="search" size={14} />Найти документы</button></div>
        <p id="search-mode-hint" className="workspace-help">{draft.mode === 'semantic' ? 'Поиск по смыслу использует локальную модель и только проиндексированные документы. Доступность зависит от сервера.' : 'Поиск по тексту и реквизитам. Оставьте запрос пустым, чтобы найти документы только по фильтрам.'}</p>
        <div className="workspace-filter-grid">
          <label>Вид документа<select value={draft.kindId} onChange={event => setDraft({ ...draft, kindId: event.target.value })}><option value="">Все виды</option>{showInactiveKind && <option value={draft.kindId}>Вид из сохранённого фильтра</option>}{catalogs.kinds.map(item => <option key={item.id} value={item.id}>{item.name}{item.isDraft ? ' — черновик' : ''}{!item.isActive ? ' — неактивен' : ''}</option>)}</select></label>
          <label>Отдел<select value={draft.departmentId} onChange={event => setDraft({ ...draft, departmentId: event.target.value })}><option value="">Все отделы</option>{showInactiveDepartment && <option value={draft.departmentId}>Отдел из сохранённого фильтра</option>}{catalogs.departments.map(item => <option key={item.id} value={item.id}>{item.name}{!item.isActive ? ' — неактивен' : ''}</option>)}</select></label>
          <label>Статус<select value={draft.status} onChange={event => setDraft({ ...draft, status: event.target.value })}><option value="">Все статусы</option>{Object.entries(statusLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label>
          <label>Архив<select value={draft.archive} onChange={event => setDraft({ ...draft, archive: event.target.value as SearchFilters['archive'] })}><option value="active">В работе</option><option value="archived">В архиве</option><option value="all">Все документы</option></select></label>
          <label>Базовая схема<select value={draft.type} onChange={event => setDraft({ ...draft, type: event.target.value })}><option value="">Все схемы</option>{Object.entries(typeLabels).map(([value, label]) => <option value={value} key={value}>{label}</option>)}</select></label>
          <label>ИНН любой стороны (или старой записи)<input ref={innInput} inputMode="numeric" value={draft.inn} onChange={event => setDraft({ ...draft, inn: event.target.value })} /></label>
        </div>
        <div className="workspace-actions"><span className="workspace-help">Фильтры применяются кнопкой «Найти документы».</span><button type="button" className="button button-secondary" onClick={() => apply(readFilters(new URLSearchParams()))} disabled={loading}><Icon name="refresh-cw" size={13} />Сбросить поиск</button></div>
      </form>
      {catalogLoading && <p className="workspace-help" role="status">Загрузка видов и отделов…</p>}
      {catalogError && <div className="alert alert-error" role="alert">Справочники не загружены: {catalogError} <button className="button button-secondary" type="button" onClick={() => setCatalogRevision(value => value + 1)}>Повторить загрузку справочников</button></div>}
      <details className="workspace-counterparties"><summary>Найти контрагента по ИНН</summary>
        <form onSubmit={event => void findCounterparties(event)} className="workspace-search-line"><label>ИНН или его часть<input type="search" inputMode="numeric" value={counterpartyQuery} onChange={event => setCounterpartyQuery(event.target.value)} /></label><button type="submit" className="button button-secondary" disabled={counterpartyLoading}>Найти контрагента</button></form>
        {counterpartyLoading && <p role="status">Загрузка контрагентов…</p>}
        {counterpartyError && <div className="alert alert-error" role="alert">{counterpartyError} <button className="button button-secondary" type="button" onClick={() => void findCounterparties()}>Повторить поиск контрагента</button></div>}
        {counterparties?.length === 0 && <p className="workspace-help" role="status">Контрагенты не найдены. Измените ИНН или его часть.</p>}
        {counterparties && counterparties.length > 0 && <ul className="workspace-counterparty-list">{counterparties.map(item => <li key={item.inn}><div><strong>ИНН {item.inn}</strong><span className="workspace-help">Документов: {item.documentCount}</span></div><button className="button button-secondary" type="button" onClick={() => { setDraft(current => ({ ...current, inn: item.inn })); innInput.current?.focus() }}>Выбрать ИНН {item.inn}</button></li>)}</ul>}
        {draft.inn && <p className="workspace-help">Выбран ИНН {draft.inn}. Нажмите «Найти документы», чтобы применить его вместе с остальными фильтрами.</p>}
      </details>
    </section>
    <section className="stitch-search-results" aria-labelledby="search-results-heading" aria-busy={loading}>
      <div className="stitch-table-toolbar"><h2 id="search-results-heading">Результаты{result && !semanticUnavailable ? `: ${result.totalCount}` : ''}</h2><div className="stitch-results-meta"><span>{applied.mode === 'semantic' ? 'По смыслу' : 'По тексту'}</span>{result && <span>В смысловом индексе: <strong className="stitch-mono">{result.indexedCount}</strong></span>}</div></div>
      {loading && <div className="state-panel" role="status">Поиск документов…</div>}
      {!loading && error && <div className="alert alert-error" role="alert"><p>{error}</p>{applied.mode === 'semantic' && <p>Смысловой поиск не был заменён поиском по тексту.</p>}<div className="workspace-actions"><button className="button button-secondary" type="button" onClick={() => setRevision(value => value + 1)}>Повторить поиск</button>{applied.mode === 'semantic' && <button className="button" type="button" onClick={() => apply({ ...applied, mode: 'text' })}>Перейти к поиску по тексту</button>}</div></div>}
      {!loading && semanticUnavailable && <div className="alert alert-info" role="status"><h3>Поиск по смыслу недоступен</h3><p>{result?.message || 'Локальная модель или смысловой индекс недоступны. Поиск по тексту можно запустить отдельно; фильтры сохранятся.'}</p><div className="workspace-actions"><button className="button" type="button" onClick={() => apply({ ...applied, mode: 'text' })}>Перейти к поиску по тексту</button><button className="button button-secondary" type="button" onClick={() => setRevision(value => value + 1)}>Проверить снова</button></div></div>}
      {!loading && !error && result && !semanticUnavailable && <>
        {result.message && <div className="alert alert-info" role="status">{result.message}</div>}
        {result.items.length === 0 ? <div className="state-panel" role="status"><h3>Документы не найдены</h3><p>Измените запрос, уберите часть фильтров или включите архив.</p>{applied.mode === 'semantic' && <p>Смысловой поиск охватывает только проиндексированные документы.</p>}</div> : <div className="stitch-table-scroll" tabIndex={0} role="region" aria-label="Таблица результатов поиска, прокручивается по горизонтали"><table className="stitch-data-table stitch-search-table">
          <caption>{applied.mode === 'semantic' ? 'Оценка сходства — мера близости запроса, а не вероятность достоверности документа.' : 'Совпадения по тексту и реквизитам.'}</caption>
          <thead><tr><th scope="col">Статус</th><th scope="col">Документ и совпадение</th><th scope="col">Реквизиты</th><th scope="col">Вид и отдел</th><th scope="col">Действие</th></tr></thead>
          <tbody>{result.items.map(item => {
            const extension = item.fileName.includes('.') ? item.fileName.split('.').pop()?.toLowerCase() : ''
            return <tr key={item.id} className={item.status === 'NeedsReview' && !item.isArchived ? 'stitch-row-attention' : undefined}>
              <td><StatusBadge status={item.status} />{item.isArchived && <span className="stitch-archive-label"><Icon name="archive" size={12} />В архиве</span>}</td>
              <td><div className="stitch-file-cell"><span className="stitch-file-format" data-format={extension}>{extension?.toUpperCase() || 'ФАЙЛ'}</span><Link className="text-link stitch-file-name" to={`/documents/${encodeURIComponent(item.id)}`} state={{ returnTo }}>{item.fileName}</Link></div>
                {item.snippet && <p className="workspace-snippet">{item.snippet}</p>}<span className="stitch-cell-note">Загружен: <time dateTime={item.createdAtUtc}>{formatDate(item.createdAtUtc)}</time></span>
                {applied.mode === 'semantic' && typeof item.score === 'number' && Number.isFinite(item.score) && <span className="stitch-cell-note">Сходство: <span className="stitch-mono">{new Intl.NumberFormat('ru-RU', { maximumFractionDigits: 3 }).format(item.score)}</span></span>}
              </td>
              <td><dl className="workspace-business-fields"><div><dt>Номер</dt><dd>{item.number || '—'}</dd></div><div><dt>Дата</dt><dd>{item.date || '—'}</dd></div><div><dt>ИНН</dt><dd><InnRoles type={item.type} values={item} compact /></dd></div><div><dt>Сумма</dt><dd>{item.amount ?? '—'}</dd></div></dl></td>
              <td><strong>{item.kindName || typeLabels[item.type] || item.type}</strong><span className="stitch-cell-note"><span className="stitch-department">{item.departmentName || departmentLabels[item.targetDepartment] || item.targetDepartment}</span></span></td>
              <td><Link className={`button stitch-row-action${item.status === 'NeedsReview' && !item.isArchived ? '' : ' button-secondary'}`} to={`/documents/${encodeURIComponent(item.id)}${item.status === 'NeedsReview' && !item.isArchived ? '#manual-review' : ''}`} state={{ returnTo }} aria-label={`${item.status === 'NeedsReview' && !item.isArchived ? 'Проверить' : 'Открыть'} ${item.fileName}`}>{item.status === 'NeedsReview' && !item.isArchived ? 'Проверить' : 'Открыть'}<Icon name="chevron-right" size={13} /></Link></td>
            </tr>
          })}</tbody>
        </table></div>}
        {result.totalCount > 0 && <nav className="stitch-pagination stitch-search-pagination" aria-label="Страницы результатов поиска"><span>Страница <strong>{page}</strong> из {lastPage} · Всего: <strong>{result.totalCount}</strong></span><div className="stitch-inline-actions"><button className="button button-secondary" type="button" disabled={page <= 1} onClick={() => apply(applied, page - 1)}><Icon name="chevron-left" size={13} />Предыдущая</button><button className="button button-secondary" type="button" disabled={page >= lastPage} onClick={() => apply(applied, page + 1)}>Следующая<Icon name="chevron-right" size={13} /></button></div></nav>}
      </>}
    </section>
  </div>
}
