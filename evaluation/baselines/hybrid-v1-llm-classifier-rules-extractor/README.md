# Hybrid v1 — LLM classifier + Rules extractor

- Classifier: `OllamaDocumentClassifier`, qwen2.5:7b-instruct via Ollama 0.34.2
- Extractor: `RuleBasedDocumentFieldExtractor`
- Model digest: `845dbda0ea48ed749caafd9e6037047aa19acfcfd82e704d7ca97d631a0b697e`
- LLM calls: exactly 1 classification call per document
- Temperature / seed: 0 / 42
- Corpus: corpus-v1
- Executed / skipped: 68 / 0
- Tesseract: 5.4.0.20240606, rus+eng
- Date: 2026-09-22

The first frozen run is stored here. Rules extractor, LLM classifier prompt, validator, router, OCR and corpus were not changed. A second run produced identical structured output for 68/68 documents.
