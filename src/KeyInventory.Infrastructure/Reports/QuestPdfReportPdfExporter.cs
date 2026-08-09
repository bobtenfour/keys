using KeyInventory.Application.Reports;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace KeyInventory.Infrastructure.Reports;

public sealed class QuestPdfReportPdfExporter : IReportPdfExporter
{
    static QuestPdfReportPdfExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Export(ReportExportTable table)
    {
        ArgumentNullException.ThrowIfNull(table);

        return Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(28);
                    page.DefaultTextStyle(style => style.FontSize(9).FontFamily(Fonts.Lato));

                    page.Header().Column(header =>
                    {
                        header.Item().Text(table.Title).SemiBold().FontSize(14);
                        if (!string.IsNullOrWhiteSpace(table.FilterContext))
                        {
                            header.Item().PaddingTop(4).Text($"Filters: {table.FilterContext}").FontSize(9);
                        }

                        header.Item().PaddingTop(6).Element(inner => ComposeHeaderRow(inner, table.Headers));
                    });

                    page.Content().PaddingTop(8).Column(column =>
                    {
                        if (table.Rows.Count == 0)
                        {
                            column.Item().Text("No rows for the applied filters.").Italic();
                        }
                        else
                        {
                            foreach (IReadOnlyList<ReportExportCell> row in table.Rows)
                            {
                                column.Item().Element(inner => ComposeDataRow(inner, table.Headers.Count, row));
                            }
                        }
                    });

                    page.Footer().AlignCenter().Text(text =>
                    {
                        text.Span("Page ");
                        text.CurrentPageNumber();
                        text.Span(" of ");
                        text.TotalPages();
                    });
                });
            })
            .GeneratePdf();
    }

    private static void ComposeHeaderRow(IContainer container, IReadOnlyList<string> headers)
    {
        container.BorderBottom(1).BorderColor(Colors.Grey.Darken2).PaddingBottom(4).Row(row =>
        {
            if (headers.Count == 0)
            {
                row.RelativeItem().Text(string.Empty);
                return;
            }

            foreach (string header in headers)
            {
                row.RelativeItem().Text(header).SemiBold();
            }
        });
    }

    private static void ComposeDataRow(IContainer container, int columnCount, IReadOnlyList<ReportExportCell> cells)
    {
        container.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(3).Row(row =>
        {
            for (int index = 0; index < columnCount; index++)
            {
                string text = index < cells.Count ? cells[index].Text : string.Empty;
                row.RelativeItem().Text(text);
            }
        });
    }
}
