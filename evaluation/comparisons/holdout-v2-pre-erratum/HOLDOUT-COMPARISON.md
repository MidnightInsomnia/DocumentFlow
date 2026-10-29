# Holdout Corpus V2 — Strategy Comparison

Corpus-v2 was fully generated, annotated, hash-validated and frozen before any strategy was run. The four frozen strategies were executed in this order: Rules, Pure LLM, Hybrid Classifier (HC), Hybrid Extractor (HE). Every run executed 42 documents with 0 skipped. No algorithm, prompt, corpus annotation or threshold was changed after results were observed.

## Overall holdout results

| Metric | Rules | LLM | HC | HE |
|---|---:|---:|---:|---:|
| Classification accuracy | 19.05% | 100% | 100% | 19.05% |
| Field accuracy | 0% | 39.69% | 14.05% | 0% |
| Exact match | 14.29% | 26.19% | 14.29% | 14.29% |
| Automatic completion | 0% | 19.05% | 0% | 0% |
| NeedsReview | 100% | 33.33% | 100% | 95.24% |
| Failure | 0% | 47.62% | 0% | 4.76% |
| Average ms | 168.14 | 1311.67 | 355.52 | 230.05 |
| Median ms | 154 | 1365 | 341.5 | 166.5 |
| P95 ms | 425 | 1988 | 612 | 463 |

## By source

Values are Classification / Fields / Exact; timing is Average / Median / P95 ms.

| Source | Strategy | Quality | NeedsReview / Failure | Timing |
|---|---|---:|---:|---:|
| TextPdf | Rules | 22.22 / 0 / 16.67% | 100 / 0% | 1.83 / 1 / 8 |
| TextPdf | LLM | 100 / 28.07 / 27.78% | 27.78 / 61.11% | 1130.72 / 1253.5 / 1755 |
| TextPdf | HC | 100 / 12.50 / 16.67% | 100 / 0% | 188.72 / 190.5 / 201 |
| TextPdf | HE | 22.22 / 0 / 16.67% | 94.44 / 5.56% | 82.72 / 1 / 1467 |
| ScannedPdf | Rules | 8.33 / 0 / 0% | 100 / 0% | 404.42 / 398 / 446 |
| ScannedPdf | LLM | 100 / 34.15 / 16.67% | 16.67 / 58.33% | 1698.25 / 1640.5 / 2201 |
| ScannedPdf | HC | 100 / 13.51 / 0% | 100 / 0% | 591.58 / 582 / 645 |
| ScannedPdf | HE | 8.33 / 0 / 0% | 91.67 / 8.33% | 500.25 / 407 / 1495 |
| Image | Rules | 25 / 0 / 25% | 100 / 0% | 181.33 / 179.5 / 226 |
| Image | LLM | 100 / 66.67 / 33.33% | 58.33 / 16.67% | 1196.50 / 1210 / 1915 |
| Image | HC | 100 / 17.86 / 25% | 100 / 0% | 369.67 / 370 / 417 |
| Image | HE | 25 / 0 / 25% | 100 / 0% | 180.83 / 178 / 224 |

## By difficulty

| Difficulty | Strategy | Classification / Fields / Exact | NeedsReview / Failure |
|---|---|---:|---:|
| Easy | Rules / LLM / HC / HE | 14.29/0/14.29 · 100/54.35/42.86 · 100/15.91/14.29 · 14.29/0/14.29% | 100/0 · 28.57/35.71 · 100/0 · 100/0% |
| Medium | Rules / LLM / HC / HE | 14.29/0/14.29 · 100/26.19/21.43 · 100/12.50/14.29 · 14.29/0/14.29% | 100/0 · 28.57/64.29 · 100/0 · 100/0% |
| Hard | Rules / LLM / HC / HE | 28.57/0/14.29 · 100/37.21/14.29 · 100/13.51/14.29 · 28.57/0/14.29% | 100/0 · 42.86/42.86 · 100/0 · 85.71/14.29% |

Easy, Medium and Hard occur in every source kind, reducing the source/difficulty coupling from corpus-v1. The 42-document sample still does not support strong causal claims.

## Fields

| Field | Rules | LLM | HC | HE |
|---|---:|---:|---:|---:|
| Number | 0% | 38.89% | 0% | 0% |
| Date | 0% | 41.67% | 0% | 0% |
| INN | 0% | 63.64% | 100% | 0% |
| KPP | 0% | 62.50% | 100% | 0% |
| Amount | 0% | 35.48% | 0% | 0% |
| RelatedContractNumber | 0% | 0% | 0% | 0% |

## Failure reasons

| Strategy | Reason | Count |
|---|---|---:|
| Rules | None | 0 |
| HC | None | 0 |
| LLM | Invalid field JSON | 8 |
| LLM | Unsupported/missing field name for Act | 4 |
| LLM | Empty Amount | 3 |
| LLM | Empty KPP | 2 |
| LLM | Unsupported/missing field name for Invoice | 2 |
| LLM | Unsupported RelatedContractNumber for Invoice | 1 |
| HE | Invalid field JSON | 2 |

No raw model response or document text is included.

## Corpus-v1 to holdout-v2 generalization gaps

Gap is Holdout-v2 minus Corpus-v1 in percentage points; datasets are not pooled.

| Strategy | Classification | Fields | Exact | Failure |
|---|---:|---:|---:|---:|
| Rules | -72.13 | -84.73 | -63.65 | 0.00 |
| LLM | 0.00 | -32.91 | -26.75 | +31.44 |
| HC | 0.00 | -81.02 | -71.00 | 0.00 |
| HE | -72.13 | -61.61 | -34.24 | -17.30 |

## Rules vs HC paired cases

- HC fixes Rules: none.
- HC breaks Rules: none.
- Both correct: the six Unknown documents.
- Both wrong: all 36 known-type documents.

HC classification remained 100%, but its frozen Rules extractor did not generalize to the new labels/layouts, so all known-type documents required review. Per-document data is stored in `holdout-comparison-documents.csv`.

## HC determinism

The second HC run produced identical type, status, department and fields for **42/42** documents. No IDs differed. Timing was excluded from structured determinism.
