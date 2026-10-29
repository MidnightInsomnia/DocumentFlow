---
name: DocumentFlow Stitch Sovereign
description: Плотная рабочая система сверки документов с холодными светлыми поверхностями и точными реквизитами.
colors:
  primary: "#00236f"
  primary-hover: "#1e3a8a"
  background: "#faf8ff"
  panel: "#fff"
  soft: "#f2f3ff"
  header: "#eaedff"
  line: "#c5c5d3"
  ink: "#131b2e"
  muted: "#444651"
  line-hover: "#8c8f9e"
  placeholder: "#5f626f"
  disabled-ink: "#616373"
  teal: "#006a61"
  warning: "#9a4700"
  danger: "#ba1a1a"
  danger-ink: "#93000a"
  danger-soft: "#fff1f2"
  danger-line: "#efb2b8"
  pending-ink: "#475569"
  pending-bg: "#f1f5f9"
  pending-line: "#cbd5e1"
  processing-ink: "#264191"
  processing-bg: "#eff3ff"
  processing-line: "#b6c4ff"
  review-ink: "#934500"
  review-bg: "#fffbeb"
  review-line: "#f5d27d"
  completed-bg: "#f0fdfa"
  completed-line: "#93ded2"
  failed-ink: "#991b1b"
  failed-line: "#efb4b9"
  original: "#0f172a"
  original-toolbar: "#1e293b"
  original-ink: "#e2e8f0"
  original-line: "#334155"
  original-control-line: "#64748b"
  original-hover-line: "#94a3b8"
  selection: "#dce1ff"
typography:
  headline:
    fontFamily: "Inter, 'Segoe UI', sans-serif"
    fontSize: "25px"
    fontWeight: 700
    lineHeight: 1.25
    letterSpacing: "-.03em"
  title:
    fontFamily: "Inter, 'Segoe UI', sans-serif"
    fontSize: "16px"
    fontWeight: 600
    lineHeight: 1.5
    letterSpacing: "-.015em"
  body:
    fontFamily: "Inter, 'Segoe UI', sans-serif"
    fontSize: "13px"
    lineHeight: 1.5
    fontFeature: "'cv02', 'cv03', 'cv04', 'cv11', 'tnum'"
  label:
    fontFamily: "Inter, 'Segoe UI', sans-serif"
    fontSize: "11px"
    fontWeight: 500
    lineHeight: 1.5
  button:
    fontFamily: "Inter, 'Segoe UI', sans-serif"
    fontSize: "11px"
    fontWeight: 600
    lineHeight: 1.4
  text-button:
    fontFamily: "Inter, 'Segoe UI', sans-serif"
    fontSize: "12px"
    fontWeight: 600
    lineHeight: 1.5
  navigation:
    fontFamily: "Inter, 'Segoe UI', sans-serif"
    fontSize: "12px"
    fontWeight: 600
    lineHeight: 1.3
  mono:
    fontFamily: "'JetBrains Mono', Consolas, monospace"
    fontSize: "12px"
  review-input:
    fontFamily: "'JetBrains Mono', Consolas, monospace"
    fontSize: "13px"
    lineHeight: 1.55
  status:
    fontFamily: "'JetBrains Mono', Consolas, monospace"
    fontSize: "10px"
    fontWeight: 500
    lineHeight: 1.3
rounded:
  flat: "0"
  control: "2px"
  container: "3px"
  count: "9px"
spacing:
  micro: "3px"
  tight: "4px"
  compact: "6px"
  action: "8px"
  cell: "10px"
  control: "12px"
  section: "14px"
  group: "16px"
  panel: "18px"
  column: "20px"
  page: "22px"
  wide: "24px"
  bottom: "32px"
