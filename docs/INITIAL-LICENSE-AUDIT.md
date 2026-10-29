# Third-party notices — DocumentFlow

Дата проверки: **23 октября 2026 года**. Это техническая инвентаризация фактических метаданных и уведомлений, **не юридическое заключение и не гарантия отсутствия иных прав/обязательств**.

## Статус: ImageSharp удалён, строгий permissive-only gate НЕ пройден

Прямая ссылка `SixLabors.ImageSharp` удалена из runtime-проекта Infrastructure; TIFF-декодирование и PNG-рендеринг переведены на `Magick.NET-Q8-AnyCPU 15.0.0`. Сам .NET-пакет объявляет `Apache-2.0`, однако **эта декларация не перелицензирует его встроенные native-библиотеки**.[1]

В фактически скачанном `Notice.txt` этого пакета присутствуют `cairo` с текстом `MPL-1.1` и библиотеки с текстами LGPL: `libcroco`, `libde265`, `fribidi`, `gdk-pixbuf`, `glib`, `libheif`, `liblqr`, `liblzma`, `pango`, `libraw`, `librsvg`. Полный оригинал сохранён: `licenses/nuget/magick.net-q8-anycpu/15.0.0/Notice.txt`. В поставляемом Win-x64 DLL статически обнаружены строки `libheif`, `libde265`, `cairo`, `LibRaw`, `LGPL`; SHA-256 и смещения находятся в JSON. Это подтверждающие признаки, не полный native SBOM.

**Нельзя честно назвать стандартный Magick.NET AnyCPU бинарный комплект «только permissive».** Выбор TIFF-кодера, отключение слоёв или неиспользование HEIF не удаляет встроенные компоненты из распространяемого DLL. Сохранение NOTICE также само по себе не выполняет все возможные LGPL/MPL-обязательства. Для строгого критерия требуется другой проверенный TIFF-декодер либо отдельно собранный и проинвентаризированный TIFF/PNG-only native-комплект без таких компонентов. Эта работа в текущую замену не включена.

LGPL/MPL не равнозначны платному ключу или коммерческой подписке и не делают всё приложение автоматически GPL. Но они **не являются permissive-лицензиями**; при распространении могут возникать требования к исходникам соответствующих библиотек/изменений и материалам повторной линковки. Эффективное `-only`/`-or-later` разрешение и условия конкретной сборки нельзя выводить только из заголовка стандартного текста лицензии. В JSON для таких случаев оставлен `UNKNOWN`, отдельно от наблюдаемого SPDX текста.

## Что именно проверено

- **NuGet:** все восемь `.csproj` — приложение, evaluation и существующие тестовые проекты; **117 уникальных package/version** в восстановленных графах `net10.0`. Из них 101 декларация `MIT`, 13 `Apache-2.0`, 2 `PostgreSQL`, 1 `UNKNOWN`.
- Restore исходного Infrastructure прошёл. Для остальных графов использованы **копии `.csproj` в scratch**, без сборки API/solution и без запуска тестов. `.nuspec`, пакетные LICENSE/COPYING/NOTICE/third-party notices скопированы из реального NuGet global cache, версии и SHA-512 взяты из `project.assets.json`.
- **npm:** весь `frontend/documentflow-web/package-lock.json`, включая dev/test, optional и platform-specific узлы: **203 расположения, 201 уникальная package/version**. Все 201 tarball скачаны только в scratch и проверены по SRI из lock; лицензии `package.json` совпали с lock. Ни `package.json`, ни lock, ни `node_modules` этой работой не изменялись.
- Для каждого пакета в `dependency-licenses.json` указаны точные версия/SPDX-декларация, проекты или расположения, direct/transitive scope, dependency edges, первичный источник и evidence с SHA-256. Декларация package SPDX **не считается разрешением всех встроенных компонентов**.
- Полные найденные уведомления сохранены в `licenses/nuget/`, `licenses/npm/`, `licenses/upstream/`. License-тексты намеренно не переводились и не редактировались; JSON/метаданные не подменяют оригинальные grant/copyright notices.
- Скомпилированы только изменённые TIFF-файлы в изолированной scratch-библиотеке с реальным `Magick.NET 15.0.0`, `TreatWarningsAsErrors=true`: **0 ошибок, 0 предупреждений**. Это проверка совместимости API, **не реальный прогон TIFF/OCR/HTTP**. Полную интеграцию выполняет родительский процесс.

