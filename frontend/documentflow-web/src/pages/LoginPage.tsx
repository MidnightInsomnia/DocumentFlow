import { useState, type FormEvent } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { login } from '../api/authApi'
import { useAuth } from '../components/AuthProvider'
import { Icon } from '../components/Icon'
import { getErrorMessage } from '../utils/formatters'

export function LoginPage() {
  const { user } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [userName, setUserName] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [busy, setBusy] = useState(false)
  if (user) return <Navigate to="/documents" replace />
  async function submit(event: FormEvent) {
    event.preventDefault()
    setBusy(true); setError(null)
    try {
      await login(userName, password)
      const from = (location.state as { from?: string } | null)?.from
      navigate(from?.startsWith('/') && !from.startsWith('//') ? from : '/documents', { replace: true })
    } catch (cause) {
      setError(getErrorMessage(cause))
    } finally { setBusy(false) }
  }
  return <main className="page-shell narrow-page auth-page">
    <section className="card"><div className="auth-brand"><Icon name="file-text" size={22} />DocumentFlow</div><h1>Вход в рабочую область</h1><p className="muted">Введите данные своей учётной записи.</p>
      {(location.state as { notice?: string } | null)?.notice && <div className="alert alert-success" role="status">{(location.state as { notice: string }).notice}</div>}
      {error && <div className="alert alert-error" role="alert">{error}</div>}
      <form className="auth-form" onSubmit={event => void submit(event)}>
        <label>Имя пользователя<input autoComplete="username" required value={userName} onChange={event => setUserName(event.target.value)} /></label>
        <label>Пароль<input type="password" autoComplete="current-password" required value={password} onChange={event => setPassword(event.target.value)} /></label>
        <button className="button" disabled={busy} type="submit">{busy ? 'Вход...' : 'Войти'}</button>
      </form>
    </section>
  </main>
}