components:
  button-primary:
    backgroundColor: "{colors.primary}"
    textColor: "{colors.panel}"
    typography: "{typography.button}"
    rounded: "{rounded.control}"
    padding: "6px 12px"
  button-primary-hover:
    backgroundColor: "{colors.primary-hover}"
  button-secondary:
    backgroundColor: "{colors.panel}"
    textColor: "{colors.ink}"
    typography: "{typography.button}"
    rounded: "{rounded.control}"
    padding: "6px 12px"
  button-secondary-hover:
    backgroundColor: "{colors.soft}"
    textColor: "{colors.primary}"
  button-text:
    textColor: "{colors.primary}"
    typography: "{typography.text-button}"
    padding: "0"
  button-catalog-delete:
    backgroundColor: "{colors.panel}"
    textColor: "{colors.danger-ink}"
    typography: "{typography.button}"
    rounded: "{rounded.control}"
    padding: "6px 8px"
  button-catalog-delete-hover:
    backgroundColor: "{colors.danger-soft}"
  input:
    backgroundColor: "{colors.panel}"
    textColor: "{colors.ink}"
    typography: "{typography.body}"
    rounded: "{rounded.control}"
    padding: "6px 9px"
  input-review:
    backgroundColor: "{colors.panel}"
    textColor: "{colors.ink}"
    typography: "{typography.review-input}"
    rounded: "{rounded.control}"
    padding: "8px 10px"
  navigation-link:
    textColor: "{colors.muted}"
    typography: "{typography.navigation}"
    rounded: "{rounded.flat}"
    padding: "10px 13px"
  navigation-link-hover:
    backgroundColor: "{colors.soft}"
    textColor: "{colors.primary}"
  navigation-link-active:
    textColor: "{colors.primary}"
  status:
    backgroundColor: "{colors.soft}"
    textColor: "{colors.muted}"
    typography: "{typography.status}"
    rounded: "{rounded.control}"
    padding: "3px 6px"
  status-pending:
    backgroundColor: "{colors.pending-bg}"
    textColor: "{colors.pending-ink}"
  status-processing:
    backgroundColor: "{colors.processing-bg}"
    textColor: "{colors.processing-ink}"
  status-review:
    backgroundColor: "{colors.review-bg}"
    textColor: "{colors.review-ink}"
  status-completed:
    backgroundColor: "{colors.completed-bg}"
    textColor: "{colors.teal}"
  status-failed:
    backgroundColor: "{colors.danger-soft}"
    textColor: "{colors.failed-ink}"
  container:
    backgroundColor: "{colors.panel}"
    textColor: "{colors.ink}"
    rounded: "{rounded.container}"
    padding: "18px"
  original-container:
    backgroundColor: "{colors.original}"
    textColor: "{colors.original-ink}"
    rounded: "{rounded.flat}"
---

# Design System: DocumentFlow Stitch Sovereign

## Overview

**Creative North Star: "Stitch Sovereign"**

Sovereign — закреплённый пользователем визуальный мир DocumentFlow: компактное, спокойное рабочее пространство без декоративной демонстративности. Белая навигация и холодные лавандовые поверхности отделяют уровни управления; насыщенный индиго обозначает действие и текущий контекст. Тонкие границы и почти прямые углы поддерживают плотность, не превращая каждую группу данных в отдельную приподнятую карточку.

Inter обслуживает управление и чтение, JetBrains Mono — реквизиты, даты, суммы и технические подписи. Тёмная область оригинала противопоставлена светлой области ручной проверки, а не задаёт тёмную тему всего продукта. Движение подчинено смене состояния и не требуется для понимания интерфейса.

**Key Characteristics:**
- Холодные светлые поверхности с индиговым акцентом.
- Плотная типографика и моноширинная сверка реквизитов.
- Тонкие границы, небольшие радиусы и плоские контейнеры.
- Статус выражен подписью, иконкой и семантическим цветом.
- Видимый клавиатурный фокус и адаптация рабочих регионов.

Документ фиксирует повторно используемые визуальные решения фактических CSS и компонентов. `PRODUCT.md` хранит продуктовую истину; этот `DESIGN.md` — визуальные токены и правила их применения; `.impeccable/surfaces/frontend-documentflow-web-src-app-tsx.md` — стратегию конкретной поверхности и direction contract. Режим поверхности не переносится в глобальные токены. Описание реализации не добавляет нового результата QA.

## Colors

Палитра сочетает холодную бумагу и глубокий индиго; семантические цвета привязаны к состоянию, а не используются как дополнительный брендовый акцент. Значения нормативны во frontmatter; имена ниже объясняют назначение, не создавая второй шкалы.

### Primary

- **Рабочий индиго** (`primary`): основные кнопки, активная навигация, ссылки, caret и светлый focus ring.
- **Индиго наведения** (`primary-hover`): фон и граница основной кнопки при наведении.

### Neutral

