import type { DocumentStatus, UploadDocumentResponse } from '../types/documents'
import type { DuplicateDocumentApiError } from '../api/apiClient'

export const MAX_BATCH_FILES = 20
export const MAX_FILE_SIZE = 20 * 1024 * 1024
export const MAX_BATCH_SIZE = 100 * 1024 * 1024
// Preserve selection order: the first of identical files wins without browser-side hashing.
export const MAX_PARALLEL_UPLOADS = 1

export type UploadItemStatus = 'waiting' | 'uploading' | 'queued' | 'duplicate' | 'error'

export interface UploadItem {
  key: string
  file: File
  status: UploadItemStatus
  processingStatus?: DocumentStatus
  pollError?: string
  documentId?: string
  existingDocumentId?: string
  existingFileName?: string
  existingIsArchived?: boolean
  error?: string
}

type UploadItemUpdate = Partial<Pick<UploadItem, 'status' | 'processingStatus' | 'pollError' | 'documentId' | 'existingDocumentId' | 'existingFileName' | 'existingIsArchived' | 'error'>>

function isDuplicateDocument(error: unknown): error is DuplicateDocumentApiError {
  if (!(error instanceof Error)) return false
  const candidate = error as Partial<DuplicateDocumentApiError>
  return candidate.status === 409 && candidate.code === 'document_duplicate' &&
    typeof candidate.existingDocumentId === 'string' && Boolean(candidate.existingDocumentId) &&
    typeof candidate.existingFileName === 'string' && Boolean(candidate.existingFileName)
}

const ALLOWED_MIME_BY_EXTENSION: Record<string, string> = {
  '.pdf': 'application/pdf',
  '.docx': 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.png': 'image/png',
  '.tif': 'image/tiff',
  '.tiff': 'image/tiff',
}

export function validateFile(file: File): string | undefined {
  const extension = /\.[^.]+$/.exec(file.name)?.[0].toLowerCase()
  const expectedMime = extension ? ALLOWED_MIME_BY_EXTENSION[extension] : undefined
  if (!expectedMime) return 'Формат файла не поддерживается.'
  if (file.size > MAX_FILE_SIZE) return 'Размер файла превышает 20 МиБ.'
  if (file.type.toLowerCase() !== expectedMime && !(expectedMime === 'image/tiff' && file.type.toLowerCase() === 'image/tif')) {
    return 'Расширение файла не соответствует типу содержимого.'
  }
}

export function validateBatch(files: readonly File[]): string | undefined {
  if (files.length > MAX_BATCH_FILES) return 'За один раз можно загрузить не более 20 файлов.'
  if (files.reduce((total, file) => total + file.size, 0) > MAX_BATCH_SIZE) {
    return 'Общий размер выбранных файлов превышает 100 МиБ.'
  }
}

export async function uploadBatch(
  items: readonly UploadItem[],
  upload: (file: File) => Promise<UploadDocumentResponse>,
  update: (key: string, value: UploadItemUpdate) => void,
  formatError: (error: unknown) => string,
  concurrency = MAX_PARALLEL_UPLOADS,
): Promise<void> {
  const pendingItems = items.filter((item) => item.status === 'waiting')
  let nextIndex = 0

  async function worker() {
    while (nextIndex < pendingItems.length) {
      const item = pendingItems[nextIndex++]
      update(item.key, { status: 'uploading', processingStatus: undefined, pollError: undefined, documentId: undefined, existingDocumentId: undefined, existingFileName: undefined, existingIsArchived: undefined, error: undefined })
      try {
        const result = await upload(item.file)
        update(item.key, { status: 'queued', processingStatus: result.status, documentId: result.id, existingDocumentId: undefined, existingFileName: undefined, existingIsArchived: undefined, error: undefined })
      } catch (error) {
        if (isDuplicateDocument(error)) {
          update(item.key, { status: 'duplicate', documentId: undefined,
            existingDocumentId: error.existingDocumentId, existingFileName: error.existingFileName, existingIsArchived: error.existingIsArchived, error: undefined })
        } else {
          update(item.key, { status: 'error', documentId: undefined,
            existingDocumentId: undefined, existingFileName: undefined, existingIsArchived: undefined, error: formatError(error) })
        }
      }
    }
  }

  await Promise.all(Array.from(
    { length: Math.min(concurrency, pendingItems.length) },
    () => worker(),
  ))
}
