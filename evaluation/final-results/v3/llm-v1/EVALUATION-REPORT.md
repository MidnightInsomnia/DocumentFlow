# DocumentFlow Evaluation Report

Corpus: **72**  
Executed: **72**  
Skipped: **0**

## Overall

- Classification accuracy: 97,22%
- Field accuracy: 62,66%
- Document exact match: 43,06%
- Automatic completion rate: 50%
- NeedsReview rate: 30,56%
- Failure rate: 19,44%
- Average / Median / P95: 1594,07 / 1690,5 / 2432 ms

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
| Amount | 52 | 38 | 14 | 0 | 7 | 64,41% |
| Date | 58 | 43 | 14 | 1 | 4 | 69,35% |
| INN | 19 | 16 | 3 | 0 | 1 | 80% |
| KPP | 10 | 8 | 2 | 0 | 6 | 50% |
| Number | 59 | 39 | 14 | 6 | 3 | 62,90% |
| RelatedContractNumber | 10 | 2 | 5 | 3 | 4 | 14,29% |

## By source type

| Source | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| TextPdf | 24 | 95,83% | 63,29% | 45,83% | 1283,75 | 1419,5 | 1869 | 33,33% | 20,83% |
| ScannedPdf | 24 | 100% | 67,53% | 45,83% | 1956,29 | 2036 | 2777 | 25% | 16,67% |
| Image | 24 | 95,83% | 57,14% | 37,5% | 1542,17 | 1670 | 2137 | 33,33% | 20,83% |

## By difficulty

| Difficulty | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Easy | 24 | 95,83% | 54,43% | 20,83% | 1625,17 | 1681 | 2499 | 33,33% | 20,83% |
| Medium | 24 | 100% | 78,48% | 62,5% | 1530,17 | 1623,5 | 2377 | 25% | 8,33% |
| Hard | 24 | 95,83% | 54,67% | 45,83% | 1626,88 | 1700 | 2269 | 33,33% | 29,17% |

## Failed / mismatched documents

- `invoice-final-textpdf-001` (TextPdf, Easy): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
- `invoice-final-textpdf-003` (TextPdf, Hard): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
- `invoice-final-textpdf-004` (TextPdf, Easy): Invoice → Invoice; Completed → Failed; fields missing/wrong/unexpected: 5/0/0.
- `invoice-final-textpdf-007` (TextPdf, Easy): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
- `invoice-final-scannedpdf-009` (ScannedPdf, Hard): Invoice → Invoice; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `invoice-final-scannedpdf-011` (ScannedPdf, Medium): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
- `invoice-final-scannedpdf-012` (ScannedPdf, Hard): Invoice → Invoice; Completed → Failed; fields missing/wrong/unexpected: 5/0/0.
- `invoice-final-scannedpdf-014` (ScannedPdf, Medium): Invoice → Invoice; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `invoice-final-image-015` (Image, Hard): Invoice → Invoice; Completed → Failed; fields missing/wrong/unexpected: 4/0/0.
- `invoice-final-image-016` (Image, Easy): Invoice → Invoice; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `invoice-final-image-017` (Image, Medium): Invoice → Invoice; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 0/0/2.
- `invoice-final-image-019` (Image, Easy): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 0/1/1.
- `contract-final-textpdf-003` (TextPdf, Easy): Contract → Contract; Completed → Completed; fields missing/wrong/unexpected: 0/0/1.
- `contract-final-textpdf-006` (TextPdf, Easy): Contract → Contract; Completed → Completed; fields missing/wrong/unexpected: 0/0/1.
- `contract-final-scannedpdf-008` (ScannedPdf, Hard): Contract → Contract; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
- `contract-final-scannedpdf-009` (ScannedPdf, Easy): Contract → Contract; Completed → Completed; fields missing/wrong/unexpected: 0/0/1.
- `contract-final-scannedpdf-012` (ScannedPdf, Easy): Contract → Contract; Completed → Completed; fields missing/wrong/unexpected: 0/0/1.
- `contract-final-image-015` (Image, Easy): Contract → Contract; Completed → Completed; fields missing/wrong/unexpected: 0/1/1.
- `contract-final-image-017` (Image, Hard): Contract → Contract; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `contract-final-image-018` (Image, Easy): Contract → Contract; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 0/0/2.
- `act-final-textpdf-001` (TextPdf, Hard): Act → Act; Completed → Failed; fields missing/wrong/unexpected: 4/0/0.
- `act-final-textpdf-002` (TextPdf, Easy): Act → Act; Completed → Completed; fields missing/wrong/unexpected: 0/0/1.
- `act-final-textpdf-003` (TextPdf, Medium): Act → Act; Completed → Failed; fields missing/wrong/unexpected: 4/0/0.
- `act-final-textpdf-004` (TextPdf, Hard): Act → Act; Completed → Failed; fields missing/wrong/unexpected: 3/0/0.
- `act-final-textpdf-005` (TextPdf, Easy): Act → Act; Completed → Failed; fields missing/wrong/unexpected: 4/0/0.
- `act-final-textpdf-006` (TextPdf, Medium): Act → Act; Completed → Completed; fields missing/wrong/unexpected: 0/0/1.
- `act-final-scannedpdf-007` (ScannedPdf, Hard): Act → Act; Completed → Failed; fields missing/wrong/unexpected: 4/0/0.
- `act-final-scannedpdf-008` (ScannedPdf, Easy): Act → Act; Completed → Failed; fields missing/wrong/unexpected: 3/0/0.
- `act-final-scannedpdf-009` (ScannedPdf, Medium): Act → Act; NeedsReview → Completed; fields missing/wrong/unexpected: 0/1/1.
- `act-final-scannedpdf-010` (ScannedPdf, Hard): Act → Act; Completed → Failed; fields missing/wrong/unexpected: 3/0/0.
- `act-final-scannedpdf-011` (ScannedPdf, Easy): Act → Act; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `act-final-scannedpdf-012` (ScannedPdf, Medium): Act → Act; Completed → Completed; fields missing/wrong/unexpected: 0/0/1.
- `act-final-image-014` (Image, Easy): Act → Act; Completed → Failed; fields missing/wrong/unexpected: 3/0/0.
- `act-final-image-015` (Image, Medium): Act → Act; Completed → Failed; fields missing/wrong/unexpected: 4/0/0.
- `act-final-image-016` (Image, Hard): Act → Act; Completed → Failed; fields missing/wrong/unexpected: 3/0/0.
- `act-final-image-017` (Image, Easy): Act → Act; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `act-final-image-018` (Image, Medium): Act → Act; Completed → Completed; fields missing/wrong/unexpected: 0/0/1.
- `act-final-image-019` (Image, Hard): Act → Act; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 0/1/1.
- `act-final-image-020` (Image, Easy): Act → Act; Completed → Failed; fields missing/wrong/unexpected: 3/0/0.
- `unknown-final-textpdf-004` (TextPdf, Easy): Unknown → Act; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 0/0/2.
- `unknown-final-image-012` (Image, Hard): Unknown → Act; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 0/0/2.
