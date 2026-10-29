import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link } from 'react-router-dom'
import { apiRequest, jsonRequest } from '../api/apiClient'
import { useAuth } from './AuthProvider'
import { Icon } from './Icon'
import { KindDiscoveryStatus } from './KindDiscoveryStatus'
import type { DocumentDetails, DocumentType } from '../types/documents'
import type { Catalogs, DocumentKind } from '../types/workspace'
import { getErrorMessage } from '../utils/formatters'

interface Assignment { documentId: string; kindId: string | null; departmentId: string | null; kindName?: string; departmentName?: string; kindIsDraft?: boolean; kindCreatedAutomatically?: boolean; resolution?: string; resolutionReason?: string }

export function DocumentAssignment({ document }: { document: DocumentDetails }) {
  const { user } = useAuth()
  const [catalogs, setCatalogs] = useState<Catalogs | null>(null)
  const [assignment, setAssignment] = useState<Assignment | null>(null)
  const [kindId, setKindId] = useState('')
  const [departmentId, setDepartmentId] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  const [revision, setRevision] = useState(0)
  const [creating, setCreating] = useState(false)
  const [newName, setNewName] = useState('')
  const [newBaseType, setNewBaseType] = useState<DocumentType>('Unknown')
  const [createError, setCreateError] = useState<string | null>(null)
  const [createdKind, setCreatedKind] = useState<DocumentKind | null>(null)
  const nameInput = useRef<HTMLInputElement>(null)
  const createTrigger = useRef<HTMLButtonElement>(null)
  const edited = useRef(false)
  const selectionDocument = useRef(document.id)
  const pendingKind = assignment?.kindIsDraft
  const readOnly = document.isArchived || document.status === 'Processing' || document.status === 'PendingProcessing'

  useEffect(() => {
    if (selectionDocument.current !== document.id) {
      selectionDocument.current = document.id; edited.current = false
      setCatalogs(null); setAssignment(null); setNotice(null); setCreating(false); setCreatedKind(null)
    }
    const controller = new AbortController()
    Promise.all([
      apiRequest<Catalogs>('/api/catalogs', { signal: controller.signal }),
      apiRequest<Assignment>(`/api/workspace/documents/${document.id}`, { signal: controller.signal }),
    ]).then(([list, current]) => {
      if (controller.signal.aborted) return
      setCatalogs(list); setAssignment(current); setError(null)
      if (!edited.current) { setKindId(current.kindId ?? ''); setDepartmentId(current.departmentId ?? '') }
    }).catch(cause => { if (!controller.signal.aborted) setError(getErrorMessage(cause)) })
    return () => controller.abort()
  }, [document.id, document.version, revision])
  useEffect(() => {
    if (document.type !== 'Unknown' || assignment?.kindId || document.isArchived) return
    const timer = window.setInterval(() => setRevision(value => value + 1), 15000)
    return () => window.clearInterval(timer)
  }, [document.type, document.isArchived, assignment?.kindId])
  useEffect(() => { if (creating) nameInput.current?.focus() }, [creating])

  async function save(event: FormEvent) {
    event.preventDefault(); setBusy(true); setError(null); setNotice(null)
    try {
      await jsonRequest<Assignment>(`/api/workspace/documents/${document.id}`, 'PUT', { kindId: kindId || null, departmentId: departmentId || null })
      const saved = await apiRequest<Assignment>(`/api/workspace/documents/${document.id}`)
      if (selectionDocument.current !== document.id) return
      if (saved.kindId !== (kindId || null) || saved.departmentId !== (departmentId || null)) throw new Error('Назначение изменилось. Обновите данные перед повторным сохранением.')
      edited.current = false; setAssignment(saved); setKindId(saved.kindId ?? ''); setDepartmentId(saved.departmentId ?? '')
      setNotice('Вид и ответственный отдел сохранены.'); setRevision(value => value + 1)
    } catch (cause) { setError(getErrorMessage(cause)) }
    finally { setBusy(false) }
  }

  async function create(event: FormEvent) {
    event.preventDefault()
    if (busy || readOnly || user?.role !== 'Admin') return
    const name = newName.normalize('NFKC').trim().replace(/\s+/gu, ' ')
    if (!name) { setCreateError('Введите название нового вида.'); nameInput.current?.focus(); return }
    setBusy(true); setCreateError(null); setNotice(null)
    try {
      // Retain a successful POST result if its read-back fails: retry must not create another kind.
      const created = createdKind ?? await jsonRequest<DocumentKind>('/api/admin/catalogs/kinds', 'POST', { name, baseType: newBaseType, departmentId: departmentId || null, isActive: true })
      setCreatedKind(created)
      const list = await apiRequest<Catalogs>('/api/catalogs')
      const confirmed = list.kinds.find(kind => kind.id === created.id && kind.isActive && !kind.isDraft)
      if (!confirmed) throw new Error('Не удалось подтвердить создание вида. Обновите справочник перед повторным действием.')
      setCatalogs(list); edited.current = true; setKindId(confirmed.id)
      setCreating(false); setCreatedKind(null); setNewName(''); setNewBaseType('Unknown')
      setNotice(`Вид «${confirmed.name}» создан и выбран. Сохраните назначение, чтобы связать его с документом.`)
      createTrigger.current?.focus()
    } catch (cause) { setCreateError(getErrorMessage(cause)) }
    finally { setBusy(false) }
  }

  return <section className="card document-assignment" aria-label="Вид и ответственный отдел">
    <h2>Вид и ответственный отдел</h2>
    {document.type === 'Unknown' && !document.isArchived && <KindDiscoveryStatus document={document} onAssignmentChanged={() => setRevision(value => value + 1)} />}
    {pendingKind && <div className="automatic-kind-notice" role="status"><strong>Предложен новый вид «{assignment?.kindName}»</strong><p>Это неактивный AI-черновик. Администратор должен проверить и подтвердить его; реквизиты документа при этом не подтверждаются.</p>{user?.role === 'Admin' && <Link to="/admin/catalogs">Проверить предложение в справочнике</Link>}</div>}
    {assignment?.resolution && <p className="alert alert-info">Решение: {assignment.resolution}. {assignment.resolutionReason}</p>}
    {error && <p className="alert alert-error" role="alert">{error} <button type="button" className="text-button" onClick={() => setRevision(value => value + 1)}>Повторить</button></p>}
    {notice && <p role="status" className="alert alert-success">{notice}</p>}
    {!catalogs && !error && <p role="status" className="muted">Загрузка видов и отделов…</p>}
    {catalogs && <>
      <form className="assignment-form" onSubmit={event => void save(event)}>
        <div className="assignment-kind-field">
          <label>Вид документа<select disabled={busy || readOnly || creating} value={kindId} onChange={event => { edited.current = true; const next = catalogs.kinds.find(kind => kind.id === event.target.value); setKindId(event.target.value); if (next?.departmentId && catalogs.departments.some(department => department.id === next.departmentId && department.isActive)) setDepartmentId(next.departmentId) }}><option value="">Не назначен</option>{pendingKind && assignment?.kindId && <option value={assignment.kindId}>{assignment.kindName} · черновик</option>}{!pendingKind && assignment?.kindId && !catalogs.kinds.some(kind => kind.id === assignment.kindId && kind.isActive && !kind.isDraft) && <option value={assignment.kindId}>{assignment.kindName || 'Текущий вид'} · неактивен</option>}{catalogs.kinds.filter(kind => kind.isActive && !kind.isDraft).map(kind => <option key={kind.id} value={kind.id}>{kind.name}</option>)}</select></label>
          {user?.role === 'Admin' && !readOnly && <button ref={createTrigger} type="button" className="text-button assignment-create-trigger" disabled={busy} aria-expanded={creating} aria-controls="inline-kind-editor" onClick={() => { setCreating(value => !value); setCreateError(null) }}><Icon name="plus" size={13} />{creating ? 'Закрыть создание вида' : 'Создать новый вид'}</button>}
        </div>
        <label>Ответственный отдел<select disabled={busy || readOnly || creating} value={departmentId} onChange={event => { edited.current = true; setDepartmentId(event.target.value) }}><option value="">Не назначен</option>{assignment?.departmentId && !catalogs.departments.some(department => department.id === assignment.departmentId && department.isActive) && <option value={assignment.departmentId}>{assignment.departmentName || 'Текущий отдел'} · неактивен</option>}{catalogs.departments.filter(department => department.isActive).map(department => <option key={department.id} value={department.id}>{department.name}</option>)}</select></label>
        {!readOnly && !creating && <button type="submit" className="button button-secondary" disabled={busy || (kindId === (assignment?.kindId ?? '') && departmentId === (assignment?.departmentId ?? ''))}>{busy ? 'Сохранение…' : 'Сохранить назначение'}</button>}
      </form>
      {creating && !readOnly && user?.role === 'Admin' && <form id="inline-kind-editor" className="assignment-kind-editor" onSubmit={event => void create(event)} aria-label="Создание вида документа">
        <h3>Новый вид документа</h3>
        <label>Название вида<input ref={nameInput} required maxLength={160} autoComplete="off" disabled={busy || !!createdKind} value={newName} aria-invalid={!!createError} aria-describedby={createError ? 'inline-kind-error' : undefined} onChange={event => { setNewName(event.target.value); setCreateError(null) }} /></label>
        {createError && <p id="inline-kind-error" className="assignment-field-error" role="alert">{createError}</p>}
        <label>Схема извлечения<select disabled={busy || !!createdKind} value={newBaseType} onChange={event => setNewBaseType(event.target.value as DocumentType)}><option value="Unknown">Без схемы — хранение и поиск</option><option value="Invoice">Счёт</option><option value="Contract">Договор</option><option value="Act">Акт</option></select></label>
        <p className="muted">Новый вид доступен сразу. Он не обучает распознаватель: для накладной, письма и других новых документов оставьте «Без схемы».</p>
        <div className="action-row"><button className="button" type="submit" disabled={busy}>{busy ? 'Создание…' : createdKind ? 'Проверить созданный вид' : 'Создать и выбрать'}</button><button className="button button-secondary" type="button" disabled={busy} onClick={() => { setCreating(false); createTrigger.current?.focus() }}>Отмена</button></div>
      </form>}
      {user?.role !== 'Admin' && !readOnly && <p className="muted">Не нашли подходящий вид? Новый вид добавляет администратор.</p>}
    </>}
    <p className="muted">Назначение отдела группирует документы. Оно не означает отправку во внешнюю систему или согласование.</p>
  </section>
}
