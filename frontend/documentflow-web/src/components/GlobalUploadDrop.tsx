import { useCallback } from 'react'
import { createPortal } from 'react-dom'
import { Link, useBeforeUnload, useLocation, useMatch, useNavigate } from 'react-router-dom'
import { useGlobalFileDrop } from '../hooks/useGlobalFileDrop'
import { clearUploadInbox, enqueueUploadFiles, useUploadInbox } from '../hooks/useUploadInbox'
import { Icon } from './Icon'
import '../upload-intake.css'

export function GlobalUploadDrop() {
  const inbox = useUploadInbox()
  const location = useLocation()
  const navigate = useNavigate()
  const onUploadPage = useMatch('/upload') !== null
  const dragging = useGlobalFileDrop(files => {
    // Retain files before asking the existing route guard to navigate. A cancelled
    // guard leaves them here, with an explicit retry action; never auto-retry it.
    if (enqueueUploadFiles(files) && !onUploadPage) void navigate('/upload')
  }, `${location.key}:${inbox.sessionVersion}`)

  useBeforeUnload(useCallback((event: BeforeUnloadEvent) => {
    if (inbox.files.length) { event.preventDefault(); event.returnValue = '' }
  }, [inbox.files.length]))

  return createPortal(<>
    {!onUploadPage && inbox.files.length > 0 && <aside className="upload-intake-pending" aria-label="Файлы ожидают загрузки">
      <div role="status"><strong>Файлов в списке ожидания: {inbox.files.length}</strong><p>Откройте загрузку, когда закончите работу с текущей страницей. Файлы хранятся только в памяти этой вкладки.</p></div>
      <div className="upload-intake-actions"><Link className="button" to="/upload"><Icon name="file-up" size={14} />Открыть список</Link><button className="button button-secondary" type="button" onClick={() => {
        if (window.confirm('Убрать перетащенные файлы из списка ожидания? На сервер они ещё не отправлены.')) clearUploadInbox()
      }}>Убрать файлы</button></div>
    </aside>}
    {dragging && <div className="upload-intake-overlay" role="status" aria-live="polite" aria-atomic="true">
      <div className="upload-intake-target">
        <Icon name="file-up" size={40} />
        <h2>Отпустите файлы, чтобы добавить их в загрузку</h2>
        <p>Один или несколько документов — в общий список. Отправка на сервер начнётся только после вашего подтверждения.</p>
        <p className="upload-intake-limits">PDF, DOCX, JPG, JPEG, PNG, TIF, TIFF · до 20 файлов<br />До 20 МиБ на файл и до 100 МиБ за одну загрузку</p>
        <span className="upload-intake-cancel">Esc — отменить перетаскивание</span>
      </div>
    </div>}
  </>, document.body)
}
