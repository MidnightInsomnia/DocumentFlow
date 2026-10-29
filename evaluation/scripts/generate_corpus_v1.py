"""Deterministically generate the synthetic DocumentFlow research corpus v1."""

import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.pdfgen import canvas

ROOT = Path(__file__).parents[1] / "corpus-v1"
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
    raise FileNotFoundError(
        "No supported system font was found. Install Arial, Calibri, DejaVu Sans, or Liberation Sans."
    )


FONT_PATH = resolve_font()
pdfmetrics.registerFont(TTFont("Arial", FONT_PATH))


def source_for(index):
    if index <= 12:
        return "TextPdf", "pdf"
    if index <= 16:
        return "ScannedPdf", "pdf"
    return "Image", "png"


def render_image(lines, scale=1.0):
    image = Image.new("RGB", (1400, 900), "white")
    draw = ImageDraw.Draw(image)
    font = ImageFont.truetype(FONT_PATH, int(34 * scale))
    y = 80
    for line in lines:
        draw.text((75, y), line, fill="black", font=font)
        y += int(58 * scale)
    return image


def write_document(filename, source_kind, lines, index):
    path = FILES / filename
    if source_kind == "TextPdf":
        pdf = canvas.Canvas(str(path), invariant=1)
        pdf.setFont("Arial", 14 + index % 2)
        y = 780
        for line in lines:
            pdf.drawString(55 + (index % 3) * 8, y, line)
            y -= 28 + index % 2
        pdf.save()
    elif source_kind == "ScannedPdf":
        image = render_image(lines, 0.95 + (index % 3) * 0.05)
        temporary = FILES / f".{filename}.png"
        image.save(temporary, optimize=True)
        pdf = canvas.Canvas(str(path), pagesize=(595, 842), invariant=1)
        pdf.drawImage(str(temporary), 30, 440, width=535, height=344, preserveAspectRatio=True)
        pdf.save()
        temporary.unlink()
    else:
        render_image(lines, 0.95 + (index % 3) * 0.05).save(path, optimize=True)


def entry(identifier, filename, source_kind, difficulty, template, expected_type,
          expected_status, expected_department, fields):
    return {
        "id": identifier,
        "file": f"files/{filename}",
        "sourceKind": source_kind,
        "difficulty": difficulty,
        "templateId": template,
        "contentType": "image/png" if filename.endswith(".png") else "application/pdf",
        "expectedType": expected_type,
        "expectedStatus": expected_status,
        "expectedDepartment": expected_department,
        "expectedFields": fields,
    }


documents = []

for i in range(1, 21):
    source, extension = source_for(i)
    identifier = f"invoice-{source.lower()}-{i:03d}"
    filename = f"{identifier}.{extension}"
    number = [f"{120+i}", f"INV-2026-{i:02d}", f"СЧ-{i:03d}/26", f"50{i:02d}"][i % 4]
    date_display = f"{(i % 27)+1:02d}.09.2026"
    date_expected = f"2026-09-{(i % 27)+1:02d}"
    inn = f"77000000{i:02d}" if i % 2 or i <= 10 else f"5000000000{i:02d}"
    kpp = f"77010{i:04d}"[:9]
    amount_display, amount_expected = [
        (f"{100000+i*1000}", f"{100000+i*1000}"),
        (f"{100+i} 000", f"{100+i}000"),
        (f"{100+i} 000,00", f"{100+i}000.00"),
        (f"{100000+i*1000}.50", f"{100000+i*1000}.50"),
    ][i % 4]
    template = ["invoice-standard-a", "invoice-standard-b", "invoice-compact-a", "invoice-reordered-a", "invoice-short-a"][i % 5]
    heading = f"Счет №{number} от {date_display}" if template == "invoice-short-a" else f"Счет на оплату №{number} от {date_display}"
    lines = [heading, "Поставщик: ООО Синтетика. Получатель: ООО Пример."]
    fields = {"Number": number, "Date": date_expected}
    if i != 15:
        if i % 2 == 0 and i <= 10:
            lines.append(f"ИНН / КПП: {inn} / {kpp}")
        else:
            lines.append(f"ИНН {inn}")
            if i <= 10:
                lines.append(f"КПП: {kpp}")
        fields["INN"] = inn
        if i <= 10:
            fields["KPP"] = kpp
    if i != 11:
        marker = "Всего к оплате" if i % 3 == 0 else "Итого к оплате"
        lines.append(f"{marker}: {amount_display} руб.")
        fields["Amount"] = amount_expected
    status = "NeedsReview" if i in (11, 15) else "Completed"
    difficulty = "Hard" if source != "TextPdf" or template == "invoice-short-a" else ("Medium" if i % 3 == 0 else "Easy")
    write_document(filename, source, lines, i)
    documents.append(entry(identifier, filename, source, difficulty, template, "Invoice", status,
                           "Unassigned" if status == "NeedsReview" else "Accounting", fields))

