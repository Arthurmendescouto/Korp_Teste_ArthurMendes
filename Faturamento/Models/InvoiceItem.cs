namespace Faturamento.Models
{
    public class InvoiceItem
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public int ProdutoId { get; set; }
        public string ProdutoCodigo { get; set; } = null!;
        public string ProdutoDescricao { get; set; } = null!;
        public int Quantidade { get; set; }
    }
}
