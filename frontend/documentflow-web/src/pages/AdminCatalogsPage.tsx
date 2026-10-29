import { useEffect, useRef, useState, type FormEvent, type KeyboardEvent } from 'react'
import { useSearchParams } from 'react-router-dom'
import {
  approveKind, createDepartment, createKind, deleteDepartment, deleteKind,
  getAdminDepartments, getAdminKinds, getSearchIndexStatus, reindexSearch,
  updateDepartment, updateKind, workspaceErrorMessage,
} from '../api/workspaceApi'
import type { Catalogs, DocumentKind, KindInput, OrganizationDepartment, SearchIndexStatus } from '../types/workspace'
import type { DocumentType } from '../types/documents'
import { typeLabels } from '../utils/labels'
import { Icon } from '../components/Icon'
import '../workspace.css'
import '../stitch-inspection.css'

type Tab = 'kinds' | 'departments'
type Editor = { tab: Tab; id: string | null; approve: boolean }
const emptyKind: KindInput = { name: '', baseType: 'Invoice', departmentId: null, isActive: true }

export function AdminCatalogsPage() {
  const [params, setParams] = useSearchParams()
  const tab: Tab = params.get('tab') === 'departments' ? 'departments' : 'kinds'
  const [catalogs, setCatalogs] = useState<Catalogs>({ kinds: [], departments: [] })
  const [loading, setLoading] = useState(true)
  const [loadError, setLoadError] = useState<string | null>(null)
  const [revision, setRevision] = useState(0)
  const [busy, setBusy] = useState(false)
  const [actionError, setActionError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [editor, setEditor] = useState<Editor | null>(null)
  const [kindDraft, setKindDraft] = useState<KindInput>(emptyKind)
  const [departmentDraft, setDepartmentDraft] = useState({ name: '', isActive: true })
  const [indexStatus, setIndexStatus] = useState<SearchIndexStatus | null>(null)
  const [indexError, setIndexError] = useState<string | null>(null)
  const [indexLoading, setIndexLoading] = useState(true)
  const [indexBusy, setIndexBusy] = useState(false)
  const [indexNotice, setIndexNotice] = useState<string | null>(null)
  const [indexRevision, setIndexRevision] = useState(0)
  const editorHeading = useRef<HTMLHeadingElement>(null)
  const kindsTab = useRef<HTMLButtonElement>(null)
  const departmentsTab = useRef<HTMLButtonElement>(null)

  useEffect(() => {
    const controller = new AbortController()
    setLoading(true); setLoadError(null)
    Promise.all([getAdminKinds(controller.signal), getAdminDepartments(controller.signal)]).then(([kinds, departments]) => {
      if (!controller.signal.aborted) setCatalogs({ kinds, departments })
    }).catch(cause => { if (!controller.signal.aborted) setLoadError(workspaceErrorMessage(cause)) })
      .finally(() => { if (!controller.signal.aborted) setLoading(false) })
    return () => controller.abort()
  }, [revision])
  useEffect(() => {
    const controller = new AbortController()
    setIndexLoading(true); setIndexError(null)
    getSearchIndexStatus(controller.signal).then(data => { if (!controller.signal.aborted) setIndexStatus(data) })
      .catch(cause => { if (!controller.signal.aborted) setIndexError(workspaceErrorMessage(cause)) })
      .finally(() => { if (!controller.signal.aborted) setIndexLoading(false) })
    return () => controller.abort()
  }, [indexRevision])
  useEffect(() => { if (editor) editorHeading.current?.focus() }, [editor])
  useEffect(() => { setEditor(current => current?.tab === tab ? current : null) }, [tab])

  function switchTab(next: Tab, focus = false) {
    if (busy) return
    if (editor && !window.confirm('Закрыть форму без сохранения изменений?')) return
    setEditor(null); setActionError(null); setNotice(null)
    const nextParams = new URLSearchParams(params); nextParams.set('tab', next); setParams(nextParams)
    if (focus) (next === 'kinds' ? kindsTab : departmentsTab).current?.focus()
  }
  function tabKey(event: KeyboardEvent<HTMLButtonElement>) {
    if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return
    event.preventDefault()
    switchTab(event.key === 'Home' ? 'kinds' : event.key === 'End' ? 'departments' : tab === 'kinds' ? 'departments' : 'kinds', true)
  }
  function editKind(item?: DocumentKind, approve = false) {
    setKindDraft(item ? { name: item.name, baseType: item.baseType, departmentId: item.departmentId ?? null, isActive: item.isActive } : { ...emptyKind })
    setActionError(null); setNotice(null); setEditor({ tab: 'kinds', id: item?.id ?? null, approve })
  }
  function editDepartment(item?: OrganizationDepartment) {
    setDepartmentDraft(item ? { name: item.name, isActive: item.isActive } : { name: '', isActive: true })
    setActionError(null); setNotice(null); setEditor({ tab: 'departments', id: item?.id ?? null, approve: false })
  }
  async function mutate(action: () => Promise<void>, verify: (data: Catalogs) => boolean, message: string, closeEditor = false) {
    setBusy(true); setActionError(null); setNotice(null)
    let sent = false
    try {
      await action(); sent = true
      if (closeEditor) setEditor(null)
      const [kinds, departments] = await Promise.all([getAdminKinds(), getAdminDepartments()])
      const fresh = { kinds, departments }; setCatalogs(fresh)
      if (!verify(fresh)) throw new Error('Сервер вернул данные, не подтверждающие изменение. Обновите справочники перед повторным действием.')
      setNotice(message)
    } catch (cause) {
      setActionError(`${sent ? 'Запрос выполнен, но подтверждение результата не получено. ' : ''}${workspaceErrorMessage(cause)}`)
    } finally { setBusy(false) }
  }
  async function save(event: FormEvent) {
    event.preventDefault()
    if (!editor || busy || loading) return
    if (editor.tab === 'kinds') {
      const name = kindDraft.name.normalize('NFKC').trim().replace(/\s+/gu, ' ')
      if (!name) { setActionError('Введите название вида документа.'); return }
      const body: KindInput = { name, baseType: kindDraft.baseType, departmentId: kindDraft.departmentId || null, ...(editor.approve ? {} : { isActive: kindDraft.isActive }) }
      const existingIds = new Set(catalogs.kinds.map(item => item.id))
      const id = editor.id
      await mutate(
        () => id ? editor.approve ? approveKind(id, body) : updateKind(id, body) : createKind({ name, baseType: body.baseType, departmentId: body.departmentId }),
        fresh => fresh.kinds.some(item => (id ? item.id === id : !existingIds.has(item.id)) && item.name === name && item.baseType === body.baseType && (item.departmentId ?? null) === body.departmentId && (!editor.approve || !item.isDraft) && (editor.approve || !id || item.isActive === body.isActive)),
        editor.approve ? 'Вид подтверждён администратором.' : id ? 'Вид документа сохранён.' : 'Вид документа создан.', true,
      )
    } else {
      const name = departmentDraft.name.normalize('NFKC').trim().replace(/\s+/gu, ' ')
      if (!name) { setActionError('Введите название отдела.'); return }
      const id = editor.id
      const existingIds = new Set(catalogs.departments.map(item => item.id))
      await mutate(
        () => id ? updateDepartment(id, { name, isActive: departmentDraft.isActive }) : createDepartment({ name }),
        fresh => fresh.departments.some(item => (id ? item.id === id : !existingIds.has(item.id)) && item.name === name && (!id || item.isActive === departmentDraft.isActive)),
        id ? 'Отдел сохранён.' : 'Отдел создан.', true,
      )
    }
  }
  function removeKind(item: DocumentKind) {
    if (!window.confirm(`Удалить вид «${item.name}»? Если он используется, сервер безопасно деактивирует его или отклонит удаление. Документы и история не удаляются.`)) return
    void mutate(() => deleteKind(item.id), fresh => !fresh.kinds.some(row => row.id === item.id && row.isActive), 'Состояние вида обновлено: он удалён или деактивирован. Документы сохранены.')
  }
  function removeDepartment(item: OrganizationDepartment) {
    if (!window.confirm(`Удалить отдел «${item.name}»? Если он используется, сервер безопасно деактивирует его или отклонит удаление. Документы и история не удаляются.`)) return
    void mutate(() => deleteDepartment(item.id), fresh => !fresh.departments.some(row => row.id === item.id && row.isActive), 'Состояние отдела обновлено: он удалён или деактивирован. Документы сохранены.')
  }
  async function reindex() {
    if (!window.confirm('Перестроить смысловой индекс? Он будет построен заново в фоне. Пока обработка не завершится, часть документов может отсутствовать в поиске по смыслу. Документы и поиск по тексту сохранятся.')) return
    setIndexBusy(true); setIndexError(null); setIndexNotice(null)
    try { setIndexStatus(await reindexSearch()); setIndexNotice('Запрос индексации выполнен. Ниже — актуальное состояние сервера; это не означает, что все документы проиндексированы.') }
    catch (cause) { setIndexError(workspaceErrorMessage(cause)) }
    finally { setIndexBusy(false) }
  }
  const displayedEditor = editor?.tab === tab ? editor : null
  const editingItem = editor?.id ? catalogs.kinds.find(item => item.id === editor.id) : null
  const autoDraft = !!editingItem?.isDraft && !!editingItem.createdAutomatically
  const draftCount = catalogs.kinds.filter(item => item.isDraft).length
  const aiDrafts = catalogs.kinds.filter(item => item.isDraft && item.createdAutomatically)
  const managedKinds = catalogs.kinds.filter(item => !(item.isDraft && item.createdAutomatically))

  function kindTable(items: DocumentKind[], label: string) {
    return <div className="catalog-table-scroll" role="region" aria-label={label} tabIndex={0}>
      <table className="catalog-table">
        <thead><tr><th scope="col">Статус</th><th scope="col">Вид документа</th><th scope="col">Схема извлечения</th><th scope="col">Отдел по умолчанию</th><th scope="col" className="catalog-actions-heading">Действия</th></tr></thead>
        <tbody>{items.map(item => <tr key={item.id} className={item.isDraft && item.createdAutomatically ? 'catalog-auto-draft-row' : undefined}>
          <td><span className={`status-badge ${item.isActive ? 'status-Completed' : 'catalog-inactive'}`}>{item.isActive ? 'Активен' : 'Неактивен'}</span>{item.isDraft && <span className="catalog-draft-badge">Черновик</span>}</td>
          <th scope="row"><strong>{item.name}</strong>{item.isDraft && item.createdAutomatically && <span className="workspace-draft-warning">Автоматически предложен — требует подтверждения</span>}{item.isSystem && <span className="workspace-help">Системный вид</span>}</th>
          <td><span className={`catalog-schema${item.baseType === 'Unknown' ? ' catalog-schema-storage' : ''}`}><Icon name="file-text" size={14} />{item.baseType === 'Unknown' ? 'Без схемы — хранение и поиск' : typeLabels[item.baseType] || item.baseType}</span></td>
          <td>{catalogs.departments.find(department => department.id === item.departmentId)?.name || (item.departmentId ? 'Недоступен' : 'Не задан')}</td>
          <td><div className="catalog-row-actions">
            {item.isDraft && <button className="button" type="button" disabled={busy || !!editor} onClick={() => editKind(item, true)} aria-label={`Подтвердить вид ${item.name}`}><Icon name="circle-check" size={14} />Проверить и подтвердить</button>}
            <button className="button button-secondary" type="button" disabled={busy || !!editor} onClick={() => editKind(item)} aria-label={`Редактировать вид ${item.name}`}><Icon name="pencil" size={14} />Редактировать</button>
            <button className="button button-secondary workspace-delete" type="button" disabled={busy || !!editor || item.isSystem} onClick={() => removeKind(item)} aria-label={`Удалить вид ${item.name}`}><Icon name="trash-2" size={14} />Удалить</button>
          </div></td>
        </tr>)}</tbody>
      </table>
    </div>
  }

  return <div className="workspace-page stitch-catalogs">
    <div className="catalog-context"><span><Icon name="settings" size={14} />Администрирование<Icon name="chevron-right" size={12} /><strong>Виды документов и отделы</strong></span>{!loading && !loadError && <span className="catalog-context-counts">Активных видов: <strong>{catalogs.kinds.filter(item => item.isActive).length}</strong><span className="catalog-context-drafts">Черновиков: <strong>{draftCount}</strong></span></span>}</div>
    <header className="catalog-page-header">
      <div><h1>Виды документов и маршрутизация</h1><p>Классификация документов, схемы распознавания и ответственные отделы. Изменения доступны только администратору.</p></div>
      <div className="workspace-tabs" role="tablist" aria-label="Справочники">
        <button ref={kindsTab} id="catalog-kinds-tab" role="tab" type="button" aria-selected={tab === 'kinds'} aria-controls="catalog-kinds-panel" tabIndex={tab === 'kinds' ? 0 : -1} onKeyDown={tabKey} onClick={() => switchTab('kinds')} disabled={busy}>Виды документов{!loading && !loadError && <span className="catalog-tab-count">{catalogs.kinds.length}</span>}</button>
        <button ref={departmentsTab} id="catalog-departments-tab" role="tab" type="button" aria-selected={tab === 'departments'} aria-controls="catalog-departments-panel" tabIndex={tab === 'departments' ? 0 : -1} onKeyDown={tabKey} onClick={() => switchTab('departments')} disabled={busy}>Отделы{!loading && !loadError && <span className="catalog-tab-count">{catalogs.departments.length}</span>}</button>
      </div>
    </header>
    {loadError && <div className="alert alert-error" role="alert"><p>{loadError}</p><button className="button button-secondary" type="button" onClick={() => setRevision(value => value + 1)}>Повторить загрузку справочников</button></div>}
    {actionError && <div className="alert alert-error" role="alert"><p>{actionError}</p><button className="button button-secondary" type="button" disabled={busy} onClick={() => setRevision(value => value + 1)}>Обновить справочники</button></div>}
    {notice && <div className="alert alert-success" role="status">{notice}</div>}
    {loading && <div className="state-panel" role="status">Загрузка справочников…</div>}
    {displayedEditor && <section className={`card workspace-editor${autoDraft ? ' workspace-auto-draft' : ''}`} aria-labelledby="catalog-editor-heading">
      <h2 id="catalog-editor-heading" tabIndex={-1} ref={editorHeading}>{displayedEditor.approve ? 'Подтвердить предложенный вид' : displayedEditor.id ? tab === 'kinds' ? 'Редактировать вид документа' : 'Редактировать отдел' : tab === 'kinds' ? 'Новый вид документа' : 'Новый отдел'}</h2>
      {autoDraft && <p className="workspace-draft-warning">Автоматически предложен — требует подтверждения</p>}
      {displayedEditor.approve && <p className="workspace-help">Проверьте название, выберите базовую схему и при необходимости отдел. Только подтверждение снимет статус черновика.</p>}
      <form onSubmit={event => void save(event)} className="workspace-form"><fieldset className="workspace-editor-fields" disabled={busy || loading}>
        <div className="workspace-editor-grid">
          <label>Название{tab === 'kinds' ? <input autoComplete="off" required maxLength={160} value={kindDraft.name} onChange={event => setKindDraft({ ...kindDraft, name: event.target.value })} /> : <input autoComplete="off" required maxLength={160} value={departmentDraft.name} onChange={event => setDepartmentDraft({ ...departmentDraft, name: event.target.value })} />}</label>
          {tab === 'kinds' && <><label>Базовая схема распознавания<select required disabled={!!editingItem?.isSystem} value={kindDraft.baseType} onChange={event => setKindDraft({ ...kindDraft, baseType: event.target.value as DocumentType })}><option value="Unknown">Без схемы — хранение и поиск</option>{(['Invoice', 'Contract', 'Act'] as const).map(value => <option key={value} value={value}>{typeLabels[value]}</option>)}</select></label><label>Отдел по умолчанию<select value={kindDraft.departmentId ?? ''} onChange={event => setKindDraft({ ...kindDraft, departmentId: event.target.value || null })}><option value="">Без отдела</option>{kindDraft.departmentId && !catalogs.departments.some(item => item.id === kindDraft.departmentId) && <option value={kindDraft.departmentId}>Текущий отдел недоступен</option>}{catalogs.departments.map(item => <option key={item.id} value={item.id} disabled={!item.isActive && item.id !== kindDraft.departmentId}>{item.name}{!item.isActive ? ' — неактивен' : ''}</option>)}</select></label></>}
          {!!displayedEditor.id && !displayedEditor.approve && !editingItem?.isDraft && <label>Доступность<select value={(tab === 'kinds' ? kindDraft.isActive : departmentDraft.isActive) ? 'active' : 'inactive'} onChange={event => tab === 'kinds' ? setKindDraft({ ...kindDraft, isActive: event.target.value === 'active' }) : setDepartmentDraft({ ...departmentDraft, isActive: event.target.value === 'active' })}><option value="active">Активен</option><option value="inactive">Неактивен</option></select></label>}
        </div>
        {tab === 'kinds' && <p className="workspace-help">Базовая схема определяет обязательные реквизиты для счёта, договора или акта. Для других документов оставьте «Без схемы»: поиск и хранение доступны, подтверждение по чужой схеме не требуется.{editingItem?.isDraft && !displayedEditor.approve ? ' Сохранение изменений не подтверждает черновик.' : ''}</p>}
        <div className="workspace-actions"><button className="button" type="submit">{busy ? 'Сохранение…' : displayedEditor.approve ? 'Подтвердить вид' : 'Сохранить'}</button><button className="button button-secondary" type="button" onClick={() => { setEditor(null); setActionError(null) }}>Отмена</button></div>
      </fieldset></form>
    </section>}
    <section id="catalog-kinds-panel" role="tabpanel" aria-labelledby="catalog-kinds-tab" hidden={tab !== 'kinds'} tabIndex={0} aria-busy={loading || busy}>
      {!loading && !loadError && <>
        {aiDrafts.length > 0 && <section className="catalog-ai-proposals" aria-labelledby="catalog-ai-heading">
          <div className="catalog-ai-heading"><Icon name="sparkles" size={20} /><div><h2 id="catalog-ai-heading">Предложенные AI виды документов <span className="catalog-draft-badge">Требуется подтверждение</span></h2><p>Автоматически предложенные виды остаются неактивными черновиками. Проверьте название, схему и отдел перед подтверждением.</p></div></div>
          {kindTable(aiDrafts, 'Автоматически предложенные черновики видов')}
        </section>}
        <div className="card catalog-directory">
          <div className="section-heading catalog-directory-toolbar"><div><h2>Виды документов</h2><p className="workspace-help">Схема извлечения и маршрутизация по умолчанию</p></div><button className="button" type="button" disabled={busy || !!editor} onClick={() => editKind()}><Icon name="plus" size={15} />Добавить вид</button></div>
          {catalogs.kinds.length === 0 ? <p className="workspace-empty" role="status">Видов документов пока нет. Добавьте первый вид.</p> : managedKinds.length > 0 ? kindTable(managedKinds, 'Каталог видов документов') : <p className="workspace-empty">Все виды пока находятся в блоке AI-предложений выше.</p>}
          <p className="workspace-help workspace-delete-help">Используемый вид при удалении деактивируется либо сервер отклоняет удаление. Связанные документы и история сохраняются. Системные виды не удаляются.</p>
        </div>
      </>}
    </section>
    <section id="catalog-departments-panel" role="tabpanel" aria-labelledby="catalog-departments-tab" hidden={tab !== 'departments'} tabIndex={0} aria-busy={loading || busy}>
      {!loading && !loadError && <div className="card catalog-directory">
        <div className="section-heading catalog-directory-toolbar"><div><h2>Ответственные отделы</h2><p className="workspace-help">Подразделения для назначения документов</p></div><button className="button" type="button" disabled={busy || !!editor} onClick={() => editDepartment()}><Icon name="plus" size={15} />Добавить отдел</button></div>
        {catalogs.departments.length === 0 ? <p className="workspace-empty" role="status">Отделов пока нет. Добавьте первый отдел.</p> : <div className="catalog-table-scroll" role="region" aria-label="Каталог отделов" tabIndex={0}><table className="catalog-table catalog-departments-table">
          <thead><tr><th scope="col">Статус</th><th scope="col">Подразделение</th><th scope="col">Категория</th><th scope="col" className="catalog-actions-heading">Действия</th></tr></thead>
          <tbody>{catalogs.departments.map(item => <tr key={item.id}>
            <td><span className={`status-badge ${item.isActive ? 'status-Completed' : 'catalog-inactive'}`}>{item.isActive ? 'Активен' : 'Неактивен'}</span></td>
            <th scope="row">{item.name}</th><td><span className="workspace-help">{item.isSystem ? 'Системный отдел' : 'Пользовательский отдел'}</span></td>
            <td><div className="catalog-row-actions"><button className="button button-secondary" type="button" disabled={busy || !!editor} onClick={() => editDepartment(item)} aria-label={`Редактировать отдел ${item.name}`}><Icon name="pencil" size={14} />Редактировать</button><button className="button button-secondary workspace-delete" type="button" disabled={busy || !!editor || item.isSystem} onClick={() => removeDepartment(item)} aria-label={`Удалить отдел ${item.name}`}><Icon name="trash-2" size={14} />Удалить</button></div></td>
          </tr>)}</tbody>
        </table></div>}
        <p className="workspace-help workspace-delete-help">Неактивные отделы сохраняются в истории, но недоступны для новых назначений. При удалении используемого отдела сервер деактивирует его либо отклоняет действие. Системные отделы не удаляются.</p>
      </div>}
    </section>
    <section className="card" aria-labelledby="search-index-heading" aria-busy={indexLoading || indexBusy}><div className="section-heading"><h2 id="search-index-heading">Смысловой индекс</h2><button className="button button-secondary" type="button" disabled={indexBusy || indexLoading} onClick={() => setIndexRevision(value => value + 1)}>Обновить статус</button></div>
      <p className="workspace-help">Поиск по смыслу использует локальную модель, без платных API. Недоступность модели не заменяет смысловой поиск текстовым.</p>
      {indexLoading && <p role="status">Проверка индекса…</p>}
      {indexError && <div className="alert alert-error" role="alert"><p>{indexError}</p><button className="button button-secondary" type="button" disabled={indexBusy} onClick={() => setIndexRevision(value => value + 1)}>Повторить проверку индекса</button></div>}
      {!indexLoading && !indexError && indexStatus && <dl className="workspace-index-status"><div><dt>Поиск по смыслу</dt><dd>{indexStatus.semanticAvailable === true ? 'Доступен' : indexStatus.semanticAvailable === false ? 'Недоступен' : 'Сервер не сообщил доступность'}</dd></div><div><dt>Проиндексировано</dt><dd>{indexStatus.indexedCount ?? 'Сервер не сообщил количество'}</dd></div>{indexStatus.model && <div><dt>Модель</dt><dd>{indexStatus.model}</dd></div>}</dl>}
      {!indexLoading && !indexError && indexStatus?.message && <p className="workspace-help">{indexStatus.message}</p>}
      {indexNotice && <p role="status" className="workspace-help">{indexNotice}</p>}
      <div className="workspace-actions"><button className="button button-secondary" type="button" disabled={indexBusy || indexLoading} onClick={() => void reindex()}>{indexBusy ? 'Запрос отправляется…' : 'Перестроить индекс'}</button><span className="workspace-help">Фоновая обработка идёт партиями. Прогресс проверяйте по статусу сервера.</span></div>
    </section>
  </div>
}
