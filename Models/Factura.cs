using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaFacturacionUI.Models
{
    public class Factura
    {
        [Key]
        public int IdFactura { get; set; }

        [StringLength(100)]
        public string NumeroFactura { get; set; }

        public int IdCliente { get; set; }
        public int IdUsuario { get; set; }

        [ForeignKey("IdUsuario")]
        public virtual Usuario Usuario { get; set; }

        public DateTime FechaRegistro { get; set; }

        public DateTime? FechaEnvio { get; set; }

        [StringLength(20)]
        public string Horario { get; set; }

        [StringLength(20)]
        public string HoraExacta { get; set; }

        [StringLength(500)]
        public string Direccion { get; set; }

        [StringLength(50)]
        public string Telefono { get; set; }

        [StringLength(150)]
        public string NombreCliente { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Envio { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Descuento { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        [StringLength(50)]
        public string MetodoPago { get; set; }

        [StringLength(50)]
        public string Estado { get; set; }

        [StringLength(500)]
        public string Comentario { get; set; }

        [StringLength(100)]
        public string Departamento { get; set; }

        [StringLength(100)]
        public string Municipio { get; set; }
        public bool TicketGenerado { get; set; }

        public virtual List<FacturaDetalle> Detalles { get; set; }
    }
}