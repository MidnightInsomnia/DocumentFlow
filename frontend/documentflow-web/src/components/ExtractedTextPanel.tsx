import { useState, type ReactNode } from 'react'

export function ExtractedTextPanel({ text }: { text: string | null }) {
  const [query, setQuery] = useState('')
  const source = text || 'Распознанный текст отсутствует.'
  const needle = query.trim()
  const fragments: ReactNode[] = []
  let matches = 0
  if (needle) {
    const lower = source.toLocaleLowerCase('ru-RU')
    const search = needle.toLocaleLowerCase('ru-RU')
    let from = 0
    let at = lower.indexOf(search, from)
    while (at >= 0) {
      fragments.push(source.slice(from, at), <mark key={at}>{source.slice(at, at + needle.length)}</mark>)
      matches++
      from = at + needle.length
      at = lower.indexOf(search, from)
    }
    fragments.push(source.slice(from))
  }
  return <details className="card extracted-text-panel">
    <summary>Распознанный текст</summary>
    <div className="extracted-search">
      <label htmlFor="extracted-text-search">Поиск по распознанному тексту</label>
      <input id="extracted-text-search" type="search" value={query} onChange={(event) => setQuery(event.target.value)} placeholder="Номер, сумма или слово из OCR" />
      {needle && <span className="muted" aria-live="polite">Совпадений: {matches}</span>}
    </div>
    <pre className="extracted-text">{needle ? fragments : source}</pre>
  </details>
}
