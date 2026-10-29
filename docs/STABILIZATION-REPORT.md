# DocumentFlow MVP Stabilization Report

Дата проверки: 2026-09-20

## Automated checks

- `dotnet build`: PASS — 0 warnings, 0 errors.
- `dotnet test`: PASS — 127 passed, 0 failed, 0 skipped.
- Frontend `npm ci` and build: PASS.
- EF pending model changes: PASS — no pending changes.
- Event ordering regression: FIXED — equal timestamps now use the domain event
  lifecycle order as a deterministic tie-breaker; integration coverage included.

## Manual E2E

| Scenario | Expected | Result |
|---|---|---|
| RUN.bat | PostgreSQL, API, Worker and frontend start | PASS* |
| Second RUN | No duplicate processes | PASS |
| Health | live/ready/alias return 200 | PASS |
| Frontend | Registry, filters and document details load | PASS* |
| Invoice | Completed / Accounting | PASS |
| Contract | Completed / Legal | PASS |
| Act | Completed / Accounting | PASS |
| NeedsReview | Missing Amount requires review | PASS |
| Manual review | Completed / Accounting | PASS |
| Unknown | Manual Contract classification / Legal | PASS |
| Image OCR | Invoice | SKIPPED — Tesseract rus+eng unavailable |
| Scanned PDF OCR | Invoice | SKIPPED — Tesseract rus+eng unavailable |
| Corrupted PDF | Failed; Worker continues | PASS |
| Invalid upload | HTTP 400; no document created | PASS |
| Review conflict / validation | HTTP 409 / 422 | PASS |
| Recovery | stale Processing -> Failed | PASS |
| PostgreSQL outage | live 200; ready/alias 503; recovery to 200 | PASS |
| STOP/RUN persistence | DB records, events, fields and files preserved | PASS |

`*` The Windows orchestration was executed. This Codex host has no system npm,
so a temporary external npm adapter served the successfully built React output
during the RUN/STOP smoke. The real Vite build completed successfully; native
Vite startup through a standard installed Node.js/npm remains the recommended
final workstation check.

Fresh Processing recovery protection is covered by the PostgreSQL integration
test `RecoverStaleProcessingAsync_RecentDocument_DoesNotRecover`.

Baseline: **DocumentFlow Stable MVP v1** (`stable-mvp-v1`).
