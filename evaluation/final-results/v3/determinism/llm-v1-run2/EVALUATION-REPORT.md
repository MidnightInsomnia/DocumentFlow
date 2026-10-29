# DocumentFlow Evaluation Report

Corpus: **72**  
Executed: **72**  
Skipped: **0**

## Overall

- Classification accuracy: 97,22%
- Field accuracy: 59,66%
- Document exact match: 41,67%
- Automatic completion rate: 48,61%
- NeedsReview rate: 29,17%
- Failure rate: 22,22%
- Average / Median / P95: 1375,14 / 1481,5 / 2165 ms

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
| Amount | 52 | 37 | 15 | 0 | 7 | 62,71% |
| Date | 58 | 42 | 16 | 0 | 4 | 67,74% |
| INN | 19 | 14 | 5 | 0 | 1 | 70% |
| KPP | 10 | 7 | 3 | 0 | 6 | 43,75% |
| Number | 59 | 37 | 16 | 6 | 3 | 59,68% |
| RelatedContractNumber | 10 | 2 | 5 | 3 | 4 | 14,29% |

## By source type

| Source | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| TextPdf | 24 | 95,83% | 58,23% | 41,67% | 1090,25 | 1221,5 | 1574 | 29,17% | 25% |
| ScannedPdf | 24 | 100% | 63,64% | 45,83% | 1671,17 | 1743 | 2325 | 25% | 20,83% |
| Image | 24 | 95,83% | 57,14% | 37,5% | 1364 | 1481,5 | 1852 | 33,33% | 20,83% |

## By difficulty

| Difficulty | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Easy | 24 | 95,83% | 49,37% | 16,67% | 1421,17 | 1481,5 | 2202 | 33,33% | 25% |
| Medium | 24 | 100% | 78,48% | 62,5% | 1357,29 | 1433,5 | 2165 | 25% | 8,33% |
| Hard | 24 | 95,83% | 50,67% | 45,83% | 1346,96 | 1424,5 | 1910 | 29,17% | 33,33% |

## Failed / mismatched documents

- `invoice-final-textpdf-001` (TextPdf, Easy): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
- `invoice-final-textpdf-003` (TextPdf, Hard): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
- `invoice-final-textpdf-004` (TextPdf, Easy): Invoice → Invoice; Completed → Failed; fields missing/wrong/unexpected: 5/0/0.
- `invoice-final-textpdf-006` (TextPdf, Hard): Invoice → Invoice; NeedsReview → Failed; fields missing/wrong/unexpected: 4/0/0.
- `invoice-final-textpdf-007` (TextPdf, Easy): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
- `invoice-final-scannedpdf-011` (ScannedPdf, Medium): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
- `invoice-final-scannedpdf-012` (ScannedPdf, Hard): Invoice → Invoice; Completed → Failed; fields missing/wrong/unexpected: 5/0/0.
- `invoice-final-scannedpdf-013` (ScannedPdf, Easy): Invoice → Invoice; Completed → Failed; fields missing/wrong/unexpected: 4/0/0.
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
