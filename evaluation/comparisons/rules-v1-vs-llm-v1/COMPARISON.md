# Rules v1 vs LLM v1

Both strategies use the same immutable `corpus-v1`, PdfPig/Tesseract extraction, validator and router. Only classification and structured field extraction differ. Delta is LLM minus Rules; this report presents measurements and does not declare an automatic winner.

## Environment

- CPU: 12th Gen Intel Core i7-12700K
- RAM: 31.77 GiB
- GPU: NVIDIA GeForce RTX 4070, 12,282 MiB VRAM
- LLM: Ollama 0.34.2, `qwen2.5:7b-instruct`, Q4_K_M

## Overall

| Metric | Rules v1 | LLM v1 | Delta |
|---|---:|---:|---:|
| Classification accuracy | 91.18% | 100% | +8.82 pp |
| Field accuracy | 84.73% | 72.60% | -12.13 pp |
| Exact match | 77.94% | 52.94% | -25.00 pp |
| Automatic completion | 69.12% | 58.82% | -10.30 pp |
| NeedsReview | 30.88% | 25.00% | -5.88 pp |
| Failure | 0% | 16.18% | +16.18 pp |
| Average ms | 121.71 | 1254.57 | +1132.86 |
| Median ms | 2 | 1344.5 | +1342.5 |
| P95 ms | 471 | 1809 | +1338 |

## By source

| Source | Metric | Rules | LLM | Delta |
|---|---|---:|---:|---:|
| TextPdf | Classification / Fields / Exact | 95.45 / 92.54 / 95.45% | 100 / 90.44 / 77.27% | +4.55 / -2.10 / -18.18 pp |
| TextPdf | NeedsReview / Failure | 29.55 / 0% | 22.73 / 4.55% | -6.82 / +4.55 pp |
| TextPdf | Avg / Median / P95 ms | 1.93 / 2 / 6 | 1102.52 / 1189 / 1736 | +1100.59 / +1187 / +1730 |
| ScannedPdf | Classification / Fields / Exact | 83.33 / 72.73 / 50% | 100 / 42.86 / 8.33% | +16.67 / -29.87 / -41.67 pp |
| ScannedPdf | NeedsReview / Failure | 33.33 / 0% | 33.33 / 33.33% | 0 / +33.33 pp |
| ScannedPdf | Avg / Median / P95 ms | 466.25 / 466 / 493 | 1676 / 1727.5 / 2040 | +1209.75 / +1261.5 / +1547 |
| Image | Classification / Fields / Exact | 83.33 / 66.67 / 41.67% | 100 / 43.90 / 8.33% | +16.67 / -22.77 / -33.34 pp |
| Image | NeedsReview / Failure | 33.33 / 0% | 25 / 41.67% | -8.33 / +41.67 pp |
| Image | Avg / Median / P95 ms | 216.33 / 212.5 / 231 | 1390.67 / 1470 / 1632 | +1174.34 / +1257.5 / +1401 |

## By difficulty

| Difficulty | Rules Classification / Fields / Exact | LLM Classification / Fields / Exact | Delta |
|---|---:|---:|---:|
| Easy | 100 / 100 / 100% | 100 / 92.68 / 84% | 0 / -7.32 / -16.00 pp |
| Medium | 100 / 100 / 100% | 100 / 90.24 / 80% | 0 / -9.76 / -20.00 pp |
| Hard | 78.57 / 61.73 / 46.43% | 100 / 47.92 / 10.71% | +21.43 / -13.81 / -35.72 pp |

Difficulty and source kind are not independent in corpus-v1: all OCR documents are Hard. Difficulty metrics must not be interpreted as a pure isolated effect of difficulty.

## Fields

| Field | Rules | LLM | Delta |
|---|---:|---:|---:|
| Number | 81.03% | 72.88% | -8.15 pp |
| Date | 91.38% | 79.66% | -11.72 pp |
| INN | 63.16% | 73.68% | +10.52 pp |
| KPP | 80% | 100% | +20.00 pp |
| Amount | 87.50% | 66.04% | -21.46 pp |
| RelatedContractNumber | 100% | 52.63% | -47.37 pp |

## Confusion matrices

Rules:

| Expected / Actual | Unknown | Invoice | Contract | Act |
|---|---:|---:|---:|---:|
| Unknown | 8 | 0 | 0 | 0 |
| Invoice | 4 | 16 | 0 | 0 |
| Contract | 0 | 0 | 20 | 0 |
| Act | 2 | 0 | 0 | 18 |

LLM:

| Expected / Actual | Unknown | Invoice | Contract | Act |
|---|---:|---:|---:|---:|
| Unknown | 8 | 0 | 0 | 0 |
| Invoice | 0 | 20 | 0 | 0 |
| Contract | 0 | 0 | 20 | 0 |
| Act | 0 | 0 | 0 | 20 |

## Per-document cases

LLM fixed a Rules exact-match error: `invoice-scannedpdf-014`, `invoice-image-019`.

Rules correct, LLM wrong: `invoice-textpdf-001`, `invoice-textpdf-005`, `invoice-textpdf-008`, `invoice-textpdf-011`, `invoice-textpdf-012`, `invoice-scannedpdf-015`, `contract-textpdf-012`, `contract-scannedpdf-013`, `contract-scannedpdf-015`, `contract-scannedpdf-016`, `contract-image-017`, `contract-image-019`, `contract-image-020`, `act-textpdf-011`, `act-textpdf-012`, `act-scannedpdf-013`, `act-scannedpdf-016`, `act-image-017`, `act-image-019`.

Both wrong: `invoice-textpdf-004`, `invoice-textpdf-009`, `invoice-scannedpdf-013`, `invoice-scannedpdf-016`, `invoice-image-017`, `invoice-image-018`, `invoice-image-020`, `contract-scannedpdf-014`, `contract-image-018`, `act-scannedpdf-014`, `act-scannedpdf-015`, `act-image-018`, `act-image-020`.

Detailed rows without document text are in `comparison-documents.csv`.

## Determinism

Structured output was identical for 66/68 documents (97.06%). Differences: `invoice-scannedpdf-013` and `invoice-image-017`. The first frozen-prompt run remains the baseline; quality metrics from two runs were not averaged.
