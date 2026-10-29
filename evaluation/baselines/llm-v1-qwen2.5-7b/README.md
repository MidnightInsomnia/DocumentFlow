# LLM Baseline v1 — Qwen 2.5 7B

- Strategy: LLM
- Provider: local Ollama
- Model: `qwen2.5:7b-instruct`
- Model digest: `845dbda0ea48ed749caafd9e6037047aa19acfcfd82e704d7ca97d631a0b697e`
- Model size: 4,683,087,332 bytes; Q4_K_M, 7.6B parameters
- Ollama version: 0.34.2
- Temperature: 0
- Seed: 42
- Calls: classifier plus extractor for known types; classifier only for Unknown
- Corpus: `corpus-v1`
- Executed / skipped: 68 / 0
- Tesseract: 5.4.0.20240606, `rus+eng`
- Date: 2026-09-22
- Git commit: unavailable (workspace is not a Git repository)

Prompts were frozen after the real dev Invoice smoke and before the first 68-document run. This directory contains that first full run; the second run was used only for determinism analysis. No corpus-v1 document was used for prompt tuning.

LLM classification and field confidence are self-reported and are not calibrated probabilities. They must not be compared directly with rules confidence. The existing validator threshold was not changed. Strict malformed/unsupported/duplicate field responses are recorded as processing failures rather than silently repaired.

Document text was sent only to the configured local Ollama endpoint. Raw prompts, document text, and raw model responses are not stored in reports.
