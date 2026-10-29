import { useCallback, useEffect, useRef } from 'react'
import { useBeforeUnload, useBlocker, type BlockerFunction } from 'react-router-dom'

/** A predicate releases the guard immediately after a successful mutation, before rerendering. */
export function useNavigationGuard(dirty: boolean | (() => boolean), message = 'Есть несохранённые изменения. Покинуть страницу без сохранения?') {
  const latest = useRef(dirty)
  latest.current = dirty
  const isDirty = useCallback(() => typeof latest.current === 'function' ? latest.current() : latest.current, [])
  const blocker = useBlocker(useCallback<BlockerFunction>(({ currentLocation, nextLocation }) =>
    (currentLocation.pathname !== nextLocation.pathname || currentLocation.search !== nextLocation.search) && isDirty(), [isDirty]))
  useBeforeUnload(useCallback((event: BeforeUnloadEvent) => {
    if (isDirty()) { event.preventDefault(); event.returnValue = '' }
  }, [isDirty]))
  useEffect(() => {
    if (blocker.state !== 'blocked') return
    if (!isDirty() || window.confirm(message)) blocker.proceed()
    else blocker.reset()
  }, [blocker, isDirty, message])
}
