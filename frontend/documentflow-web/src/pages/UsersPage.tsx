import { useEffect, useState, type FormEvent } from 'react'
import { createUser, getUsers, resetUserPassword, setUserActive, setUserRole, type NewUser, type UsersPageResult } from '../api/authApi'
import type { Role } from '../api/session'
import { getErrorMessage, formatDate } from '../utils/formatters'

const emptyDraft: NewUser = { userName: '', displayName: '', email: '', password: '', role: 'Operator' }

export function UsersPage() {
  const [result, setResult] = useState<UsersPageResult>({ items: [], totalCount: 0, page: 1, pageSize: 20 })
  const [page, setPage] = useState(1)
  const [searchInput, setSearchInput] = useState('')
  const [search, setSearch] = useState('')
  const [revision, setRevision] = useState(0)
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [listError, setListError] = useState<string | null>(null)
  const [actionError, setActionError] = useState<string | null>(null)
  const [notice, setNotice] = useState<string | null>(null)
  const [draft, setDraft] = useState<NewUser>(emptyDraft)
  const [resetId, setResetId] = useState<string | null>(null)
  const [newPassword, setNewPassword] = useState('')

  useEffect(() => {
    if (searchInput.trim() === search) return
    const timer = window.setTimeout(() => { setPage(1); setSearch(searchInput.trim()) }, 300)
    return () => window.clearTimeout(timer)
  }, [searchInput, search])
  useEffect(() => {
    let active = true
    setLoading(true)
    getUsers(page, 20, search).then(data => { if (active) {
      const last = Math.max(1, Math.ceil(data.totalCount / data.pageSize))
      if (page > last) { setPage(last); return }
      setResult(data); setListError(null)
    } })
      .catch(cause => { if (active) setListError(getErrorMessage(cause)) })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [page, search, revision])

  async function run(action: () => Promise<unknown>, message: string) {
    setBusy(true); setActionError(null); setNotice(null)
    try {
      await action()
      setRevision(value => value + 1)
      setNotice(message)
      return true
    } catch (cause) { setActionError(getErrorMessage(cause)); return false }
    finally { setBusy(false) }
  }
  async function add(event: FormEvent) {
    event.preventDefault()
    if (await run(() => createUser({ ...draft, email: draft.email || undefined }), 'Пользователь создан.')) setDraft(emptyDraft)
  }
  async function reset(event: FormEvent) {
    event.preventDefault()
    if (!resetId) return
    if (await run(() => resetUserPassword(resetId, newPassword), 'Пароль сброшен.')) { setResetId(null); setNewPassword('') }
  }
  const { items: users, totalCount, pageSize } = result
  const lastPage = Math.max(1, Math.ceil(totalCount / pageSize))

  return <div className="users-page"><div className="page-heading"><h1>Пользователи</h1></div>
    {listError && <div className="alert alert-error" role="alert">{listError}</div>}
    {actionError && <div className="alert alert-error" role="alert">{actionError}</div>}
    {notice && <div className="alert alert-success" role="status">{notice}</div>}
    <section className="card"><h2>Создать пользователя</h2>
      <form onSubmit={event => void add(event)} className="auth-form">
        <div className="form-grid">
          <label>Имя пользователя<input required autoComplete="off" value={draft.userName} onChange={event => setDraft({ ...draft, userName: event.target.value })} /></label>
          <label>Отображаемое имя<input required value={draft.displayName} onChange={event => setDraft({ ...draft, displayName: event.target.value })} /></label>
          <label>Email (необязательно)<input type="email" value={draft.email ?? ''} onChange={event => setDraft({ ...draft, email: event.target.value })} /></label>
          <label>Пароль пользователя<input type="password" required autoComplete="new-password" value={draft.password} onChange={event => setDraft({ ...draft, password: event.target.value })} /></label>
          <label>Роль нового пользователя<select value={draft.role} onChange={event => setDraft({ ...draft, role: event.target.value as Role })}><option value="Operator">Оператор</option><option value="Admin">Администратор</option></select></label>
        </div>
        <button type="submit" className="button" disabled={busy}>Создать</button>
      </form>
    </section>
    <section className="card"><h2>Учетные записи</h2>
      <label>Поиск по имени пользователя или отображаемому имени<input type="search" value={searchInput} onChange={event => setSearchInput(event.target.value)} /></label>
      {loading ? <p role="status">Загрузка...</p> : !listError && <div className="table-scroll" role="region" aria-label="Учётные записи" tabIndex={0}><table className="users-table"><thead><tr><th>Пользователь</th><th>Email</th><th>Создан</th><th>Роль</th><th>Статус</th><th>Действия</th></tr></thead><tbody>
        {users.map(item => <tr key={item.id}><td>{item.displayName}<small className="muted" style={{ display: 'block' }}>{item.userName}</small></td><td>{item.email || '—'}</td><td>{formatDate(item.createdAtUtc)}</td>
          <td className="user-role-cell"><select aria-label={`Роль пользователя ${item.userName}`} disabled={busy} value={item.role} onChange={event => { const role = event.target.value as Role; if (item.role === 'Admin' && role === 'Operator' && !window.confirm(`Понизить ${item.userName} до оператора? Доступ администратора прекратится, в том числе для вас самих.`)) return; void run(() => setUserRole(item.id, role), 'Роль изменена.') }}><option value="Operator">Оператор</option><option value="Admin">Администратор</option></select></td>
          <td><span className={`status-badge ${item.isActive ? 'status-Completed' : 'status-Failed'}`}>{item.isActive ? 'Активен' : 'Отключён'}</span></td>
          <td><button className="button button-secondary" disabled={busy} type="button" onClick={() => { if (item.isActive && !window.confirm(`Отключить ${item.userName}? Его текущий доступ сразу прекратится, в том числе если это ваша учётная запись.`)) return; void run(() => setUserActive(item.id, !item.isActive), 'Статус изменён.') }}>{item.isActive ? 'Отключить' : 'Активировать'}</button> <button className="button button-secondary" disabled={busy} type="button" onClick={() => { setResetId(item.id); setNewPassword('') }}>Сбросить пароль</button></td>
        </tr>)}
      </tbody></table></div>}
      {!loading && users.length === 0 && !listError && <p className="muted">{search ? 'Ничего не найдено.' : 'Пользователей пока нет.'}</p>}
      {!loading && !listError && <div className="action-row"><span>{totalCount ? `${(page - 1) * pageSize + 1}–${Math.min(page * pageSize, totalCount)} из ${totalCount}` : '0 из 0'} · Страница {page} из {lastPage}</span><button type="button" className="button button-secondary" disabled={busy || page <= 1} onClick={() => setPage(value => value - 1)}>Назад</button><button type="button" className="button button-secondary" disabled={busy || page >= lastPage} onClick={() => setPage(value => value + 1)}>Вперёд</button></div>}
    </section>
    {resetId && <section className="card narrow-page"><h2>Сбросить пароль: {users.find(item => item.id === resetId)?.userName}</h2><form className="auth-form" onSubmit={event => void reset(event)}>
      <label>Новый пароль пользователя<input type="password" autoComplete="new-password" required value={newPassword} onChange={event => setNewPassword(event.target.value)} /></label>
      <div className="action-row"><button className="button" type="submit" disabled={busy}>Сохранить</button><button className="button button-secondary" type="button" onClick={() => { setResetId(null); setNewPassword('') }}>Отмена</button></div>
    </form></section>}
  </div>
}
