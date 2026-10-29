import { createContext, useContext, useEffect, useState, type ReactNode } from 'react'
import { getMe } from '../api/authApi'
import { clearSession, getSession, saveSession, SESSION_CHANGED, type AuthUser } from '../api/session'

interface AuthState { user: AuthUser | null; loading: boolean; logout: () => void }
const AuthContext = createContext<AuthState>({ user: null, loading: true, logout: () => {} })
export const useAuth = () => useContext(AuthContext)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [user, setUser] = useState<AuthUser | null>(null)
  const [loading, setLoading] = useState(() => !!getSession())
  useEffect(() => {
    let active = true
    const sync = () => { if (active) { setUser(getSession()?.user ?? null); setLoading(false) } }
    window.addEventListener(SESSION_CHANGED, sync)
    const session = getSession()
    if (session) {
      getMe().then(me => {
        if (active && getSession()?.accessToken === session.accessToken) {
          saveSession({ ...session, user: me }); setUser(me)
        }
      }).catch(() => {
        // Network failure does not prove the token invalid; 401 is handled centrally.
      }).finally(() => { if (active) setLoading(false) })
    } else setLoading(false)
    return () => { active = false; window.removeEventListener(SESSION_CHANGED, sync) }
  }, [])
  return <AuthContext.Provider value={{ user, loading, logout: clearSession }}>{children}</AuthContext.Provider>
}
