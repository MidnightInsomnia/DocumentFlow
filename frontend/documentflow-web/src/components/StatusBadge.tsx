import type { DocumentStatus } from '../types/documents'
import { Icon } from './Icon'
import { statusLabels } from '../utils/labels'

const statusIcons: Record<DocumentStatus, string> = { PendingProcessing: 'file-text', Processing: 'refresh-cw', NeedsReview: 'triangle-alert', Completed: 'circle-check', Failed: 'x' }

export function StatusBadge({ status }: { status: DocumentStatus }) {
  return <span className={`status-badge status-${status}`}><Icon name={statusIcons[status]} size={12} />{statusLabels[status]}</span>
}
