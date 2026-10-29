# Corpus-v2 corrected holdout comparison

These reports apply the documented department annotation correction in `corpus-v2-holdout/ERRATA.md`. They do not represent a new benchmark or model tuning. The four strategies-v1, documents, expected types, statuses and fields are unchanged.

| Metric | Rules | Pure LLM | HC | HE |
|---|---:|---:|---:|---:|
| Classification | 19.05% | 100.00% | 100.00% | 19.05% |
| Fields | 0.00% | 39.69% | 14.05% | 0.00% |
| Exact | 14.29% | 28.57% | 14.29% | 14.29% |
| Completed | 0 | 8 | 0 | 0 |
| NeedsReview | 42 | 14 | 42 | 40 |
| Failed | 0 | 20 | 0 | 2 |
| Average / Median / P95, ms | 166.88 / 154 / 424 | 1264.67 / 1319.5 / 1924 | 353.24 / 335.5 / 626 | 229.40 / 168.5 / 453 |

Compared with pre-erratum results, classification, field accuracy and status counts are unchanged. ExactMatch changed only for Pure LLM, from 26.19% to 28.57% (+2.38 pp). Rules, HC and HE ExactMatch remained 14.29%.

Historical pre-erratum reports remain under `evaluation/holdout-results/v2-pre-erratum` and `evaluation/comparisons/holdout-v2-pre-erratum`.
