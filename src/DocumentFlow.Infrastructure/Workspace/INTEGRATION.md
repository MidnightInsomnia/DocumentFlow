# Интеграция Document Workspace

## Startup (только API host)

```csharp
using DocumentFlow.Infrastructure.Workspace;

builder.Services.AddInfrastructure(builder.Configuration); // существующий вызов
builder.Services.AddDocumentWorkspace(builder.Configuration);

// После Build(), применения общей EF migration и до app.Run():
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<WorkspaceInitializer>().InitializeAsync();
}
```

`AddDocumentWorkspace` регистрирует scoped services, singleton runtime state, named HttpClient и `WorkspaceIndexingHostedService`. Второй раз в Worker регистрировать не нужно. Initializer не применяет миграции: это обязанность существующего запуска/деплоя. Он создаёт стабильные системные виды Invoice/Contract/Act и отделы Accounting/Legal; повторный вызов не перезаписывает административные изменения. Старые завершившие обработку документы получают workspace порциями по 100.

`DocumentFlowDbContext` уже вызывает `WorkspaceModel.Configure(modelBuilder)` и `ReviewWorkModel.Configure(modelBuilder)`. Создать **общую** миграцию после интеграции всех агентов. Помимо DocumentKinds, OrganizationDepartments, DocumentWorkspace, DocumentSearchIndex нужна таблица **AiKindDiscoveryState**: она отделяет состояние/retry AI discovery от embedding-индекса.

```json
{
  "SemanticSearch": {
    "Enabled": true,
    "BaseUrl": "http://localhost:11434",
    "Model": "bge-m3",
    "BatchSize": 8,
    "IntervalSeconds": 30,
    "TimeoutSeconds": 90,
    "MaxInputCharacters": 24000
  },
  "AiReview": { "Enabled": true },
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "Model": "qwen2.5:7b-instruct",
    "TimeoutSeconds": 120
  }
}
```

Для embeddings используется `/api/embed`; для validated JSON proposal — `/api/generate`. Приложение не загружает модели и не требует платных ключей. AiReview по существующему умолчанию выключен: для автоматических черновиков его нужно включить явно. Недоступность обеих моделей не блокирует справочники и текстовый поиск.

## HTTP-контракты

Все routes требуют авторизации. `/api/admin/*` дополнительно защищены `Roles="Admin"`.

