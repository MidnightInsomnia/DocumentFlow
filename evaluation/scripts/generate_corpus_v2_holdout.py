"""Deterministically generate the frozen synthetic DocumentFlow holdout corpus v2."""

import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.pdfgen import canvas

ROOT = Path(__file__).parents[1] / "corpus-v2-holdout"
FILES = ROOT / "files"
FILES.mkdir(parents=True, exist_ok=True)


def resolve_font():
    candidates = [
        Path(r"C:\Windows\Fonts\arial.ttf"),
        Path(r"C:\Windows\Fonts\calibri.ttf"),
        Path("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"),
        Path("/usr/share/fonts/truetype/liberation2/LiberationSans-Regular.ttf"),
        Path("/Library/Fonts/Arial.ttf"),
    ]
    for candidate in candidates:
        if candidate.is_file():
            return candidate
    raise FileNotFoundError("No supported system font was found.")


FONT_PATH = resolve_font()
pdfmetrics.registerFont(TTFont("HoldoutSans", FONT_PATH))


def render_image(lines, difficulty, variant):
    sizes = {"Easy": (1500, 950, 38), "Medium": (1350, 880, 33), "Hard": (1250, 840, 30)}
    width, height, font_size = sizes[difficulty]
    image = Image.new("RGB", (width, height), "white")
    draw = ImageDraw.Draw(image)
    font = ImageFont.truetype(FONT_PATH, font_size + variant % 2)
    y = 70
    for index, line in enumerate(lines):
        x = 65 + ((index + variant) % 3) * (12 if difficulty != "Easy" else 0)
        draw.text((x, y), line, fill="black", font=font)
        y += font_size + (22 if difficulty == "Easy" else 18)
    if difficulty == "Medium":
        image = ImageEnhance.Contrast(image).enhance(0.94)
    elif difficulty == "Hard":
        image = image.rotate(1 if variant % 2 else -1, expand=False, fillcolor="white")
        image = image.filter(ImageFilter.GaussianBlur(0.35))
        image = ImageEnhance.Contrast(image).enhance(0.90)
    return image


def write_document(filename, source, difficulty, lines, variant):
    path = FILES / filename
    if source == "TextPdf":
        pdf = canvas.Canvas(str(path), invariant=1)
        pdf.setFont("HoldoutSans", 13 + variant % 3)
        y = 790
        for index, line in enumerate(lines):
            pdf.drawString(48 + ((index + variant) % 3) * 18, y, line)
            y -= 26 + variant % 3
        pdf.save()
        return
    image = render_image(lines, difficulty, variant)
    if source == "Image":
        image.save(path, optimize=True)
        return
    temporary = FILES / f".{filename}.png"
    image.save(temporary, optimize=True)
    pdf = canvas.Canvas(str(path), pagesize=(595, 842), invariant=1)
    pdf.drawImage(str(temporary), 28, 430, width=540, height=380, preserveAspectRatio=True)
    pdf.save()
    temporary.unlink()


def entry(identifier, source, difficulty, template, expected_type, status, department, fields, lines, variant):
    extension = "png" if source == "Image" else "pdf"
    filename = f"{identifier}.{extension}"
    write_document(filename, source, difficulty, lines, variant)
    return {
        "id": identifier,
        "file": f"files/{filename}",
        "sourceKind": source,
        "difficulty": difficulty,
        "templateId": template,
        "contentType": "image/png" if source == "Image" else "application/pdf",
        "expectedType": expected_type,
        "expectedStatus": status,
        "expectedDepartment": department,
        "expectedFields": fields,
    }


def known_source(index):
    return "TextPdf" if index <= 5 else "ScannedPdf" if index <= 9 else "Image"


def known_difficulty(index):
    return ["Easy", "Medium", "Hard", "Easy", "Medium", "Easy", "Medium", "Hard", "Hard", "Easy", "Medium", "Hard"][index - 1]


documents = []
invoice_templates = ["invoice-holdout-a", "invoice-holdout-b", "invoice-holdout-c", "invoice-holdout-d", "invoice-holdout-e", "invoice-holdout-f"]
for i in range(1, 13):
    source, difficulty = known_source(i), known_difficulty(i)
    number = ["HX-701", "2026-H/02", "СФХ-03", "7404", "PAY-005", "H-06/26", "7717", "BILL-08", "ХС-009", "7810", "HOLD-11", "812/Н"][i - 1]
    date = f"2026-10-{i + 2:02d}"
    inn = f"780000{i:04d}"
    kpp = f"7802{i:05d}"[:9]
    amount = f"{210000 + i * 1375:.2f}"
    fields = {"Number": number, "Date": date, "INN": inn, "Amount": amount}
    if i % 2 == 0:
        fields["KPP"] = kpp
    if i == 4:
        fields.pop("Amount")
    if i == 9:
        fields.pop("INN")
    labels = [
        f"ПЛАТЕЖНЫЙ СЧЕТ {number}",
        f"Документ к оплате: {number}",
        f"Счет поставщика № {number}",
        f"Оплата по счету {number}",
        f"Расчетный документ № {number}",
        f"СЧЕТ ДЛЯ ОПЛАТЫ — {number}",
    ]
    lines = [labels[(i - 1) % 6], f"Дата оформления: {date[8:10]}.{date[5:7]}.{date[:4]}"]
    if "INN" in fields:
        lines.append(f"Реквизиты поставщика: ИНН {inn}" + (f"; КПП {kpp}" if "KPP" in fields else ""))
    if "Amount" in fields:
        lines.append(f"К перечислению: {amount.replace('.', ',')} RUB")
    lines += ["Поставщик: ООО Учебный Контур", "Покупатель: ООО Тестовый Получатель"]
    status = "NeedsReview" if i in (4, 9) else "Completed"
    department = "Unassigned" if status == "NeedsReview" else "Accounting"
    documents.append(entry(f"invoice-holdout-{source.lower()}-{i:03d}", source, difficulty,
                           invoice_templates[(i - 1) % 6], "Invoice", status, department, fields, lines, i))

