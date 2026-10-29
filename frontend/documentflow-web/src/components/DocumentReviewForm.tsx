import { useEffect, useMemo, useRef, useState, type FormEvent, type KeyboardEvent } from 'react'
import { flushSync } from 'react-dom'
import { ApiError, getAiReviewSuggestion, reviewDocument } from '../api/documentsApi'
import { getReviewDraft, saveReviewDraft, type ReviewDraft } from '../api/reviewWorkApi'
import { getSession } from '../api/session'
import { useNavigationGuard } from '../hooks/useNavigationGuard'
import type { AiReviewSuggestion, DocumentDetails, ReviewableDocumentType, ReviewContext, ReviewContextIssue } from '../types/documents'
import { fieldLabels, typeLabels } from '../utils/labels'
import { formatConfidence, formatDate, getErrorMessage } from '../utils/formatters'
import './review-form.css'

interface ReviewFieldDefinition { name: string; required: boolean; hint: string; maxLength: number }
const fieldsByType: Record<ReviewableDocumentType, ReviewFieldDefinition[]> = {
  Invoice: [
    { name: 'Number', required: true, hint: 'До 50 символов. Например, СЧ-123/26.', maxLength: 50 },
    { name: 'Date', required: true, hint: 'Дата документа по оригиналу.', maxLength: 10 },
    { name: 'SupplierINN', required: true, hint: 'ИНН поставщика: 10 или 12 цифр, без пробелов.', maxLength: 12 },
    { name: 'BuyerINN', required: false, hint: 'ИНН покупателя: 10 или 12 цифр, без пробелов.', maxLength: 12 },
    { name: 'Amount', required: true, hint: 'Положительная сумма. Например, 1 234,56; пробелы и запятая допустимы.', maxLength: 40 },
    { name: 'KPP', required: false, hint: '9 цифр, без пробелов.', maxLength: 9 },
  ],
  Contract: [
    { name: 'Number', required: true, hint: 'До 50 символов. Например, Д-123/26.', maxLength: 50 },
    { name: 'Date', required: true, hint: 'Дата документа по оригиналу.', maxLength: 10 },
    { name: 'Amount', required: false, hint: 'Положительная сумма. Например, 1 234,56; пробелы и запятая допустимы.', maxLength: 40 },
    { name: 'CustomerINN', required: false, hint: 'ИНН заказчика: 10 или 12 цифр, без пробелов.', maxLength: 12 },
    { name: 'ContractorINN', required: false, hint: 'ИНН исполнителя: 10 или 12 цифр, без пробелов.', maxLength: 12 },
  ],
  Act: [
    { name: 'Number', required: true, hint: 'До 50 символов. Например, А-123/26.', maxLength: 50 },
    { name: 'Date', required: true, hint: 'Дата документа по оригиналу.', maxLength: 10 },
    { name: 'Amount', required: true, hint: 'Положительная сумма. Например, 1 234,56; пробелы и запятая допустимы.', maxLength: 40 },
    { name: 'RelatedContractNumber', required: false, hint: 'До 50 букв, цифр, дефисов и косых черт. Например, Д-123/26.', maxLength: 50 },
    { name: 'CustomerINN', required: false, hint: 'ИНН заказчика: 10 или 12 цифр, без пробелов.', maxLength: 12 },
    { name: 'ContractorINN', required: false, hint: 'ИНН исполнителя: 10 или 12 цифр, без пробелов.', maxLength: 12 },
  ],
}
const knownFields = new Set(['INN', ...Object.values(fieldsByType).flatMap(fields => fields.map(field => field.name))])
const emergencyPrefix = 'documentflow.review-emergency.v1:'
const emergencyLifetime = 30 * 60 * 1000

