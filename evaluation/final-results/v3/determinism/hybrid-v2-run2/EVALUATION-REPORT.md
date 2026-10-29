# DocumentFlow Evaluation Report

Corpus: **72**  
Executed: **72**  
Skipped: **0**

## Overall

- Classification accuracy: 97,22%
- Field accuracy: 4,81%
- Document exact match: 13,89%
- Automatic completion rate: 0%
- NeedsReview rate: 100%
- Failure rate: 0%
- Average / Median / P95: 457,88 / 451,5 / 1068 ms

## Classification confusion matrix

| Expected \ Actual | Unknown | Invoice | Contract | Act |
|---|---|---|---|---|
| Unknown | 10 | 0 | 0 | 2 |
| Invoice | 0 | 20 | 0 | 0 |
| Contract | 0 | 0 | 20 | 0 |
| Act | 0 | 0 | 0 | 20 |

## Fields

| Field | Expected | Correct | Missing | Wrong | Unexpected | Accuracy |
|---|---:|---:|---:|---:|---:|---:|
| Amount | 52 | 0 | 52 | 0 | 0 | 0% |
| Date | 58 | 0 | 58 | 0 | 0 | 0% |
| INN | 19 | 0 | 19 | 0 | 0 | 0% |
| KPP | 10 | 10 | 0 | 0 | 0 | 100% |
| Number | 59 | 0 | 39 | 20 | 0 | 0% |
| RelatedContractNumber | 10 | 0 | 10 | 0 | 0 | 0% |

## By source type

| Source | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| TextPdf | 24 | 95,83% | 4,29% | 12,5% | 181,29 | 185 | 194 | 100% | 0% |
| ScannedPdf | 24 | 100% | 5,63% | 16,67% | 752,46 | 696,5 | 1100 | 100% | 0% |
| Image | 24 | 95,83% | 4,48% | 12,5% | 439,88 | 451,5 | 485 | 100% | 0% |

## By difficulty

| Difficulty | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Easy | 24 | 95,83% | 4,55% | 12,5% | 493,67 | 442 | 1100 | 100% | 0% |
| Medium | 24 | 100% | 5,56% | 16,67% | 435,79 | 456,5 | 721 | 100% | 0% |
| Hard | 24 | 95,83% | 4,29% | 12,5% | 444,17 | 462 | 717 | 100% | 0% |

## Failed / mismatched documents

- `invoice-final-textpdf-001` (TextPdf, Easy): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/1/0.
- `invoice-final-textpdf-002` (TextPdf, Medium): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/1/0.
- `invoice-final-textpdf-003` (TextPdf, Hard): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/1/0.
- `invoice-final-textpdf-004` (TextPdf, Easy): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/1/0.
- `invoice-final-textpdf-005` (TextPdf, Medium): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/1/0.
- `invoice-final-textpdf-006` (TextPdf, Hard): Invoice → Invoice; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 2/1/0.
- `invoice-final-textpdf-007` (TextPdf, Easy): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/1/0.
- `invoice-final-scannedpdf-008` (ScannedPdf, Medium): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/1/0.
- `invoice-final-scannedpdf-009` (ScannedPdf, Hard): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/1/0.
- `invoice-final-scannedpdf-010` (ScannedPdf, Easy): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/1/0.
- `invoice-final-scannedpdf-011` (ScannedPdf, Medium): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/1/0.
- `invoice-final-scannedpdf-012` (ScannedPdf, Hard): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/1/0.
- `invoice-final-scannedpdf-013` (ScannedPdf, Easy): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/1/0.
- `invoice-final-scannedpdf-014` (ScannedPdf, Medium): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/1/0.
- `invoice-final-image-015` (Image, Hard): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/1/0.
- `invoice-final-image-016` (Image, Easy): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/1/0.
- `invoice-final-image-017` (Image, Medium): Invoice → Invoice; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 2/1/0.
- `invoice-final-image-018` (Image, Hard): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/1/0.
- `invoice-final-image-019` (Image, Easy): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/1/0.
- `invoice-final-image-020` (Image, Medium): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 3/1/0.
- `contract-final-textpdf-001` (TextPdf, Medium): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-final-textpdf-002` (TextPdf, Hard): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-final-textpdf-003` (TextPdf, Easy): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
- `contract-final-textpdf-004` (TextPdf, Medium): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-final-textpdf-005` (TextPdf, Hard): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-final-textpdf-006` (TextPdf, Easy): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
- `contract-final-textpdf-007` (TextPdf, Medium): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-final-scannedpdf-008` (ScannedPdf, Hard): Contract → Contract; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
- `contract-final-scannedpdf-009` (ScannedPdf, Easy): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
- `contract-final-scannedpdf-010` (ScannedPdf, Medium): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-final-scannedpdf-011` (ScannedPdf, Hard): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-final-scannedpdf-012` (ScannedPdf, Easy): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
- `contract-final-scannedpdf-013` (ScannedPdf, Medium): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-final-image-014` (Image, Hard): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-final-image-015` (Image, Easy): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
- `contract-final-image-016` (Image, Medium): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-final-image-017` (Image, Hard): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-final-image-018` (Image, Easy): Contract → Contract; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 1/0/0.
- `contract-final-image-019` (Image, Medium): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-final-image-020` (Image, Hard): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-final-textpdf-001` (TextPdf, Hard): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-final-textpdf-002` (TextPdf, Easy): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-final-textpdf-003` (TextPdf, Medium): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-final-textpdf-004` (TextPdf, Hard): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-final-textpdf-005` (TextPdf, Easy): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-final-textpdf-006` (TextPdf, Medium): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-final-scannedpdf-007` (ScannedPdf, Hard): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-final-scannedpdf-008` (ScannedPdf, Easy): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-final-scannedpdf-009` (ScannedPdf, Medium): Act → Act; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-final-scannedpdf-010` (ScannedPdf, Hard): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-final-scannedpdf-011` (ScannedPdf, Easy): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-final-scannedpdf-012` (ScannedPdf, Medium): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-final-scannedpdf-013` (ScannedPdf, Hard): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-final-image-014` (Image, Easy): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-final-image-015` (Image, Medium): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-final-image-016` (Image, Hard): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-final-image-017` (Image, Easy): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-final-image-018` (Image, Medium): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-final-image-019` (Image, Hard): Act → Act; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-final-image-020` (Image, Easy): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `unknown-final-textpdf-004` (TextPdf, Easy): Unknown → Act; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 0/0/0.
- `unknown-final-image-012` (Image, Hard): Unknown → Act; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 0/0/0.
