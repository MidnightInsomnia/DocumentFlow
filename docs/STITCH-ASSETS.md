# Дополнительные ассеты дизайна Stitch Sovereign

Эта запись дополняет `THIRD_PARTY_NOTICES.md` для ветки редизайна. npm/NuGet dependencies и lock-файлы не менялись.

## Шрифты

Inter (400/500/600/700) и JetBrains Mono (400/500/600) выбраны самим референсом. Используются семь локальных TTF; при открытии приложения загрузка Google Fonts не требуется. Файлы находятся в `frontend/documentflow-web/public/fonts/`, объявления — `src/fonts.css`.

Первоначальное получение: официальный CSS Google Fonts. URL CSS, исходный URL каждого файла и его SHA-256 записаны в `public/fonts/provenance.json`. Все перечисленные хеши проверены после получения. Исходные SIL Open Font License сохранены в `public/licenses/INTER-OFL.txt` и `public/licenses/JETBRAINS-MONO-OFL.txt` и входят в production build. Имена шрифтов не менялись, бинарные файлы не модифицировались.

## Иконки

Небольшие SVG-иконки одного семейства Lucide адаптированы из локального upstream clone `C:/SRC/PL/projects/ui-reference-libraries/lucide/icons/` в `src/components/Icon.tsx`. Оригинальная лицензия и включённые upstream уведомления сохранены полностью в `public/licenses/LUCIDE.txt`. Нового npm-пакета нет. У иконок декоративный `aria-hidden`, смысл задают текст или доступное имя управляющего элемента.

## Референсы

Пользователь предоставил `stitch_project_design_concept`: это источник композиции и токенов, а не шаблон данных/интеграций. Экспортированные HTML, PNG и удалённые Tailwind/Material CDN не входят в runtime приложения. Новый frontend не требует CDN. `3_/screen.png` содержит сообщение об ошибке вместо изображения; для загрузки использован его `code.html`.

Новых растровых иллюстраций, скриншотов под видом данных приложения или сгенерированных документов в production assets не добавлено. Статические QA-снимки отделены от поставляемого интерфейса и не доказывают работу backend.