- **Холодная бумага** (`background`): общий фон приложения.
- **Белая рабочая поверхность** (`panel`): шапка, формы, карточки и таблицы; также светлая надпись основной кнопки.
- **Мягкая лаванда** (`soft`): вспомогательные панели, заголовки таблиц, фон вкладок и мягкое наведение.
- **Контекстная лаванда** (`header`): выбранные фильтры и локальные hover-состояния вкладок, а не фон всей шапки.
- **Тонкая граница** (`line`) и **усиленная граница** (`line-hover`): разделение и hover обычных полей/вторичной кнопки.
- **Чернильный текст** (`ink`) и **вторичный текст** (`muted`): содержимое и подписи; `placeholder` и `disabled-ink` сохраняют отдельные роли.
- **Тёмный оригинал** (`original`, `original-toolbar`, `original-ink`, `original-line`): фон, инструменты, текст и разделители просмотра оригинала. Светлые контуры управления используют `original-control-line`, `original-hover-line` и `processing-line` для фокуса.
- **Выделение** (`selection`): выделенный пользователем текст; это не цвет статуса.

Семантические семейства `pending-*`, `processing-*`, `review-*`, `completed-*`, `failed-*` описывают именно пять состояний документа. `teal`, `warning`, `danger` также используются в метриках. `danger-ink`, `danger-soft`, `danger-line` относятся к опасным действиям и предупреждениям; красная граница не превращает AI-черновик в окончательную ошибку обработки. Отдельные оттенки очереди загрузки и предупреждений полей остаются локальными CSS, а не выдуманной общей tonal ramp.

**The Semantic Status Rule.** Цвет статуса сопровождается текстом и существующей иконкой; нельзя сообщать состояние только цветом или переиспользовать цвет ошибки как декоративный акцент.

## Typography

**Display Font:** отдельного display-шрифта и самостоятельной display-шкалы нет.

**Body Font:** Inter с fallback `'Segoe UI', sans-serif`.

**Label/Mono Font:** JetBrains Mono с fallback `Consolas, monospace`.

Обе семьи закреплены пользовательским референсом. В `fonts.css` подключены локальные TTF с `font-display: swap`: Inter — веса 400/500/600/700, JetBrains Mono — 400/500/600. Предупреждение `overused-font` оставлено осознанно: это не основание менять выбранную пару или подключать CDN. Глобально запрещён синтез начертаний; включены `cv02`, `cv03`, `cv04`, `cv11` и табличные цифры `tnum`.

### Hierarchy

- **Headline** (`headline`): основной заголовок страницы; на узком экране уменьшается до (23px). Реестр/загрузка/поиск имеют локальный заголовок (20px, 1.3; до 18px при 480px), каталоги — (27px, 1.25; до 24px при 600px). Это наблюдаемые роли, не единая математическая шкала.
- **Title** (`title`): раздел; локальные заголовки проверки/каталогов используют (14–15px), обычный третий уровень — (14px, 600).
- **Body** (`body`): основной текст и управление; списки локально используют (12px, 1.45), проверка и каталоги — (13px, 1.55). Описание каталогов ограничено (75ch), пояснение списков — (90ch); общей меры для всех текстов нет.
- **Label** (`label`): компактные подписи; в проверке подпись поля — (12px). Заголовки таблиц не превращаются в сплошной uppercase.
- **Button / Navigation** (`button`, `navigation`): короткие действия и навигация с полужирным начертанием.
- **Mono / Status** (`mono`, `review-input`, `status`): реквизиты и машинно-точные значения; поле проверки использует табличные цифры. На экране до (600px) поля проверки и каталогов увеличиваются до (16px).

**The Exact Values Rule.** Использовать JetBrains Mono для реквизитов, сумм, дат и технических подписей; основной русский текст и команды оставлять в Inter.

## Layout

Рабочая область растягивается по ширине без прежнего общего ограничения контейнера. Шапка имеет минимальную высоту (52px), строка контекста — (31px); основной отступ страницы — (22px 22px 32px). Контейнеры используют внутренний отступ `panel`, расстояния между действиями — `action`, между полями — `control`. Наблюдаемые интервалы перечислены во frontmatter: это компактный рабочий ритм, а не новая равномерная дизайн-шкала.

Таблицы сохраняют читаемую ширину внутри собственного прокручиваемого региона: реестр — минимум (1100px), поиск — (870px), загрузка — (780px), каталог видов — (850px), отделы — (640px). Числовые суммы выровнены вправо; имена и реквизиты могут переноситься. Фильтры реестра переходят с шести колонок на четыре, затем на две; поисковые фильтры — с шести на три и две.

