using System.ComponentModel.DataAnnotations;

namespace EstoqueService.Models
{
    public class Produto
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        public string Descricao { get; set; } = null!;

        public int Saldo { get; set; }
    }
}
