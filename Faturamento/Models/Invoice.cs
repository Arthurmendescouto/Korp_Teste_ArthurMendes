namespace Faturamento.Models
{
    public enum InvoiceStatus { Aberta = 0, Fechada = 1 }

    public class Invoice
    {
        public int Id { get; set; }

        // Sequencial - gerado pelo serviço no momento do cadastro
        public int Number { get; set; }

        public InvoiceStatus Status { get; set; } = InvoiceStatus.Aberta;

        public DateTime Date { get; set; }

        public decimal Total { get; set; }

        public List<InvoiceItem> Items { get; set; } = new();
    }
}
