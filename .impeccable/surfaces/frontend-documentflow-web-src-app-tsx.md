---
version: 1
slug: "frontend-documentflow-web-src-app-tsx"
primary_target: "frontend/documentflow-web/src/App.tsx"
related_targets: ["frontend/documentflow-web/src/components/Layout.tsx","frontend/documentflow-web/src/sovereign.css"]
---

# DocumentFlow Sovereign — surface brief

Mode: Operate. Scope: весь существующий React frontend DocumentFlow.

## Direction contract

THESIS: перенести выбранную пользователем рабочую систему Stitch Sovereign, сохранив настоящую обработку документов. Убрать прежнюю тёмно-синюю SaaS-шапку, крупные скруглённые карточки и разреженные формы.

OWN-WORLD: Inter для управления; JetBrains Mono для реквизитов/сумм/дат; белая верхняя навигация, холодные лавандовые поверхности #faf8ff/#f2f3ff, индиго #00236f/#1e3a8a, тонкие границы, радиусы 2–4px, честные цветовые статусы. Иконки одного семейства Lucide.

STORY: оператор находит документ, сверяет исходник и поля, сохраняет/подтверждает; администратор управляет AI-черновиками видов и отделами. Не копировать неподдерживаемые ERP/ЭЦП/OCR overlay/фиктивные счётчики из экспортированного HTML.

FIRST VIEWPORT: общая компактная шапка 52px с навигацией и загрузкой, строка контекста 31px, под ней рабочая таблица либо splitview с тёмным исходником слева и светлой формой справа. Реестр — строка статусов и фильтров, таблица и пагинация; админка — заголовок, tabs и каталоги. На узком экране управление переносится, таблицы прокручиваются внутри своего региона, split не сжимается в нечитаемые колонки.

FORM: пользовательский фиксированный референс, seed key `user-pinned-stitch-sovereign`; генерация альтернатив и concept roll не нужны. Референсы: исходные 1_/2_/4_ screen.png и все четыре code.html. 3_/screen.png повреждён. Signature: плотная сверка моноширинных реквизитов с оригиналом; без декоративного движения, только focus/hover/state.

FINISH: unreviewed and undocumented is unfinished; this build ends with the finish review, the verdict, DESIGN.md, and every shipping raster carrying its provenance

## Execution and evidence

Последнее подтверждение пользователя: только изменить дизайн, не разворачивать. Рабочие данные и master не менять. Изолированный worktree/ветка; API и contracts неизменны. Для QA допустимы только сборка и статический browser-render настоящих компонентов без API/БД/фиктивных ответов; такие captures не доказывают end-to-end функционирование. Отсутствующее покрытие обозначить явно.
