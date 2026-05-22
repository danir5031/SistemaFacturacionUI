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

        [ForeignKey("IdProducto")]
        public virtual Producto Producto { get; set; }

        public int? IdVariante { get; set; }
        public string Variante { get; set; }
    }
}