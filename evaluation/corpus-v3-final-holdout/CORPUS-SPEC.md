# Corpus-v3 final holdout specification

This specification was written before generating the corpus and before any strategy was run on it.

## Fixed shape

- 72 synthetic documents: 20 Invoice, 20 Contract, 20 Act, 12 Unknown.
- Source kinds: 24 TextPdf, 24 raster-only ScannedPdf, 24 PNG Image.
- Difficulty: 24 Easy, 24 Medium, 24 Hard; every source contains all levels.
- Expected status: 54 Completed and 18 NeedsReview. The review set is 12 Unknown plus two intentionally incomplete documents of each known type.
- Department semantics: review/failed/unknown are Unassigned; completed Invoice and Act are Accounting; completed Contract is Legal.
- At least six new template families per document type; no IDs or template IDs may overlap corpus-v1/v2.

## Business content

Invoices contain an unambiguous document number, date, INN and payable amount; KPP is optional. Natural distractors may include order, bank-account and telephone numbers, VAT and subtotals.

Contracts contain an unambiguous document number and date; total contract amount is optional. Terms, stages, penalties and party details may appear as distractors.

Acts contain an unambiguous document number, date and accepted-work amount; a related contract number is optional. Order, appendix and intermediate-sum references may appear.

Unknown documents cover letters, notifications, requests, protocols, memoranda, information messages, approval letters and internal orders. Ordinary mentions of invoices, acts or contracts do not change their business type.

## Layout and OCR variation

Templates vary block order, wording, whitespace, tables/linear layout, date and monetary formatting, optional fields and irrelevant lines. TextPdf has a real text layer. ScannedPdf is raster-only. Scans and PNGs use moderate rotation up to 1.5 degrees, light blur, contrast/font-size changes and JPEG-like visual degradation while remaining readable. Nine known-type documents are two pages.

All organizations, identifiers, amounts and prose are synthetic. The corpus excludes real personal/company data, prompt injection and deliberately unreadable or adversarial files.
