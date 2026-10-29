# DocumentFlow Evaluation Report

Corpus: **68**  
Executed: **68**  
Skipped: **0**

## Overall

- Classification accuracy: 100%
- Field accuracy: 95,07%
- Document exact match: 85,29%
- Automatic completion rate: 75%
- NeedsReview rate: 25%
- Failure rate: 0%
- Average / Median / P95: 284,15 / 188 / 603 ms

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
| Amount | 48 | 48 | 0 | 0 | 0 | 100% |
| Date | 58 | 58 | 0 | 0 | 0 | 100% |
| INN | 19 | 16 | 3 | 0 | 0 | 84,21% |
| KPP | 10 | 10 | 0 | 0 | 0 | 100% |
| Number | 58 | 51 | 1 | 6 | 0 | 87,93% |
| RelatedContractNumber | 10 | 10 | 0 | 0 | 0 | 100% |

## By source type

| Source | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| TextPdf | 44 | 100% | 100% | 100% | 181,48 | 185 | 191 | 25% | 0% |
| ScannedPdf | 12 | 100% | 87,88% | 66,67% | 578,58 | 576,5 | 629 | 25% | 0% |
| Image | 12 | 100% | 83,33% | 50% | 366,17 | 360,5 | 390 | 25% | 0% |

## By difficulty

| Difficulty | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Easy | 25 | 100% | 100% | 100% | 183,12 | 187 | 192 | 20% | 0% |
| Medium | 15 | 100% | 100% | 100% | 179,73 | 183 | 188 | 26,67% | 0% |
| Hard | 28 | 100% | 87,65% | 64,29% | 430,29 | 384 | 621 | 28,57% | 0% |

## Failed / mismatched documents

- `invoice-scannedpdf-013` (ScannedPdf, Hard): Invoice → Invoice; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `invoice-scannedpdf-016` (ScannedPdf, Hard): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 1/0/0.
- `invoice-image-017` (Image, Hard): Invoice → Invoice; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `invoice-image-018` (Image, Hard): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 1/0/0.
- `invoice-image-020` (Image, Hard): Invoice → Invoice; Completed → NeedsReview; fields missing/wrong/unexpected: 1/0/0.
- `contract-scannedpdf-014` (ScannedPdf, Hard): Contract → Contract; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `contract-image-018` (Image, Hard): Contract → Contract; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `act-scannedpdf-014` (ScannedPdf, Hard): Act → Act; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `act-image-018` (Image, Hard): Act → Act; Completed → Completed; fields missing/wrong/unexpected: 0/1/0.
- `act-image-020` (Image, Hard): Act → Act; Completed → NeedsReview; fields missing/wrong/unexpected: 1/0/0.
