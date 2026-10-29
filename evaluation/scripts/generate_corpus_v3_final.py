"""Deterministically generate corpus-v3 from its pre-benchmark business specification."""

import json
from pathlib import Path
from PIL import Image, ImageDraw, ImageEnhance, ImageFilter, ImageFont
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.pdfgen import canvas

ROOT = Path(__file__).parents[1] / "corpus-v3-final-holdout"
FILES = ROOT / "files"
FILES.mkdir(parents=True, exist_ok=True)


def resolve_font():
    for candidate in [Path(r"C:\Windows\Fonts\arial.ttf"), Path(r"C:\Windows\Fonts\calibri.ttf"),
                      Path("/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf")]:
        if candidate.is_file():
            return candidate
    raise FileNotFoundError("A Cyrillic-capable system font is required.")


FONT_PATH = resolve_font()
pdfmetrics.registerFont(TTFont("FinalSans", FONT_PATH))


def split_pages(lines, multipage):
    if not multipage:
        return [lines]
    pivot = max(3, len(lines) // 2)
    return [lines[:pivot] + ["Продолжение на следующей странице"],
            ["Страница 2"] + lines[pivot:] + ["Конец документа"]]


def render_page(lines, difficulty, variant, page):
    dims = {"Easy": (1550, 1080, 38), "Medium": (1400, 1020, 34), "Hard": (1320, 980, 31)}
    width, height, size = dims[difficulty]
    image = Image.new("RGB", (width, height), "white")
    draw = ImageDraw.Draw(image)
    font = ImageFont.truetype(FONT_PATH, size + variant % 2)
    y = 55
    for index, line in enumerate(lines):
        x = 55 + ((index + variant + page) % 4) * (0 if difficulty == "Easy" else 9)
        draw.text((x, y), line, font=font, fill=(12, 12, 12))
        y += size + 17
    if difficulty == "Medium":
        image = ImageEnhance.Contrast(image).enhance(0.95)
    if difficulty == "Hard":
        angle = [-1.4, -0.8, 0.9, 1.3][variant % 4]
        image = image.rotate(angle, fillcolor="white")
        image = image.filter(ImageFilter.GaussianBlur(0.42))
        image = ImageEnhance.Contrast(image).enhance(0.89)
    return image


def write_document(filename, source, difficulty, lines, variant, multipage):
    path = FILES / filename
    pages = split_pages(lines, multipage)
    if source == "TextPdf":
        pdf = canvas.Canvas(str(path), invariant=1)
        for page_index, page_lines in enumerate(pages):
            pdf.setFont("FinalSans", 12 + variant % 3)
            y = 795
            for index, line in enumerate(page_lines):
                pdf.drawString(44 + ((index + variant) % 3) * 12, y, line)
                y -= 25
            if page_index < len(pages) - 1:
                pdf.showPage()
        pdf.save()
        return
    images = [render_page(page_lines, difficulty, variant, index) for index, page_lines in enumerate(pages)]
    if source == "Image":
        images[0].save(path, optimize=True)
        return
    images[0].save(path, "PDF", resolution=150, save_all=True, append_images=images[1:])


def add(identifier, source, difficulty, template, expected_type, status, department, fields, lines, variant, multipage=False):
    extension = "png" if source == "Image" else "pdf"
    filename = f"{identifier}.{extension}"
    write_document(filename, source, difficulty, lines, variant, multipage)
    documents.append({"id": identifier, "file": f"files/{filename}", "sourceKind": source,
                      "difficulty": difficulty, "templateId": template,
                      "contentType": "image/png" if source == "Image" else "application/pdf",
                      "expectedType": expected_type, "expectedStatus": status,
                      "expectedDepartment": department, "expectedFields": fields})


def distributions(counts, shift):
    sources = ["TextPdf"] * counts[0] + ["ScannedPdf"] * counts[1] + ["Image"] * counts[2]
    difficulties = (["Easy", "Medium", "Hard"] * 20)[shift:shift + len(sources)]
    return list(zip(sources, difficulties))


documents = []
invoice_layouts = ["invoice-final-ledger", "invoice-final-remittance", "invoice-final-supply",
                   "invoice-final-summary", "invoice-final-table", "invoice-final-commercial", "invoice-final-settlement", "invoice-final-payment"]
invoice_titles = ["РАСЧЕТ ЗА ПОСТАВКУ", "Платежный документ поставщика", "Коммерческий счет",
                  "Документ на перечисление", "Свод к оплате", "Счет за товары", "Расчетный лист поставки", "Требование об оплате"]
for i, (source, difficulty) in enumerate(distributions((7, 7, 6), 0), 1):
    number = ["ФН-8101", "R/2027-02", "ПЛТ-803", "8047", "FIN-805", "806/С", "П-807", "8088", "Q-809", "810/27", "СЧ-811", "812-A", "8139", "R-814/Б", "815", "F816", "817-К", "818/9", "819-X", "8200"][i-1]
    date = f"2027-01-{i+2:02d}"
    inn, kpp, amount = f"770100{i:04d}", f"7703{i:05d}"[:9], f"{48000+i*2387:.2f}"
    fields = {"Number": number, "Date": date, "INN": inn, "Amount": amount}
    if i % 2 == 0: fields["KPP"] = kpp
    if i == 6: fields.pop("Amount")
    if i == 17: fields.pop("INN")
    number_lines = [f"Документ № {number}", f"Учетный номер: {number}", f"Исходящий платежный № {number}", f"Номер счета: {number}"]
    date_lines = [f"Выставлен {date[8:10]}.{date[5:7]}.{date[:4]}", f"Дата составления: {date[8:10]}/{date[5:7]}/{date[:4]}"]
    lines = [invoice_titles[(i-1)%8], number_lines[(i-1)%4], f"Заказ поставки: ZK-{300+i}", date_lines[i%2],
             "Получатель: ООО Синтетическая Линия", f"Банковский счет: 40702810000000{i:06d}", "Телефон: +7 000 000-00-00"]
    if "INN" in fields: lines.append(f"Налоговый идентификатор (ИНН): {inn}" + (f"; код постановки КПП {kpp}" if "KPP" in fields else ""))
    lines += [f"Промежуточный итог: {12000+i*100},00", "НДС: расчетная строка"]
    if "Amount" in fields: lines.append(f"Окончательная сумма платежа: {amount.replace('.', ',')} руб.")
    status = "NeedsReview" if i in (6, 17) else "Completed"
    add(f"invoice-final-{source.lower()}-{i:03d}", source, difficulty, invoice_layouts[(i-1)%8], "Invoice", status,
        "Unassigned" if status == "NeedsReview" else "Accounting", fields, lines, i, i in (3, 10, 13))

contract_layouts = ["contract-final-framework", "contract-final-services", "contract-final-delivery", "contract-final-stages",
                    "contract-final-partnership", "contract-final-annexed", "contract-final-procurement", "contract-final-terms"]
contract_titles = ["СОГЛАШЕНИЕ О СОТРУДНИЧЕСТВЕ", "Условия оказания услуг", "Контракт на поставку",
                   "Договоренность сторон", "Соглашение о выполнении этапов", "Договор закупки", "Основные условия сделки", "Рамочное соглашение"]
for i, (source, difficulty) in enumerate(distributions((7, 6, 7), 1), 1):
    number = ["КФ-4101", "AGR-42/2", "СД-403", "4404", "C-405/27", "406-К", "4077", "AGR-408", "409-С", "4100", "D-411", "412/Т", "413-А", "414", "K-415/5", "4166", "417-Р", "418/27", "419-С", "4200"][i-1]
    date = f"2027-02-{i+1:02d}"
    amount = f"{310000+i*4110:.2f}"
    fields = {"Number": number, "Date": date}
    if i % 3 != 0: fields["Amount"] = amount
    if i == 8: fields.pop("Number")
    if i == 18: fields.pop("Date")
    lines = [contract_titles[(i-1)%8], f"Реестровая запись сделки: {number}" if "Number" in fields else "Реестровая запись сделки отсутствует",
             f"Заключено сторонами {date[8:10]}.{date[5:7]}.{date[:4]}" if "Date" in fields else "Дата заключения не указана",
             "Сторона А: ООО Учебная Перспектива", "Сторона Б: ООО Макетные Решения", f"Этап работ № {i}: 45 календарных дней",
             f"Ответственность: штраф {5000+i*100} рублей", f"Частичный аванс: {25000+i*500} рублей"]
    if "Amount" in fields: lines.append(f"Полная цена настоящего соглашения составляет {amount.replace('.', ' ')} RUB")
    lines += ["Реквизиты сторон приведены в приложении", f"Приложение № APP-{i}"]
    status = "NeedsReview" if i in (8, 18) else "Completed"
    add(f"contract-final-{source.lower()}-{i:03d}", source, difficulty, contract_layouts[(i-1)%8], "Contract", status,
        "Unassigned" if status == "NeedsReview" else "Legal", fields, lines, 30+i, i in (2, 9, 12))

act_layouts = ["act-final-acceptance", "act-final-completion", "act-final-delivery", "act-final-service",
               "act-final-stage", "act-final-commission", "act-final-results", "act-final-transfer"]
act_titles = ["ВЕДОМОСТЬ ПРИЕМКИ РАБОТ", "Подтверждение завершения услуг", "Приемка результата этапа",
              "Документ сдачи результата", "Итог приемочной комиссии", "Акт передачи выполненного", "Свод принятого объема", "Подтверждение исполнения"]
for i, (source, difficulty) in enumerate(distributions((6, 7, 7), 2), 1):
    number = ["ПР-5101", "AC-52/2", "ПМ-503", "5504", "A-505/27", "506-П", "5077", "ACT-508", "509-С", "5100", "R-511", "512/А", "513-Т", "514", "A-515/5", "5166", "517-Р", "518/27", "519-С", "5200"][i-1]
    date = f"2027-03-{i+1:02d}"
    amount, related = f"{76000+i*1725:.2f}", f"BASE-{600+i}"
    fields = {"Number": number, "Date": date, "Amount": amount}
    if i % 2 == 1: fields["RelatedContractNumber"] = related
    if i == 9: fields.pop("Amount")
    if i == 19: fields.pop("Date")
    lines = [act_titles[(i-1)%8], f"Идентификатор приемочного документа: {number}", f"Заказ № ORD-{900+i}",
             f"Подписано комиссией {date[8:10]}/{date[5:7]}/{date[:4]}" if "Date" in fields else "Дата приемки отсутствует",
             f"Приложение № ATT-{i}"]
    if "RelatedContractNumber" in fields: lines.append(f"Работы выполнялись в рамках соглашения {related}")
    lines += [f"Стоимость отдельного этапа: {18000+i*250} руб.", "Результат принят без замечаний"]
    if "Amount" in fields: lines.append(f"Общая стоимость принятого объема: {amount.replace('.', ',')} рублей")
    status = "NeedsReview" if i in (9, 19) else "Completed"
    add(f"act-final-{source.lower()}-{i:03d}", source, difficulty, act_layouts[(i-1)%8], "Act", status,
        "Unassigned" if status == "NeedsReview" else "Accounting", fields, lines, 60+i, i in (4, 11, 13))

unknown_templates = ["unknown-final-memo", "unknown-final-notice", "unknown-final-request", "unknown-final-protocol",
                     "unknown-final-info", "unknown-final-approval", "unknown-final-order", "unknown-final-letter"]
unknown_texts = [
    ["СЛУЖЕБНОЕ ПИСЬМО", "Просим уточнить время внутреннего совещания."],
    ["УВЕДОМЛЕНИЕ", "Архивный договор можно получить у секретаря."],
    ["ЗАЯВКА", "Требуется настроить счет пользователя в учебной системе."],
    ["ПРОТОКОЛ РАБОЧЕЙ ВСТРЕЧИ", "Рассмотрен акт инвентаризации переговорной."],
    ["СЛУЖЕБНАЯ ЗАПИСКА", "Предлагается обновить график дежурств."],
    ["ИНФОРМАЦИОННОЕ СООБЩЕНИЕ", "Материалы доступны во внутреннем каталоге."],
    ["ПИСЬМО-СОГЛАСОВАНИЕ", "Согласован порядок доступа в помещение."],
    ["ВНУТРЕННЕЕ РАСПОРЯЖЕНИЕ", "Назначить ответственных за учебный стенд."],
]
for i, (source, difficulty) in enumerate(distributions((4, 4, 4), 0), 1):
    lines = unknown_texts[(i-1)%8] + [f"Справочный номер сообщения: MSG-{700+i}", f"Дата сообщения: {i+1:02d}.04.2027"]
    add(f"unknown-final-{source.lower()}-{i:03d}", source, difficulty, unknown_templates[(i-1)%8], "Unknown",
        "NeedsReview", "Unassigned", {}, lines, 90+i)

(ROOT / "manifest.json").write_text(json.dumps({"documents": documents}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(f"Generated {len(documents)} documents")
