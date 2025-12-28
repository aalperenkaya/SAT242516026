using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace SAT242516026.Models.MyReports;

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
                page.Margin(50);

                page.Header()
                    .Text("Beyanname List")
                    .FontSize(20)
                    .Bold();

                page.Content().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        if (imageData is not null)
                            row.ConstantColumn(100).Image(imageData).FitArea();
                        else
                            row.ConstantColumn(100);

                        row.ConstantColumn(20);

                        row.RelativeColumn().Column(c =>
                        {
                            c.Item().Text("SAT242516026 - Beyanname Raporu").FontSize(16).Bold();
                            c.Item().Text($"DateTime: {DateTime.Now:dd.MM.yyyy HH:mm}").FontSize(10);
                            c.Item().Text($"Toplam kayıt: {rows.Count}").FontSize(10);
                        });
                    });

                    col.Item().PaddingTop(20);

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(40);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(2);
                            columns.ConstantColumn(45);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1);
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
                });

                page.Footer().Row(row =>
                {
                    row.RelativeColumn().AlignLeft().Text("Footer Left").FontSize(10);
                    row.RelativeColumn().AlignCenter().Text(text =>
                    {
                        text.Span("Page: ").FontSize(10);
                        text.CurrentPageNumber().FontSize(10).Bold();
                        text.Span(" / ").FontSize(10);
                        text.TotalPages().FontSize(10).Bold();
                    });
                    row.RelativeColumn().AlignRight().Text($"DateTime: {DateTime.Now:d}").FontSize(10);
                });
            });
        }).GeneratePdf();
    }
}

