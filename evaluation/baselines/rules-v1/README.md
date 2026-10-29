# Rules Baseline v1

- Algorithm: Rules-based DocumentFlow
- Corpus: `evaluation/corpus-v1`
- Corpus size: 68
- OCR: Tesseract 5.4.0 with `rus+eng`
- Date: 2026-09-22
- Git commit: unavailable (workspace is not a Git repository)

Все 68 документов были выполнены, `Skipped == 0`. Rules classifier, extractor и validator после первого полного прогона не настраивались.

## Text-only provisional vs full run

| Metric | TextPdf provisional | Full Rules v1 |
|---|---:|---:|
| Documents | 44 | 68 |
| Classification accuracy | 95.45% | 91.18% |
| Field accuracy | 92.54% | 84.73% |
| Exact match | 95.45% | 77.94% |
| NeedsReview rate | 29.55% | 30.88% |
| Failure rate | 0% | 0% |
| Average / Median / P95 | 8.82 / 1 / 5 ms | 121.71 / 2 / 471 ms |

Общие показатели нельзя интерпретировать как изолированную оценку OCR: отдельные метрики для TextPdf, ScannedPdf и Image находятся в `EVALUATION-REPORT.md` и `evaluation-summary.json`. Provisional run был выполнен до добавления warm-up, поэтому его timing напрямую не сопоставим с Rules v1.
