import { apiFetch, apiRequest, jsonRequest } from './apiClient'
import { saveSession, type AuthUser, type Role, type Session } from './session'

export interface ManagedUser extends AuthUser { email: string | null; isActive: boolean; createdAtUtc: string }
export interface NewUser { userName: string; displayName: string; email?: string; password: string; role: Role }
export async function login(userName: string, password: string): Promise<Session> {
  const session = await jsonRequest<Session>('/api/auth/login', 'POST', { userName, password })
  saveSession(session)
  return session
}
export const getMe = () => apiRequest<AuthUser>('/api/auth/me')
export async function changePassword(currentPassword: string, newPassword: string): Promise<void> {
  await apiFetch('/api/auth/change-password', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ currentPassword, newPassword }) })
}
export interface UsersPageResult { items: ManagedUser[]; totalCount: number; page: number; pageSize: number }
export const getUsers = (page = 1, pageSize = 20, search = '') => {
  const query = new URLSearchParams({ page: String(page), pageSize: String(pageSize) })
  if (search) query.set('search', search)
  return apiRequest<UsersPageResult>(`/api/admin/users?${query}`)
}
export async function createUser(user: NewUser): Promise<void> {
  await apiFetch('/api/admin/users', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(user) })
}
export async function setUserRole(id: string, role: Role): Promise<void> {
  await apiFetch(`/api/admin/users/${encodeURIComponent(id)}/role`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ role }) })
}
export async function setUserActive(id: string, isActive: boolean): Promise<void> {
  await apiFetch(`/api/admin/users/${encodeURIComponent(id)}/active`, { method: 'PUT', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ isActive }) })
}
export async function resetUserPassword(id: string, newPassword: string): Promise<void> {
  await apiFetch(`/api/admin/users/${encodeURIComponent(id)}/reset-password`, { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ newPassword }) })
}
