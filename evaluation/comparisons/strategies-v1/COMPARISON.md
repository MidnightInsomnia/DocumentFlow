# Strategies v1 — Hybrid Ablation

Abbreviations: **Rules**, **LLM**, **HC** = LLM classifier + Rules extractor, **HE** = Rules classifier + LLM extractor. All strategies share the immutable corpus-v1, text/OCR extraction, validator and router. This report presents measurements and does not select an overall winner.

## Overall

| Metric | Rules | LLM | HC | HE |
|---|---:|---:|---:|---:|
| Classification accuracy | 91.18% | 100% | 100% | 91.18% |
| Field accuracy | 84.73% | 72.60% | 95.07% | 61.61% |
| Exact match | 77.94% | 52.94% | 85.29% | 48.53% |
| Automatic completion | 69.12% | 58.82% | 75% | 48.53% |
| NeedsReview | 30.88% | 25% | 25% | 29.41% |
| Failure | 0% | 16.18% | 0% | 22.06% |
| Average ms | 121.71 | 1254.57 | 284.15 | 953.56 |
| Median ms | 2 | 1344.5 | 188 | 1086 |
| P95 ms | 471 | 1809 | 603 | 1563 |

LLM calls: Rules 0; LLM 2 for a classified known type and 1 for Unknown; HC 1 per document; HE 0 for Rules-Unknown and 1 for Rules-known.

## By source

Values are Classification / Fields / Exact; timing is Average / Median / P95 ms.

| Source | Strategy | Quality | NeedsReview / Failure | Timing |
|---|---|---:|---:|---:|
| TextPdf | Rules | 95.45 / 92.54 / 95.45% | 29.55 / 0% | 1.93 / 2 / 6 |
| TextPdf | LLM | 100 / 90.44 / 77.27% | 22.73 / 4.55% | 1102.52 / 1189 / 1736 |
| TextPdf | HC | 100 / 100 / 100% | 25 / 0% | 181.48 / 185 / 191 |
| TextPdf | HE | 95.45 / 82.35 / 75% | 27.27 / 6.82% | 845.82 / 929.5 / 1545 |
| ScannedPdf | Rules | 83.33 / 72.73 / 50% | 33.33 / 0% | 466.25 / 466 / 493 |
| ScannedPdf | LLM | 100 / 42.86 / 8.33% | 33.33 / 33.33% | 1676 / 1727.5 / 2040 |
| ScannedPdf | HC | 100 / 87.88 / 66.67% | 25 / 0% | 578.58 / 576.5 / 629 |
| ScannedPdf | HE | 83.33 / 29.73 / 0% | 33.33 / 41.67% | 1267.25 / 1360.5 / 1810 |
| Image | Rules | 83.33 / 66.67 / 41.67% | 33.33 / 0% | 216.33 / 212.5 / 231 |
| Image | LLM | 100 / 43.90 / 8.33% | 25 / 41.67% | 1390.67 / 1470 / 1632 |
| Image | HC | 100 / 83.33 / 50% | 25 / 0% | 366.17 / 360.5 / 390 |
| Image | HE | 83.33 / 18.42 / 0% | 33.33 / 58.33% | 1034.92 / 1157.5 / 1439 |

## By difficulty

| Difficulty | Strategy | Classification / Fields / Exact | NeedsReview / Failure |
|---|---|---:|---:|
| Easy | Rules / LLM / HC / HE | 100/100/100 · 100/92.68/84 · 100/100/100 · 100/89.02/80% | 20/0 · 16/4 · 20/0 · 16/8% |
| Medium | Rules / LLM / HC / HE | 100/100/100 · 100/90.24/80 · 100/100/100 · 100/90.24/80% | 26.67/0 · 26.67/6.67 · 26.67/0 · 26.67/6.67% |
| Hard | Rules / LLM / HC / HE | 78.57/61.73/46.43 · 100/47.92/10.71 · 100/87.65/64.29 · 78.57/22.73/3.57% | 42.86/0 · 32.14/32.14 · 28.57/0 · 42.86/42.86% |

Difficulty and source are correlated: OCR documents are Hard, so difficulty is not an independent causal factor.

## Fields

| Field | Rules | LLM | HC | HE |
|---|---:|---:|---:|---:|
| Number | 81.03% | 72.88% | 87.93% | 60.34% |
| Date | 91.38% | 79.66% | 100% | 65.52% |
| INN | 63.16% | 73.68% | 84.21% | 57.89% |
| KPP | 80% | 100% | 100% | 80% |
| Amount | 87.50% | 66.04% | 100% | 54.90% |
| RelatedContractNumber | 100% | 52.63% | 100% | 66.67% |

## Failure reasons

| Strategy | Reason | Count |
|---|---|---:|
| Rules | None | 0 |
| HC | None | 0 |
| LLM | Unsupported/missing field name for Invoice | 5 |
| LLM | Empty Amount | 4 |
| LLM | Empty KPP | 1 |
| LLM | Unsupported/missing field name for Act | 1 |
| HE | Empty Amount | 6 |
| HE | Unsupported/missing field name for Invoice | 5 |
| HE | Invalid field JSON | 4 |

No raw model response or document text is included.

## Case groups

- HC fixes Rules: `invoice-textpdf-004`, `invoice-textpdf-009`, `invoice-scannedpdf-014`, `invoice-image-019`, `act-scannedpdf-015`.
- HC breaks Rules: none.
- HE fixes Rules: none.
- HE breaks Rules: `invoice-textpdf-001`, `invoice-textpdf-005`, `invoice-textpdf-008`, `invoice-textpdf-011`, `invoice-textpdf-012`, `invoice-scannedpdf-015`, `contract-textpdf-004`, `contract-textpdf-012`, `contract-scannedpdf-013`, `contract-scannedpdf-015`, `contract-scannedpdf-016`, `contract-image-017`, `contract-image-019`, `contract-image-020`, `act-textpdf-011`, `act-textpdf-012`, `act-scannedpdf-013`, `act-scannedpdf-016`, `act-image-017`, `act-image-019`.
- Pure LLM wrong, HC correct: `invoice-textpdf-001`, `invoice-textpdf-004`, `invoice-textpdf-005`, `invoice-textpdf-008`, `invoice-textpdf-009`, `invoice-textpdf-011`, `invoice-textpdf-012`, `invoice-scannedpdf-015`, `contract-textpdf-012`, `contract-scannedpdf-013`, `contract-scannedpdf-015`, `contract-scannedpdf-016`, `contract-image-017`, `contract-image-019`, `contract-image-020`, `act-textpdf-011`, `act-textpdf-012`, `act-scannedpdf-013`, `act-scannedpdf-015`, `act-scannedpdf-016`, `act-image-017`, `act-image-019`.

## Determinism

- HC: 68/68 identical structured documents; no differing IDs.
- HE: 68/68 identical structured documents; no differing IDs.

The first run of each strategy remains its baseline. Timing is allowed to vary and was excluded from structured determinism.
