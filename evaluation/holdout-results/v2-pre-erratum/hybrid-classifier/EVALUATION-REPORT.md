# DocumentFlow Evaluation Report

Corpus: **42**  
Executed: **42**  
Skipped: **0**

## Overall

- Classification accuracy: 100%
- Field accuracy: 14,05%
- Document exact match: 14,29%
- Automatic completion rate: 0%
- NeedsReview rate: 100%
- Failure rate: 0%
- Average / Median / P95: 355,52 / 341,5 / 612 ms

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
| Amount | 28 | 0 | 28 | 0 | 0 | 0% |
| Date | 35 | 0 | 35 | 0 | 0 | 0% |
| INN | 11 | 11 | 0 | 0 | 0 | 100% |
| KPP | 6 | 6 | 0 | 0 | 0 | 100% |
| Number | 35 | 0 | 35 | 0 | 0 | 0% |
| RelatedContractNumber | 6 | 0 | 6 | 0 | 0 | 0% |

## By source type

| Source | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| TextPdf | 18 | 100% | 12,5% | 16,67% | 188,72 | 190,5 | 201 | 100% | 0% |
| ScannedPdf | 12 | 100% | 13,51% | 0% | 591,58 | 582 | 645 | 100% | 0% |
| Image | 12 | 100% | 17,86% | 25% | 369,67 | 370 | 417 | 100% | 0% |

## By difficulty

| Difficulty | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Easy | 14 | 100% | 15,91% | 14,29% | 332 | 261,5 | 645 | 100% | 0% |
| Medium | 14 | 100% | 12,5% | 14,29% | 324,57 | 260 | 612 | 100% | 0% |
| Hard | 14 | 100% | 13,51% | 14,29% | 410 | 398,5 | 632 | 100% | 0% |

## Failed / mismatched documents

- `invoice-holdout-textpdf-001` (TextPdf, Easy): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `invoice-holdout-textpdf-002` (TextPdf, Medium): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `invoice-holdout-textpdf-003` (TextPdf, Hard): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `invoice-holdout-textpdf-004` (TextPdf, Easy): Invoice → Invoice; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
- `invoice-holdout-textpdf-005` (TextPdf, Medium): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `invoice-holdout-scannedpdf-006` (ScannedPdf, Easy): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `invoice-holdout-scannedpdf-007` (ScannedPdf, Medium): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `invoice-holdout-scannedpdf-008` (ScannedPdf, Hard): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `invoice-holdout-scannedpdf-009` (ScannedPdf, Hard): Invoice → Invoice; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `invoice-holdout-image-010` (Image, Easy): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `invoice-holdout-image-011` (Image, Medium): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `invoice-holdout-image-012` (Image, Hard): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-holdout-textpdf-001` (TextPdf, Easy): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-holdout-textpdf-002` (TextPdf, Medium): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-holdout-textpdf-003` (TextPdf, Hard): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-holdout-textpdf-004` (TextPdf, Easy): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-holdout-textpdf-005` (TextPdf, Medium): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-holdout-scannedpdf-006` (ScannedPdf, Easy): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-holdout-scannedpdf-007` (ScannedPdf, Medium): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
- `contract-holdout-scannedpdf-008` (ScannedPdf, Hard): Contract → Contract; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 1/0/0.
- `contract-holdout-scannedpdf-009` (ScannedPdf, Hard): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
- `contract-holdout-image-010` (Image, Easy): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
- `contract-holdout-image-011` (Image, Medium): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
- `contract-holdout-image-012` (Image, Hard): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
- `act-holdout-textpdf-001` (TextPdf, Easy): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-holdout-textpdf-002` (TextPdf, Medium): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-holdout-textpdf-003` (TextPdf, Hard): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-holdout-textpdf-004` (TextPdf, Easy): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-holdout-textpdf-005` (TextPdf, Medium): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-holdout-scannedpdf-006` (ScannedPdf, Easy): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-holdout-scannedpdf-007` (ScannedPdf, Medium): Act → Act; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
- `act-holdout-scannedpdf-008` (ScannedPdf, Hard): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-holdout-scannedpdf-009` (ScannedPdf, Hard): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-holdout-image-010` (Image, Easy): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-holdout-image-011` (Image, Medium): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-holdout-image-012` (Image, Hard): Act → Act; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
