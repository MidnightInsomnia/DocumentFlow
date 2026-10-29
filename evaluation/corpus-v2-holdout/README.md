# Corpus V2 Holdout

Corpus-v2-holdout was created only after all four strategies-v1 had been frozen. No strategy, prompt, regex, threshold, OCR setting, validator or router was developed or selected using this corpus. The complete manifest and expected annotations were validated and frozen before the first evaluation run.

- Original pre-erratum manifest SHA-256: `483863C4C4A905E0F6F2EDB523E4BED27DDC8B741538142E4C13B976B1D9BFD3`
- Corrected manifest SHA-256: `CDE6E6A889E674536FE1EFBC85CA0154791A5C2F9E3FA10A92A7CDBB8DEF9590`
- Size: 42 synthetic documents
- Types: 12 Invoice, 12 Contract, 12 Act, 6 Unknown
- Sources: 18 TextPdf, 12 image-only ScannedPdf, 12 PNG Image
- Difficulty: 14 Easy, 14 Medium, 14 Hard
- Expected status: 31 Completed, 11 NeedsReview (including 5 intentional known-type review cases and 6 Unknown)
- Templates: 24 new template IDs; none occur in corpus-v1
- IDs: all unique and disjoint from corpus-v1
- Duplicate binary hashes inside v2: 0
- Binary SHA-256 overlaps with corpus-v1: 0

`FILES-SHA256.txt` records every corpus file hash. Generation is deterministic and implemented in `evaluation/scripts/generate_corpus_v2_holdout.py`. ScannedPdf files contain raster images without a normal text layer. Easy, Medium and Hard examples occur in every source kind; OCR variation is limited to small rotation, light blur, resolution/font-size and contrast changes.

All organizations, identifiers and document values are synthetic. No real company documents, taxpayer identifiers or personal data are included.

For strategies-v1 this corpus remains the historical holdout with the annotation correction documented in `ERRATA.md`. For strategies-v2 it is a development corpus, not an independent final holdout. The erratum changes only five expected department labels to match production NeedsReview semantics; it does not modify documents or algorithms.

The corpus has limited statistical precision at 42 documents. Differences of one or two percentage points must not be treated as statistically proven. It does not cover real heterogeneous company documents, handwriting, extreme scan damage, large tables, long multipage documents, foreign languages or adversarial files.
