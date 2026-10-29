import { useCallback, useEffect, useRef, useState } from 'react'
import { getDocumentDownloadUrl, getDocumentFileUrl, getDocumentPreviewPages, getDocumentPreviewPageUrl } from '../api/documentsApi'
import { apiFetch } from '../api/apiClient'
import { getSession } from '../api/session'
import type { DocumentDetails } from '../types/documents'

interface Props {
  document: DocumentDetails
  theater?: boolean
  onToggleTheater?: () => void
}

function useProtectedUrl(url: string | null, onFailure?: () => void, retryKey = 0): string | null {
  const [blobUrl, setBlobUrl] = useState<string | null>(null)
  const protectedFile = !!getSession()
  useEffect(() => {
    if (!url || !protectedFile) return
    const controller = new AbortController()
    let created: string | null = null
    setBlobUrl(null)
    apiFetch(new URL(url).pathname, { signal: controller.signal })
      .then(response => response.blob())
      .then(blob => { if (!controller.signal.aborted) { created = URL.createObjectURL(blob); setBlobUrl(created) } })
      .catch(() => { if (!controller.signal.aborted) { setBlobUrl(null); onFailure?.() } })
    return () => { controller.abort(); if (created) URL.revokeObjectURL(created) }
  }, [url, protectedFile, retryKey, onFailure])
  return protectedFile ? blobUrl : url
}

export function DocumentPreview({ document, theater, onToggleTheater }: Props) {
  const [previewFailed, setPreviewFailed] = useState(false)
  const [downloadError, setDownloadError] = useState<string | null>(null)
  const [retryKey, setRetryKey] = useState(0)
  const previewFailure = useCallback(() => setPreviewFailed(true), [])
  const previewUrl = getDocumentFileUrl(document.id)
  const downloadUrl = getDocumentDownloadUrl(document.id)
  const isPdf = document.contentType === 'application/pdf'
  const isImage = document.contentType === 'image/png' || document.contentType === 'image/jpeg'
  const isDocx = document.contentType === 'application/vnd.openxmlformats-officedocument.wordprocessingml.document'
  const isTiff = document.contentType === 'image/tiff' || document.contentType === 'image/tif'
  const protectedPreviewUrl = useProtectedUrl(!isDocx && !isTiff ? previewUrl : null, previewFailure, retryKey)

  async function download(event: React.MouseEvent<HTMLAnchorElement>) {
    if (!getSession()) return
    event.preventDefault()
    setDownloadError(null)
    try {
      const blob = await (await apiFetch(new URL(downloadUrl).pathname)).blob()
      const url = URL.createObjectURL(blob)
      const link = window.document.createElement('a')
      link.href = url; link.download = document.fileName
      window.document.body.appendChild(link)
      link.click(); link.remove()
      window.setTimeout(() => URL.revokeObjectURL(url), 1000)
    } catch { setDownloadError('Не удалось скачать оригинал. Проверьте соединение и повторите скачивание.') }
  }

  useEffect(() => setPreviewFailed(false), [document.id, document.contentType])

  return (
    <section className="card document-preview-card">
      <div className="section-heading preview-heading">
        <div><h2>Оригинал документа</h2>{onToggleTheater && <button className="button button-secondary theater-toggle" type="button" aria-pressed={!!theater} onClick={onToggleTheater}>{theater ? 'Обычный режим' : 'Кинотеатр'}</button>}</div>
        <span className="preview-file-name">{document.fileName}</span>
      </div>

      {previewFailed && <div className="preview-message" role="alert">
        Не удалось загрузить оригинал документа. <button className="text-button" type="button" onClick={() => { setPreviewFailed(false); setRetryKey(key => key + 1) }}>Повторить</button>
      </div>}

      {!previewFailed && (isPdf || isImage) && !protectedPreviewUrl && <p className="preview-message" role="status">Загрузка оригинала...</p>}
      {downloadError && <p className="alert alert-error" role="alert">{downloadError}</p>}
      {!previewFailed && isPdf && protectedPreviewUrl && <iframe
        className="document-preview-frame"
        src={protectedPreviewUrl}
        title={`Оригинал документа ${document.fileName}`}
        onError={() => setPreviewFailed(true)}
      />}

      {!previewFailed && isImage && protectedPreviewUrl && <div className="document-image-container">
        <img
          className="document-preview-image"
          src={protectedPreviewUrl}
          alt={`Оригинал документа ${document.fileName}`}
          onError={() => setPreviewFailed(true)}
        />
      </div>}

      {isDocx && <DocxPreview key={document.id} url={previewUrl} />}
      {isTiff && <TiffPreview key={`${document.id}-${document.contentType}`} id={document.id} fileName={document.fileName} />}
      {!previewFailed && !isPdf && !isImage && !isDocx && !isTiff && <div className="preview-message">
        Предварительный просмотр этого формата не поддерживается.
      </div>}

      <div className="preview-actions">
        {!isDocx && !isTiff && protectedPreviewUrl && <a className="button button-secondary" href={protectedPreviewUrl ?? undefined} target="_blank" rel="noopener noreferrer">
          Открыть в новой вкладке
        </a>}
        <a className="button" href={downloadUrl} onClick={event => void download(event)}>Скачать оригинал</a>
      </div>
    </section>
  )
}