## Прямые NuGet зависимости application-проектов

Это декларации `.nuspec`, а не утверждение о лицензиях всего native-содержимого. `Microsoft.EntityFrameworkCore.Design` — build/design-time пакет; наличие в application-проекте не означает, что он публикуется в runtime.

| Пакет | Версия | SPDX из `.nuspec` |
|---|---|---|
| `DocumentFormat.OpenXml` | `3.3.0` | `MIT` |
| `Magick.NET-Q8-AnyCPU` | `15.0.0` | `Apache-2.0` |
| `Microsoft.AspNetCore.Authentication.JwtBearer` | `10.0.4` | `MIT` |
| `Microsoft.AspNetCore.Identity.EntityFrameworkCore` | `10.0.4` | `MIT` |
| `Microsoft.AspNetCore.OpenApi` | `10.0.10` | `MIT` |
| `Microsoft.EntityFrameworkCore.Design` | `10.0.4` | `MIT` |
| `Microsoft.EntityFrameworkCore` | `10.0.4` | `MIT` |
| `Microsoft.Extensions.Configuration.Abstractions` | `10.0.10` | `MIT` |
| `Microsoft.Extensions.Configuration.Json` | `10.0.10` | `MIT` |
| `Microsoft.Extensions.DependencyInjection.Abstractions` | `10.0.10` | `MIT` |
| `Microsoft.Extensions.Hosting` | `10.0.10` | `MIT` |
| `Microsoft.Extensions.Logging.Abstractions` | `10.0.10` | `MIT` |
| `Microsoft.Extensions.Options.ConfigurationExtensions` | `10.0.10` | `MIT` |
| `Microsoft.OpenApi` | `2.12.2` | `MIT` |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | `10.0.3` | `PostgreSQL` |
| `PdfPig` | `0.1.16` | `Apache-2.0` |
| `PDFtoImage` | `5.4.0` | `MIT` |

Полный перечень, включая транзитивные, evaluation и тестовые зависимости: `dependency-licenses.json` → `nuget`, `nugetDependencyEdges`, `inputs.nugetProjects`.

### Дополнительные native-ограничения существующего PDF-стека

1. **PDFtoImage 5.4.0 — MIT**, его исходный LICENSE на commit из `.nuspec` сохранён. PDF-путь не переписан: остаются PDFtoImage/PDFium/SkiaSharp. **Ghostscript не добавлен, не установлен и не вызывается этой реализацией**.
2. **SkiaSharp / NativeAssets 4.150.1 — MIT на уровне NuGet**, но пакет содержит большой агрегированный `THIRD-PARTY-NOTICES.txt`.[2] В нём есть старый GIF-код (`MPL-1.1 OR GPL-2.0-or-later OR LGPL-2.1-or-later`), `libmicrohttpd` с условной LGPL/eCos альтернативой, а также отдельные условия Adobe DNG SDK. Это **не доказательство**, что каждый перечисленный компонент реально включён в конкретный текущий RID: такой notice может быть шире сборки. И обратное — MIT заголовка NuGet недостаточно для заявления, что бинарный стек строго permissive. В JSON оставлен `UNKNOWN` для фактического target-specific покрытия, без автоматического выбора GPL. Для FreeType допустимая FTL-ветка сохраняет отдельные attribution-условия.
3. **bblanchon.PDFium 152.0.7961** для Linux/macOS/Win32 объявляет `Apache-2.0` в `.nuspec`. LICENSE репозитория упаковщика на указанном commit содержит MIT; LICENSE ветки самого PDFium `chromium/7961` содержит BSD-3-Clause и Apache-2.0 тексты.[3] Это разные слои, а не произвольный выбор `MIT OR Apache`. Полный перечень native-компонентов каждой DLL/so в NuGet-пакете отсутствует; исходные тексты сохранены, но завершённый binary-license manifest **не заявляется**.

### Неопределённость старого тестового пакета

`xunit.abstractions 2.0.3` не содержит SPDX/license-file, только подвижный `licenseUrl`. Текущий upstream URL содержит Apache-2.0 и MIT notices, но не был надёжно привязан к историческому артефакту 2.0.3. Поэтому его лицензия в JSON — **`UNKNOWN`**, а не «проверено».[8]

