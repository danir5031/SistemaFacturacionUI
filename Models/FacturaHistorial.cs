using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaFacturacionUI.Models
{
    public class FacturaHistorial
    {
        [Key]
        public int IdHistorial { get; set; }

        public int IdFactura { get; set; }

        [ForeignKey("IdFactura")]
        public virtual Factura Factura { get; set; }

        public int IdUsuario { get; set; }

        [ForeignKey("IdUsuario")]
        public virtual Usuario Usuario { get; set; }

        public DateTime Fecha { get; set; }

        [StringLength(50)]
        public string TipoAccion { get; set; }

        [StringLength(2000)]
        public string Descripcion { get; set; }
    }
}
