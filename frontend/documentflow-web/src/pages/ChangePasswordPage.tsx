import { useState, type FormEvent } from 'react'
import { changePassword } from '../api/authApi'
import { getErrorMessage } from '../utils/formatters'
import { useAuth } from '../components/AuthProvider'
import { useNavigate } from 'react-router-dom'

export function ChangePasswordPage() {
  const { logout } = useAuth()
  const navigate = useNavigate()
  const [currentPassword, setCurrentPassword] = useState('')
  const [newPassword, setNewPassword] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [success, setSuccess] = useState(false)
  async function submit(event: FormEvent) {
    event.preventDefault(); setBusy(true); setError(null); setSuccess(false)
    try {
      await changePassword(currentPassword, newPassword)
      setCurrentPassword(''); setNewPassword(''); setSuccess(true)
      logout()
      navigate('/login', { replace: true, state: { notice: 'Пароль изменён. Войдите с новым паролем.' } })
    } catch (cause) { setError(getErrorMessage(cause)) }
    finally { setBusy(false) }
  }
  return <div className="narrow-page"><h1>Сменить пароль</h1><section className="card">
    {error && <div className="alert alert-error" role="alert">{error}</div>}
    {success && <div className="alert alert-success" role="status">Пароль изменён.</div>}
    <form className="auth-form" onSubmit={event => void submit(event)}>
      <label>Текущий пароль<input type="password" autoComplete="current-password" required value={currentPassword} onChange={event => setCurrentPassword(event.target.value)} /></label>
      <label>Новый пароль<input type="password" autoComplete="new-password" minLength={12} required value={newPassword} onChange={event => setNewPassword(event.target.value)} /></label>
      <p className="muted">Не менее 12 символов: строчные и прописные буквы, цифра и специальный символ. После смены пароля потребуется войти снова.</p>
      <button className="button" disabled={busy} type="submit">Сохранить пароль</button>
    </form>
  </section></div>
}
