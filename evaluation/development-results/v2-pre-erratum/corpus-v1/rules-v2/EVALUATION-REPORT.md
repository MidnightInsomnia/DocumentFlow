# DocumentFlow Evaluation Report

Corpus: **68**  
Executed: **68**  
Skipped: **0**

## Overall

- Classification accuracy: 91,18%
- Field accuracy: 84,73%
- Document exact match: 77,94%
- Automatic completion rate: 69,12%
- NeedsReview rate: 30,88%
- Failure rate: 0%
- Average / Median / P95: 102,15 / 2 / 404 ms

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
| Amount | 48 | 42 | 6 | 0 | 0 | 87,5% |
| Date | 58 | 53 | 5 | 0 | 0 | 91,38% |
| INN | 19 | 12 | 7 | 0 | 0 | 63,16% |
| KPP | 10 | 8 | 2 | 0 | 0 | 80% |
| Number | 58 | 47 | 5 | 6 | 0 | 81,03% |
| RelatedContractNumber | 10 | 10 | 0 | 0 | 0 | 100% |

## By source type

| Source | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| TextPdf | 44 | 95,45% | 92,54% | 95,45% | 1,27 | 1 | 3 | 29,55% | 0% |
| ScannedPdf | 12 | 83,33% | 72,73% | 50% | 394,58 | 393 | 420 | 33,33% | 0% |
| Image | 12 | 83,33% | 66,67% | 41,67% | 179,58 | 175,5 | 197 | 33,33% | 0% |

## By difficulty

| Difficulty | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Easy | 25 | 100% | 100% | 100% | 1,4 | 1 | 4 | 20% | 0% |
| Medium | 15 | 100% | 100% | 100% | 1,07 | 1 | 2 | 26,67% | 0% |
| Hard | 28 | 78,57% | 61,73% | 46,43% | 246,25 | 193 | 411 | 42,86% | 0% |

## Failed / mismatched documents

- `invoice-textpdf-004` (TextPdf, Hard): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 5/0/0.
- `invoice-textpdf-009` (TextPdf, Hard): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 5/0/0.
- `invoice-scannedpdf-013` (ScannedPdf, Hard): Invoice → Invoice; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `invoice-scannedpdf-014` (ScannedPdf, Hard): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `invoice-scannedpdf-016` (ScannedPdf, Hard): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 1/0/0.
- `invoice-image-017` (Image, Hard): Invoice → Invoice; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `invoice-image-018` (Image, Hard): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 1/0/0.
- `invoice-image-019` (Image, Hard): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 4/0/0.
- `invoice-image-020` (Image, Hard): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 1/0/0.
- `contract-scannedpdf-014` (ScannedPdf, Hard): Contract → Contract; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `contract-image-018` (Image, Hard): Contract → Contract; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `act-scannedpdf-014` (ScannedPdf, Hard): Act → Act; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `act-scannedpdf-015` (ScannedPdf, Hard): Act → Unknown; NeedsReview → NeedsReview; fields missing/wrong/unexpected: 1/0/0.
- `act-image-018` (Image, Hard): Act → Act; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `act-image-020` (Image, Hard): Act → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 3/0/0.
