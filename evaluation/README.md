# DocumentFlow Evaluation

## Corpus methodology

- `corpus-v1` is the initial evaluation corpus used while developing strategies v1.
- `corpus-v2-holdout` was created after strategies v1 were frozen and was their independent holdout in Research 4.
- After Research 4 was analysed, corpus-v2 became a development/validation corpus for Rules v2 and Hybrid v2. Therefore all v2 results on corpus-v2 are **development results**, not an independent final generalization estimate.
- The final independent evaluation of frozen strategies v2 will use a future untouched corpus-v3. Corpus-v3 is intentionally not part of this iteration.
- Evaluation state semantics require `ExpectedStatus = NeedsReview` (and `Failed`) to use `ExpectedDepartment = Unassigned`, because routing occurs only after successful validation. Future corpus-v3 annotations must follow this rule.

Corpus-v2 has a documented annotation erratum: five known-type NeedsReview entries originally expected routed departments. Only those department labels were corrected. Documents, algorithms, prompts, OCR, expected type, status and fields were unchanged, so this is a dataset label correction rather than tuning.

Rules v1, LLM v1 and both v1 hybrid compositions remain unchanged and reproducible. Production API and Worker continue to use Rules v1.

Инструмент запускает размеченный corpus через реальные production-компоненты извлечения текста/OCR, классификации, извлечения полей, validation и routing. PostgreSQL, API и Worker не используются.

## Corpus

`manifest.json` содержит человекочитаемый `id`, относительный безопасный путь `file`, `sourceKind` (`TextPdf`, `ScannedPdf`, `Image`), `difficulty` (`Easy`, `Medium`, `Hard`), `templateId`, `contentType`, ожидаемые type/status/department и словарь `expectedFields`. Значения полей задаются в canonical формате и сравниваются строго: дата `yyyy-MM-dd`, сумма с точкой, canonical field names.

Файлы должны быть synthetic или обезличенными. `corpus/` — маленький sample для проверки tooling. `corpus-v1/` — детерминированно сгенерированный исследовательский набор из 68 synthetic-документов; состав и ограничения описаны в `corpus-v1/README.md`, генератор находится в `scripts/generate_corpus_v1.py`.

## Запуск

```bash
dotnet run --project tools/DocumentFlow.Evaluation -- \
  --corpus evaluation/corpus \
  --output evaluation/results

dotnet run --project tools/DocumentFlow.Evaluation -- \
  --corpus evaluation/corpus-v1 \
  --output evaluation/results \
  --strategy rules

dotnet run --project tools/DocumentFlow.Evaluation -- \
  --corpus evaluation/corpus-v1 \
  --output evaluation/results \
  --strategy ollama

dotnet run --project tools/DocumentFlow.Evaluation -- \
  --corpus evaluation/corpus-v1 \
  --output evaluation/results \
  --strategy hybrid-llm-classifier

dotnet run --project tools/DocumentFlow.Evaluation -- \
  --corpus evaluation/corpus-v1 \
  --output evaluation/results \
  --strategy hybrid-llm-extractor
```

`--strategy` по умолчанию равна `rules`. `ollama` использует LLM classifier и extractor; `hybrid-llm-classifier` — LLM classifier с Rules extractor; `hybrid-llm-extractor` — Rules classifier с LLM extractor. Это только DI composition существующих implementations внутри evaluation process; text extraction/OCR, validator и router остаются общими. Production API и Worker всегда используют Rules + Rules и не зависят от Ollama.

Ollama strategy настраивается через `Ollama__BaseUrl` (по умолчанию `http://localhost:11434`), `Ollama__Model` (по умолчанию `qwen2.5:7b-instruct`) и `Ollama__TimeoutSeconds` (по умолчанию 120). Benchmark использует temperature 0 и seed 42. Если модель отсутствует, установите её вручную командой `ollama pull qwen2.5:7b-instruct`; evaluation tool не скачивает модели автоматически. Document text отправляется только configured local Ollama endpoint.

LLM v1 выполняет один вызов classifier и второй вызов extractor для known-type документа; для `Unknown` выполняется только classifier. Confidence модели является self-reported и не является калиброванной вероятностью, поэтому его нельзя напрямую сравнивать с rules confidence. NeedsReview rate также зависит от self-reported field confidence.

Число LLM-вызовов: Rules — 0; Ollama — 2 для known type и 1 для Unknown; hybrid LLM classifier — 1 для каждого документа; hybrid LLM extractor — 0 для Rules-Unknown и 1 для Rules-known.

Для `ScannedPdf`/`Image` установите Tesseract с языками `rus` и `eng`. Путь и языки задаются через `Tesseract__ExecutablePath` и `Tesseract__Languages`. Если OCR недоступен, OCR-документы отмечаются `Skipped` и исключаются из знаменателей.

## Метрики

- Classification accuracy = правильные типы / executed documents.
- Field accuracy = Correct / (Correct + Missing + WrongValue + Unexpected).
- Document exact match требует полного совпадения type, final status, department и всех полей.
- Automatic completion rate = actual `Completed` / executed documents; это corpus-метрика, не Dashboard formula.
- NeedsReview/Failure rate считаются аналогично по actual final status.
- Average — среднее время pipeline; Median — центральное значение (или среднее двух центральных); P95 — nearest-rank с индексом `ceil(0.95 × N) - 1` в отсортированном массиве.
- `ProcessingTimeMs` измеряет только processing pipeline после отдельного TextPdf/OCR warm-up. Время запуска CLI, чтения manifest и генерации отчёта не включается; warm-up документы не входят в результаты и не увеличивают `Executed`.

Каждый запуск создаёт timestamp-directory в `evaluation/results/` с файлами `evaluation-summary.json`, `evaluation-documents.csv`, `evaluation-fields.csv` и `EVALUATION-REPORT.md`. Отчёты не содержат extracted/OCR text или binary content.

Зафиксированный первый результат находится в `baselines/rules-v1-text-only-provisional/`. Он намеренно помечен provisional: в среде прогона отсутствовал рабочий Tesseract `rus+eng`, поэтому 24 OCR-документа были пропущены, а метрики посчитаны по 44 text-PDF.

`baselines/rules-v1/` — финальный полный прогон `corpus-v1`, в котором все 68 документов выполнены и `Skipped == 0`.

После полного Ollama-прогона frozen-prompt artifact сохраняется в `baselines/llm-v1-qwen2.5-7b/`, а сравнение с rules — в `comparisons/rules-v1-vs-llm-v1/`.

Hybrid ablation artifacts находятся в `baselines/hybrid-v1-*`, four-way comparison — в `comparisons/strategies-v1/`.

`corpus-v2-holdout/` — отдельный frozen holdout corpus, созданный после фиксации strategies-v1. Его результаты хранятся отдельно в `holdout-results/v2/`, а generalization comparison — в `comparisons/holdout-v2/`; они не перезаписывают corpus-v1 baselines.
