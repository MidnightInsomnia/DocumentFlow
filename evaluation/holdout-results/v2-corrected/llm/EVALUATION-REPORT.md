# DocumentFlow Evaluation Report

Corpus: **42**  
Executed: **42**  
Skipped: **0**

## Overall

- Classification accuracy: 100%
- Field accuracy: 39,69%
- Document exact match: 28,57%
- Automatic completion rate: 19,05%
- NeedsReview rate: 33,33%
- Failure rate: 47,62%
- Average / Median / P95: 1264,67 / 1319,5 / 1924 ms

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
| TextPdf | 18 | 100% | 28,07% | 33,33% | 1071,78 | 1190,5 | 1660 | 27,78% | 61,11% |
| ScannedPdf | 12 | 100% | 34,15% | 16,67% | 1660,17 | 1605 | 2130 | 16,67% | 58,33% |
| Image | 12 | 100% | 66,67% | 33,33% | 1158,5 | 1172 | 1849 | 58,33% | 16,67% |

## By difficulty

| Difficulty | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Easy | 14 | 100% | 54,35% | 50% | 1239,5 | 1264,5 | 2130 | 28,57% | 35,71% |
| Medium | 14 | 100% | 26,19% | 21,43% | 1234,36 | 1319 | 1924 | 28,57% | 64,29% |
| Hard | 14 | 100% | 37,21% | 14,29% | 1320,14 | 1345,5 | 2116 | 42,86% | 42,86% |

## Failed / mismatched documents

- `invoice-holdout-textpdf-001` (TextPdf, Easy): Invoice → Invoice; Completed → Failed; fields missing/wrong/unexpected: 4/0/0.
- `invoice-holdout-textpdf-003` (TextPdf, Hard): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
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