Существующие `TiffProcessingTests.cs` и `TiffPreviewControllerTests.cs` всё ещё импортируют ImageSharp для генерации/проверки образцов. Они **вне разрешённого scope**, не изменялись и не запускались. После удаления транзитивной ссылки такие legacy-шаблоны могут не собраться; это не runtime-зависимость приложения и не основание возвращать ImageSharp в поставку.

## Прямые frontend-зависимости

Версии — фактически зафиксированные в lock, а не диапазоны из `package.json`.

| Пакет | Версия | SPDX | Scope |
|---|---|---|---|
| `@testing-library/react` | `16.3.3` | `MIT` | dev/test |
| `@types/react` | `19.3.0` | `MIT` | dev/test |
| `@types/react-dom` | `19.3.0` | `MIT` | dev/test |
| `@vitejs/plugin-react` | `5.2.0` | `MIT` | dev/test |
| `docx-preview` | `0.4.1` | `Apache-2.0` | production dependency |
| `jsdom` | `30.1.1` | `MIT` | dev/test |
| `react` | `19.3.0` | `MIT` | production dependency |
| `react-dom` | `19.3.0` | `MIT` | production dependency |
| `react-router-dom` | `7.18.4` | `MIT` | production dependency |
| `typescript` | `5.8.3` | `Apache-2.0` | dev/test |
| `vite` | `7.3.6` | `MIT` | dev/test |
| `vitest` | `5.0.2` | `MIT` | dev/test |

### Условия важных транзитивных frontend-пакетов

- `jszip 3.10.2`: `(MIT OR GPL-3.0-or-later)`; для этой поставки **выбрана MIT-ветка**, что разрешено самим архивным LICENSE. Наличие альтернативного текста GPL не делает её обязательной. Обе ветки оригинального notice сохранены без удаления.
- `pako 1.0.11`: **`MIT AND Zlib`**, не просто MIT. Сохранены и корневой LICENSE, и `package/lib/zlib/README` с отдельными Zlib условиями/авторами.
- `caniuse-lite 1.0.30001810`: **`CC-BY-4.0`** для данных. Не называть её MIT или OSI software license; при распространении соответствующего материала сохранять авторство, лицензию и сведения об изменениях в требуемом объёме. Здесь данные не изменялись.
- `mdn-data 2.27.1`: `CC0-1.0`; `lru-cache 11.5.3`: `BlueOak-1.0.0`. BSD/ISC/MIT-0 и прочие декларации перечислены по каждому пакету в JSON.
- Platform tarballs esbuild/rollup не содержат отдельного LICENSE; рядом сохранены уведомления парного основного пакета **той же версии**. Это не доказывает полный состав/уведомления скомпилированного Go/native payload esbuild: остаётся `UNKNOWN-native-notice-completeness`.
- `saxes 6.0.0` не вложил LICENSE в npm-архив; ISC текст восстановлен из upstream-тега `v6.0.0`, источник указан отдельно в evidence.
- `@napi-rs/lzma-linux-x64-gnu 1.5.1` объявляет MIT, но опубликованный binary tarball **не содержит license/attribution текста**. Проверен также архив parent-пакета той же версии — там его тоже нет. Источник полного уведомления не восстановлен; это явный пробел, не pass. Пакет платформенный/dev и не является браузерным runtime-кодом, но входит в lock и не исключён из инвентаризации.

## Внешние локальные модели и инструменты

| Компонент | Что подтверждено | Что НЕ подтверждено |
|---|---|---|
| `BAAI/bge-m3` | `MIT` в первичной model card.[4] | Конкретный Ollama/GGUF digest, лицензии конкретного repack/квантизации и полного окружения. |
| `Qwen/Qwen2.5-7B-Instruct` | `Apache-2.0` в первичном LICENSE.[5] | Другие размеры/версии Qwen и конкретный установленный repack; переносить разрешение на них автоматически нельзя. |
| Ollama | `MIT` исходного кода сервера.[6] | Состав конкретного бинарного установщика, bundled libs/GPU runtime и лицензии загруженных моделей. |
| Tesseract | `Apache-2.0` исходного OCR-движка.[7] | Конкретный installer, Leptonica/codecs и каждый файл traineddata. |

