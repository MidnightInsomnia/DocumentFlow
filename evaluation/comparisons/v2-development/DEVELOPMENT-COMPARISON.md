# Rules v2 development comparison

These are **development results**, not final generalization results. Corpus-v2 was an independent holdout only for strategies v1; after Research 4 it became development feedback for v2. Final v2 validation requires a future untouched corpus-v3.

## NeedsReview department erratum

The original corpus-v2 annotation assigned routed departments to five known-type NeedsReview documents. Production returns before routing when validation requests review, so their expected department was corrected to `Unassigned`. This dataset-label correction changed neither documents nor algorithms. Pre-erratum Rules v2 and Hybrid v2 ExactMatch was 76.19%; corrected ExactMatch is 88.10% (37/42). Classification, fields, status counts and unexpected-field counts are unchanged. Pre-erratum result files remain under `evaluation/development-results/v2-pre-erratum`; corrected result files are under `evaluation/development-results/v2-corrected`.

## Corpus-v1 regression

| Metric | Rules v1 | Rules v2 | HC v1 | Hybrid v2 |
|---|---:|---:|---:|---:|
| Classification | 91.18% | 91.18% | 100.00% | 100.00% |
| Fields | 84.73% | 84.73% | 95.07% | 95.07% |
| Exact | 77.94% | 77.94% | 85.29% | 85.29% |
| Completed | 47 | 47 | 51 | 51 |
| NeedsReview | 21 | 21 | 17 | 17 |
| Failed | 0 | 0 | 0 | 0 |
| Unexpected fields | 0 | 0 | 0 | 0 |
| Average / Median / P95, ms | 121.71 / 2 / 471 | 102.15 / 2 / 404 | 284.15 / 188 / 603 | 285.31 / 187 / 594 |

Rules v2 and Hybrid v2 introduce no quality regression on corpus-v1.

## Corpus-v2 development

| Metric | Rules v1 | Rules v2 | HC v1 | Hybrid v2 |
|---|---:|---:|---:|---:|
| Classification | 19.05% | 100.00% | 100.00% | 100.00% |
| Fields | 0.00% | 95.87% | 14.05% | 95.87% |
| Exact | 14.29% | 88.10% | 14.29% | 88.10% |
| Completed | 0 | 31 | 0 | 31 |
| NeedsReview | 42 | 11 | 42 | 11 |
| Failed | 0 | 0 | 0 | 0 |
| Unexpected fields | 0 | 0 | 0 | 0 |
| Average / Median / P95, ms | 166.88 / 154 / 424 | 172.60 / 156.5 / 439 | 353.24 / 335.5 / 626 | 355.74 / 333.5 / 629 |

## Field accuracy — corpus-v1

| Field | Rules v1 | Rules v2 | HC v1 | Hybrid v2 |
|---|---:|---:|---:|---:|
| Number | 81.03% | 81.03% | 87.93% | 87.93% |
| Date | 91.38% | 91.38% | 100.00% | 100.00% |
| INN | 63.16% | 63.16% | 84.21% | 84.21% |
| KPP | 80.00% | 80.00% | 100.00% | 100.00% |
| Amount | 87.50% | 87.50% | 100.00% | 100.00% |
| RelatedContractNumber | 100.00% | 100.00% | 100.00% | 100.00% |

## Field accuracy — corpus-v2

| Field | Rules v1 | Rules v2 | HC v1 | Hybrid v2 |
|---|---:|---:|---:|---:|
| Number | 0.00% | 88.57% | 0.00% | 88.57% |
| Date | 0.00% | 100.00% | 0.00% | 100.00% |
| INN | 0.00% | 100.00% | 100.00% | 100.00% |
| KPP | 0.00% | 100.00% | 100.00% | 100.00% |
| Amount | 0.00% | 100.00% | 0.00% | 100.00% |
| RelatedContractNumber | 0.00% | 83.33% | 0.00% | 83.33% |

The remaining wrong values are OCR character substitutions in document numbers/references; v2 deliberately does not add fuzzy matching. Missing-field improvement did not introduce unexpected fields: all four v2 runs have an unexpected-field count of zero.
