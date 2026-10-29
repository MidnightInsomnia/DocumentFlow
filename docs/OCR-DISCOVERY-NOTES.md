# OCR и автоматические предложения видов: причины, изменения, проверка

## Границы

Изменения относятся только к backend `src/`, `tools/local_stack.py` и этому файлу. Frontend, исходный ZIP, соседний master, рабочие документы, БД и `local-secrets.json` этим исполнителем не изменялись. Стек не запускался, миграции не применялись, тесты/моки и фоновые проверочные задачи не создавались и не запускались. Для CLI-диагностики использован отдельный scratch-каталог Hermes вне Git.

## Подтвержденные причины

### Tesseract

- Worker конфигурировал `ExecutablePath="tesseract"`, `Languages="rus+eng"`; старый launcher не находил executable и не передавал отдельный каталог tessdata.
- Реальный `shutil.which('tesseract')` вернул `None`; `TESSDATA_PREFIX` отсутствует. Попытка `subprocess.run(['tesseract', '--list-langs'], timeout=5)` завершилась `FileNotFoundError [WinError 2]`.
- Поиск по доступным каталогам `C:/Program Files`, `C:/Program Files (x86)`, LocalAppData пользователя и `DocumentFlow-runtime` не нашел `tesseract.exe`, `rus.traineddata` или `eng.traineddata`. Проверены также места Chocolatey/Scoop: этих каталогов нет. Обход просмотрел 68123 каталога; 27 защищенных каталогов недоступны, включая WindowsApps. Поэтому утверждается отсутствие доступного движка в проверенных местах и невозможность запуска из текущего окружения, а не доказанное отсутствие файла на каждом диске.
- Нативный backend `OcrHealthProbe`, вызванный через отдельную CLI-программу без запуска API/БД, подтвердил `Unavailable`, `errorCode=ocr_executable_unavailable`, `NativeStartError=2`. Вызов настоящего `TesseractOcrEngine` с существующим PNG из `docs/verification/screenshots` вернул такой же предметный отказ на preflight.
- Это отказ **до** распознавания, а не доказанная ошибка русского языка, качества изображения или OCR timeout. Нативного stderr пока нет: дочерний OCR-процесс не запускается.
- Для плоского worktree старое выражение `SOURCE.parents[2] / 'DocumentFlow-runtime'` давало `C:/Users/<user>/DocumentFlow-runtime`, тогда как существующий runtime расположен в `C:/Users/<user>/Desktop/ddd/DocumentFlow-runtime`.

### Discovery

- Цепочка остается прежней: Worker завершает извлечение текста → API-hosted `WorkspaceIndexingHostedService` обходит документы → `WorkspaceIndexingService` создает workspace и вызывает discovery → `AiKindProposalClient` вызывает Ollama `/api/generate` → создается/назначается черновик каталожного вида → администратор подтверждает вид существующим API.
- Старый `DiscoverAsync` ставил `Succeeded=true` после любого валидного ответа, включая `{"name":null}`. Условие `previous.Succeeded || recentAttempt` навсегда запрещало следующие попытки при том же тексте/модели.
- Аналогичный окончательный успех сохранялся, если модель вернула название уже существующего неактивного подтвержденного вида, который код фактически не назначал.
- Исключения сети/JSON в старом коде уже оставляли `Succeeded=false` и повторялись через пять минут. Их проблема — отсутствие отдельного пользовательского статуса и ручного повтора, а не доказанное вечное отключение после любого исключения.
- В `src/DocumentFlow.Api/appsettings.json` `AiReview:Enabled=false` по умолчанию; `AiReviewOptions.Enabled` тоже false без настройки. Это ожидаемое отключение, не сбой сети. Локальный launcher явно передает `AiReview__Enabled=true`. При самостоятельном запуске API без этого override автоматические предложения не выполняются; новый статус `AiDisabled` показывает причину. Значение по умолчанию намеренно не включалось молча.
- Ошибка OCR оставляет документ без достаточного текста; discovery не может корректно предложить вид до восстановления обработки. Условия `Unknown`, отсутствие назначения, достаточный текст и включенный AI теперь явно отражены статусами.

## Что изменено

### OCR и launcher

