# Corpus-v2 annotation erratum

This erratum corrects evaluation annotation/state semantics. It is a dataset label correction, not model tuning.

- Original pre-erratum manifest SHA-256: `483863C4C4A905E0F6F2EDB523E4BED27DDC8B741538142E4C13B976B1D9BFD3`
- Corrected manifest SHA-256: `CDE6E6A889E674536FE1EFBC85CA0154791A5C2F9E3FA10A92A7CDBB8DEF9590`

Production `DocumentProcessingPipeline` calls `MarkNeedsReview()` and returns before routing. Therefore every document whose expected status is `NeedsReview` must have `ExpectedDepartment = Unassigned` until successful manual review.

The original manifest incorrectly assigned a routed department to five known-type NeedsReview documents:

- `invoice-holdout-textpdf-004`
- `invoice-holdout-scannedpdf-009`
- `contract-holdout-scannedpdf-008`
- `act-holdout-scannedpdf-007`
- `act-holdout-image-012`

Only `expectedDepartment` was changed for these entries. Document binaries, source metadata, type, status, expected fields, difficulty and template IDs are unchanged. No algorithm, prompt, regex, OCR configuration or LLM configuration was changed.
