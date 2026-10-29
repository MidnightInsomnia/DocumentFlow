# DocumentFlow Evaluation Report

Corpus: **42**  
Executed: **42**  
Skipped: **0**

## Overall

- Classification accuracy: 100%
- Field accuracy: 95,87%
- Document exact match: 88,10%
- Automatic completion rate: 73,81%
- NeedsReview rate: 26,19%
- Failure rate: 0%
- Average / Median / P95: 355,74 / 333,5 / 629 ms

## Classification confusion matrix

| Expected \ Actual | Unknown | Invoice | Contract | Act |
|---|---|---|---|---|
| Unknown | 6 | 0 | 0 | 0 |
| Invoice | 0 | 12 | 0 | 0 |
| Contract | 0 | 0 | 12 | 0 |
| Act | 0 | 0 | 0 | 12 |

## Fields

| Field | Expected | Correct | Missing | Wrong | Unexpected | Accuracy |
|---|---:|---:|---:|---:|---:|---:|
| Amount | 28 | 28 | 0 | 0 | 0 | 100% |
| Date | 35 | 35 | 0 | 0 | 0 | 100% |
| INN | 11 | 11 | 0 | 0 | 0 | 100% |
| KPP | 6 | 6 | 0 | 0 | 0 | 100% |
| Number | 35 | 31 | 0 | 4 | 0 | 88,57% |
| RelatedContractNumber | 6 | 5 | 0 | 1 | 0 | 83,33% |

## By source type

| Source | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| TextPdf | 18 | 100% | 100% | 100% | 179,44 | 179,5 | 197 | 22,22% | 0% |
| ScannedPdf | 12 | 100% | 94,59% | 83,33% | 609,42 | 605,5 | 660 | 25% | 0% |
| Image | 12 | 100% | 89,29% | 75% | 366,5 | 362 | 429 | 33,33% | 0% |

## By difficulty

| Difficulty | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Easy | 14 | 100% | 97,73% | 92,86% | 329,71 | 262 | 659 | 21,43% | 0% |
| Medium | 14 | 100% | 92,5% | 78,57% | 322,43 | 253,5 | 629 | 21,43% | 0% |
| Hard | 14 | 100% | 97,30% | 92,86% | 415,07 | 393,5 | 660 | 35,71% | 0% |

## Failed / mismatched documents

- `invoice-holdout-image-012` (Image, Hard): Invoice → Invoice; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `contract-holdout-scannedpdf-007` (ScannedPdf, Medium): Contract → Contract; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `contract-holdout-image-011` (Image, Medium): Contract → Contract; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `act-holdout-scannedpdf-006` (ScannedPdf, Easy): Act → Act; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `act-holdout-image-011` (Image, Medium): Act → Act; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
