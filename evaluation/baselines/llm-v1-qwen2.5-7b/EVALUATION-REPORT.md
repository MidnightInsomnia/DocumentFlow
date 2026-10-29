# DocumentFlow Evaluation Report

Corpus: **68**  
Executed: **68**  
Skipped: **0**

## Overall

- Classification accuracy: 100%
- Field accuracy: 72,60%
- Document exact match: 52,94%
- Automatic completion rate: 58,82%
- NeedsReview rate: 25%
- Failure rate: 16,18%
- Average / Median / P95: 1254,57 / 1344,5 / 1809 ms

## Classification confusion matrix

| Expected \ Actual | Unknown | Invoice | Contract | Act |
|---|---|---|---|---|
| Unknown | 8 | 0 | 0 | 0 |
| Invoice | 0 | 20 | 0 | 0 |
| Contract | 0 | 0 | 20 | 0 |
| Act | 0 | 0 | 0 | 20 |

## Fields

| Field | Expected | Correct | Missing | Wrong | Unexpected | Accuracy |
|---|---:|---:|---:|---:|---:|---:|
| Amount | 48 | 35 | 6 | 7 | 5 | 66,04% |
| Date | 58 | 47 | 11 | 0 | 1 | 79,66% |
| INN | 19 | 14 | 5 | 0 | 0 | 73,68% |
| KPP | 10 | 10 | 0 | 0 | 0 | 100% |
| Number | 58 | 43 | 11 | 4 | 1 | 72,88% |
| RelatedContractNumber | 10 | 10 | 0 | 0 | 9 | 52,63% |

## By source type

| Source | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| TextPdf | 44 | 100% | 90,44% | 77,27% | 1102,52 | 1189 | 1736 | 22,73% | 4,55% |
| ScannedPdf | 12 | 100% | 42,86% | 8,33% | 1676 | 1727,5 | 2040 | 33,33% | 33,33% |
| Image | 12 | 100% | 43,90% | 8,33% | 1390,67 | 1470 | 1632 | 25% | 41,67% |

## By difficulty

| Difficulty | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Easy | 25 | 100% | 92,68% | 84% | 1156,24 | 1276 | 1736 | 16% | 4% |
| Medium | 15 | 100% | 90,24% | 80% | 998 | 1133 | 1742 | 26,67% | 6,67% |
| Hard | 28 | 100% | 47,92% | 10,71% | 1479,82 | 1548,5 | 2006 | 32,14% | 32,14% |

## Failed / mismatched documents

- `invoice-textpdf-001` (TextPdf, Easy): Invoice → Invoice; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `invoice-textpdf-004` (TextPdf, Hard): Invoice → Invoice; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `invoice-textpdf-005` (TextPdf, Easy): Invoice → Invoice; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `invoice-textpdf-008` (TextPdf, Easy): Invoice → Invoice; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `invoice-textpdf-009` (TextPdf, Hard): Invoice → Invoice; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `invoice-textpdf-011` (TextPdf, Easy): Invoice → Invoice; NeedsReview → Failed; fields missing/wrong/unexpected: 3/0/0.
- `invoice-textpdf-012` (TextPdf, Medium): Invoice → Invoice; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `invoice-scannedpdf-013` (ScannedPdf, Hard): Invoice → Invoice; Completed → Failed; fields missing/wrong/unexpected: 4/0/0.
- `invoice-scannedpdf-015` (ScannedPdf, Hard): Invoice → Invoice; NeedsReview → Failed; fields missing/wrong/unexpected: 3/0/0.
- `invoice-scannedpdf-016` (ScannedPdf, Hard): Invoice → Invoice; Completed → Failed; fields missing/wrong/unexpected: 4/0/0.
- `invoice-image-017` (Image, Hard): Invoice → Invoice; Completed → Completed; fields missing/wrong/unexpected: 0/2/0.
- `invoice-image-018` (Image, Hard): Invoice → Invoice; Completed → Failed; fields missing/wrong/unexpected: 4/0/0.
- `invoice-image-020` (Image, Hard): Invoice → Invoice; Completed → Failed; fields missing/wrong/unexpected: 4/0/0.
- `contract-textpdf-012` (TextPdf, Medium): Contract → Contract; Completed → Failed; fields missing/wrong/unexpected: 2/0/0.
- `contract-scannedpdf-013` (ScannedPdf, Hard): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
- `contract-scannedpdf-014` (ScannedPdf, Hard): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 0/1/1.
- `contract-scannedpdf-015` (ScannedPdf, Hard): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
- `contract-scannedpdf-016` (ScannedPdf, Hard): Contract → Contract; Completed → Failed; fields missing/wrong/unexpected: 2/0/0.
- `contract-image-017` (Image, Hard): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
- `contract-image-018` (Image, Hard): Contract → Contract; Completed → Failed; fields missing/wrong/unexpected: 2/0/0.
- `contract-image-019` (Image, Hard): Contract → Contract; Completed → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
- `contract-image-020` (Image, Hard): Contract → Contract; Completed → Failed; fields missing/wrong/unexpected: 2/0/0.
- `act-textpdf-011` (TextPdf, Hard): Act → Act; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
- `act-textpdf-012` (TextPdf, Medium): Act → Act; Completed → Completed; fields missing/wrong/unexpected: 0/0/1.
- `act-scannedpdf-013` (ScannedPdf, Hard): Act → Act; Completed → Completed; fields missing/wrong/unexpected: 0/0/1.
- `act-scannedpdf-014` (ScannedPdf, Hard): Act → Act; Completed → Completed; fields missing/wrong/unexpected: 0/1/1.
- `act-scannedpdf-015` (ScannedPdf, Hard): Act → Act; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 0/0/3.
- `act-scannedpdf-016` (ScannedPdf, Hard): Act → Act; Completed → Completed; fields missing/wrong/unexpected: 0/0/1.
- `act-image-017` (Image, Hard): Act → Act; Completed → Completed; fields missing/wrong/unexpected: 0/0/1.
- `act-image-018` (Image, Hard): Act → Act; Completed → Completed; fields missing/wrong/unexpected: 0/1/1.
- `act-image-019` (Image, Hard): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 0/0/1.
- `act-image-020` (Image, Hard): Act → Act; Completed → Failed; fields missing/wrong/unexpected: 3/0/0.