- Общая конфигурация процесса для worker и health: разрешение `tesseract` по PATH и стандартным Windows/runtime-путям, без изменения системного PATH; явный пользовательский executable не подменяется другим.
- Добавлен необязательный `Tesseract:TessdataPath` (`Tesseract__TessdataPath` для окружения). Он передается отдельным аргументом `--tessdata-dir` и в `--list-langs`, и в OCR. Существующий `TESSDATA_PREFIX` не переписывается.
- Launcher ищет существующий runtime рядом с source и далее по предкам; явный `DOCUMENTFLOW_RUNTIME` имеет приоритет. Ни runtime, ни секреты, ни БД функцией поиска не создаются.
- Launcher формирует единое OCR-окружение API/worker, ищет соседний tessdata при отсутствии явной настройки/`TESSDATA_PREFIX` и выполняет ограниченный preflight. Отсутствие OCR печатается как предупреждение, не подменяется успехом и не блокирует документы с собственным текстом.
- Коды ошибок распознавания: `ocr_executable_unavailable`, `ocr_missing_languages`, `ocr_timeout`, `ocr_process_failed`, `ocr_no_text`. В `ProcessingError` они передаются как `[code]` с русской инструкцией.
- stdout/stderr читаются одновременно; ошибка загрузки языка распознается до общей ошибки exit code. Раннее закрытие stdin не теряет диагностику завершившегося процесса. При timeout/cancellation процесс принудительно завершается. В логи не попадают сырой stderr, пути или содержимое документа: сохраняются код завершения, категория и размер stderr.
- Language preflight ограничен минимумом из настроенного timeout и 5 секунд; собственно OCR — `Tesseract:TimeoutSeconds`. Внешняя отмена не превращается в сообщение о внутреннем timeout.
- Существующий `GET /api/dashboard/health` для Admin возвращает `ocr: { status, languages, missingLanguages, errorCode }`. OCR status: `Healthy`, `MissingLanguages`, `Unavailable`, `Timeout`, `Failed`; Operator по прежнему контракту получает только `overall`. Пути и stderr в health не раскрываются.

### Discovery без миграции схемы

- Новый `KindDiscoveryService` использует существующую `AiKindDiscoveryState`. Старые окончательные результаты не блокируют новую логику: fingerprint имеет отдельную версию `kind-discovery-v2` и считается по фактическому ограниченному AI-входу и модели, независимо от embedding fingerprint.
- Ошибка/непригодный ответ — автоматический повтор не ранее пяти минут от начала попытки. Валидный результат без назначения (`NoProposal`) — повтор не ранее 30 минут, а не вечный запрет. После истечения интервала индексатор подхватит документ при следующем обходе; это не обещание запуска в точную секунду.
- Перед сетевым вызовом сохраняется короткая DB-claim под существующей catalog advisory lock и document row lock. Нет DB-транзакции на время AI inference. Timestamp claim нормализован до точности PostgreSQL. Завершение проверяет claim, fingerprint и неизменность workspace, поэтому устаревший ответ не перетирает более новый запрос или сделанное параллельно назначение.
- Одновременные локальные вызовы для одного документа исключаются singleton-guard. После остановки процесса сохраненная неуспешная claim истекает и повторяется; вечной блокировки нет.
- Новые AI-виды остаются `IsDraft=true`, `IsActive=false`, `CreatedAutomatically=true`; ручное подтверждение администратора не обходится. Неактивный подтвержденный вид не активируется автоматически.
- Состояние `Discovering` наблюдается в API-процессе, исполняющем inference. Другой API-instance без локального guard до завершения может показать `RetryScheduled` по persisted claim; защита от повторной claim и устаревшей записи остается в БД. Текущий локальный launcher запускает один API-instance.

## Точный контракт для UI

Относительно текущего API base URL:

- `GET /api/workspace/documents/{id}/kind-discovery` — authenticated, `200 KindDiscoveryResponse`, только чтение, без запуска AI/OCR.
- `POST /api/workspace/documents/{id}/kind-discovery/retry` — только роль `Admin`, **без тела запроса**, `202 KindDiscoveryResponse`. Ставит durable-отметку очереди; Ollama синхронно в HTTP-запросе не вызывается. Обрабатывается существующим hosted indexer при следующем обходе.
- Ошибки: `400` некорректный/пустой UUID; `401` нет сессии; `403` недостаточная роль на POST; `404` документа нет; `409` повтор неприменим (архив, известная схема, назначенный вид, processing/failed, мало текста, AI выключен или выполняется локальная попытка); `429` ручной запрос раньше 30 секунд от предыдущей попытки/запроса. Ошибки приложения имеют существующий `ProblemDetails` контракт.

C# DTO; JSON — camelCase, status и errorCode — строки, даты UTC или null:

```csharp
public sealed record KindDiscoveryResponse(
    Guid DocumentId, string Status, string Message, bool CanRetry,
    DateTime? LastAttemptAtUtc, DateTime? NextRetryAtUtc,
    Guid? KindId, string? KindName, string? ErrorCode);
```

