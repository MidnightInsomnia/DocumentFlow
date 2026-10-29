import { useState } from 'react'
import type { DocumentType } from '../types/documents'
import { Icon } from './Icon'

interface InnValues {
  inn?: string | null
  customerInn?: string | null
  contractorInn?: string | null
  supplierInn?: string | null
  buyerInn?: string | null
  customerINN?: string | null
  contractorINN?: string | null
  supplierINN?: string | null
  buyerINN?: string | null
}

export interface InnEntry { key: string; label: string; value: string; roleSpecific: boolean }

export function innEntries(type: DocumentType, values: InnValues): InnEntry[] {
  const entries: InnEntry[] = []
  const customer = values.customerInn || values.customerINN
  const contractor = values.contractorInn || values.contractorINN
  const supplierRole = values.supplierInn || values.supplierINN
  const buyer = values.buyerInn || values.buyerINN
  if (type === 'Invoice') {
    const supplier = supplierRole || values.inn
    if (supplier) entries.push({ key: 'SupplierINN', label: 'ИНН поставщика', value: supplier, roleSpecific: true })
    if (buyer) entries.push({ key: 'BuyerINN', label: 'ИНН покупателя', value: buyer, roleSpecific: true })
    if (supplierRole && values.inn && values.inn !== supplierRole)
      entries.push({ key: 'INN', label: 'ИНН (старая запись, отличается от поставщика)', value: values.inn, roleSpecific: false })
  } else {
    if (customer) entries.push({ key: 'CustomerINN', label: 'ИНН заказчика', value: customer, roleSpecific: true })
    if (contractor) entries.push({ key: 'ContractorINN', label: 'ИНН исполнителя', value: contractor, roleSpecific: true })
    if (values.inn) entries.push({ key: 'INN', label: 'ИНН (старая запись, сторона не указана)', value: values.inn, roleSpecific: false })
  }
  return entries
}

const compactLabels: Record<string, string> = {
  CustomerINN: 'Заказчик',
  ContractorINN: 'Исполнитель',
  SupplierINN: 'Поставщик',
  BuyerINN: 'Покупатель',
  INN: 'Без роли',
}

export function InnRoles({ type, values, compact = false }: { type: DocumentType; values: InnValues; compact?: boolean }) {
  const [copiedKey, setCopiedKey] = useState<string | null>(null)
  const [copyErrorKey, setCopyErrorKey] = useState<string | null>(null)
  const entries = innEntries(type, values)
  if (!compact) return entries.length ? <dl className="workspace-business-fields">{entries.map(entry => <div key={entry.key}><dt>{entry.label}</dt><dd>{entry.value}</dd></div>)}</dl> : <span>—</span>

  const visible = entries.filter(entry => entry.key !== 'INN' || !entries.some(role => role.roleSpecific && role.value === entry.value))
  async function copyInn(key: string, label: string, value: string) {
    try {
      await navigator.clipboard.writeText(`${label}: ИНН ${value}`)
      setCopiedKey(key)
      setCopyErrorKey(null)
    } catch {
      setCopiedKey(null)
      setCopyErrorKey(key)
    }
  }
  return visible.length ? <dl className="stitch-inn-compact">{visible.map(entry =>
    <div key={entry.key} title={`${entry.label}: ${entry.value}`}>
      <dt aria-label={entry.label}>{compactLabels[entry.key]}:</dt>
      <dd><span>{entry.value}</span><button type="button" className="stitch-inn-copy" onClick={() => void copyInn(entry.key, compactLabels[entry.key], entry.value)} aria-label={copyErrorKey === entry.key ? `Не удалось скопировать ${entry.label}` : copiedKey === entry.key ? `${entry.label} скопирован` : `Копировать ${entry.label}`} title={copyErrorKey === entry.key ? 'Не удалось скопировать' : copiedKey === entry.key ? 'Скопировано' : `Копировать ${entry.label}`}>
        {copiedKey === entry.key ? <Icon name="check" size={12} /> : <svg width="12" height="12" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" aria-hidden="true"><rect x="8" y="8" width="13" height="13" rx="2" /><path d="M16 8V5a2 2 0 0 0-2-2H5a2 2 0 0 0-2 2v9a2 2 0 0 0 2 2h3" /></svg>}
      </button></dd>
    </div>
  )}</dl> : <span>—</span>
}
