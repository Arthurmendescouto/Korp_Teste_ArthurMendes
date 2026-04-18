using System.Globalization;
using Faturamento.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Faturamento.Services
{
    public static class InvoicePdfGenerator
    {
        public static byte[] Generate(Invoice invoice)
        {
            var culture = CultureInfo.GetCultureInfo("pt-BR");
            var dataEmissao = invoice.Date.ToLocalTime().ToString("dd/MM/yyyy", culture);
            var totalFormatado = invoice.Total.ToString("C2", culture);

            var document = Document.Create(doc =>
            {
                doc.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(header =>
                    {
                        header.Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("NOTA FISCAL").SemiBold().FontSize(20);
                                col.Item().Text($"Número: {invoice.Number}").SemiBold();
                            });
                        });
                    });

                    page.Content().Column(column =>
                    {
                        column.Item().Text($"Data de emissão: {dataEmissao}");

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.ConstantColumn(90);
                                columns.RelativeColumn();
                                columns.ConstantColumn(70);
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Código").SemiBold();
                                header.Cell().Text("Descrição").SemiBold();
                                header.Cell().AlignRight().Text("Qtde").SemiBold();
                            });

                            foreach (var item in invoice.Items)
                            {
                                table.Cell().Text(item.ProdutoCodigo);
                                table.Cell().Text(item.ProdutoDescricao);
                                table.Cell().AlignRight().Text(item.Quantidade.ToString(culture));
                            }
                        });

                        column.Item().AlignRight().Text($"Total: {totalFormatado}").SemiBold().FontSize(12);
                    });

                    page.Footer().AlignCenter().Text(x => x.Span("Gerado pelo sistema"));
                });
            });

            return document.GeneratePdf();
        }
    }
}

