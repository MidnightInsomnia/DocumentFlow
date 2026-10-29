# Hybrid v1 — Rules classifier + LLM extractor

- Classifier: `RuleBasedDocumentClassifier`
- Extractor: `OllamaDocumentFieldExtractor`, qwen2.5:7b-instruct via Ollama 0.34.2
- Model digest: `845dbda0ea48ed749caafd9e6037047aa19acfcfd82e704d7ca97d631a0b697e`
- LLM calls: 0 when Rules returns Unknown; otherwise 1 extraction call
- Temperature / seed: 0 / 42
- Corpus: corpus-v1
- Executed / skipped: 68 / 0
- Tesseract: 5.4.0.20240606, rus+eng
- Date: 2026-09-22

The first frozen run is stored here. Strict LLM response validation was not relaxed. Rules classifier, LLM extractor prompt, validator, router, OCR and corpus were not changed. A second run produced identical structured output for 68/68 documents.
