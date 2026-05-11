using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaFacturacionUI.Models
{
    public class FacturaDetalle
    {
        [Key]
        public int IdDetalle { get; set; }

        public int IdFactura { get; set; }

        public int IdProducto { get; set; }

        public int Cantidad { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Precio { get; set; }

        [ForeignKey("IdFactura")]
        public virtual Factura Factura { get; set; }
    }
}