Новые платные API, лицензионные ключи или подписки этой заменой не добавлены. Локальный запуск всё равно требует ресурсов машины и соблюдения условий соответствующих свободных лицензий. Инвентаризация NuGet/npm не охватывает автоматически .NET shared framework/runtime packs, ОС, шрифты, PostgreSQL server distribution, системные codecs/GPU drivers или данные пользователя.

## Технические границы TIFF-замены

- Настоящие TIFF IFD-страницы, 1-based API/OCR страницы; чтение native через `FrameIndex` и `FrameCount=1`, Photoshop layers не считаются отдельными документными страницами.
- Classic TIFF и BigTIFF, обе endian-схемы; проверяется **каждая** IFD до native-выделения пикселей. Проверки циклов, количества записей, отсутствующих/дублированных размеров, согласованности native-размера с заголовком.
- До **30 страниц**, **6000 px** по стороне, **12 млн px** на страницу, **48 млн px** суммарно, **100 МиБ** входного потока. Смешанные размеры страниц считаются по фактической сумме, а не размеру первой страницы × количество.
- Native pixel-cache budget **512 МиБ на процесс**, max memory request **128 МиБ**, profile **4 МиБ**, **2 native thread**, disk pixel-cache **0** — при исчерпании лимита ошибка, не выгрузка decoded-страниц на диск. Это **не ограничитель полного RSS**: память codecs/CLR/потоков может быть вне pixel cache. Конкурентные запросы делят process-wide budget; сильная изоляция в отдельном процессе этой заменой не добавлена.
- У каждой страницы применяется собственная orientation, metadata/profiles удаляются из PNG после ориентации. PNG формируется в памяти. Cancellation проверяется между стадиями и через native Progress; это **кооперативная отмена**, не жёсткий per-request CPU/deadline watchdog.
- JPEG/PNG OCR и PDF-путь не менялись. HTTP-контракт `preview/pages` / `preview/pages/{page}` и покадровый OCR сохранены. Реальные multi-page/CCITT/LZW/Deflate/ориентация/повреждённые TIFF должен подтвердить интеграционный прогон; изолированная компиляция такого подтверждения не заменяет.

## Перед распространением

1. **Не выдавать текущий image/native dependency tree за прошедший permissive-only gate.** Разрешить перечисленные copyleft/custom/unknown вопросы либо заменить/пересобрать соответствующий бинарный состав.
2. Поставлять применимые оригинальные license/copyright/NOTICE вместе с артефактами; не удалять их при trimming/упаковке. Для Apache сохранять применимые NOTICE и отмечать изменения; для MIT/BSD/ISC/PostgreSQL/FTL/Zlib соблюдать соответствующие условия. Общий текст MIT без авторства конкретного компонента не заменяет его copyright notice.
3. Для LGPL/MPL недостаточно одной ссылки на этот документ. Состав и характер линковки, исходники, relinking/installation information и допустимые варианты соблюдения требуют отдельной проверки применительно к реальному распространяемому бинарнику.
4. При обновлении зависимостей заново restore в изолированной копии, извлечь `.nuspec` и реальные license-файлы, прочитать весь npm-lock, проверить tarball integrity и встроенные native notices. Сравнить входные SHA-256 с `inputs` в JSON; старый snapshot не распространяется на новые версии.
5. Отдельно фиксировать RID, native binary hashes и модельные digests поставки. Отсутствие находок не является ни юридической гарантией, ни подтверждением отсутствия лицензии.

## Sources

[1] https://www.nuget.org/packages/Magick.NET-Q8-AnyCPU/15.0.0
[2] https://www.nuget.org/packages/SkiaSharp.NativeAssets.Win32/4.150.1
[3] https://pdfium.googlesource.com/pdfium/+/refs/heads/chromium/7961/LICENSE?format=TEXT
[4] https://huggingface.co/BAAI/bge-m3/raw/main/README.md
[5] https://huggingface.co/Qwen/Qwen2.5-7B-Instruct/raw/main/LICENSE
[6] https://raw.githubusercontent.com/ollama/ollama/main/LICENSE
[7] https://raw.githubusercontent.com/tesseract-ocr/tesseract/main/LICENSE
[8] https://www.nuget.org/packages/xunit.abstractions/2.0.3