- `GET /api/catalogs` → `{ kinds:[{id,name,baseType,isDraft,isActive,isSystem,createdAutomatically,departmentId}], departments:[{id,name,isActive,isSystem}] }`. Обычный пользователь видит active non-draft виды и active отделы, Admin — все.
- `GET /api/admin/catalogs/kinds` → массив видов, без envelope.
- `POST /api/admin/catalogs/kinds` → 201 + вид; `PUT /api/admin/catalogs/kinds/{id}` → 200 + вид. Body: `{name,baseType,departmentId?,isActive?}`. `baseType` — строка `Unknown|Invoice|Contract|Act`, техническая схема системного вида неизменна.
- `DELETE /api/admin/catalogs/kinds/{id}` → `{id,deleted,deactivated,message}`. Используемый либо системный вид деактивируется; неиспользуемый пользовательский удаляется.
- `POST /api/admin/catalogs/kinds/{id}/approve` → вид с `isDraft=false,isActive=true`. Допускается пустое body либо body того же формата, что PUT: изменение и подтверждение атомарны. CreatedAutomatically сохраняется как происхождение вида. Обычный PUT не подтверждает AI-черновик.
- `GET /api/admin/catalogs/departments` → массив отделов; POST/PUT body `{name,isActive?}`, responses 201/200 + отдел; DELETE → `{id,deleted,deactivated,message}`. Использование в workspace либо kind предотвращает физическое удаление. Системные отделы деактивируются, не удаляются.
- `GET /api/workspace/documents/{id}` → `{documentId,kindId,departmentId,kindName,departmentName,kindIsDraft,kindCreatedAutomatically,resolution,resolutionReason}`. Для решения из ReviewWork есть чтение текущего `ReviewDisposition` по timestamp текущей архивации; старое решение после восстановления не возвращается.
- `PUT /api/workspace/documents/{id}` body `{kindId:null|guid,departmentId:null|guid}` → workspace. Это полная замена назначений. Только active non-draft виды и active отделы. Archive/processing → 409. Назначение метаданных **не** меняет enum Type, статус документа или подтверждённые реквизиты; для Unknown техническую схему выбирают в существующей ручной форме.
- `GET /api/search/documents` params `q,mode=text|semantic,status,type,departmentId,kindId,inn,archive=active|archived|all,page,pageSize`. Умолчания text/active/1/20; pageSize 1..100, q ≤500, inn — 1..12 цифр. Semantic требует q и ограничивает глубину `(page*pageSize)` 10000.
- Ответ поиска: `{page,pageSize,totalCount,items,semanticAvailable,indexedCount,message}`. Item содержит существующий DocumentListItem shape: `id,fileName,status,type,classificationConfidence,targetDepartment,createdAtUtc,processedAtUtc,isArchived,matchReason` плюс `number,date,inn,amount,kindName,departmentName,score,snippet`. Enum-значения строковые. Реквизиты — исходные строки либо null; score — cosine, не вероятность. Для semantic totalCount — число доступных проиндексированных результатов под фильтрами; частичная готовность явно отражается в message.
- `GET /api/search/counterparties?q=` → `{items:[{inn,documentCount}]}`. До 50 ИНН, поиск цифрового фрагмента, distinct documents; это агрегат, не задолженность.
- `GET /api/admin/search/status` → `{enabled,model,modelFingerprint,semanticAvailable,indexedCount,totalIndexedCount,eligibleDocumentCount,lastBatchAtUtc,indexedInLastBatch,rebuildRequested,message}`.
- `POST /api/admin/search/reindex?rebuild=true` (по умолчанию true) → 202 `{accepted,rebuild,message}`. Настоящее очищение/перестроение выполняется hosted service ограниченными партиями; при недоступной модели до постановки запроса возвращается 503. `rebuild=false` сохраняет существующий индекс для фонового инкрементального обновления.

Ошибка semantic → 503 ProblemDetails с `code="semantic_unavailable"`, `semanticAvailable=false`, понятным detail. Ни один semantic route не заменяется text search. Некорректные GUID/enum/page/body → 400; missing entity → 404; конфликт нормализованного имени/ссылок → 409.

## Индексирование и согласованность

Hosted worker сканирует terminal documents через GUID-keyset, не загружая весь корпус. За проход обрабатывается до BatchSize документов; embeddings пересчитываются только при изменении SHA-256 нормализованного входа либо digest модели из `/api/tags`. Вход включает имя, техническую схему, вид/отдел, до 64 полей и ограниченный OCR-текст. Metadata-only freshness обновляет timestamp без повторного inference. При изменении назначения/названия справочника соответствующие индексы инвалидируются транзакционно.

Поиск использует потоковое чтение JSON embeddings текущего digest и top-K priority queue с потолком 10000; векторы других моделей, processing и обнаруженно устаревшие индексы не ранжируются. Модель проверяется до/после inference; некорректные векторы не становятся результатами. Хранилище — PostgreSQL jsonb, без vector DB или дополнительных пакетов.

AI discovery касается только активных неархивных Unknown с текстом и без назначенного вида. Строгая JSON schema и повторная валидация ограничивают имя, запрещают номера/URL; dedupe по NFKC/регистру/пробелам/ё-е. Новый вид всегда `IsDraft=true,IsActive=false,CreatedAutomatically=true,BaseType=Unknown`. Администратор проверяет и утверждает; оригинальный документ автоматически не подтверждается. Ошибки не блокируют pipeline, retry не чаще раза в пять минут на неизменённый источник. Текст и prompts не пишутся в логи.

## Проверки этой части

Выполнены offline NuGet restore из существующего cache, сборка API и scratch executable с проверкой DI/EF model и генерации SQL настоящих search/freshness/keyset выражений через ToQueryString. Серверы, БД и inference не запускались; реальные HTTP/миграция/Ollama остаются интеграционной проверкой родителя. Тесты, migrations и commits не создавались.
