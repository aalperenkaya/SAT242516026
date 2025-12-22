using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SAT242516026.Models.MyReports;

// PDF'e basacağımız satır modeli
public class BeyannameRow
{
    public int Id { get; set; }
    public string MukellefAd { get; set; } = "";
    public string TipAd { get; set; } = "";
    public int Yil { get; set; }
    public string Donem { get; set; } = "";
    public string Durum { get; set; } = "";
    public string SonTarih { get; set; } = "";
}

public class Report_BeyannameList
{
    static IContainer CellStyle(IContainer container) =>
        container.Padding(5).BorderBottom(1).BorderColor(Colors.Grey.Lighten2);

    public byte[] Generate(List<BeyannameRow> rows)
    {
        QuestPDF.Settings.License = LicenseType.Community;

        var imagePath = Path.Combine("wwwroot", "logo_siyah.png");
        byte[]? imageData = File.Exists(imagePath) ? File.ReadAllBytes(imagePath) : null;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(35);

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        if (imageData is not null)
                        {
                            row.ConstantColumn(90).Image(imageData).FitArea();
                            row.ConstantColumn(15);
                        }

                        row.RelativeColumn().Column(c =>
                        {
                            c.Item().Text("SAT242516026 - Beyanname Raporu").FontSize(16).Bold();
                            c.Item().Text($"Tarih: {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(10);
                            c.Item().Text($"Toplam kayıt: {rows.Count}").FontSize(10);
                        });
                    });

                    col.Item().PaddingTop(10).LineHorizontal(1).LineColor(Colors.Grey.Lighten2);
                });

                page.Content().PaddingTop(10).Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(40);   // Id
                        columns.RelativeColumn(3);    // Mukellef
                        columns.RelativeColumn(2);    // Tip
                        columns.ConstantColumn(45);   // Yıl
                        columns.RelativeColumn(1);    // Dönem
                        columns.RelativeColumn(1);    // Durum
                        columns.RelativeColumn(1);    // SonTarih
                    });

                    table.Header(header =>
                    {
                        header.Cell().Element(CellStyle).Text("Id").Bold();
                        header.Cell().Element(CellStyle).Text("Mükellef").Bold();
                        header.Cell().Element(CellStyle).Text("Tip").Bold();
                        header.Cell().Element(CellStyle).Text("Yıl").Bold();
                        header.Cell().Element(CellStyle).Text("Dönem").Bold();
                        header.Cell().Element(CellStyle).Text("Durum").Bold();
                        header.Cell().Element(CellStyle).Text("Son Tarih").Bold();
                    });

                    foreach (var r in rows)
                    {
                        table.Cell().Element(CellStyle).Text(r.Id.ToString());
                        table.Cell().Element(CellStyle).Text(r.MukellefAd);
                        table.Cell().Element(CellStyle).Text(r.TipAd);
                        table.Cell().Element(CellStyle).Text(r.Yil.ToString());
                        table.Cell().Element(CellStyle).Text(r.Donem);
                        table.Cell().Element(CellStyle).Text(r.Durum);
                        table.Cell().Element(CellStyle).Text(r.SonTarih);
                    }
                });

                page.Footer().Row(row =>
                {
                    row.RelativeColumn().AlignLeft().Text("SAT242516026").FontSize(9);
                    row.RelativeColumn().AlignCenter().Text(t =>
                    {
                        t.Span("Sayfa: ").FontSize(9);
                        t.CurrentPageNumber().FontSize(9).Bold();
                        t.Span(" / ").FontSize(9);
                        t.TotalPages().FontSize(9).Bold();
                    });
                    row.RelativeColumn().AlignRight().Text(DateTime.Now.ToString("dd.MM.yyyy")).FontSize(9);
                });
            });
        }).GeneratePdf();
    }
}