Область сверки использует колонки `minmax(0, 3fr) minmax(360px, 2fr)` без зазора и высоту `clamp(660px, calc(100dvh - 230px), 1080px)`. Оригинал и форма имеют отдельную прокрутку. До (1050px) они располагаются последовательно; cinema занимает viewport и на узком экране прокручивается вертикально. Это описание существующего CSS, не утверждение о пройденной end-to-end проверке оригинала/split/cinema.

Реальные условия адаптации, также вынесенные в sidecar:

- **1250px:** шапка уплотняется, скрываются подсказка клавиши поиска и текст учётной записи.
- **1100px:** перестраиваются фильтры реестра и поиска.
- **1050px:** split становится одной колонкой, сводка проверки — тремя; заголовок каталогов перестраивается вертикально.
- **960px:** основная навигация переносится на отдельную строку, метрики — в три колонки; отступ страницы (20px 16px 28px).
- **900px:** сохраняется базовое условие для двухколоночных деталей и одноколоночных общих фильтров.
- **760px:** списковые фильтры становятся двухколоночными, зона загрузки уплотняется.
- **650px:** сохраняется базовое условие для одноколоночных общих форм/деталей, dashboard и переноса общих действий; локальные Sovereign-правила имеют приоритет там, где переопределяют его.
- **600px:** поиск шапки занимает отдельную строку, отступ страницы (16px 12px 24px); обычная кнопка получает минимум (44px), вкладки каталогов растягиваются, редактор каталогов становится одной колонкой. Локальные списковые/инспекционные кнопки сохраняют свои более специфичные размеры — общего обещания «всё 44px» нет.
- **480px:** заголовок списка и пагинация выстраиваются вертикально, списковая кнопка имеет минимум (36px), поисковая строка становится одной колонкой.

Минимальная ширина body — (320px). Не сжимать таблицы и split до нечитаемых колонок и не подменять существующие контрольные точки стандартным набором брейкпоинтов библиотеки.

## Elevation & Depth

Основная глубина — тональная: белая рабочая поверхность, лавандовая подложка, тонкая граница. Шапка, обычные карточки, метрики, кнопки и область сверки явно используют `box-shadow: none`. Единственная тень в рассмотренных Sovereign-поверхностях принадлежит выпадающему меню учётной записи: она отделяет перекрывающий слой, а не украшает содержимое.

### Shadow Vocabulary

- **Account popover:** `0 4px 12px #131b2e16` — меню учётной записи поверх рабочей области.
- **Flat:** `none` — шапка, карточки, кнопки, метрики и область оригинала.

**The Flat Workbench Rule.** Рабочие контейнеры плоские; не добавлять им тени вместо тональной и пограничной иерархии. Тень меню обозначает перекрытие.

Декоративного движения нет. В рассмотренных стилях явный переход есть у зоны загрузки: `background-color 140ms ease-out, border-color 140ms ease-out`. Для обычной кнопки новая длительность не назначается. При `prefers-reduced-motion: reduce` общие анимации/переходы сокращаются до (.01ms), итерация анимации — одна, прокрутка — `auto`; у зоны загрузки переход полностью отключается.

## Shapes

Контрольные элементы и статусы используют `control`, обычные контейнеры — `container`; навигация и внутренние границы split остаются прямыми (`flat`). Типовая граница — (1px). Пунктир у пустого состояния и зоны загрузки обозначает соответствующий контекст, а не общий стиль панели.

Небольшой счётчик вкладки каталогов сохраняет свой радиус `count`. Круглые точки статусов/таймлайна — локальные маркеры, а не основание превращать кнопки и badges в pills. Не переносить радиусы (2–4px) из описания референса механически: в реализации нормативны конкретные `control` и `container`.

## Components

### Buttons

Основная кнопка — компактный уверенный прямоугольник. `button-primary` задаёт индиговый фон и светлую надпись, минимум высоты — (32px); hover меняет фон и границу на `primary-hover`. `button-secondary` белая с тонкой границей; hover даёт мягкий фон, усиленную границу и индиговый текст. `button-text` прозрачная, без рамки и внутреннего отступа; hover подчёркивает надпись. У каталожного удаления используется `button-catalog-delete`: светлый фон, красные текст/граница, мягкий красный hover.

Клавиатурный focus — контур (2px) с отступом (3px); disabled обычной кнопки — opacity (.55) и запрещающий курсор. Дополнительного transform, подъёма, active-анимации или нового ghost-варианта в системе нет. Списки локально используют минимум (30px) и отступ (5px 9px), проверка/каталоги — (32px, 7px 11px), действия строки каталога — (30px, 6px 8px).

### Chips / Status

