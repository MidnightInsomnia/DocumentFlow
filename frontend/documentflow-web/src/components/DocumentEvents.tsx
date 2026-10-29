import type { DocumentEvent } from '../types/documents'
import { formatDate } from '../utils/formatters'
import { eventLabels } from '../utils/labels'

export function DocumentEvents({ events }: { events: DocumentEvent[] }) {
  return (
    <section className="card">
      <h2>История обработки</h2>
      {events.length === 0 ? <p className="muted">Событий пока нет.</p> : (
        <ol className="timeline">
          {events.map((event, index) => (
            <li key={`${event.createdAtUtc}-${index}`}>
              <time>{formatDate(event.createdAtUtc)}</time>
              <strong>{eventLabels[event.type] ?? event.type}</strong>
              <span className="event-actor">{event.actorDisplayName || 'Система'}</span>
              {event.message && <p>{event.message}</p>}
            </li>
          ))}
        </ol>
      )}
    </section>
  )
}
