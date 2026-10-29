# DocumentFlow Evaluation Report

Corpus: **42**  
Executed: **42**  
Skipped: **0**

## Overall

- Classification accuracy: 19,05%
- Field accuracy: 0%
- Document exact match: 14,29%
- Automatic completion rate: 0%
- NeedsReview rate: 95,24%
- Failure rate: 4,76%
- Average / Median / P95: 229,40 / 168,5 / 453 ms

## Classification confusion matrix

| Expected \ Actual | Unknown | Invoice | Contract | Act |
|---|---|---|---|---|
| Unknown | 6 | 0 | 0 | 0 |
| Invoice | 12 | 0 | 0 | 0 |
| Contract | 12 | 0 | 0 | 0 |
| Act | 10 | 0 | 0 | 2 |

## Fields

| Field | Expected | Correct | Missing | Wrong | Unexpected | Accuracy |
|---|---:|---:|---:|---:|---:|---:|
| Amount | 28 | 0 | 28 | 0 | 0 | 0% |
| Date | 35 | 0 | 35 | 0 | 0 | 0% |
| INN | 11 | 0 | 11 | 0 | 0 | 0% |
| KPP | 6 | 0 | 6 | 0 | 0 | 0% |
| Number | 35 | 0 | 35 | 0 | 0 | 0% |
| RelatedContractNumber | 6 | 0 | 6 | 0 | 0 | 0% |

## By source type

| Source | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| TextPdf | 18 | 22,22% | 0% | 16,67% | 79,28 | 2 | 1401 | 94,44% | 5,56% |
| ScannedPdf | 12 | 8,33% | 0% | 0% | 501,83 | 414 | 1476 | 91,67% | 8,33% |
| Image | 12 | 25% | 0% | 25% | 182,17 | 178,5 | 223 | 100% | 0% |

## By difficulty

| Difficulty | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Easy | 14 | 14,29% | 0% | 14,29% | 146 | 81,5 | 453 | 100% | 0% |
| Medium | 14 | 14,29% | 0% | 14,29% | 141 | 80 | 430 | 100% | 0% |
| Hard | 14 | 28,57% | 0% | 14,29% | 401,21 | 300 | 1476 | 85,71% | 14,29% |

## Failed / mismatched documents

- `invoice-holdout-textpdf-001` (TextPdf, Easy): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `invoice-holdout-textpdf-002` (TextPdf, Medium): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 5/0/0.
- `invoice-holdout-textpdf-003` (TextPdf, Hard): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `invoice-holdout-textpdf-004` (TextPdf, Easy): Invoice → Unknown; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `invoice-holdout-textpdf-005` (TextPdf, Medium): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `invoice-holdout-scannedpdf-006` (ScannedPdf, Easy): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 5/0/0.
- `invoice-holdout-scannedpdf-007` (ScannedPdf, Medium): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `invoice-holdout-scannedpdf-008` (ScannedPdf, Hard): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 5/0/0.
- `invoice-holdout-scannedpdf-009` (ScannedPdf, Hard): Invoice → Unknown; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `invoice-holdout-image-010` (Image, Easy): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 5/0/0.
- `invoice-holdout-image-011` (Image, Medium): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `invoice-holdout-image-012` (Image, Hard): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 5/0/0.
- `contract-holdout-textpdf-001` (TextPdf, Easy): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-holdout-textpdf-002` (TextPdf, Medium): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-holdout-textpdf-003` (TextPdf, Hard): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-holdout-textpdf-004` (TextPdf, Easy): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-holdout-textpdf-005` (TextPdf, Medium): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-holdout-scannedpdf-006` (ScannedPdf, Easy): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-holdout-scannedpdf-007` (ScannedPdf, Medium): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
- `contract-holdout-scannedpdf-008` (ScannedPdf, Hard): Contract → Unknown; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 1/0/0.
- `contract-holdout-scannedpdf-009` (ScannedPdf, Hard): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
- `contract-holdout-image-010` (Image, Easy): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
- `contract-holdout-image-011` (Image, Medium): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
- `contract-holdout-image-012` (Image, Hard): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
- `act-holdout-textpdf-001` (TextPdf, Easy): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-holdout-textpdf-002` (TextPdf, Medium): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-holdout-textpdf-003` (TextPdf, Hard): Act → Act; Completed → Failed; fields missing/wrong/unexpected: 4/0/0.
- `act-holdout-textpdf-004` (TextPdf, Easy): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-holdout-textpdf-005` (TextPdf, Medium): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-holdout-scannedpdf-006` (ScannedPdf, Easy): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-holdout-scannedpdf-007` (ScannedPdf, Medium): Act → Unknown; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
- `act-holdout-scannedpdf-008` (ScannedPdf, Hard): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-holdout-scannedpdf-009` (ScannedPdf, Hard): Act → Act; Completed → Failed; fields missing/wrong/unexpected: 3/0/0.
- `act-holdout-image-010` (Image, Easy): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-holdout-image-011` (Image, Medium): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-holdout-image-012` (Image, Hard): Act → Unknown; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
