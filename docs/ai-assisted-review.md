# AI-assisted manual review

AI assistance is an optional, user-triggered aid for documents already in
`NeedsReview`. It does not participate in background processing and does not
replace the production Rules v1 classifier or field extractor.

Enable it explicitly:

```text
AiReview__Enabled=true
Ollama__BaseUrl=http://localhost:11434
Ollama__Model=<local-model>
```

On Windows, put these parameters in the root `.env` and run `RUN.bat`.
`scripts/Run-DocumentFlow.ps1` reads `AiReview__Enabled` and the supported
`Ollama__*` settings from that file and passes them to the API process. It does
not start Ollama, pull models, or include Ollama in readiness checks.

The reviewer requests a suggestion with
`POST /api/documents/{id}/review/ai-suggestion`. The endpoint reads the stored
extracted text, invokes the frozen Ollama classifier/extractor, and returns a
transient suggested type, confidence, and only the fields supplied by the
model. It does not update the document, its fields, status, events, or routing.
Values are persisted only through the existing manual-review confirmation.

When disabled or when Ollama is unavailable the normal Rules v1 pipeline and
manual review remain usable. Document text, prompts, field values, and raw model
responses are deliberately excluded from suggestion logs.

This engineering feature follows the completed Research 6 evaluation. The
frozen final holdout corpus and evidence remain under
`evaluation/corpus-v3-final-holdout` and `evaluation/final-results`. Verify the
frozen corpus metadata before packaging with:

```powershell
./scripts/verify-research-freeze.ps1
```
