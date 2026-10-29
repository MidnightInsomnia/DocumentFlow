# Frozen strategies v2

Status: **frozen after Research 5 development**. Do not tune these implementations before evaluation on the future untouched corpus-v3.

Research 5.1 corrected five corpus-v2 expected-department annotations to match production NeedsReview semantics. No V2 implementation changed. Rules v2 and Hybrid v2 are re-frozen unchanged after this dataset-label erratum.

## Rules v2

`RuleBasedDocumentClassifierV2` + `RuleBasedDocumentFieldExtractorV2`.

The classifier retains the confidence threshold and winner-margin design while adding weighted multi-word semantic markers for invoice, contract and act layouts. A single generic word (`счет`, `договор`, `акт`) has deliberately low weight and cannot independently produce a confident classification.

The extractor uses `DocumentFieldLabelLexicon` and a shared label/value matcher. It supports reusable aliases for Number, Date, Amount and RelatedContractNumber, while retaining deterministic normalization and the established INN/KPP patterns. Type context controls ambiguous labels such as `регистрационный номер`, `номер документа`, and generic amount labels.

False-positive protections require explicit semantic labels: penalties are not contract amounts, phone numbers are not document numbers, birth dates are not document dates, and a contract reference in an act is not its act number.

## Hybrid v2

Frozen `OllamaDocumentClassifier` v1 + `RuleBasedDocumentFieldExtractorV2`. The Ollama prompt, model, temperature/seed contract, validator, router and OCR pipeline were not changed.

## Methodological status

Results under `evaluation/development-results/v2` use corpus-v1 and corpus-v2 for development/regression analysis. They are not final generalization results. No corpus-v3 was created in Research 5.

Production remains wired to Rules v1.