function TiffPreview({ id, fileName }: { id: string, fileName: string }) {
  const [status, setStatus] = useState<'loading' | 'ready' | 'failed'>('loading')
  const [pageCount, setPageCount] = useState(0)
  const [page, setPage] = useState(1)
  const [mode, setMode] = useState<'fit' | 'actual'>('fit')
  const pageUrl = useProtectedUrl(getDocumentPreviewPageUrl(id, page))

  useEffect(() => {
    const controller = new AbortController()
    getDocumentPreviewPages(id, controller.signal).then(({ pageCount: count }) => {
      if (!Number.isSafeInteger(count) || count < 1) throw new Error('Invalid TIFF page count')
      setPageCount(count)
      setStatus('ready')
    }).catch(() => {
      if (!controller.signal.aborted) setStatus('failed')
    })
    return () => controller.abort()
  }, [id])

  return <>
    {status === 'loading' && <div className="preview-message" role="status">Загрузка TIFF...</div>}
    {status === 'failed' && <div className="preview-message" role="alert">Не удалось сформировать предварительный просмотр TIFF. Оригинальный файл можно скачать.</div>}
    {status === 'ready' && <>
      <div className="document-tiff-controls">
        <div className="document-tiff-pages" role="group" aria-label="Страницы TIFF">
          <button type="button" aria-label="Предыдущая страница" disabled={page === 1} onClick={() => setPage(current => Math.max(1, current - 1))}>←</button>
          <span>Страница {page} из {pageCount}</span>
          <button type="button" aria-label="Следующая страница" disabled={page === pageCount} onClick={() => setPage(current => Math.min(pageCount, current + 1))}>→</button>
        </div>
        <div className="document-docx-controls" role="group" aria-label="Масштаб TIFF">
          <button type="button" aria-pressed={mode === 'fit'} onClick={() => setMode('fit')}>По ширине</button>
          <button type="button" aria-pressed={mode === 'actual'} onClick={() => setMode('actual')}>100 %</button>
        </div>
      </div>
      <div className="document-image-container document-tiff-container">
        {pageUrl && <img key={page} className={mode === 'fit' ? 'document-preview-image' : 'document-tiff-original-size'}
          src={pageUrl} alt={`Страница ${page} документа ${fileName}`}
          onError={() => setStatus('failed')} />}
      </div>
    </>}
  </>
}

function DocxPreview({ url }: { url: string }) {
  const containerRef = useRef<HTMLDivElement>(null)
  const stageRef = useRef<HTMLDivElement>(null)
  const [status, setStatus] = useState<'loading' | 'ready' | 'failed'>('loading')
  const [mode, setMode] = useState<'fit' | 'actual'>('fit')
  const [size, setSize] = useState({ width: 0, height: 0, scale: 1 })

  useEffect(() => {
    if (status !== 'ready') return
    const viewport = containerRef.current
    const stage = stageRef.current
    if (!viewport || !stage) return
    const wrapper = stage.querySelector<HTMLElement>('.docx-wrapper')
    if (!wrapper) return

    // Measure the original Word page (including docx-preview's wrapper padding),
    // never the transformed box: scaling must not alter document layout.
    const measure = () => {
      const padding = window.getComputedStyle(wrapper)
      const pages = Array.from(wrapper.querySelectorAll<HTMLElement>('.docx'))
      const pageWidth = Math.max(0, ...pages.map(page => page.offsetWidth))
      const naturalWidth = Math.max(wrapper.scrollWidth, pageWidth + parseFloat(padding.paddingLeft || '0') + parseFloat(padding.paddingRight || '0'))
      const naturalHeight = wrapper.scrollHeight
      if (!naturalWidth || !naturalHeight || !viewport.clientWidth) return
      const scale = mode === 'fit' ? Math.min(1, viewport.clientWidth / naturalWidth) : 1
      setSize({ width: naturalWidth, height: naturalHeight, scale })
    }
    measure()
    const observer = typeof ResizeObserver === 'undefined' ? null : new ResizeObserver(measure)
    observer?.observe(viewport)
    window.addEventListener('resize', measure)
    return () => {
      observer?.disconnect()
      window.removeEventListener('resize', measure)
    }
  }, [status, mode])

  useEffect(() => {
    const controller = new AbortController()
    const stage = stageRef.current
    const renderedDocument = window.document.createElement('div')
    let active = true

    async function load() {
      try {
        const response = await apiFetch(new URL(url).pathname, { signal: controller.signal })
        if (!response.ok) throw new Error('DOCX file could not be loaded.')
        const blob = await response.blob()
        if (!active) return

        const { renderAsync } = await import('docx-preview')
        if (!active) return
        await renderAsync(blob, renderedDocument, renderedDocument, {
          breakPages: true,
          ignoreLastRenderedPageBreak: false,
          renderAltChunks: false,
        })
        if (!active) return
        stage?.replaceChildren(renderedDocument)
        setStatus('ready')
      } catch {
        if (active) setStatus('failed')
      }
    }

    void load()
    return () => {
      active = false
      controller.abort()
      stage?.replaceChildren()
    }
  }, [url])

  return <>
    {status === 'loading' && <div className="preview-message" role="status">Загрузка DOCX...</div>}
    {status === 'failed' && <div className="preview-message" role="alert">
      Не удалось отобразить DOCX. Оригинальный файл можно скачать.
    </div>}
    {status === 'ready' && <div className="document-docx-controls" role="group" aria-label="Масштаб DOCX">
      <button type="button" aria-pressed={mode === 'fit'} onClick={() => setMode('fit')}>По ширине</button>
      <button type="button" aria-pressed={mode === 'actual'} onClick={() => setMode('actual')}>100 %</button>
    </div>}
    <div className={`document-docx-container${mode === 'fit' ? ' document-docx-fit' : ''}`} data-testid="docx-preview" ref={containerRef} hidden={status !== 'ready'}>
      <div className="document-docx-extent" style={size.width ? { width: size.width * size.scale, height: size.height * size.scale } : undefined}>
        <div className="document-docx-stage" ref={stageRef} style={size.width ? { width: size.width, transform: `scale(${size.scale})` } : undefined} />
      </div>
    </div>
  </>
}