| status | Значение / UI |
| --- | --- |
| `Processing` | Ждем окончания извлечения текста. |
| `ProcessingFailed` | Сначала повторить обработку документа; код `document_processing_failed`. |
| `AiDisabled` | Предложения выключены; код `ai_disabled`. Ручное создание вида остается отдельным действием. |
| `InsufficientText` | Нужно не менее 40 непробельных по краям символов текста; код `insufficient_text`. |
| `Pending` | Первая попытка/новый fingerprint/ручная очередь/истекший интервал. |
| `Discovering` | Идет inference в этом API-процессе. |
| `RetryScheduled` | Ошибка AI или непригодный ответ; код `ai_unavailable_or_invalid_response`, следующая автоматическая попытка в `nextRetryAtUtc`. |
| `NoProposal` | Валидное определение завершилось без нового назначения; есть следующий автоматический повтор, возможно ручное создание. |
| `Suggested` | `kindId` и `kindName` указывают черновик, нужен Admin approve. |
| `Assigned` | Вид уже назначен. |
| `NotApplicable` | Техническая схема не Unknown. |
| `Archived` | Архивный документ. |

`canRetry` описывает готовность документа/таймера, не роль текущего пользователя: кнопку POST показывать только Admin. Для остальных разрешено показывать статус. Поле `nextRetryAtUtc` — нижняя граница автоматического повтора, а ручной повтор возможен раньше, когда `canRetry=true`. Для Pending после ручного запроса даты могут быть null: старый результат уже инвалидирован; 30-секундный cooldown все равно сохранен.

Существующие API создания, подтверждения и назначения вида не изменялись: Admin создает вид через `POST /api/admin/catalogs/kinds`, подтверждает AI-черновик через `POST /api/admin/catalogs/kinds/{id}/approve`, затем документ назначается через `PUT /api/workspace/documents/{id}`. Новый пользовательский вид не является новой OCR/AI-схемой извлечения.

## Фактические проверки

1. Первоначальный build с `--no-restore` получил `NETSDK1004`: у свежего worktree отсутствовал `obj/project.assets.json`. Обычный `dotnet build` восстановил реальные NuGet-зависимости; это не дефект исходников.
2. `dotnet build src/DocumentFlow.Api/DocumentFlow.Api.csproj -v minimal` и аналогичный Worker build: успешно, 0 предупреждений, 0 ошибок. После последних изменений повторены оба build с `--no-restore`: успешно, 0/0.
3. AST-разбор и импорт `tools/local_stack.py` с `python -B`, без вызова `stack`: успешно. Реально найден существующий runtime; `pgsql/bin/pg_ctl.exe` и `ollama/ollama.exe` в нем существуют. Секреты не читались этим диагностическим вызовом.
4. Настоящий launcher `configure_ocr_environment(os.environ.copy())`: `OCR preflight status: Unavailable`, системный PATH не изменился.
5. Реальная CLI-программа, ссылающаяся на production Infrastructure, без API-host/БД:

```text
ResolvedExecutable=tesseract
Health={"status":"Unavailable","availableLanguages":[],"missingLanguages":[],"errorCode":"ocr_executable_unavailable"}
NativeStartError=2; Win32Exception
OCR=[ocr_executable_unavailable] Tesseract не найден или не запускается. Установите движок и rus/eng.traineddata; укажите Tesseract:ExecutablePath. Системный PATH менять не требуется.
```

Последняя строка получена от production `TesseractOcrEngine` с существующим PNG, а не от подставного executable или HTTP-мока. Нулевой exit диагностической программы означает завершение диагностики, **не успешное распознавание**.

6. `git diff --check` для измененных backend/launcher: без ошибок. Frontend-изменения параллельных исполнителей не редактировались.

## Внешний блокер и проверка parent после объединения

Успешное распознавание `rus+eng`, нативные stderr/exit/timeout-сценарии не подтверждены: доступного Tesseract нет. Нужен согласованный установленный движок с соответствующими `rus.traineddata` и `eng.traineddata` либо путь к уже имеющейся нестандартной установке. Бинарники и языковые данные не скачивались.

Parent запускает только изолированную интеграцию: отдельная БД/хранилище, явный `DOCUMENTFLOW_RUNTIME`; реальные авторизация GET/POST (Admin/Operator/anonymous), неизвестный DOCX с достаточным текстом, доступная Ollama, временный отказ AI и последующий ручной повтор, `name:null`, подтверждение черновика и назначение. Эти HTTP/DB/AI-сценарии в данной backend-сессии **не запускались** по прямому ограничению. Проверки остановки реального зависшего OCR также остаются до появления движка. Никакие утверждения о runtime-успехе этих сценариев не делаются.

## Первичные технические источники

Официальный CLI manual подтверждает вход stdin, `--list-langs`, совместное использование с `--tessdata-dir` и рекомендует явный `--tessdata-dir` вместо неоднозначного окружения.[1] Официальная документация установки указывает типовой Windows-каталог `C:\Program Files\Tesseract-OCR` и отдельные языковые `.traineddata`; добавление в PATH не обязательно, когда задан полный путь executable.[2]

## Sources

[1] https://raw.githubusercontent.com/tesseract-ocr/tesseract/main/doc/tesseract.1.asc — Tesseract official CLI manual
[2] https://raw.githubusercontent.com/tesseract-ocr/tessdoc/main/Installation.md — Tesseract official installation documentation
