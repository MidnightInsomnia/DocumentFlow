# DocumentFlow Evaluation Report

Corpus: **72**  
Executed: **72**  
Skipped: **0**

## Overall

- Classification accuracy: 16,67%
- Field accuracy: 0%
- Document exact match: 16,67%
- Automatic completion rate: 0%
- NeedsReview rate: 100%
- Failure rate: 0%
- Average / Median / P95: 280,83 / 269 / 904 ms

## Classification confusion matrix

| Expected \ Actual | Unknown | Invoice | Contract | Act |
|---|---|---|---|---|
| Unknown | 12 | 0 | 0 | 0 |
| Invoice | 20 | 0 | 0 | 0 |
| Contract | 20 | 0 | 0 | 0 |
| Act | 20 | 0 | 0 | 0 |

## Fields

| Field | Expected | Correct | Missing | Wrong | Unexpected | Accuracy |
|---|---:|---:|---:|---:|---:|---:|
| Amount | 52 | 0 | 52 | 0 | 0 | 0% |
| Date | 58 | 0 | 58 | 0 | 0 | 0% |
| INN | 19 | 0 | 19 | 0 | 0 | 0% |
| KPP | 10 | 0 | 10 | 0 | 0 | 0% |
| Number | 59 | 0 | 59 | 0 | 0 | 0% |
| RelatedContractNumber | 10 | 0 | 10 | 0 | 0 | 0% |

## By source type

| Source | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| TextPdf | 24 | 16,67% | 0% | 16,67% | 1,17 | 1 | 2 | 100% | 0% |
| ScannedPdf | 24 | 16,67% | 0% | 16,67% | 581,25 | 517 | 935 | 100% | 0% |
| Image | 24 | 16,67% | 0% | 16,67% | 260,08 | 269 | 302 | 100% | 0% |

## By difficulty

| Difficulty | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Easy | 24 | 16,67% | 0% | 16,67% | 317,17 | 263 | 935 | 100% | 0% |
| Medium | 24 | 16,67% | 0% | 16,67% | 260,38 | 268,5 | 545 | 100% | 0% |
| Hard | 24 | 16,67% | 0% | 16,67% | 264,96 | 271 | 543 | 100% | 0% |

## Failed / mismatched documents

- `invoice-final-textpdf-001` (TextPdf, Easy): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `invoice-final-textpdf-002` (TextPdf, Medium): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 5/0/0.
- `invoice-final-textpdf-003` (TextPdf, Hard): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `invoice-final-textpdf-004` (TextPdf, Easy): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 5/0/0.
- `invoice-final-textpdf-005` (TextPdf, Medium): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `invoice-final-textpdf-006` (TextPdf, Hard): Invoice → Unknown; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `invoice-final-textpdf-007` (TextPdf, Easy): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `invoice-final-scannedpdf-008` (ScannedPdf, Medium): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 5/0/0.
- `invoice-final-scannedpdf-009` (ScannedPdf, Hard): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `invoice-final-scannedpdf-010` (ScannedPdf, Easy): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 5/0/0.
- `invoice-final-scannedpdf-011` (ScannedPdf, Medium): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `invoice-final-scannedpdf-012` (ScannedPdf, Hard): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 5/0/0.
- `invoice-final-scannedpdf-013` (ScannedPdf, Easy): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `invoice-final-scannedpdf-014` (ScannedPdf, Medium): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 5/0/0.
- `invoice-final-image-015` (Image, Hard): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `invoice-final-image-016` (Image, Easy): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 5/0/0.
- `invoice-final-image-017` (Image, Medium): Invoice → Unknown; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `invoice-final-image-018` (Image, Hard): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 5/0/0.
- `invoice-final-image-019` (Image, Easy): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `invoice-final-image-020` (Image, Medium): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 5/0/0.
- `contract-final-textpdf-001` (TextPdf, Medium): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-final-textpdf-002` (TextPdf, Hard): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-final-textpdf-003` (TextPdf, Easy): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
- `contract-final-textpdf-004` (TextPdf, Medium): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-final-textpdf-005` (TextPdf, Hard): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-final-textpdf-006` (TextPdf, Easy): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
- `contract-final-textpdf-007` (TextPdf, Medium): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-final-scannedpdf-008` (ScannedPdf, Hard): Contract → Unknown; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
- `contract-final-scannedpdf-009` (ScannedPdf, Easy): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
- `contract-final-scannedpdf-010` (ScannedPdf, Medium): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-final-scannedpdf-011` (ScannedPdf, Hard): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-final-scannedpdf-012` (ScannedPdf, Easy): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
- `contract-final-scannedpdf-013` (ScannedPdf, Medium): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-final-image-014` (Image, Hard): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-final-image-015` (Image, Easy): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 2/0/0.
- `contract-final-image-016` (Image, Medium): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-final-image-017` (Image, Hard): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-final-image-018` (Image, Easy): Contract → Unknown; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 1/0/0.
- `contract-final-image-019` (Image, Medium): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `contract-final-image-020` (Image, Hard): Contract → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-final-textpdf-001` (TextPdf, Hard): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-final-textpdf-002` (TextPdf, Easy): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-final-textpdf-003` (TextPdf, Medium): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-final-textpdf-004` (TextPdf, Hard): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-final-textpdf-005` (TextPdf, Easy): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-final-textpdf-006` (TextPdf, Medium): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-final-scannedpdf-007` (ScannedPdf, Hard): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-final-scannedpdf-008` (ScannedPdf, Easy): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-final-scannedpdf-009` (ScannedPdf, Medium): Act → Unknown; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-final-scannedpdf-010` (ScannedPdf, Hard): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-final-scannedpdf-011` (ScannedPdf, Easy): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-final-scannedpdf-012` (ScannedPdf, Medium): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-final-scannedpdf-013` (ScannedPdf, Hard): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-final-image-014` (Image, Easy): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-final-image-015` (Image, Medium): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-final-image-016` (Image, Hard): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-final-image-017` (Image, Easy): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `act-final-image-018` (Image, Medium): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-final-image-019` (Image, Hard): Act → Unknown; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
- `act-final-image-020` (Image, Easy): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
