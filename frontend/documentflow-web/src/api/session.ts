export type Role = 'Admin' | 'Operator'
export interface AuthUser { id: string; userName: string; displayName: string; role: Role }
export interface Session { accessToken: string; expiresAtUtc: string; user: AuthUser }
const key = 'documentflow.session'
export const SESSION_CHANGED = 'documentflow-session-changed'

export function getSession(): Session | null {
  try {
    const raw = sessionStorage.getItem(key)
    if (!raw) return null
    const value = JSON.parse(raw) as Session
    if (!value.accessToken || !value.user?.id || !['Admin', 'Operator'].includes(value.user.role) || !Number.isFinite(Date.parse(value.expiresAtUtc)) || Date.parse(value.expiresAtUtc) <= Date.now()) {
      clearSession()
      return null
    }
    return value
  } catch {
    clearSession()
    return null
  }
}
export function saveSession(session: Session) {
  sessionStorage.setItem(key, JSON.stringify(session))
  window.dispatchEvent(new Event(SESSION_CHANGED))
}
export function clearSession() {
  sessionStorage.removeItem(key)
  window.dispatchEvent(new Event(SESSION_CHANGED))
}
