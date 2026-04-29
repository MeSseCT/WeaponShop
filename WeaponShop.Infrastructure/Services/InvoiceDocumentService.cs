using System.Globalization;
using System.Net;
using System.Text;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using WeaponShop.Application.Interfaces;
using WeaponShop.Domain;

namespace WeaponShop.Infrastructure.Services;

public class InvoiceDocumentService : IInvoiceDocumentService
{
    private static readonly CultureInfo CzechCulture = new("cs-CZ");
    private readonly IConfiguration _configuration;

    public InvoiceDocumentService(IConfiguration configuration)
    {
        _configuration = configuration;
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public InvoiceDocument BuildInvoice(Order order)
    {
        var model = BuildModel(order);
        var html = BuildHtml(model);
        var pdf = BuildPdf(model);

        return new InvoiceDocument
        {
            InvoiceNumber = model.InvoiceNumber,
            HtmlFileName = $"{model.InvoiceNumber.ToLowerInvariant()}.html",
            HtmlContent = html,
            PdfFileName = $"{model.InvoiceNumber.ToLowerInvariant()}.pdf",
            PdfContent = pdf
        };
    }

    private InvoiceModel BuildModel(Order order)
    {
        var seller = ReadSeller();
        var issueDate = ResolveIssueDate(order);
        var dueDate = ResolveDueDate(order, issueDate);
        var taxableSupplyDate = issueDate;
        var invoiceNumber = BuildInvoiceNumber(order, issueDate);
        var variableSymbol = invoiceNumber.Replace("INV-", string.Empty, StringComparison.Ordinal);
        var vatRatePercent = ReadDecimal("Invoice:VatRatePercent", 21m);
        var isVatPayer = ReadBool("Invoice:IsVatPayer", true);

        var lines = order.Items
            .Select(item =>
            {
                var gross = Math.Round(item.UnitPrice * item.Quantity, 2);
                var net = isVatPayer
                    ? Math.Round(gross / (1m + (vatRatePercent / 100m)), 2)
                    : gross;
                var vat = isVatPayer
                    ? Math.Round(gross - net, 2)
                    : 0m;

                return new InvoiceLine
                {
                    Name = BuildLineName(order, item),
                    Quantity = item.Quantity,
                    UnitGross = item.UnitPrice,
                    LineGross = gross,
                    LineNet = net,
                    LineVat = vat
                };
            })
            .ToList();

        return new InvoiceModel
        {
            InvoiceNumber = invoiceNumber,
            VariableSymbol = variableSymbol,
            IssueDate = issueDate,
            DueDate = dueDate,
            TaxableSupplyDate = taxableSupplyDate,
            Seller = seller,
            OrderNumber = order.GetPublicOrderNumber(),
            CustomerName = string.IsNullOrWhiteSpace(order.BillingName) ? order.ShippingName : order.BillingName,
            CustomerStreet = string.IsNullOrWhiteSpace(order.BillingStreet) ? order.ShippingStreet : order.BillingStreet,
            CustomerCity = string.IsNullOrWhiteSpace(order.BillingCity) ? order.ShippingCity : order.BillingCity,
            CustomerPostalCode = string.IsNullOrWhiteSpace(order.BillingPostalCode) ? order.ShippingPostalCode : order.BillingPostalCode,
            CustomerCountry = "Česká republika",
            ContactEmail = order.ContactEmail,
            ContactPhone = order.ContactPhone,
            DeliveryLabel = order.DeliveryMethod == "shipping" ? "Doručení na adresu" : "Osobní odběr",
            PaymentLabel = ResolvePaymentLabel(order.PaymentMethod),
            StatusLabel = ResolveInvoiceStatus(order.Status),
            CustomerNote = order.CustomerNote,
            VatRatePercent = vatRatePercent,
            IsVatPayer = isVatPayer,
            Lines = lines,
            TotalGross = lines.Sum(x => x.LineGross),
            TotalNet = lines.Sum(x => x.LineNet),
            TotalVat = lines.Sum(x => x.LineVat)
        };
    }

    private static string BuildLineName(Order order, OrderItem item)
    {
        if (!item.IsWeapon || item.Weapon is null)
        {
            return item.GetDisplayName();
        }

        var assignedUnits = item.Weapon.Units
            .Where(unit => unit.ReservedOrderId == order.Id || unit.SoldOrderId == order.Id)
            .OrderBy(unit => unit.PrimarySerialNumber)
            .ToList();

        if (assignedUnits.Count == 0)
        {
            return item.GetDisplayName();
        }

        var details = assignedUnits
            .Select(unit =>
            {
                var parts = unit.Parts
                    .OrderBy(part => part.SlotNumber)
                    .Select(part => $"{part.PartName}: {part.SerialNumber}");
                return $"V. č. {unit.PrimarySerialNumber}" + (parts.Any() ? $" ({string.Join(", ", parts)})" : string.Empty);
            });

        return $"{item.GetDisplayName()} | {string.Join(" | ", details)}";
    }

    private string BuildHtml(InvoiceModel model)
    {
        var rows = string.Join(
            string.Empty,
            model.Lines.Select(line =>
                $$"""
                <tr>
                    <td>{{Encode(line.Name)}}</td>
                    <td class="text-right">{{line.Quantity.ToString(CzechCulture)}}</td>
                    <td class="text-right">{{FormatCurrency(line.LineNet)}}</td>
                    <td class="text-right">{{(model.IsVatPayer ? $"{FormatCurrency(line.LineVat)} ({model.VatRatePercent:0.#} %)" : "Neplátce")}}</td>
                    <td class="text-right">{{FormatCurrency(line.LineGross)}}</td>
                </tr>
"""));

        var noteBlock = string.IsNullOrWhiteSpace(model.CustomerNote)
            ? string.Empty
            : $$"""
                <div class="note">Poznámka zákazníka: {{Encode(model.CustomerNote)}}</div>
""";

        var vatFootnote = model.IsVatPayer
            ? "Doklad obsahuje údaje potřebné pro daňový doklad v českém prostředí."
            : "Dodavatel vystupuje jako neplátce DPH. Na dokladu není DPH účtována.";

        return $$"""
<!DOCTYPE html>
<html lang="cs">
<head>
    <meta charset="utf-8">
    <title>Faktura {{Encode(model.InvoiceNumber)}}</title>
    <style>
        body { font-family: Arial, Helvetica, sans-serif; color: #1f2937; margin: 0; background: #f4f1ea; }
        .toolbar { position: sticky; top: 0; z-index: 10; display: flex; gap: 12px; justify-content: center; padding: 14px; background: rgba(24, 34, 48, 0.96); }
        .toolbar button { border: none; background: #d6c4a1; color: #182230; padding: 10px 16px; border-radius: 999px; font-weight: 700; cursor: pointer; }
        .invoice { max-width: 980px; margin: 24px auto; background: #ffffff; border: 1px solid #ddd4c5; box-shadow: 0 12px 32px rgba(15, 23, 42, 0.08); }
        .header { display: flex; justify-content: space-between; gap: 24px; padding: 32px; border-bottom: 2px solid #d6c4a1; }
        .brand { font-size: 30px; font-weight: 700; letter-spacing: 0.04em; color: #182230; }
        .subtitle { margin-top: 8px; color: #6b7280; font-size: 13px; }
        .invoice-meta { text-align: right; }
        .invoice-meta h1 { margin: 0 0 12px 0; font-size: 28px; letter-spacing: 0.08em; }
        .invoice-meta div { margin-bottom: 6px; font-size: 14px; }
        .section-grid { display: grid; grid-template-columns: 1fr 1fr; gap: 24px; padding: 28px 32px 8px; }
        .card { border: 1px solid #e5dfd2; padding: 18px 20px; background: #fcfbf8; }
        .card h2 { margin: 0 0 12px 0; font-size: 15px; text-transform: uppercase; letter-spacing: 0.08em; color: #8a6f39; }
        .card div { margin-bottom: 6px; font-size: 14px; }
        .summary { padding: 8px 32px 28px; }
        .summary-grid { display: grid; grid-template-columns: repeat(4, 1fr); gap: 12px; }
        .summary-item { border: 1px solid #e5dfd2; background: #fffdfa; padding: 14px 16px; }
        .summary-item span { display: block; color: #6b7280; font-size: 12px; margin-bottom: 6px; text-transform: uppercase; letter-spacing: 0.08em; }
        .summary-item strong { font-size: 16px; color: #111827; }
        table { width: calc(100% - 64px); margin: 0 32px 28px; border-collapse: collapse; }
        thead th { background: #182230; color: #ffffff; font-size: 12px; text-transform: uppercase; letter-spacing: 0.06em; padding: 14px 12px; text-align: left; }
        tbody td { border-bottom: 1px solid #ece6d8; padding: 14px 12px; font-size: 14px; vertical-align: top; }
        .text-right { text-align: right; }
        .totals { width: 360px; margin: 0 32px 32px auto; border: 1px solid #e5dfd2; background: #fcfbf8; }
        .totals-row { display: flex; justify-content: space-between; padding: 12px 16px; border-bottom: 1px solid #ece6d8; font-size: 14px; }
        .totals-row:last-child { border-bottom: none; }
        .totals-row.grand { font-size: 18px; font-weight: 700; background: #f6efe2; }
        .footer { padding: 0 32px 32px; color: #6b7280; font-size: 13px; }
        .note { margin-top: 10px; }
        @media print {
            body { background: #ffffff; }
            .toolbar { display: none; }
            .invoice { margin: 0; border: none; box-shadow: none; max-width: none; }
        }
    </style>
</head>
<body>
    <div class="toolbar">
        <button type="button" onclick="window.print()">Tisknout fakturu</button>
    </div>
    <div class="invoice">
        <div class="header">
            <div>
                <div class="brand">ZBROJNICE</div>
                <div class="subtitle">Elektronická faktura vystavená k objednávce e-shopu</div>
            </div>
            <div class="invoice-meta">
                <h1>FAKTURA</h1>
                <div><strong>Číslo dokladu:</strong> {{Encode(model.InvoiceNumber)}}</div>
                <div><strong>Datum vystavení:</strong> {{model.IssueDate.ToString("d. M. yyyy", CzechCulture)}}</div>
                <div><strong>Datum zdanitelného plnění:</strong> {{model.TaxableSupplyDate.ToString("d. M. yyyy", CzechCulture)}}</div>
                <div><strong>Datum splatnosti:</strong> {{model.DueDate.ToString("d. M. yyyy", CzechCulture)}}</div>
                <div><strong>Variabilní symbol:</strong> {{Encode(model.VariableSymbol)}}</div>
            </div>
        </div>
        <div class="section-grid">
            <section class="card">
                <h2>Dodavatel</h2>
                <div>{{Encode(model.Seller.Name)}}</div>
                <div>{{Encode(model.Seller.Street)}}</div>
                <div>{{Encode($"{model.Seller.PostalCode} {model.Seller.City}")}}</div>
                <div>{{Encode(model.Seller.Country)}}</div>
                <div>IČO: {{Encode(model.Seller.Ico)}}</div>
                {{(model.IsVatPayer && !string.IsNullOrWhiteSpace(model.Seller.Dic) ? $"<div>DIČ: {Encode(model.Seller.Dic)}</div>" : string.Empty)}}
                {{(!string.IsNullOrWhiteSpace(model.Seller.Register) ? $"<div>{Encode(model.Seller.Register)}</div>" : string.Empty)}}
                {{(!string.IsNullOrWhiteSpace(model.Seller.BankAccount) ? $"<div>Bankovní účet: {Encode(model.Seller.BankAccount)}</div>" : string.Empty)}}
                {{(!string.IsNullOrWhiteSpace(model.Seller.Iban) ? $"<div>IBAN: {Encode(model.Seller.Iban)}</div>" : string.Empty)}}
            </section>
            <section class="card">
                <h2>Odběratel</h2>
                <div>{{Encode(model.CustomerName)}}</div>
                <div>{{Encode(model.CustomerStreet)}}</div>
                <div>{{Encode($"{model.CustomerPostalCode} {model.CustomerCity}".Trim())}}</div>
                <div>{{Encode(model.CustomerCountry)}}</div>
                {{(!string.IsNullOrWhiteSpace(model.ContactEmail) ? $"<div>E-mail: {Encode(model.ContactEmail)}</div>" : string.Empty)}}
                {{(!string.IsNullOrWhiteSpace(model.ContactPhone) ? $"<div>Telefon: {Encode(model.ContactPhone)}</div>" : string.Empty)}}
            </section>
        </div>
        <div class="summary">
            <div class="summary-grid">
                <div class="summary-item"><span>Objednávka</span><strong>{{Encode(model.OrderNumber)}}</strong></div>
                <div class="summary-item"><span>Platba</span><strong>{{Encode(model.PaymentLabel)}}</strong></div>
                <div class="summary-item"><span>Doručení</span><strong>{{Encode(model.DeliveryLabel)}}</strong></div>
                <div class="summary-item"><span>Stav</span><strong>{{Encode(model.StatusLabel)}}</strong></div>
            </div>
        </div>
        <table>
            <thead>
                <tr>
                    <th>Položka</th>
                    <th class="text-right">Množství</th>
                    <th class="text-right">Cena bez DPH</th>
                    <th class="text-right">DPH</th>
                    <th class="text-right">Cena s DPH</th>
                </tr>
            </thead>
            <tbody>
                {{rows}}
            </tbody>
        </table>
        <div class="totals">
            <div class="totals-row"><span>Základ daně</span><strong>{{FormatCurrency(model.TotalNet)}}</strong></div>
            <div class="totals-row"><span>{{(model.IsVatPayer ? $"DPH ({model.VatRatePercent:0.#} %)" : "DPH")}}</span><strong>{{(model.IsVatPayer ? FormatCurrency(model.TotalVat) : "Neplátce DPH")}}</strong></div>
            <div class="totals-row grand"><span>Celkem k úhradě</span><strong>{{FormatCurrency(model.TotalGross)}}</strong></div>
        </div>
        <div class="footer">
            <div class="note">{{Encode(vatFootnote)}}</div>
            {{noteBlock}}
            <div class="note">Faktura byla vygenerována elektronicky a je vystavena bez podpisu.</div>
        </div>
    </div>
</body>
</html>
""";
    }

    private byte[] BuildPdf(InvoiceModel model)
    {
        return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(2, Unit.Centimetre);
                    page.PageColor(Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                    page.Header().Element(header => ComposePdfHeader(header, model));
                    page.Content().PaddingVertical(16).Element(content => ComposePdfContent(content, model));
                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span($"Faktura {model.InvoiceNumber}  |  Strana ");
                        text.CurrentPageNumber();
                    });
                });
            })
            .GeneratePdf();
    }

    private void ComposePdfHeader(IContainer container, InvoiceModel model)
    {
        container.Row(row =>
        {
            row.RelativeItem().Column(column =>
            {
                column.Item().Text("ZBROJNICE").FontSize(22).Bold().FontColor("#182230");
                column.Item().Text("Elektronická faktura vystavená k objednávce e-shopu").FontSize(10).FontColor("#6B7280");
            });

            row.ConstantItem(240).Column(column =>
            {
                column.Item().AlignRight().Text("FAKTURA").FontSize(24).Bold().FontColor("#182230");
                column.Item().AlignRight().Text($"Číslo dokladu: {model.InvoiceNumber}");
                column.Item().AlignRight().Text($"Datum vystavení: {model.IssueDate:d. M. yyyy}");
                column.Item().AlignRight().Text($"Datum zdanitelného plnění: {model.TaxableSupplyDate:d. M. yyyy}");
                column.Item().AlignRight().Text($"Datum splatnosti: {model.DueDate:d. M. yyyy}");
                column.Item().AlignRight().Text($"Variabilní symbol: {model.VariableSymbol}");
            });
        });
    }

    private void ComposePdfContent(IContainer container, InvoiceModel model)
    {
        container.Column(column =>
        {
            column.Spacing(16);

            column.Item().Row(row =>
            {
                row.RelativeItem().Element(x => ComposePdfAddressCard(x, "Dodavatel", new[]
                {
                    model.Seller.Name,
                    model.Seller.Street,
                    $"{model.Seller.PostalCode} {model.Seller.City}",
                    model.Seller.Country,
                    $"IČO: {model.Seller.Ico}",
                    model.IsVatPayer && !string.IsNullOrWhiteSpace(model.Seller.Dic) ? $"DIČ: {model.Seller.Dic}" : null,
                    model.Seller.Register,
                    !string.IsNullOrWhiteSpace(model.Seller.BankAccount) ? $"Bankovní účet: {model.Seller.BankAccount}" : null,
                    !string.IsNullOrWhiteSpace(model.Seller.Iban) ? $"IBAN: {model.Seller.Iban}" : null
                }));

                row.ConstantItem(16);

                row.RelativeItem().Element(x => ComposePdfAddressCard(x, "Odběratel", new[]
                {
                    model.CustomerName,
                    model.CustomerStreet,
                    $"{model.CustomerPostalCode} {model.CustomerCity}".Trim(),
                    model.CustomerCountry,
                    !string.IsNullOrWhiteSpace(model.ContactEmail) ? $"E-mail: {model.ContactEmail}" : null,
                    !string.IsNullOrWhiteSpace(model.ContactPhone) ? $"Telefon: {model.ContactPhone}" : null
                }));
            });

            column.Item().Row(row =>
            {
                row.RelativeItem().Element(x => ComposePdfSummaryBox(x, "Objednávka", model.OrderNumber));
                row.RelativeItem().Element(x => ComposePdfSummaryBox(x, "Platba", model.PaymentLabel));
                row.RelativeItem().Element(x => ComposePdfSummaryBox(x, "Doručení", model.DeliveryLabel));
                row.RelativeItem().Element(x => ComposePdfSummaryBox(x, "Stav", model.StatusLabel));
            });

            column.Item().Element(x => ComposePdfLinesTable(x, model));
            column.Item().AlignRight().Width(250).Element(x => ComposePdfTotals(x, model));

            if (!string.IsNullOrWhiteSpace(model.CustomerNote))
            {
                column.Item().Text($"Poznámka zákazníka: {model.CustomerNote}");
            }

            column.Item().Text(model.IsVatPayer
                ? "Doklad obsahuje údaje potřebné pro daňový doklad v českém prostředí."
                : "Dodavatel vystupuje jako neplátce DPH. Na dokladu není DPH účtována.");
            column.Item().Text("Faktura byla vygenerována elektronicky a je vystavena bez podpisu.").FontColor("#6B7280");
        });
    }

    private void ComposePdfAddressCard(IContainer container, string title, IEnumerable<string?> lines)
    {
        container.Border(1).BorderColor("#E5DFD2").Background("#FCFBF8").Padding(14).Column(column =>
        {
            column.Spacing(4);
            column.Item().Text(title).Bold().FontColor("#8A6F39");

            foreach (var line in lines.Where(line => !string.IsNullOrWhiteSpace(line)))
            {
                column.Item().Text(line!);
            }
        });
    }

    private void ComposePdfSummaryBox(IContainer container, string label, string value)
    {
        container.Border(1).BorderColor("#E5DFD2").Background("#FFFDFA").Padding(12).Column(column =>
        {
            column.Spacing(2);
            column.Item().Text(label).FontSize(9).FontColor("#8A6F39");
            column.Item().Text(value).SemiBold().FontSize(11);
        });
    }

    private void ComposePdfLinesTable(IContainer container, InvoiceModel model)
    {
        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(5);
                columns.ConstantColumn(60);
                columns.ConstantColumn(90);
                columns.ConstantColumn(90);
                columns.ConstantColumn(90);
            });

            table.Header(header =>
            {
                header.Cell().Element(HeaderCell).Text("Položka").FontColor(Colors.White).Bold();
                header.Cell().Element(HeaderCell).AlignRight().Text("Množství").FontColor(Colors.White).Bold();
                header.Cell().Element(HeaderCell).AlignRight().Text("Cena bez DPH").FontColor(Colors.White).Bold();
                header.Cell().Element(HeaderCell).AlignRight().Text("DPH").FontColor(Colors.White).Bold();
                header.Cell().Element(HeaderCell).AlignRight().Text("Cena s DPH").FontColor(Colors.White).Bold();
            });

            foreach (var line in model.Lines)
            {
                table.Cell().Element(BodyCell).Text(line.Name);
                table.Cell().Element(BodyCell).AlignRight().Text(line.Quantity.ToString(CzechCulture));
                table.Cell().Element(BodyCell).AlignRight().Text(FormatCurrency(line.LineNet));
                table.Cell().Element(BodyCell).AlignRight().Text(model.IsVatPayer
                    ? $"{FormatCurrency(line.LineVat)} ({model.VatRatePercent:0.#} %)"
                    : "Neplátce");
                table.Cell().Element(BodyCell).AlignRight().Text(FormatCurrency(line.LineGross));
            }
        });

        static IContainer HeaderCell(IContainer container)
        {
            return container.Background("#182230").PaddingVertical(10).PaddingHorizontal(8);
        }

        static IContainer BodyCell(IContainer container)
        {
            return container.BorderBottom(1).BorderColor("#ECE6D8").PaddingVertical(10).PaddingHorizontal(8);
        }
    }

    private void ComposePdfTotals(IContainer container, InvoiceModel model)
    {
        container.Border(1).BorderColor("#E5DFD2").Background("#FCFBF8").Column(column =>
        {
            column.Item().Element(x => TotalsRow(x, "Základ daně", FormatCurrency(model.TotalNet)));
            column.Item().Element(x => TotalsRow(x, model.IsVatPayer ? $"DPH ({model.VatRatePercent:0.#} %)" : "DPH", model.IsVatPayer ? FormatCurrency(model.TotalVat) : "Neplátce DPH"));
            column.Item().Element(x => x.Background("#F6EFE2").Padding(12).Row(row =>
            {
                row.RelativeItem().Text("Celkem k úhradě").Bold();
                row.ConstantItem(90).AlignRight().Text(FormatCurrency(model.TotalGross)).Bold();
            }));
        });

        static void TotalsRow(IContainer container, string label, string value)
        {
            container.BorderBottom(1).BorderColor("#ECE6D8").Padding(12).Row(row =>
            {
                row.RelativeItem().Text(label);
                row.ConstantItem(90).AlignRight().Text(value);
            });
        }
    }

    private DateTime ResolveIssueDate(Order order)
    {
        return (order.ApprovedAtUtc ?? order.CreatedAt).ToLocalTime();
    }

    private DateTime ResolveDueDate(Order order, DateTime issueDate)
    {
        if (order.PaymentMethod is "cash-on-delivery" or "cash-on-pickup")
        {
            return issueDate;
        }

        var dueDays = ReadInt("Invoice:DueDays", 7);
        return issueDate.AddDays(dueDays);
    }

    private string BuildInvoiceNumber(Order order, DateTime issueDate)
    {
        return $"INV-{issueDate.Year}-{order.Id:D6}";
    }

    private InvoiceSeller ReadSeller()
    {
        return new InvoiceSeller
        {
            Name = _configuration["Invoice:SellerName"] ?? "Zbrojnice s.r.o.",
            Street = _configuration["Invoice:SellerStreet"] ?? "Na střelnici 12",
            City = _configuration["Invoice:SellerCity"] ?? "Brno",
            PostalCode = _configuration["Invoice:SellerPostalCode"] ?? "602 00",
            Country = _configuration["Invoice:SellerCountry"] ?? "Česká republika",
            Ico = _configuration["Invoice:SellerIco"] ?? "12345678",
            Dic = _configuration["Invoice:SellerDic"] ?? "CZ12345678",
            Register = _configuration["Invoice:SellerRegister"] ?? "Společnost zapsaná v obchodním rejstříku vedeném Krajským soudem v Brně.",
            BankAccount = _configuration["Invoice:BankAccount"] ?? "2301234567/2010",
            Iban = _configuration["Invoice:Iban"] ?? "CZ6520100000002301234567"
        };
    }

    private static string ResolvePaymentLabel(string paymentMethod)
    {
        return paymentMethod switch
        {
            "cash-on-delivery" => "Dobírka",
            "cash-on-pickup" => "Platba při převzetí",
            _ => "Bankovní převod"
        };
    }

    private static string ResolveInvoiceStatus(OrderStatus status)
    {
        return status switch
        {
            OrderStatus.AwaitingApproval => "Čeká na schválení",
            OrderStatus.Approved => "Schváleno",
            OrderStatus.AwaitingGunsmith => "Kontrola zbrojířem",
            OrderStatus.AwaitingDispatch => "Příprava expedice",
            OrderStatus.Shipped => "Odesláno",
            OrderStatus.ReadyForPickup => "Připraveno k vyzvednutí",
            OrderStatus.Completed => "Dokončeno",
            OrderStatus.Rejected => "Zamítnuto",
            _ => "Přijato"
        };
    }

    private string FormatCurrency(decimal value)
    {
        return value.ToString("C", CzechCulture);
    }

    private bool ReadBool(string key, bool fallback)
    {
        return bool.TryParse(_configuration[key], out var value) ? value : fallback;
    }

    private int ReadInt(string key, int fallback)
    {
        return int.TryParse(_configuration[key], out var value) ? value : fallback;
    }

    private decimal ReadDecimal(string key, decimal fallback)
    {
        return decimal.TryParse(_configuration[key], NumberStyles.Number, CultureInfo.InvariantCulture, out var value)
            ? value
            : fallback;
    }

    private static string Encode(string? value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }

    private sealed class InvoiceModel
    {
        public string InvoiceNumber { get; init; } = string.Empty;
        public string VariableSymbol { get; init; } = string.Empty;
        public DateTime IssueDate { get; init; }
        public DateTime DueDate { get; init; }
        public DateTime TaxableSupplyDate { get; init; }
        public InvoiceSeller Seller { get; init; } = new();
        public string OrderNumber { get; init; } = string.Empty;
        public string CustomerName { get; init; } = string.Empty;
        public string CustomerStreet { get; init; } = string.Empty;
        public string CustomerCity { get; init; } = string.Empty;
        public string CustomerPostalCode { get; init; } = string.Empty;
        public string CustomerCountry { get; init; } = string.Empty;
        public string ContactEmail { get; init; } = string.Empty;
        public string ContactPhone { get; init; } = string.Empty;
        public string DeliveryLabel { get; init; } = string.Empty;
        public string PaymentLabel { get; init; } = string.Empty;
        public string StatusLabel { get; init; } = string.Empty;
        public string CustomerNote { get; init; } = string.Empty;
        public bool IsVatPayer { get; init; }
        public decimal VatRatePercent { get; init; }
        public List<InvoiceLine> Lines { get; init; } = new();
        public decimal TotalGross { get; init; }
        public decimal TotalNet { get; init; }
        public decimal TotalVat { get; init; }
    }

    private sealed class InvoiceLine
    {
        public string Name { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public decimal UnitGross { get; init; }
        public decimal LineGross { get; init; }
        public decimal LineNet { get; init; }
        public decimal LineVat { get; init; }
    }

    private sealed class InvoiceSeller
    {
        public string Name { get; init; } = string.Empty;
        public string Street { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string PostalCode { get; init; } = string.Empty;
        public string Country { get; init; } = string.Empty;
        public string Ico { get; init; } = string.Empty;
        public string Dic { get; init; } = string.Empty;
        public string Register { get; init; } = string.Empty;
        public string BankAccount { get; init; } = string.Empty;
        public string Iban { get; init; } = string.Empty;
    }
}
