# Corpus-v3 final independent holdout

Corpus-v3 was specified, generated, validated and frozen after Rules v2 and Hybrid v2 were frozen. It was not used for prompt, regex, alias, threshold, confidence, OCR or model tuning. It is the final independent holdout for the frozen strategies.

- 72 synthetic documents: Invoice 20, Contract 20, Act 20, Unknown 12.
- Sources: TextPdf 24, raster-only ScannedPdf 24, PNG Image 24.
- Difficulty: Easy 24, Medium 24, Hard 24; every source includes every difficulty.
- Expected status: Completed 54, NeedsReview 18.
- Templates: 32 new template IDs, disjoint from corpus-v1/v2.
- Nine two-page known-type PDF documents.
- Internal duplicate hashes: 0; SHA-256 overlaps with corpus-v1/v2: 0.
- ID/template overlaps with corpus-v1/v2: 0.

Freeze hashes are recorded in `FREEZE.md`; binary hashes are recorded in `FILES-SHA256.txt`. All content is synthetic and contains no real personal or company data.

Limitations include synthetic layouts and language, a modest sample of 72 documents, one OCR engine/configuration and one local LLM. It does not represent handwriting, highly damaged scans, very long documents, complex accounting tables or all real-world organizations. Small percentage differences must not be treated as conclusive without paired statistical context.
