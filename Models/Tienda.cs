
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaFacturacionUI.Models
{
    public class Tienda
    {
        [Key]
        public int IdTienda { get; set; }

        public int IdEmpresa { get; set; }

        [Required]
        [StringLength(100)]
        public string Nombre { get; set; }

        [StringLength(300)]
        public string Direccion { get; set; }

        [StringLength(30)]
        public string Telefono { get; set; }

        public bool Activa { get; set; } = true;

        [ForeignKey(nameof(IdEmpresa))]
        public virtual Empresa Empresa { get; set; }
    }
}
