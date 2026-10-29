# DocumentFlow Evaluation Report

Corpus: **42**  
Executed: **42**  
Skipped: **0**

## Overall

- Classification accuracy: 100%
- Field accuracy: 39,69%
- Document exact match: 26,19%
- Automatic completion rate: 19,05%
- NeedsReview rate: 33,33%
- Failure rate: 47,62%
- Average / Median / P95: 1311,67 / 1365 / 1988 ms

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
| Amount | 28 | 11 | 17 | 0 | 3 | 35,48% |
| Date | 35 | 15 | 20 | 0 | 1 | 41,67% |
| INN | 11 | 7 | 4 | 0 | 0 | 63,64% |
| KPP | 6 | 5 | 1 | 0 | 2 | 62,5% |
| Number | 35 | 14 | 20 | 1 | 1 | 38,89% |
| RelatedContractNumber | 6 | 0 | 6 | 0 | 3 | 0% |

## By source type

| Source | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| TextPdf | 18 | 100% | 28,07% | 27,78% | 1130,72 | 1253,5 | 1755 | 27,78% | 61,11% |
| ScannedPdf | 12 | 100% | 34,15% | 16,67% | 1698,25 | 1640,5 | 2201 | 16,67% | 58,33% |
| Image | 12 | 100% | 66,67% | 33,33% | 1196,5 | 1210 | 1915 | 58,33% | 16,67% |

## By difficulty

| Difficulty | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Easy | 14 | 100% | 54,35% | 42,86% | 1286,14 | 1329 | 2197 | 28,57% | 35,71% |
| Medium | 14 | 100% | 26,19% | 21,43% | 1284,71 | 1366,5 | 1988 | 28,57% | 64,29% |
| Hard | 14 | 100% | 37,21% | 14,29% | 1364,14 | 1365 | 2201 | 42,86% | 42,86% |

## Failed / mismatched documents

- `invoice-holdout-textpdf-001` (TextPdf, Easy): Invoice → Invoice; Completed → Failed; fields missing/wrong/unexpected: 4/0/0.
- `invoice-holdout-textpdf-003` (TextPdf, Hard): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
- `invoice-holdout-textpdf-004` (TextPdf, Easy): Invoice → Invoice; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 0/0/0.
- `invoice-holdout-textpdf-005` (TextPdf, Medium): Invoice → Invoice; Completed → Failed; fields missing/wrong/unexpected: 4/0/0.
- `invoice-holdout-scannedpdf-007` (ScannedPdf, Medium): Invoice → Invoice; Completed → Failed; fields missing/wrong/unexpected: 4/0/0.
- `invoice-holdout-scannedpdf-008` (ScannedPdf, Hard): Invoice → Invoice; Completed → Failed; fields missing/wrong/unexpected: 5/0/0.
- `invoice-holdout-scannedpdf-009` (ScannedPdf, Hard): Invoice → Invoice; NeedsReview → Failed; fields missing/wrong/unexpected: 3/0/0.
- `invoice-holdout-image-011` (Image, Medium): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
- `invoice-holdout-image-012` (Image, Hard): Invoice → Invoice; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `contract-holdout-textpdf-001` (TextPdf, Easy): Contract → Contract; Completed → Failed; fields missing/wrong/unexpected: 3/0/0.
- `contract-holdout-textpdf-002` (TextPdf, Medium): Contract → Contract; Completed → Failed; fields missing/wrong/unexpected: 3/0/0.
- `contract-holdout-textpdf-003` (TextPdf, Hard): Contract → Contract; Completed → Failed; fields missing/wrong/unexpected: 3/0/0.
- `contract-holdout-textpdf-005` (TextPdf, Medium): Contract → Contract; Completed → Failed; fields missing/wrong/unexpected: 3/0/0.
- `contract-holdout-scannedpdf-007` (ScannedPdf, Medium): Contract → Contract; Completed → Failed; fields missing/wrong/unexpected: 2/0/0.
- `contract-holdout-scannedpdf-008` (ScannedPdf, Hard): Contract → Contract; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 0/0/2.
- `contract-holdout-scannedpdf-009` (ScannedPdf, Hard): Contract → Contract; Completed → Failed; fields missing/wrong/unexpected: 2/0/0.
- `contract-holdout-image-010` (Image, Easy): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
- `contract-holdout-image-011` (Image, Medium): Contract → Contract; Completed → Failed; fields missing/wrong/unexpected: 2/0/0.
- `contract-holdout-image-012` (Image, Hard): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
- `act-holdout-textpdf-001` (TextPdf, Easy): Act → Act; Completed → Failed; fields missing/wrong/unexpected: 4/0/0.
- `act-holdout-textpdf-002` (TextPdf, Medium): Act → Act; Completed → Failed; fields missing/wrong/unexpected: 4/0/0.
- `act-holdout-textpdf-003` (TextPdf, Hard): Act → Act; Completed → Failed; fields missing/wrong/unexpected: 4/0/0.
- `act-holdout-textpdf-004` (TextPdf, Easy): Act → Act; Completed → Failed; fields missing/wrong/unexpected: 4/0/0.
- `act-holdout-textpdf-005` (TextPdf, Medium): Act → Act; Completed → Failed; fields missing/wrong/unexpected: 4/0/0.
- `act-holdout-scannedpdf-006` (ScannedPdf, Easy): Act → Act; Completed → Failed; fields missing/wrong/unexpected: 4/0/0.
- `act-holdout-scannedpdf-007` (ScannedPdf, Medium): Act → Act; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
- `act-holdout-scannedpdf-008` (ScannedPdf, Hard): Act → Act; Completed → Completed; fields missing/wrong/unexpected: 0/0/1.
- `act-holdout-scannedpdf-009` (ScannedPdf, Hard): Act → Act; Completed → Failed; fields missing/wrong/unexpected: 3/0/0.
- `act-holdout-image-010` (Image, Easy): Act → Act; Completed → Completed; fields missing/wrong/unexpected: 0/0/1.
- `act-holdout-image-011` (Image, Medium): Act → Act; Completed → Failed; fields missing/wrong/unexpected: 3/0/0.
- `act-holdout-image-012` (Image, Hard): Act → Act; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