for i in range(1, 21):
    source, extension = source_for(i)
    identifier = f"contract-{source.lower()}-{i:03d}"
    filename = f"{identifier}.{extension}"
    number = [f"Д-{330+i}", f"{120+i}/26", f"CTR-2026-{i:02d}", f"{10+i}"][i % 4]
    day = (i % 27) + 1
    template = ["contract-standard-a", "contract-standard-b", "contract-short-a", "contract-legal-a", "contract-compact-a"][i % 5]
    lines = ([] if i == 11 else [f"Договор №{number} от {day:02d}.08.2026"])
    lines.append("Предмет договора. Права и обязанности сторон.")
    fields = {}
    if i != 11:
        fields["Number"] = number
        fields["Date"] = f"2026-08-{day:02d}"
    if i <= 10:
        marker = ["Цена договора", "Стоимость договора", "Сумма договора", "Общая стоимость по договору"][i % 4]
        lines.append(f"{marker}: {200+i} 000,00 руб.")
        fields["Amount"] = f"{200+i}000.00"
    lines.append("Реквизиты сторон. Срок действия договора.")
    status = "NeedsReview" if i == 11 else "Completed"
    difficulty = "Hard" if source != "TextPdf" or i == 11 else ("Medium" if i % 3 == 0 else "Easy")
    write_document(filename, source, lines, i)
    documents.append(entry(identifier, filename, source, difficulty, template, "Contract", status,
                           "Unassigned" if status == "NeedsReview" else "Legal", fields))

for i in range(1, 21):
    source, extension = source_for(i)
    identifier = f"act-{source.lower()}-{i:03d}"
    filename = f"{identifier}.{extension}"
    number = [f"{i+10}", f"АКТ-{i:03d}", f"A-{i:02d}/26", f"{500+i}"][i % 4]
    day = (i % 27) + 1
    template = ["act-works-a", "act-services-a", "act-related-a", "act-compact-a", "act-reordered-a"][i % 5]
    heading = "Акт выполненных работ" if i % 2 else "Акт оказанных услуг"
    lines = ([] if i == 15 else [f"{heading} №{number} от {day:02d}.07.2026"])
    fields = {}
    if i != 15:
        fields["Number"] = number
        fields["Date"] = f"2026-07-{day:02d}"
    if i <= 10:
        related = f"Д-{400+i}"
        lines.append(f"к договору №{related}")
        fields["RelatedContractNumber"] = related
    lines.append("Заказчик принял выполненные работы.")
    if i != 11:
        lines.append(f"Итого: {30+i} 000,00 руб.")
        fields["Amount"] = f"{30+i}000.00"
    status = "NeedsReview" if i in (11, 15) else "Completed"
    difficulty = "Hard" if source != "TextPdf" or i in (11, 15) else ("Medium" if i % 3 == 0 else "Easy")
    write_document(filename, source, lines, i)
    documents.append(entry(identifier, filename, source, difficulty, template, "Act", status,
                           "Unassigned" if status == "NeedsReview" else "Accounting", fields))

unknown_templates = ["memo-a", "letter-a", "notification-a", "information-a"]
for i in range(1, 9):
    identifier = f"unknown-text-{i:03d}"
    filename = f"{identifier}.pdf"
    template = f"unknown-{unknown_templates[(i-1) % len(unknown_templates)]}"
    lines = [
        ["Служебная записка", "Искусственное сообщение о внутреннем совещании."],
        ["Информационное письмо", "Синтетическое уведомление без реквизитов документа."],
        ["Уведомление", "Произвольный обезличенный текст для тестирования."],
        ["Справочная информация", "Данный файл не является финансовым документом."],
    ][(i-1) % 4]
    write_document(filename, "TextPdf", lines + [f"Код примера: U-{i:03d}"], i)
    documents.append(entry(identifier, filename, "TextPdf", "Medium" if i % 2 else "Easy", template,
                           "Unknown", "NeedsReview", "Unassigned", {}))

with (ROOT / "manifest.json").open("w", encoding="utf-8", newline="\n") as output:
    json.dump({"documents": documents}, output, ensure_ascii=False, indent=2)
    output.write("\n")

print(f"Generated {len(documents)} documents in {ROOT}")