contract_templates = ["contract-holdout-a", "contract-holdout-b", "contract-holdout-c", "contract-holdout-d", "contract-holdout-e", "contract-holdout-f"]
for i in range(1, 13):
    source, difficulty = known_source(i), known_difficulty(i)
    number = ["HC-901", "СГ-22/2", "HOLD-C3", "944", "C-905", "96/К", "HCT-07", "988", "КОН-09", "9100", "H-CTR-11", "912/26"][i - 1]
    date = f"2026-11-{i + 1:02d}"
    amount = f"{320000 + i * 2110:.2f}"
    fields = {"Number": number, "Date": date}
    if i <= 6:
        fields["Amount"] = amount
    if i == 8:
        fields.pop("Number")
    headers = ["СОГЛАШЕНИЕ СТОРОН", "Договор поставки", "КОНТРАКТ", "Договор оказания услуг", "Хозяйственный договор", "Соглашение о работах"]
    lines = [headers[(i - 1) % 6]]
    if "Number" in fields:
        lines.append(f"Регистрационный номер: {number}")
    lines += [f"Дата подписания {date[8:10]}.{date[5:7]}.{date[:4]}", "Сторона 1: ООО Проектный Макет", "Сторона 2: ООО Синтетический Партнер"]
    if "Amount" in fields:
        lines.append(f"Общая стоимость соглашения — {amount.replace('.', ',')} рублей")
    status = "NeedsReview" if i == 8 else "Completed"
    department = "Unassigned" if status == "NeedsReview" else "Legal"
    documents.append(entry(f"contract-holdout-{source.lower()}-{i:03d}", source, difficulty,
                           contract_templates[(i - 1) % 6], "Contract", status, department, fields, lines, 20 + i))

act_templates = ["act-holdout-a", "act-holdout-b", "act-holdout-c", "act-holdout-d", "act-holdout-e", "act-holdout-f"]
for i in range(1, 13):
    source, difficulty = known_source(i), known_difficulty(i)
    number = ["HA-301", "АКХ-32", "H-ACT-03", "344", "A-305/26", "366", "HOLD-A7", "388", "АК-309", "3100", "HA-11", "312/Р"][i - 1]
    date = f"2026-12-{i + 1:02d}"
    amount = f"{91000 + i * 975:.2f}"
    contract = f"HC-{700 + i}"
    fields = {"Number": number, "Date": date, "Amount": amount}
    if i <= 6:
        fields["RelatedContractNumber"] = contract
    if i == 7:
        fields.pop("Date")
    if i == 12:
        fields.pop("Amount")
    headers = ["Документ приемки работ", "АКТ РЕЗУЛЬТАТОВ", "Акт оказанных услуг", "Приемочный акт", "Акт сдачи работ", "Итоговый акт"]
    lines = [headers[(i - 1) % 6], f"Номер акта: {number}"]
    if "Date" in fields:
        lines.append(f"Оформлен {date[8:10]}.{date[5:7]}.{date[:4]}")
    if "RelatedContractNumber" in fields:
        lines.append(f"Основание — договор {contract}")
    if "Amount" in fields:
        lines.append(f"Стоимость принятых работ: {amount.replace('.', ',')} руб.")
    lines.append("Работы приняты без замечаний")
    status = "NeedsReview" if i in (7, 12) else "Completed"
    department = "Unassigned" if status == "NeedsReview" else "Accounting"
    documents.append(entry(f"act-holdout-{source.lower()}-{i:03d}", source, difficulty,
                           act_templates[(i - 1) % 6], "Act", status, department, fields, lines, 40 + i))

unknowns = [
    ("TextPdf", "Easy", "unknown-holdout-letter", ["Информационное письмо", "Сообщаем об изменении графика консультаций."]),
    ("TextPdf", "Medium", "unknown-holdout-memo", ["Внутренняя служебная записка", "Просьба подготовить переговорную комнату."]),
    ("TextPdf", "Hard", "unknown-holdout-protocol", ["Протокол рабочей встречи", "Обсуждены этапы учебного проекта и контрольные даты."]),
    ("Image", "Easy", "unknown-holdout-notice", ["УВЕДОМЛЕНИЕ", "Техническое обслуживание состоится в выходной день."]),
    ("Image", "Medium", "unknown-holdout-request", ["ЗАЯВКА", "Необходимо предоставить тестовое оборудование для демонстрации."]),
    ("Image", "Hard", "unknown-holdout-message", ["Информационное сообщение", "Материалы семинара размещены во внутреннем учебном каталоге."]),
]
for i, (source, difficulty, template, lines) in enumerate(unknowns, 1):
    documents.append(entry(f"unknown-holdout-{source.lower()}-{i:03d}", source, difficulty, template,
                           "Unknown", "NeedsReview", "Unassigned", {}, lines, 60 + i))

manifest = {"documents": documents}
(ROOT / "manifest.json").write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(f"Generated {len(documents)} holdout documents in {ROOT}")