function makeDraft(type: ReviewDraft['type'], values: Record<string, string>, baseVersion: string, legacyInn = ''): ReviewDraft {
  const fields = (type ? fieldsByType[type] : []).map(({ name }) => ({ name, value: values[name] ?? '' }))
  // Invoice still validates the historical INN field. Mirror the single supplier input.
  // Never infer an Act/Contract party from an ownerless legacy INN.
  if (type === 'Invoice') fields.push({ name: 'INN', value: values.SupplierINN ?? '' })
  else if (legacyInn && (type === 'Act' || type === 'Contract')) fields.push({ name: 'INN', value: legacyInn })
  return { type, fields, baseVersion }
}
function fingerprint(draft: ReviewDraft) {
  return JSON.stringify({ type: draft.type, fields: draft.fields, baseVersion: draft.baseVersion })
}
function draftValues(draft: ReviewDraft, originals: Record<string, string>) {
  const values = { ...originals, ...Object.fromEntries(draft.fields.map(field => [field.name, field.value])) }
  if (draft.type === 'Invoice' && !draft.fields.some(field => field.name === 'SupplierINN')) values.SupplierINN = values.INN ?? ''
  return values
}
function emergencyKey(userId: string, documentId: string) { return `${emergencyPrefix}${encodeURIComponent(userId)}:${encodeURIComponent(documentId)}` }
function removeEmergency(userId: string | null, documentId: string) {
  try { if (userId) sessionStorage.removeItem(emergencyKey(userId, documentId)) } catch { /* Storage may be unavailable. */ }
}
function readEmergency(userId: string | null, documentId: string, excludingVersion?: string): ReviewDraft | null {
  try {
    for (let index = sessionStorage.length - 1; index >= 0; index--) {
      const key = sessionStorage.key(index)
      if (!key?.startsWith(emergencyPrefix)) continue
      try {
        const value = JSON.parse(sessionStorage.getItem(key) ?? 'null')
        if (!value || !Number.isFinite(value.expiresAt) || value.expiresAt <= Date.now()) sessionStorage.removeItem(key)
      } catch { sessionStorage.removeItem(key) }
    }
    if (!userId) return null
    const primary = emergencyKey(userId, documentId)
    const candidates: { draft: ReviewDraft; expiresAt: number; primary: boolean }[] = []
    for (let index = 0; index < sessionStorage.length; index++) {
      const key = sessionStorage.key(index)
      if (!key || (key !== primary && !key.startsWith(`${primary}:conflict:`))) continue
      const raw = sessionStorage.getItem(key)
      if (!raw || raw.length > 40000) continue
      const value = JSON.parse(raw)
      const draft = value?.draft
      if (value.userId !== userId || value.documentId !== documentId || value.expiresAt > Date.now() + emergencyLifetime ||
          !draft || !['', 'Invoice', 'Contract', 'Act'].includes(draft.type) || typeof draft.baseVersion !== 'string' ||
          draft.baseVersion === excludingVersion || !Array.isArray(draft.fields) || draft.fields.length > 20 ||
          draft.fields.some((field: { name?: unknown; value?: unknown } | null) => !field || typeof field.name !== 'string' ||
            !knownFields.has(field.name) || typeof field.value !== 'string' || field.value.length > 4000)) continue
      candidates.push({ draft, expiresAt: value.expiresAt, primary: key === primary })
    }
    candidates.sort((a, b) => Number(b.primary) - Number(a.primary) || b.expiresAt - a.expiresAt)
    return candidates[0]?.draft ?? null
  } catch { return null }
}
function preserveEmergencyConflict(userId: string | null, documentId: string, draft: ReviewDraft) {
  if (!userId) return false
  try {
    const primary = emergencyKey(userId, documentId)
    const raw = sessionStorage.getItem(primary)
    if (raw && fingerprint(JSON.parse(raw).draft) === fingerprint(draft)) {
      // Keep the original expiry: opening a conflict must not erase or prolong it.
      sessionStorage.setItem(`${primary}:conflict:${encodeURIComponent(draft.baseVersion)}`, raw)
    }
    return true
  } catch { return false }
}
function writeEmergency(userId: string | null, documentId: string, draft: ReviewDraft): boolean {
  if (!userId) return false
  try {
    const raw = JSON.stringify({ userId, documentId, expiresAt: Date.now() + emergencyLifetime, draft })
    if (raw.length > 40000) return false
    sessionStorage.setItem(emergencyKey(userId, documentId), raw)
    return true
  } catch { return false }
}
function normalizeAmount(value: string) { return value.replace(/\s/gu, '').replace(/,/g, '.') }
function validateField(field: ReviewFieldDefinition, value: string): string | null {
  if (!value.trim()) return field.required ? 'Заполните обязательное поле.' : null
  if (value.length > field.maxLength) return `Не более ${field.maxLength} символов.`
  if (field.name.endsWith('INN') && !/^(?:[0-9]{10}|[0-9]{12})$/.test(value)) return 'ИНН должен содержать 10 или 12 цифр без пробелов.'
  if (field.name === 'KPP' && !/^[0-9]{9}$/.test(value)) return 'КПП должен содержать 9 цифр.'
  if (field.name === 'RelatedContractNumber' && !/^[\p{L}\p{Nd}/-]{1,50}$/u.test(value)) return 'Используйте только буквы, цифры, дефисы и косые черты.'
  if (field.name === 'Amount') {
    const amount = normalizeAmount(value)
    if (!/^\+?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)$/.test(amount) || !Number.isFinite(Number(amount)) || Number(amount) <= 0) return 'Введите положительную сумму, например 1 234,56.'
  }
  if (field.name === 'Date') {
    const date = new Date(`${value}T00:00:00Z`)
    if (!/^[0-9]{4}-[0-9]{2}-[0-9]{2}$/.test(value) || value.startsWith('0000') || !Number.isFinite(date.getTime()) || date.toISOString().slice(0, 10) !== value) return 'Укажите существующую дату документа.'
  }
  return null
}
function normalizedAiValue(name: string, input: string): string | null {
  let value = input.trim()
  if (!value || /^(?:value|string|null|undefined|n\/?a|не указано)$/iu.test(value)) return null
  if (name === 'Date') {
    const russianDate = /^(\d{1,2})[./](\d{1,2})[./](\d{4})$/.exec(value)
    if (russianDate) value = `${russianDate[3]}-${russianDate[2].padStart(2, '0')}-${russianDate[1].padStart(2, '0')}`
  }
  if (name === 'Amount') value = normalizeAmount(value)
  const definition = Object.values(fieldsByType).flat().find(field => field.name === name)
  return definition && !validateField(definition, value) ? value : null
}
function issueDescription(issue: ReviewContextIssue, server = false): string {
  if (server && issue.message && !/^[\x00-\x7F]+$/.test(issue.message)) return issue.message
  const label = fieldLabels[issue.fieldName ?? ''] ?? issue.fieldName ?? 'поле'
  if (issue.code === 'UnknownDocumentType') return 'Не удалось определить тип документа.'
  if (issue.code === 'MissingRequiredField') return `Заполните поле «${label}».`
  if (issue.code === 'LowFieldConfidence') return `Сверьте поле «${label}» с оригиналом: оценка извлечения низкая.`
  if (issue.code === 'InvalidFieldValue') return `Проверьте формат поля «${label}».`
  return issue.message
}
function requestMessage(error: unknown) {
  if (error instanceof ApiError && error.status === 401) return 'Сессия истекла. Войдите снова под тем же пользователем и откройте этот документ. Несохранённый ввод доступен в этой вкладке до 30 минут, если браузер разрешает sessionStorage.'
  return getErrorMessage(error)
}

