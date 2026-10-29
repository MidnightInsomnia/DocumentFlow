export function formatDate(value: string | null): string {
  if (!value) return '—'
  return new Intl.DateTimeFormat('ru-RU', {
    dateStyle: 'short',
    timeStyle: 'short',
  }).format(new Date(value))
}

export function formatConfidence(value: number | null): string {
  return value === null ? '—' : `${Math.round(value * 100)}%`
}

export function formatFileSize(bytes: number): string {
  if (bytes < 1024) return `${bytes} Б`
  if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} КБ`
  return `${(bytes / (1024 * 1024)).toFixed(1)} МБ`
}

export function formatDocumentFormat(contentType: string): string {
  if (contentType === 'application/vnd.openxmlformats-officedocument.wordprocessingml.document') return 'DOCX'
  if (contentType === 'image/tiff' || contentType === 'image/tif') return 'TIFF'
  return contentType
}

export function getErrorMessage(error: unknown): string {
  return error instanceof Error ? error.message : 'Произошла неизвестная ошибка.'
}
