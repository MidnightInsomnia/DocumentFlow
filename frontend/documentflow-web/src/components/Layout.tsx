import { useEffect, useRef, useState, type FormEvent } from 'react'
import { Link, Outlet, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from './AuthProvider'
import { Icon } from './Icon'
import { GlobalUploadDrop } from './GlobalUploadDrop'
import { useUploadInbox } from '../hooks/useUploadInbox'

export function Layout() {
  const { user, logout } = useAuth()
  const location = useLocation()
  const navigate = useNavigate()
  const { sessionVersion } = useUploadInbox()
  const [query, setQuery] = useState('')
  const searchInput = useRef<HTMLInputElement>(null)
  const review = location.pathname.startsWith('/documents/') || location.pathname === '/documents' && new URLSearchParams(location.search).get('status') === 'NeedsReview'
  const admin = location.pathname.startsWith('/admin/') || location.pathname === '/users'
  const routeLabel = location.pathname.startsWith('/documents/') ? 'Проверка документа' : location.pathname === '/search' ? 'Поиск документов' : location.pathname === '/upload' ? 'Загрузить документы' : location.pathname === '/dashboard' ? 'Обзор обработки' : location.pathname === '/users' ? 'Пользователи' : location.pathname === '/change-password' ? 'Смена пароля' : location.pathname.startsWith('/admin/') ? 'Виды документов и отделы' : location.pathname.startsWith('/counterparties/') ? 'Документы контрагента' : 'Реестр документов'
  useEffect(() => {
    const handleKey = (event: KeyboardEvent) => {
      if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === 'k') { event.preventDefault(); searchInput.current?.focus() }
    }
    window.addEventListener('keydown', handleKey)
    return () => window.removeEventListener('keydown', handleKey)
  }, [])
  function search(event: FormEvent) {
    event.preventDefault()
    const params = new URLSearchParams({ mode: 'text' })
    if (query.trim()) params.set('q', query.trim())
    navigate(`/search?${params}`)
  }
  return <div className="sovereign-app">
    <a className="skip-to-content" href="#workspace-content" onClick={event => { event.preventDefault(); document.getElementById('workspace-content')?.focus() }}>К содержимому</a>
    <header className="app-header"><div className="header-content">
      <Link className="brand" to="/documents" aria-label="DocumentFlow — реестр документов"><Icon name="file-text" size={20} /><span>DocumentFlow</span></Link>
      <nav className="primary-nav" aria-label="Основная навигация">
        <Link className={!review && location.pathname === '/documents' ? 'active' : undefined} aria-current={!review && location.pathname === '/documents' ? 'page' : undefined} to="/documents">Реестр документов</Link>
        <Link className={review ? 'active' : undefined} aria-current={review ? 'page' : undefined} to="/documents?status=NeedsReview">Верификация</Link>
        <Link className={location.pathname === '/search' ? 'active' : undefined} aria-current={location.pathname === '/search' ? 'page' : undefined} to="/search">Поиск</Link>
        {user?.role === 'Admin' && <Link className={admin ? 'active' : undefined} aria-current={admin ? 'page' : undefined} to="/admin/catalogs">Администрирование</Link>}
      </nav>
      <form className="header-search" role="search" onSubmit={search}><Icon name="search" size={14} /><input ref={searchInput} aria-label="Поиск по документам" placeholder="Поиск по документам…" value={query} maxLength={200} onChange={event => setQuery(event.target.value)} /><kbd>Ctrl K</kbd></form>
      <Link className="button header-upload upload-entry" to="/upload"><Icon name="file-up" /><span>Загрузить документы</span></Link>
      <details className="account-menu"><summary aria-label="Меню учётной записи"><span className="account-initials" aria-hidden="true">{(user?.displayName || user?.userName || 'П').slice(0, 1).toUpperCase()}</span><span className="account-identity"><strong>{user?.displayName || user?.userName}</strong><small>{user?.role === 'Admin' ? 'Администратор' : 'Оператор'}</small></span><Icon name="chevron-right" size={13} /></summary>
        <div className="account-popover" onClick={event => { const details = event.currentTarget.closest('details'); if (details) details.open = false }}><Link to="/dashboard"><Icon name="layout-dashboard" />Обзор обработки</Link>{user?.role === 'Admin' && <Link to="/users"><Icon name="users" />Пользователи</Link>}<Link to="/change-password"><Icon name="key-round" />Сменить пароль</Link><button type="button" onClick={logout}><Icon name="log-out" />Выйти</button></div>
      </details>
    </div></header>
    <div className="workspace-context"><div className="context-trail"><Link to="/documents"><Icon name="folder-cog" size={13} />Рабочая область</Link><Icon name="chevron-right" size={12} /><span>{routeLabel}</span></div><div className="context-links"><Link to="/dashboard"><Icon name="layout-dashboard" size={13} />Обзор обработки</Link>{user?.role === 'Admin' && <Link to="/users"><Icon name="users" size={13} />Пользователи</Link>}</div></div>
    <main className="page-shell" id="workspace-content" tabIndex={-1}><Outlet key={sessionVersion} /></main>
    <footer className="workspace-footer"><span>DocumentFlow</span><span>Проверка и хранение документов</span><Link to="/dashboard">Состояние обработки<Icon name="chevron-right" size={12} /></Link></footer>
    <GlobalUploadDrop />
  </div>
}
