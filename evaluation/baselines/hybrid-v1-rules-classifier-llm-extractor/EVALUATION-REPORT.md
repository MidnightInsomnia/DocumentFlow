# DocumentFlow Evaluation Report

Corpus: **68**  
Executed: **68**  
Skipped: **0**

## Overall

- Classification accuracy: 91,18%
- Field accuracy: 61,61%
- Document exact match: 48,53%
- Automatic completion rate: 48,53%
- NeedsReview rate: 29,41%
- Failure rate: 22,06%
- Average / Median / P95: 953,56 / 1086 / 1563 ms

## Classification confusion matrix

| Expected \ Actual | Unknown | Invoice | Contract | Act |
|---|---|---|---|---|
| Unknown | 8 | 0 | 0 | 0 |
| Invoice | 4 | 16 | 0 | 0 |
| Contract | 0 | 0 | 20 | 0 |
| Act | 2 | 0 | 0 | 18 |

## Fields

| Field | Expected | Correct | Missing | Wrong | Unexpected | Accuracy |
|---|---:|---:|---:|---:|---:|---:|
| Amount | 48 | 28 | 14 | 6 | 3 | 54,90% |
| Date | 58 | 38 | 20 | 0 | 0 | 65,52% |
| INN | 19 | 11 | 8 | 0 | 0 | 57,89% |
| KPP | 10 | 8 | 2 | 0 | 0 | 80% |
| Number | 58 | 35 | 20 | 3 | 0 | 60,34% |
| RelatedContractNumber | 10 | 10 | 0 | 0 | 5 | 66,67% |

## By source type

| Source | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| TextPdf | 44 | 95,45% | 82,35% | 75% | 845,82 | 929,5 | 1545 | 27,27% | 6,82% |
| ScannedPdf | 12 | 83,33% | 29,73% | 0% | 1267,25 | 1360,5 | 1810 | 33,33% | 41,67% |
| Image | 12 | 83,33% | 18,42% | 0% | 1034,92 | 1157,5 | 1439 | 33,33% | 58,33% |

## By difficulty

| Difficulty | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Easy | 25 | 100% | 89,02% | 80% | 965,72 | 1081 | 1545 | 16% | 8% |
| Medium | 15 | 100% | 90,24% | 80% | 812,2 | 930 | 1547 | 26,67% | 6,67% |
| Hard | 28 | 78,57% | 22,73% | 3,57% | 1018,43 | 1200,5 | 1659 | 42,86% | 42,86% |

## Failed / mismatched documents

- `invoice-textpdf-001` (TextPdf, Easy): Invoice → Invoice; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `invoice-textpdf-004` (TextPdf, Hard): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 5/0/0.
- `invoice-textpdf-005` (TextPdf, Easy): Invoice → Invoice; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `invoice-textpdf-008` (TextPdf, Easy): Invoice → Invoice; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `invoice-textpdf-009` (TextPdf, Hard): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 5/0/0.
- `invoice-textpdf-011` (TextPdf, Easy): Invoice → Invoice; NeedsReview → Failed; fields missing/wrong/unexpected: 3/0/0.
- `invoice-textpdf-012` (TextPdf, Medium): Invoice → Invoice; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `invoice-scannedpdf-013` (ScannedPdf, Hard): Invoice → Invoice; Completed → Completed; fields missing/wrong/unexpected: 0/2/0.
- `invoice-scannedpdf-014` (ScannedPdf, Hard): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `invoice-scannedpdf-015` (ScannedPdf, Hard): Invoice → Invoice; NeedsReview → Failed; fields missing/wrong/unexpected: 3/0/0.
- `invoice-scannedpdf-016` (ScannedPdf, Hard): Invoice → Invoice; Completed → Failed; fields missing/wrong/unexpected: 4/0/0.
- `invoice-image-017` (Image, Hard): Invoice → Invoice; Completed → Completed; fields missing/wrong/unexpected: 0/2/0.
- `invoice-image-018` (Image, Hard): Invoice → Invoice; Completed → Failed; fields missing/wrong/unexpected: 4/0/0.
- `invoice-image-019` (Image, Hard): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `invoice-image-020` (Image, Hard): Invoice → Invoice; Completed → Failed; fields missing/wrong/unexpected: 4/0/0.
- `contract-textpdf-004` (TextPdf, Easy): Contract → Contract; Completed → Failed; fields missing/wrong/unexpected: 3/0/0.
- `contract-textpdf-012` (TextPdf, Medium): Contract → Contract; Completed → Failed; fields missing/wrong/unexpected: 2/0/0.
- `contract-scannedpdf-013` (ScannedPdf, Hard): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
- `contract-scannedpdf-014` (ScannedPdf, Hard): Contract → Contract; Completed → Failed; fields missing/wrong/unexpected: 2/0/0.
- `contract-scannedpdf-015` (ScannedPdf, Hard): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
- `contract-scannedpdf-016` (ScannedPdf, Hard): Contract → Contract; Completed → Failed; fields missing/wrong/unexpected: 2/0/0.
- `contract-image-017` (Image, Hard): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
- `contract-image-018` (Image, Hard): Contract → Contract; Completed → Failed; fields missing/wrong/unexpected: 2/0/0.
- `contract-image-019` (Image, Hard): Contract → Contract; Completed → Failed; fields missing/wrong/unexpected: 2/0/0.
- `contract-image-020` (Image, Hard): Contract → Contract; Completed → Failed; fields missing/wrong/unexpected: 2/0/0.
- `act-textpdf-011` (TextPdf, Hard): Act → Act; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
- `act-textpdf-012` (TextPdf, Medium): Act → Act; Completed → Completed; fields missing/wrong/unexpected: 0/0/1.
- `act-scannedpdf-013` (ScannedPdf, Hard): Act → Act; Completed → Failed; fields missing/wrong/unexpected: 3/0/0.
- `act-scannedpdf-014` (ScannedPdf, Hard): Act → Act; Completed → Completed; fields missing/wrong/unexpected: 0/1/1.
- `act-scannedpdf-015` (ScannedPdf, Hard): Act → Unknown; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 1/0/0.
- `act-scannedpdf-016` (ScannedPdf, Hard): Act → Act; Completed → Completed; fields missing/wrong/unexpected: 0/0/1.
- `act-image-017` (Image, Hard): Act → Act; Completed → Failed; fields missing/wrong/unexpected: 3/0/0.
- `act-image-018` (Image, Hard): Act → Act; Completed → Failed; fields missing/wrong/unexpected: 3/0/0.
- `act-image-019` (Image, Hard): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
- `act-image-020` (Image, Hard): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
