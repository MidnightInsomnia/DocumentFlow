import type { DocumentField, DocumentType } from '../types/documents'
import { formatConfidence } from '../utils/formatters'
import { fieldLabels } from '../utils/labels'

export function DocumentFieldsTable({ fields, type }: { fields: DocumentField[]; type: DocumentType }) {
  const supplier = fields.find(field => field.name === 'SupplierINN' && field.value)
  const displayed = fields.filter(field => type !== 'Invoice' ||
    (field.name !== 'INN' || !supplier || supplier.value !== field.value) &&
    (field.name !== 'SupplierINN' || !!supplier || !fields.some(item => item.name === 'INN' && item.value)))
  return (
    <section className="card">
      <h2>Извлечённые поля</h2>
      {displayed.length === 0 ? (
        <p className="muted">Поля не извлечены.</p>
      ) : (
        <div className="table-scroll">
          <table>
            <thead><tr><th>Поле</th><th>Значение</th><th>Уверенность</th><th>Источник</th></tr></thead>
            <tbody>
              {displayed.map((field) => (
                <tr key={field.id}>
                  <td>{type === 'Invoice' && field.name === 'INN'
                    ? supplier && supplier.value !== field.value ? 'ИНН (старая запись, отличается от ИНН поставщика)' : 'ИНН поставщика'
                    : fieldLabels[field.name] ?? field.name}</td>
                  <td>{field.value || '—'}</td>
                  <td>{formatConfidence(field.confidence)}</td>
                  <td>{field.wasManuallyEdited ? 'Вручную' : 'Автоматически'}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </section>
  )
}