interface Props {
  document: DocumentDetails
  reviewContext: ReviewContext
  onReviewed: (next?: boolean) => Promise<void>
}

export function DocumentReviewForm({ document, reviewContext, onReviewed }: Props) {
  const [initial] = useState(() => {
    const originals = Object.fromEntries(document.fields.map(field => [field.name, field.value ?? '']))
    if (document.type === 'Invoice' && !originals.SupplierINN) originals.SupplierINN = originals.INN ?? ''
    const legacyInn = document.type === 'Act' || document.type === 'Contract' ? originals.INN ?? '' : ''
    const type: ReviewDraft['type'] = document.type === 'Unknown' ? '' : document.type
    const userId = getSession()?.user.id ?? null
    const emergency = readEmergency(userId, document.id)
    const restored = emergency?.baseVersion === document.version ? emergency : null
    const staleEmergency = readEmergency(userId, document.id, document.version)
    return { userId, originals, legacyInn, baseVersion: document.version, baseline: makeDraft(type, originals, document.version, legacyInn), emergency, restored, staleEmergency }
  })
  const [selectedType, setSelectedType] = useState<ReviewDraft['type']>(initial.restored?.type ?? initial.baseline.type)
  const [values, setValues] = useState<Record<string, string>>(() => initial.restored ? draftValues(initial.restored, initial.originals) : initial.originals)
  const [savedFingerprint, setSavedFingerprint] = useState(() => fingerprint(initial.baseline))
  const [activeTab, setActiveTab] = useState<'fields' | 'ai'>('fields')
  const [operation, setOperation] = useState<'draft' | 'review' | null>(null)
  const [reviewed, setReviewed] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [conflict, setConflict] = useState(false)
  const [localErrors, setLocalErrors] = useState<Record<string, string>>({})
  const [serverIssues, setServerIssues] = useState<ReviewContextIssue[]>([])
  const [aiAppliedFields, setAiAppliedFields] = useState<Set<string>>(new Set())
  const [aiSuggestion, setAiSuggestion] = useState<AiReviewSuggestion | null>(null)
  const [aiLoading, setAiLoading] = useState(false)
  const [aiError, setAiError] = useState<string | null>(null)
  const [draftAttempt, setDraftAttempt] = useState(0)
  const [draftLoading, setDraftLoading] = useState(true)
  const [draftError, setDraftError] = useState<string | null>(null)
  const [draftErrorAction, setDraftErrorAction] = useState<'load' | 'save'>('load')
  const [availableDraft, setAvailableDraft] = useState<ReviewDraft | null>(null)
  const [staleDraft, setStaleDraft] = useState<ReviewDraft | null>(initial.staleEmergency)
  const [draftMessage, setDraftMessage] = useState(initial.restored ? 'Восстановлен несохранённый ввод этой вкладки. Сохраните его на сервере.' : '')
  const [emergencyAvailable, setEmergencyAvailable] = useState<boolean | null>(null)
  const [transitionFailed, setTransitionFailed] = useState(false)
  const visibleFields = selectedType ? fieldsByType[selectedType] : []
  const snapshot = useMemo(() => makeDraft(selectedType, values, initial.baseVersion, initial.legacyInn), [selectedType, values, initial])
  const currentFingerprint = fingerprint(snapshot)
  const dirty = !reviewed && currentFingerprint !== savedFingerprint
  const hasConflict = conflict || document.version !== initial.baseVersion
  const disabled = draftLoading || operation !== null || reviewed
  const fieldMetadata = useMemo(() => new Map(document.fields.map(field => [field.name, field])), [document.fields])
  const formRef = useRef<HTMLFormElement>(null)
  const snapshotRef = useRef(snapshot)
  const savedRef = useRef(savedFingerprint)
  const operationRef = useRef(false)
  const reviewedRef = useRef(false)
  const mountedRef = useRef(true)
  const editEpoch = useRef(0)
  const submittedNext = useRef(false)
  const latestVersionRef = useRef(document.version)
  const draftRevisionRef = useRef<string | undefined>(undefined)
  snapshotRef.current = snapshot
  savedRef.current = savedFingerprint
  latestVersionRef.current = document.version
  useNavigationGuard(() => !reviewedRef.current && (operationRef.current || fingerprint(snapshotRef.current) !== savedRef.current))

  useEffect(() => { mountedRef.current = true; return () => { mountedRef.current = false } }, [])
  useEffect(() => {
    if (initial.emergency && !initial.restored && !preserveEmergencyConflict(initial.userId, document.id, initial.emergency)) {
      // A full/blocked storage must not destroy the only copy while trying to duplicate it.
      setEmergencyAvailable(false); return
    }
    if (!dirty) { removeEmergency(initial.userId, document.id); return }
    setEmergencyAvailable(writeEmergency(initial.userId, document.id, snapshot))
  }, [dirty, snapshot, initial, document.id])

  useEffect(() => {
    const controller = new AbortController()
    const epoch = editEpoch.current
    setDraftLoading(true)
    setDraftError(null)
    setDraftErrorAction('load')
    void getReviewDraft(document.id, controller.signal).then(draft => {
      if (controller.signal.aborted) return
      draftRevisionRef.current = draft?.updatedAtUtc
      if (!draft) { if (!initial.restored) setDraftMessage('Серверного черновика пока нет. Изменения сохраняются только по кнопке.'); return }
      if (draft.baseVersion !== initial.baseVersion || draft.baseVersion !== latestVersionRef.current) { setStaleDraft(current => current ?? draft); setDraftMessage('Черновик другой версии не применён.'); return }
      const restoredValues = draftValues(draft, initial.originals)
      const restoredFingerprint = fingerprint(makeDraft(draft.type, restoredValues, draft.baseVersion, initial.legacyInn))
      if (epoch !== editEpoch.current || fingerprint(snapshotRef.current) !== savedRef.current && fingerprint(snapshotRef.current) !== restoredFingerprint) {
        setAvailableDraft(draft)
        setDraftMessage('Есть серверный черновик. Текущий ввод не заменён; можно восстановить сохранённый вариант отдельно.')
        return
      }
      setSelectedType(draft.type)
      setValues(restoredValues)
      savedRef.current = restoredFingerprint
      setSavedFingerprint(restoredFingerprint)
      setAvailableDraft(null)
      removeEmergency(initial.userId, document.id)
      setDraftMessage(`Восстановлен серверный черновик${draft.updatedAtUtc ? ` от ${formatDate(draft.updatedAtUtc)}` : ''}.`)
    }).catch(requestError => {
      if (!controller.signal.aborted) setDraftError(`Не удалось загрузить черновик. ${requestMessage(requestError)}`)
    }).finally(() => { if (!controller.signal.aborted) setDraftLoading(false) })
    return () => controller.abort()
  }, [document.id, draftAttempt, initial])

  function changeType(type: ReviewDraft['type']) {
    if (disabled || operationRef.current) return
    editEpoch.current++
    setSelectedType(type)
    setLocalErrors({})
    setServerIssues([])
    setError(null)
  }
  function changeField(name: string, value: string, ai = false) {
    if (disabled || operationRef.current) return
    editEpoch.current++
    setValues(current => ({ ...current, [name]: value }))
    setLocalErrors(current => { const next = { ...current }; delete next[name]; return next })
    setServerIssues(current => current.filter(issue => issue.fieldName !== name))
    setAiAppliedFields(current => { const next = new Set(current); if (ai) next.add(name); else next.delete(name); return next })
    setError(null)
  }
  function restoreDraft(draft: ReviewDraft) {
    if (disabled || hasConflict || draft.baseVersion !== initial.baseVersion) return
    if (dirty && !window.confirm('Заменить текущий несохранённый ввод сохранённым серверным черновиком?')) return
    const restoredValues = draftValues(draft, initial.originals)
    const restoredFingerprint = fingerprint(makeDraft(draft.type, restoredValues, draft.baseVersion, initial.legacyInn))
    editEpoch.current++
    setSelectedType(draft.type); setValues(restoredValues)
    savedRef.current = restoredFingerprint; setSavedFingerprint(restoredFingerprint)
    setServerIssues([]); setLocalErrors({}); setAiAppliedFields(new Set()); setError(null)
    setAvailableDraft(null); setActiveTab('fields')
    setDraftMessage('Серверный черновик восстановлен. Несохранённых изменений нет.')
  }
  function showFieldsAndValidity(firstName?: string) {
    // Native validation cannot focus required controls while their tab is hidden.
    operationRef.current = false
    flushSync(() => { setOperation(null); setActiveTab('fields') })
    const input = firstName ? formRef.current?.elements.namedItem(firstName) : null
    if (input instanceof HTMLInputElement || input instanceof HTMLSelectElement) input.focus()
    formRef.current?.reportValidity()
  }
  function recordConflict(message = 'Документ изменён другим пользователем или больше не доступен для проверки. Ваш ввод не заменён. Откройте актуальную карточку в новой вкладке, сравните значения и перенесите нужные правки. Старую версию подтвердить нельзя.') {
    setConflict(true)
    setError(message)
  }
  async function saveDraft() {
    if (disabled || hasConflict || operationRef.current) return
    const sent = snapshotRef.current
    const sentFingerprint = fingerprint(sent)
    operationRef.current = true; setOperation('draft'); setDraftError(null); setDraftErrorAction('save')
    if (sentFingerprint !== savedRef.current) writeEmergency(initial.userId, document.id, sent)
    try {
      const result = await saveReviewDraft(document.id, { ...sent, updatedAtUtc: draftRevisionRef.current })
      if (!mountedRef.current) return
      if (result && result.baseVersion !== sent.baseVersion) { recordConflict(); return }
      draftRevisionRef.current = result?.updatedAtUtc
      const persistedFingerprint = result ? fingerprint(makeDraft(result.type, draftValues(result, initial.originals), result.baseVersion, initial.legacyInn)) : sentFingerprint
      savedRef.current = persistedFingerprint; setSavedFingerprint(persistedFingerprint)
      setAvailableDraft(null)
      if (persistedFingerprint !== sentFingerprint) {
        setAvailableDraft(result)
        setDraftError('Сохранённый вариант отличается от отправленного. Текущий ввод не заменён: сравните или восстановите серверный черновик.')
        return
      }
      removeEmergency(initial.userId, document.id)
      setDraftMessage(`Черновик сохранён на сервере${result?.updatedAtUtc ? ` · ${formatDate(result.updatedAtUtc)}` : ''}. Документ ещё не подтверждён.`)
    } catch (requestError) {
      if (!mountedRef.current) return
      if (requestError instanceof ApiError && requestError.status === 409) recordConflict(requestError.code === 'review_draft_conflict'
        ? 'Черновик сохранён в другой вкладке. Ваш ввод сохранён здесь и не заменён. Откройте документ в новой вкладке, сравните оба варианта и перенесите нужные правки.' : undefined)
      else setDraftError(`Черновик не сохранён. ${requestMessage(requestError)}`)
    } finally { operationRef.current = false; if (mountedRef.current) setOperation(null) }
  }
  async function requestAiSuggestion() {
    if (aiLoading || disabled || operationRef.current) return
    setActiveTab('ai'); setAiLoading(true); setAiError(null)
    try {
      const suggestion = await getAiReviewSuggestion(document.id)
      if (mountedRef.current) {
        const offered = suggestion.suggestedType === 'Invoice' && suggestion.fields.some(field => field.name === 'SupplierINN')
          ? suggestion.fields.filter(field => field.name !== 'INN') : suggestion.fields
        setAiSuggestion({ ...suggestion, fields: offered.map(field => {
          const name = suggestion.suggestedType === 'Invoice' && field.name === 'INN' ? 'SupplierINN' : field.name
          return { ...field, name, value: normalizedAiValue(name, field.value) ?? '' }
        }) })
      }
    } catch (requestError) {
      if (!mountedRef.current) return
      const messages: Record<string, string> = {
        ai_disabled: 'AI-подсказки отключены в настройках приложения.', ai_unavailable: 'AI-сервис недоступен. Повторите позже или заполните поля вручную.',
        ai_model_unavailable: 'Настроенная AI-модель недоступна.', ai_timeout: 'AI-сервис не ответил вовремя. Повторите запрос.', ai_invalid_response: 'Не удалось разобрать ответ AI. Повторите запрос.',
      }
      setAiError(requestError instanceof ApiError ? messages[requestError.code ?? ''] ?? requestMessage(requestError) : requestMessage(requestError))
    } finally { if (mountedRef.current) setAiLoading(false) }
  }
  function applyAiToEmptyFields() {
    if (!aiSuggestion || !selectedType || disabled || operationRef.current) return
    const next = { ...values }
    const applied: string[] = []
    for (const field of aiSuggestion.fields) {
      if (visibleFields.some(item => item.name === field.name) && field.value.trim() && !next[field.name]?.trim()) {
        next[field.name] = field.value; applied.push(field.name)
      }
    }
    if (!applied.length) return
    editEpoch.current++
    setValues(next)
    setAiAppliedFields(current => new Set([...current, ...applied]))
    setLocalErrors(current => Object.fromEntries(Object.entries(current).filter(([name]) => !applied.includes(name))))
    setServerIssues(current => current.filter(issue => !issue.fieldName || !applied.includes(issue.fieldName)))
    setError(null)
  }
  async function finishTransition() {
    setTransitionFailed(false)
    try { await onReviewed(submittedNext.current) }
    catch {
      if (mountedRef.current) { setTransitionFailed(true); setError('Документ подтверждён, но не удалось обновить карточку или перейти дальше. Повторите переход; повторное подтверждение не требуется.') }
    }
  }
  async function submit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (disabled || hasConflict || operationRef.current) return
    setError(null)
    if (!selectedType) { setError('Выберите поддерживаемый тип по оригиналу.'); showFieldsAndValidity('review-type'); return }
    const errors: Record<string, string> = {}
    for (const field of visibleFields) {
      const message = validateField(field, values[field.name] ?? '')
      if (message) errors[field.name] = message
    }
    setLocalErrors(errors)
    if (Object.keys(errors).length || !event.currentTarget.checkValidity()) {
      setError('Исправьте отмеченные поля перед подтверждением. Черновик можно сохранить и с незаполненными полями.')
      showFieldsAndValidity(Object.keys(errors)[0]); return
    }
    const submitter = (event.nativeEvent as SubmitEvent).submitter
    submittedNext.current = submitter instanceof HTMLButtonElement && submitter.value === 'next'
    const sent = { ...snapshotRef.current, fields: snapshotRef.current.fields.map(field => ({ ...field, value: field.name === 'Amount' ? normalizeAmount(field.value) : field.value.trim() })) }
    writeEmergency(initial.userId, document.id, snapshotRef.current)
    operationRef.current = true; setOperation('review')
    try {
      await reviewDocument(document.id, { type: selectedType, fields: sent.fields, expectedVersion: initial.baseVersion })
      reviewedRef.current = true
      removeEmergency(initial.userId, document.id)
      if (!mountedRef.current) return
      setReviewed(true); setServerIssues([]); setDraftMessage('Документ подтверждён.'); setError(null)
      // Ref-based guard is released before the parent may navigate synchronously.
      await finishTransition()
    } catch (requestError) {
      if (!mountedRef.current) return
      if (requestError instanceof ApiError && requestError.status === 422) {
        setServerIssues(requestError.issues)
        setError('Сервер не подтвердил документ. Исправьте отмеченные поля; введённые значения сохранены в форме.')
        showFieldsAndValidity(requestError.issues.find(issue => issue.fieldName)?.fieldName ?? undefined)
      } else if (requestError instanceof ApiError && requestError.status === 409) recordConflict()
      else setError(requestMessage(requestError))
    } finally { operationRef.current = false; if (mountedRef.current) setOperation(null) }
  }
  function tabKeyDown(event: KeyboardEvent<HTMLButtonElement>) {
    if (!['ArrowLeft', 'ArrowRight', 'Home', 'End'].includes(event.key)) return
    event.preventDefault()
    const next = event.key === 'Home' ? 'fields' : event.key === 'End' ? 'ai' : activeTab === 'fields' ? 'ai' : 'fields'
    setActiveTab(next)
    formRef.current?.querySelector<HTMLButtonElement>(`#review-tab-${next}`)?.focus()
  }
  const originalIssuesCurrent = selectedType === reviewContext.type
  const activeOriginalIssues = reviewContext.issues.filter(issue => originalIssuesCurrent && (!issue.fieldName ||
    visibleFields.some(field => field.name === issue.fieldName) && (values[issue.fieldName] ?? '') === (initial.originals[issue.fieldName] ?? '')))
  const allowedAiFields = new Set(visibleFields.map(field => field.name))

  return <section className="card review-card review-editor" id="manual-review" aria-labelledby="review-title">
    <div className="section-heading"><h2 id="review-title">Ручная проверка</h2></div>
    <p className="review-summary">Сверьте реквизиты с оригиналом. Черновик не завершает проверку и виден только вам.</p>
    <form ref={formRef} onSubmit={submit} noValidate aria-busy={operation !== null}>
      <div className="review-tabs" role="tablist" aria-label="Ручная проверка документа">
        <button id="review-tab-fields" type="button" role="tab" aria-selected={activeTab === 'fields'} aria-controls="review-panel-fields" tabIndex={activeTab === 'fields' ? 0 : -1} onClick={() => setActiveTab('fields')} onKeyDown={tabKeyDown}>Реквизиты</button>
        <button id="review-tab-ai" type="button" role="tab" aria-selected={activeTab === 'ai'} aria-controls="review-panel-ai" tabIndex={activeTab === 'ai' ? 0 : -1} onClick={() => setActiveTab('ai')} onKeyDown={tabKeyDown}>AI-подсказка</button>
      </div>
      <div className="review-draft-status" role="status" aria-live="polite">
        {draftLoading ? 'Загрузка серверного черновика…' : operation === 'draft' ? 'Сохранение черновика…' : dirty ? 'Есть несохранённые изменения.' : draftMessage || 'Несохранённых изменений нет.'}
        {dirty && draftMessage && <small>{draftMessage}</small>}
        {dirty && <small>{emergencyAvailable === true ? 'Временная копия в этой вкладке — до 30 минут. Для надёжного сохранения используйте серверный черновик.' : emergencyAvailable === false ? 'Временная копия недоступна. Сохраните серверный черновик перед выходом.' : 'Сохраните черновик перед выходом.'}</small>}
      </div>
      {draftError && <div className="alert alert-error" role="alert"><p>{draftError}</p><button className="text-button" type="button" disabled={disabled || hasConflict} onClick={() => { if (draftErrorAction === 'save') void saveDraft(); else setDraftAttempt(current => current + 1) }}>{draftErrorAction === 'save' ? 'Повторить сохранение черновика' : 'Повторить загрузку черновика'}</button></div>}
      {availableDraft && <button className="text-button" type="button" disabled={disabled || hasConflict} onClick={() => restoreDraft(availableDraft)}>Восстановить серверный черновик{availableDraft.updatedAtUtc ? ` от ${formatDate(availableDraft.updatedAtUtc)}` : ''}</button>}
      {staleDraft && <details className="review-recovery"><summary>Черновик другой версии — не применён</summary><p>Документ изменился после сохранения. Сравните эти данные с актуальным оригиналом и перенесите нужные значения вручную.</p><textarea aria-label="Данные черновика другой версии для копирования" readOnly value={JSON.stringify(staleDraft, null, 2)} rows={7} /></details>}
      {hasConflict && <div className="alert alert-error" role="alert"><p>{error || 'Версия документа изменилась. Ввод сохранён в форме, но не будет отправлен поверх новой версии.'}</p><a href={`/documents/${encodeURIComponent(document.id)}`} target="_blank" rel="noopener noreferrer">Открыть актуальную карточку в новой вкладке</a><details className="review-recovery"><summary>Введённые значения для сравнения и копирования</summary><textarea aria-label="Текущие введённые значения" readOnly value={JSON.stringify(snapshot, null, 2)} rows={7} /></details></div>}
      <div className="review-tab-panel" id="review-panel-fields" role="tabpanel" aria-labelledby="review-tab-fields" tabIndex={0} hidden={activeTab !== 'fields'}>
        <details className="review-reasons review-original-reasons"><summary>Автоматическое распознавание и причины проверки</summary>
          <p>Исходный тип: <strong>{typeLabels[reviewContext.type]}</strong> · Оценка классификации: {formatConfidence(reviewContext.classificationConfidence)}.</p>
          <p className="muted">Оценка по правилам извлечения — не вероятность правильности. Для AI оценку даёт модель; это также не гарантия. Проверяйте оригинал.</p>
          {reviewContext.issues.length ? <ul>{reviewContext.issues.map((issue, index) => <li key={`${issue.code}-${issue.fieldName}-${index}`}>{issueDescription(issue)}{!activeOriginalIssues.includes(issue) && <span className="muted"> — исходная причина; ввод изменён, ожидает серверной проверки</span>}</li>)}</ul> : <p>Исходных причин проверки нет.</p>}
        </details>
        <label htmlFor="review-type">Тип документа<select id="review-type" name="review-type" value={selectedType} required aria-required="true" disabled={disabled} onChange={event => changeType(event.target.value as ReviewDraft['type'])} aria-describedby="review-type-hint" aria-invalid={!selectedType}>
          <option value="">Не выбран</option><option value="Invoice">{typeLabels.Invoice}</option><option value="Contract">{typeLabels.Contract}</option><option value="Act">{typeLabels.Act}</option>
        </select></label>
        <p id="review-type-hint" className="review-input-hint">{!selectedType ? 'Выберите тип только по оригиналу. Если документ не поддерживается, сохраните черновик и оставьте его на проверке.' : 'Тип задаёт схему обязательных реквизитов, а не пользовательский вид документа.'}</p>
        {selectedType && selectedType !== reviewContext.type && <p className="review-type-changed">Тип изменён. Сервер проверит новую схему при подтверждении.</p>}
        {selectedType && selectedType !== 'Invoice' && initial.legacyInn && <p className="review-input-hint">ИНН (старая запись, сторона не указана): <strong>{initial.legacyInn}</strong>. Сверьте оригинал и заполните ИНН заказчика и/или исполнителя отдельно; старая запись не назначается стороне автоматически.</p>}
        {serverIssues.filter(issue => !issue.fieldName).map((issue, index) => <p className="review-field-hint" key={`${issue.code}-${index}`}>{issueDescription(issue, true)}</p>)}
        <div className="review-fields">{visibleFields.map(field => {
          const { name, required, hint, maxLength } = field
          const metadata = fieldMetadata.get(name)
          const edited = (values[name] ?? '') !== (initial.originals[name] ?? '')
          const originalIssues = activeOriginalIssues.filter(issue => issue.fieldName === name)
          const validationIssues = serverIssues.filter(issue => issue.fieldName === name)
          const invalid = Boolean(localErrors[name] || validationIssues.length || originalIssues.some(issue => issue.code !== 'LowFieldConfidence'))
          const warning = invalid || originalIssues.length > 0
          const state = localErrors[name] || validationIssues.length ? 'Нужно исправить' : edited ? 'Ожидает проверки сервера' : originalIssues.length ? 'Сверьте с оригиналом' : 'Без замечаний правил'
          return <div className={`review-field${warning ? ' review-field-warning' : ''}`} key={name}>
            <div className="review-field-heading"><label htmlFor={`review-${name}`}>{fieldLabels[name] ?? name}<span className="optional-field-label"> · {required ? 'обязательно' : 'необязательно'}</span></label><span className="review-field-state">{state}</span></div>
            <input id={`review-${name}`} name={name} type={name === 'Date' ? 'date' : 'text'} value={values[name] ?? ''} disabled={disabled} required={required} aria-required={required} maxLength={maxLength} min={name === 'Date' ? '0001-01-01' : undefined} max={name === 'Date' ? '9999-12-31' : undefined}
              inputMode={name === 'Amount' ? 'decimal' : name.endsWith('INN') || name === 'KPP' ? 'numeric' : undefined} pattern={name.endsWith('INN') ? '(?:[0-9]{10}|[0-9]{12})' : name === 'KPP' ? '[0-9]{9}' : undefined}
              placeholder={name === 'Amount' ? '1 234,56' : name === 'Number' ? 'Д-123/26' : undefined} autoComplete="off" aria-invalid={invalid} aria-describedby={`review-${name}-hint${warning ? ` review-${name}-issue` : ''}`}
              onChange={event => changeField(name, event.target.value)} onBlur={event => { if (name === 'Amount' && event.target.value.trim()) { const normalized = normalizeAmount(event.target.value); if (normalized !== event.target.value) changeField(name, normalized) } }} />
            <p className="review-input-hint" id={`review-${name}-hint`}>{hint}</p>
            <div className="review-field-meta"><span>Источник: <strong>{aiAppliedFields.has(name) ? 'AI-подсказка, применена вами' : edited || metadata?.wasManuallyEdited ? 'Ручной ввод' : metadata ? 'Автоматическое извлечение' : 'Не распознано'}</strong></span>
              {!edited && metadata?.confidence != null && <span>Оценка извлечения: <strong>{formatConfidence(metadata.confidence)}</strong></span>}
              {edited && <span>Исходное значение: <strong>{initial.originals[name] || '—'}</strong></span>}
            </div>
            {warning && <div id={`review-${name}-issue`}>{localErrors[name] && <p className="review-field-hint">{localErrors[name]}</p>}{validationIssues.map((issue, index) => <p className="review-field-hint" key={`server-${index}`}>{issueDescription(issue, true)}</p>)}{!localErrors[name] && !validationIssues.length && originalIssues.map((issue, index) => <p className="review-field-hint" key={`original-${index}`}>{issueDescription(issue)}</p>)}</div>}
            {edited && <button className="text-button" type="button" disabled={disabled} onClick={() => changeField(name, initial.originals[name] ?? '')}>Вернуть исходное значение</button>}
          </div>
        })}</div>
      </div>
      <div className="review-tab-panel" id="review-panel-ai" role="tabpanel" aria-labelledby="review-tab-ai" tabIndex={0} hidden={activeTab !== 'ai'}>
        <div className="ai-review-suggestion" id="ai-review">
          <div className="ai-review-heading"><div><h3>Сверка с AI-подсказкой</h3><p className="muted">AI может ошибаться. Сравнивайте предложения с оригиналом рядом. Получение и применение подсказки ничего не сохраняет.</p></div>
            <button className="button button-secondary" type="button" disabled={aiLoading || disabled} onClick={() => void requestAiSuggestion()}>{aiLoading ? 'Получение…' : aiSuggestion ? 'Обновить подсказку' : 'Получить AI-подсказку'}</button>
          </div>
          {aiLoading && <p role="status">Ожидание ответа AI… Реквизиты доступны на соседней вкладке.</p>}
          {aiError && <div className="alert alert-error" role="alert">{aiError}</div>}
          {!aiSuggestion && !aiLoading && !aiError && <p className="muted">Запросите подсказку или заполните реквизиты вручную. AI не обязателен для проверки.</p>}
          {aiSuggestion && <div className="ai-review-result">
            <div className="ai-type-row"><span>Предлагаемый тип: <strong>{typeLabels[aiSuggestion.suggestedType]}</strong> · Оценка модели: {formatConfidence(aiSuggestion.classificationConfidence)}</span>
              {aiSuggestion.suggestedType !== 'Unknown' && <button className="text-button" type="button" disabled={disabled || aiSuggestion.suggestedType === selectedType} onClick={() => { if (aiSuggestion.suggestedType !== 'Unknown') changeType(aiSuggestion.suggestedType) }}>Применить тип</button>}
            </div>
            {aiSuggestion.fields.length ? <div className="ai-fields-table">{aiSuggestion.fields.map((field, index) => <div className="ai-fields-row" key={`${field.name}-${index}`}>
              <strong>{fieldLabels[field.name] ?? field.name}</strong><span>{values[field.name] || '—'}</span><span>{field.value || '—'}</span><span>{formatConfidence(field.confidence)}</span>
              <button className="text-button" type="button" disabled={disabled || !allowedAiFields.has(field.name) || !field.value.trim() || values[field.name] === field.value} title={!allowedAiFields.has(field.name) ? 'Поле не входит в выбранную схему. Сначала проверьте тип документа.' : undefined} onClick={() => changeField(field.name, field.value, true)}>{values[field.name]?.trim() ? 'Заменить это поле' : 'Применить поле'}</button>
            </div>)}</div> : <p className="muted">Модель не предложила значения полей.</p>}
            <p className="muted">Общая кнопка заполняет только пустые поля выбранной схемы. Значения с недопустимым форматом отсеяны; даты и суммы приведены к формату формы. Замена заполненного поля — только отдельной кнопкой рядом с ним. Оценка модели не гарантирует правильность.</p>
            <button className="button button-secondary" type="button" disabled={disabled || !selectedType || !aiSuggestion.fields.some(field => allowedAiFields.has(field.name) && field.value.trim() && !values[field.name]?.trim())} onClick={applyAiToEmptyFields}>Заполнить пустые реквизиты</button>
          </div>}
        </div>
      </div>
      {!hasConflict && error && <div className="alert alert-error" role="alert">{error}</div>}
      {transitionFailed && <button className="button button-secondary" type="button" onClick={() => void finishTransition()}>Повторить переход</button>}
      <div className="review-form-actions">
        <button className="button button-secondary" type="button" disabled={disabled || hasConflict} onClick={() => void saveDraft()}>{operation === 'draft' ? 'Сохранение черновика…' : 'Сохранить черновик'}</button>
        <button className="button" type="submit" name="review-action" value="confirm" disabled={disabled || hasConflict || !selectedType}>{operation === 'review' ? 'Подтверждение…' : reviewed ? 'Документ подтверждён' : 'Подтвердить документ'}</button>
        <button className="button button-secondary" type="submit" name="review-action" value="next" disabled={disabled || hasConflict || !selectedType}>Подтвердить и следующий</button>
      </div>
    </form>
  </section>
}
