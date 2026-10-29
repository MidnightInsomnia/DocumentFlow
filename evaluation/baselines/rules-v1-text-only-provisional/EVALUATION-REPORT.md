# DocumentFlow Evaluation Report

Corpus: **68**  
Executed: **44**  
Skipped: **24**

## Overall

- Classification accuracy: 95,45%
- Field accuracy: 92,54%
- Document exact match: 95,45%
- Automatic completion rate: 70,45%
- NeedsReview rate: 29,55%
- Failure rate: 0%
- Average / Median / P95: 8,82 / 1 / 5 ms

## Classification confusion matrix

| Expected \ Actual | Unknown | Invoice | Contract | Act |
|---|---|---|---|---|
| Unknown | 8 | 0 | 0 | 0 |
| Invoice | 2 | 10 | 0 | 0 |
| Contract | 0 | 0 | 12 | 0 |
| Act | 0 | 0 | 0 | 12 |

## Fields

| Field | Expected | Correct | Missing | Wrong | Unexpected | Accuracy |
|---|---:|---:|---:|---:|---:|---:|
| Amount | 32 | 30 | 2 | 0 | 0 | 93,75% |
| Date | 35 | 33 | 2 | 0 | 0 | 94,29% |
| INN | 12 | 10 | 2 | 0 | 0 | 83,33% |
| KPP | 10 | 8 | 2 | 0 | 0 | 80% |
| Number | 35 | 33 | 2 | 0 | 0 | 94,29% |
| RelatedContractNumber | 10 | 10 | 0 | 0 | 0 | 100% |

## By source type

| Source | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| TextPdf | 44 | 95,45% | 92,54% | 95,45% | 8,82 | 1 | 5 | 29,55% | 0% |
| ScannedPdf | 0 | 0% | 0% | 0% | 0 | 0 | 0 | 0% | 0% |
| Image | 0 | 0% | 0% | 0% | 0 | 0 | 0 | 0% | 0% |

## By difficulty

| Difficulty | Documents | Classification | Fields | Exact | Avg ms | Median ms | P95 ms | NeedsReview | Failed |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| Easy | 25 | 100% | 100% | 100% | 14,52 | 2 | 9 | 20% | 0% |
| Medium | 15 | 100% | 100% | 100% | 1,2 | 1 | 2 | 26,67% | 0% |
| Hard | 4 | 50% | 16,67% | 50% | 1,75 | 1,5 | 3 | 100% | 0% |

## Failed / mismatched documents

- `invoice-textpdf-004` (TextPdf, Hard): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 5/0/0.
- `invoice-textpdf-009` (TextPdf, Hard): Invoice → Unknown; Completed → NeedsReview; fields missing/wrong/unexpected: 5/0/0.

## Skipped documents

- `invoice-scannedpdf-013`: Tesseract OCR is unavailable.
- `invoice-scannedpdf-014`: Tesseract OCR is unavailable.
- `invoice-scannedpdf-015`: Tesseract OCR is unavailable.
- `invoice-scannedpdf-016`: Tesseract OCR is unavailable.
- `invoice-image-017`: Tesseract OCR is unavailable.
- `invoice-image-018`: Tesseract OCR is unavailable.
- `invoice-image-019`: Tesseract OCR is unavailable.
- `invoice-image-020`: Tesseract OCR is unavailable.
- `contract-scannedpdf-013`: Tesseract OCR is unavailable.
- `contract-scannedpdf-014`: Tesseract OCR is unavailable.
- `contract-scannedpdf-015`: Tesseract OCR is unavailable.
- `contract-scannedpdf-016`: Tesseract OCR is unavailable.
- `contract-image-017`: Tesseract OCR is unavailable.
- `contract-image-018`: Tesseract OCR is unavailable.
- `contract-image-019`: Tesseract OCR is unavailable.
- `contract-image-020`: Tesseract OCR is unavailable.
- `act-scannedpdf-013`: Tesseract OCR is unavailable.
- `act-scannedpdf-014`: Tesseract OCR is unavailable.
- `act-scannedpdf-015`: Tesseract OCR is unavailable.
- `act-scannedpdf-016`: Tesseract OCR is unavailable.
- `act-image-017`: Tesseract OCR is unavailable.
- `act-image-018`: Tesseract OCR is unavailable.
- `act-image-019`: Tesseract OCR is unavailable.
- `act-image-020`: Tesseract OCR is unavailable.