Статус документа — пассивная метка `status`, не кнопка. Пять вариантов: «Ожидает обработки», «Обрабатывается», «Требует проверки», «Завершён», «Ошибка»; каждому соответствует существующая Lucide-иконка и отдельная тройка текста/фона/границы. Не делать метки фокусируемыми ради декоративного состояния.

Кнопки фильтра статусов — отдельный интерактивный паттерн: минимум (28px), отступ (4px 8px), прозрачные фон/граница; hover — `soft`, выбранность (`aria-pressed`) — `header`, индиговый текст и граница (`#bbc5e4`). Точка рядом с подписью не заменяет подпись. Не смешивать семантику кнопки фильтра и пассивного badge.

### Cards / Containers

`container` — белая плоскость с границей, небольшим радиусом и внутренним отступом `panel`; на узком экране отступ становится `section`. Карточка сама по себе не получает hover/focus и не объявляется кликабельной: состояния принадлежат её ссылкам/кнопкам. Табличные и каталожные контейнеры могут иметь нулевой padding и отдельный toolbar; разделы формы внутри split соединены прямыми границами без внешней тени.

### Inputs / Fields

Обычное `input` — белая поверхность с тонкой границей и минимумом (32px); hover усиливает границу, focus использует общий контур. Placeholder имеет отдельный приглушённый цвет, disabled — мягкий фон и отдельный цвет текста. Чекбоксы и radio используют индиговый accent.

`input-review` — поле реквизитов, минимум (36px), моноширинное значение; `aria-invalid` меняет границу на `danger` без тени. Надпись и пояснение остаются отдельными читаемыми элементами. Предупреждение поля использует существующую тёплую подложку; это не новая success/error-проверка. Поиск шапки фокусируется через `focus-within` рамкой (2px, offset 1px), без второго контура внутреннего input.

### Navigation

Белая верхняя навигация использует `navigation-link`: приглушённую надпись, прямые углы и прозрачную нижнюю границу (2px). Hover даёт мягкий фон и индиговый текст; текущая страница сохраняет прозрачный фон и получает индиговую нижнюю линию вместе с `aria-current="page"`. Общий keyboard focus остаётся видимым. На узком экране навигация переносится и прокручивается внутри своей строки; это не новый hamburger-паттерн.

Вкладки форм и каталогов — отдельный существующий паттерн на мягкой подложке: выбранная вкладка белая с границей, hover — контекстная лаванда, состояние передаётся через `aria-selected`. Не выдавать статический snippet за реализацию переключения React-вкладок.

### Original / Review Workspace

Сигнатурный контейнер — тёмный оригинал рядом со светлой формой. `original-container` задаёт материал оригинала; toolbar и его кнопки светлее фона, hover использует `original-line`, focus — светлый индиговый контур. Тёмный материал не распространяется на шапку приложения или каталог. Полноэкранный cinema сохраняет те же материалы и существующую структуру управления.

Все иконки — семейство Lucide; у управляющей иконки есть подпись либо доступное имя. Локальные ассеты и лицензии не заменяются внешними icon fonts. Sidecar содержит самостоятельные HTML/CSS-образцы существующих примитивов, а не новые компоненты и не runtime приложения.

## Do's and Don'ts

### Do:

- **Do** сохранять закреплённый мир Stitch Sovereign и холодную светлую палитру.
- **Do** брать повторно используемые значения из frontmatter, а локальные исключения — из соответствующего CSS.
- **Do** разделять Inter для управления и JetBrains Mono для точных значений.
- **Do** показывать видимый keyboard focus и текстовое обозначение статуса вместе с иконкой.
- **Do** сохранять самостоятельную прокрутку таблиц и последовательное расположение сверки на узком экране.
- **Do** оставлять рабочие контейнеры плоскими и учитывать reduced motion.

### Don't:

- **Don't** менять Inter/JetBrains Mono из-за предупреждения overused-font или подключать CDN вместо локальных шрифтов.
- **Don't** возвращать тёмную общую SaaS-шапку, крупные скруглённые карточки и разреженные формы отвергнутого оформления.
- **Don't** придумывать новые варианты кнопок, tonal ramps, тени или анимации при расширении документации.
- **Don't** превращать пассивный status badge в интерактивный элемент или сообщать состояние только цветом.
- **Don't** подменять продуктовую истину и контракт поверхности визуальной документацией или добавлять неподдерживаемые действия ради макета.
- **Don't** выдавать описание CSS и статические образцы за новое end-to-end подтверждение работы приложения.
