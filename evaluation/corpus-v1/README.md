# Evaluation Corpus V1

Corpus v1 — контролируемая synthetic evaluation выборка для сравнительного эксперимента Rules baseline. Она не является статистически доказательной выборкой всех документов организаций и не содержит реальных организаций, ИНН, договоров или персональных данных.

- Всего: 68 документов.
- Known types: 20 Invoice, 20 Contract, 20 Act; дополнительно 8 Unknown/negative.
- Для каждого known type: 12 TextPdf, 4 image-only ScannedPdf, 4 PNG Image.
- Difficulty: Easy — стандартные метки; Medium — вариации layout/format; Hard — OCR, необычный порядок или намеренно отсутствующее обязательное поле.
- Не менее пяти шаблонов на каждый known type; четыре negative templates.
- Примерно половина Contract не содержит optional Amount, половина Act — optional RelatedContractNumber, половина Invoice — optional KPP.
- Несколько known-type документов намеренно ожидают NeedsReview из-за отсутствующего required field.

Файлы и manifest детерминированно создаются скриптом `evaluation/scripts/generate_corpus_v1.py`. Генератор ищет доступный системный Arial, Calibri, DejaVu Sans или Liberation Sans и не копирует font-файлы в repository. TextPdf содержит text layer; ScannedPdf содержит только embedded raster image; Image хранится как PNG. Все значения manifest находятся в canonical формате.

Corpus v1 не оценивает реальные heterogeneous company documents, рукописные документы, экстремально повреждённые сканы, таблицы/line items, многостраничные договоры, иностранные языки или adversarial files.
