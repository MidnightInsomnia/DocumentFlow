import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { ApiError, getCounterparty } from '../api/documentsApi'
import { StatusBadge } from '../components/StatusBadge'
import type { CounterpartyCard, CounterpartyDocumentItem, DocumentType } from '../types/documents'
import { formatDate, getErrorMessage } from '../utils/formatters'
import { typeLabels } from '../utils/labels'

const PAGE_SIZE = 20
const types: DocumentType[] = ['Invoice', 'Contract', 'Act', 'Unknown']

function DocumentRow({ document }: { document: CounterpartyDocumentItem }) {
  return <li>
    <Link to={`/documents/${document.id}`}>{document.fileName} {document.number && `№ ${document.number}`}</Link>
    {' · '}{typeLabels[document.type]} · <StatusBadge status={document.status} />
    {document.isArchived && <> · <span className="status-badge">В архиве</span></>}
  </li>
}

export function CounterpartyPage() {
  const { inn } = useParams()
  const [page, setPage] = useState(1)
  const [card, setCard] = useState<CounterpartyCard | null>(null)
  const [error, setError] = useState<string | null>(null)
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    if (!inn) return
    let cancelled = false
    setLoading(true)
    setError(null)
    void getCounterparty(inn, page, PAGE_SIZE)
      .then((response) => { if (!cancelled) setCard(response) })
      .catch((cause) => { if (!cancelled) {
        setCard(null)
        setError(cause instanceof ApiError && cause.status === 404
          ? 'Контрагент с таким ИНН не найден.' : getErrorMessage(cause))
      } })
      .finally(() => { if (!cancelled) setLoading(false) })
    return () => { cancelled = true }
  }, [inn, page])

  if (loading) return <div className="state-panel">Загрузка контрагента...</div>
  if (error) return <div className="alert alert-error" role="alert">{error}</div>
  if (!card) return null
  const pageCount = Math.ceil(card.documentCount / card.pageSize)
  return <div>
    <div className="page-heading"><div><Link className="back-link" to="/documents">← К документам</Link>
      <h1>Контрагент по ИНН {card.inn}</h1></div></div>
    <section className="card">
      <p className="muted">Карточка объединяет документы по сохранённым ИНН сторон и старым записям без указанной стороны; архивные документы включены. Совпадение ИНН не устанавливает роль контрагента в старой записи.</p>
      <div className="details-grid">
        <div className="detail-item"><span>ИНН</span><strong>{card.inn}</strong></div>
        <div className="detail-item"><span>Известные КПП</span><strong>{card.knownKpps.length ? card.knownKpps.join(', ') : '—'}</strong></div>
        <div className="detail-item"><span>Документов</span><strong>{card.documentCount}</strong></div>
        <div className="detail-item"><span>Требуют проверки</span><strong>{card.needsReviewCount}</strong></div>
        <div className="detail-item"><span>Сумма значений Amount в найденных документах</span>
          <strong>{new Intl.NumberFormat('ru-RU', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(card.knownDocumentAmountTotal)}</strong>
          <small className="muted">По {card.amountDocumentCount} документам с валидным Amount; не оборот и не задолженность.</small></div>
      </div>
      <h2>По типам</h2><p>{types.map((type) => `${typeLabels[type]}: ${card.countsByType[type]}`).join(' · ')}</p>
    </section>
    <section className="card"><h2>Последние документы</h2>
      <ul>{card.latestDocuments.map((document) => <DocumentRow key={document.id} document={document} />)}</ul>
    </section>
    <section className="card"><h2>Все документы</h2>
      <ul>{card.documents.map((document) => <DocumentRow key={document.id} document={document} />)}</ul>
      <div className="pagination">
        <button className="button button-secondary" type="button" disabled={page <= 1} onClick={() => setPage(page - 1)}>Назад</button>
        <span>Страница {page} из {pageCount}</span>
        <button className="button button-secondary" type="button" disabled={page >= pageCount} onClick={() => setPage(page + 1)}>Вперёд</button>
      </div>
    </section>
  </div>
}
