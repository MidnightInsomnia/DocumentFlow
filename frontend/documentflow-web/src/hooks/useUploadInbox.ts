import { useSyncExternalStore } from 'react'
import { getSession, SESSION_CHANGED } from '../api/session'

interface UploadInboxSnapshot {
  readonly files: readonly File[]
  readonly sessionVersion: number
}

// File references only: never serialize bytes, put files in router state, or upload here.
// The module outlives Layout when navigation crosses the admin route boundary.
const initialSession = getSession()
let owner = initialSession ? { userId: initialSession.user.id, token: initialSession.accessToken } : null
let snapshot: UploadInboxSnapshot = { files: [], sessionVersion: 0 }
const listeners = new Set<() => void>()
let expiryTimer: number | undefined

function publish(next: UploadInboxSnapshot) {
  snapshot = next
  listeners.forEach(listener => listener())
}

function syncSession() {
  const session = getSession()
  const nextOwner = session ? { userId: session.user.id, token: session.accessToken } : null
  if (owner?.userId !== nextOwner?.userId || owner?.token !== nextOwner?.token) {
    owner = nextOwner
    publish({ files: [], sessionVersion: snapshot.sessionVersion + 1 })
  }
  window.clearTimeout(expiryTimer)
  if (session && listeners.size) {
    expiryTimer = window.setTimeout(syncSession, Math.min(2_147_483_647, Math.max(0, Date.parse(session.expiresAtUtc) - Date.now())))
  }
}

function clearOnPageHide() {
  // Also remount the local upload queue if this page is restored from bfcache.
  publish({ files: [], sessionVersion: snapshot.sessionVersion + 1 })
}

function subscribe(listener: () => void) {
  listeners.add(listener)
  if (listeners.size === 1) {
    window.addEventListener(SESSION_CHANGED, syncSession)
    window.addEventListener('pagehide', clearOnPageHide)
  }
  syncSession()
  return () => {
    listeners.delete(listener)
    if (!listeners.size) {
      window.removeEventListener(SESSION_CHANGED, syncSession)
      window.removeEventListener('pagehide', clearOnPageHide)
      window.clearTimeout(expiryTimer)
      syncSession()
    }
  }
}

export function useUploadInbox() {
  return useSyncExternalStore(subscribe, () => snapshot)
}

export function enqueueUploadFiles(files: readonly File[]): boolean {
  syncSession()
  if (!owner || !files.length) return false
  publish({ ...snapshot, files: [...snapshot.files, ...files] })
  return true
}

/** Claim once, in an effect/event (not a render or a React state updater). */
export function takeUploadFiles(sessionVersion: number): readonly File[] {
  if (!isUploadSessionCurrent(sessionVersion) || !snapshot.files.length) return []
  const files = snapshot.files
  publish({ ...snapshot, files: [] })
  return files
}

export function clearUploadInbox() {
  if (snapshot.files.length) publish({ ...snapshot, files: [] })
}

export function isUploadSessionCurrent(sessionVersion: number): boolean {
  syncSession()
  return owner !== null && snapshot.sessionVersion === sessionVersion
}
