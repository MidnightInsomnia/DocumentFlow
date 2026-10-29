import { useEffect, useRef, useState } from 'react'

function hasFiles(event: DragEvent): boolean {
  return !!event.dataTransfer && (
    Array.from(event.dataTransfer.types).includes('Files') ||
    Array.from(event.dataTransfer.items).some(item => item.kind === 'file')
  )
}

/** One capture-phase owner for all file drags, including portals and cinema. */
export function useGlobalFileDrop(onFiles: (files: File[]) => void, resetKey: string) {
  const [dragging, setDragging] = useState(false)
  const receive = useRef(onFiles)
  receive.current = onFiles

  useEffect(() => {
    let depth = 0
    let active = false
    let cancelled = false
    let leaveFrame: number | undefined

    function show() {
      if (leaveFrame !== undefined) window.cancelAnimationFrame(leaveFrame)
      leaveFrame = undefined
      if (cancelled || active) return
      active = true
      // Disable iframe hit-testing synchronously, before React paints the overlay.
      document.documentElement.classList.add('upload-file-drag-active')
      setDragging(true)
    }
    function hide() {
      if (leaveFrame !== undefined) window.cancelAnimationFrame(leaveFrame)
      leaveFrame = undefined
      depth = 0
      active = false
      document.documentElement.classList.remove('upload-file-drag-active')
      setDragging(false)
    }
    function reset() { cancelled = false; hide() }
    function intercept(event: DragEvent) {
      event.preventDefault()
      event.stopPropagation()
    }
    function enter(event: DragEvent) {
      if (!hasFiles(event)) return
      intercept(event)
      depth++
      show()
    }
    function over(event: DragEvent) {
      if (!hasFiles(event)) return
      intercept(event)
      if (event.dataTransfer) event.dataTransfer.dropEffect = cancelled ? 'none' : 'copy'
      show()
    }
    function leave(event: DragEvent) {
      if (!active && !cancelled) return
      if (hasFiles(event)) intercept(event)
      depth = Math.max(0, depth - 1)
      const outside = event.relatedTarget instanceof Node && !document.documentElement.contains(event.relatedTarget)
      if (outside) { reset(); return }
      if (depth === 0) {
        if (leaveFrame !== undefined) window.cancelAnimationFrame(leaveFrame)
        // A child-to-child transition can enter again before the next paint.
        leaveFrame = window.requestAnimationFrame(reset)
      }
    }
    function drop(event: DragEvent) {
      if (!hasFiles(event)) { reset(); return }
      intercept(event)
      // Read actual File objects while the native drop's data store is readable.
      const files = Array.from(event.dataTransfer?.files ?? [])
      const accept = !cancelled
      reset()
      if (accept && files.length) receive.current(files)
    }
    function escape(event: KeyboardEvent) {
      if (event.key !== 'Escape' || (!active && !depth)) return
      // Escape cancels this drag, not the underlying cinema/editor.
      event.stopImmediatePropagation()
      hide()
      cancelled = true
    }
    function visibility() { if (document.hidden) reset() }

    reset()
    window.addEventListener('dragenter', enter, true)
    window.addEventListener('dragover', over, true)
    window.addEventListener('dragleave', leave, true)
    window.addEventListener('drop', drop, true)
    window.addEventListener('dragend', reset, true)
    window.addEventListener('keydown', escape, true)
    window.addEventListener('blur', reset)
    // Pointer events resume when the OS cancels a drag without dragend/drop.
    window.addEventListener('pointermove', reset, true)
    window.addEventListener('pagehide', reset)
    document.addEventListener('visibilitychange', visibility)
    return () => {
      window.removeEventListener('dragenter', enter, true)
      window.removeEventListener('dragover', over, true)
      window.removeEventListener('dragleave', leave, true)
      window.removeEventListener('drop', drop, true)
      window.removeEventListener('dragend', reset, true)
      window.removeEventListener('keydown', escape, true)
      window.removeEventListener('blur', reset)
      window.removeEventListener('pointermove', reset, true)
      window.removeEventListener('pagehide', reset)
      document.removeEventListener('visibilitychange', visibility)
      if (leaveFrame !== undefined) window.cancelAnimationFrame(leaveFrame)
      document.documentElement.classList.remove('upload-file-drag-active')
    }
  }, [resetKey])

  return dragging
}
