
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaFacturacionUI.Models
{
    public class TipoCambio
    {
        [Key]
        public int IdTipoCambio { get; set; }

        public int IdEmpresa { get; set; }

        public int IdMoneda { get; set; }

        [Column(TypeName = "date")]
        public DateTime FechaVigencia { get; set; }

        [Column(TypeName = "decimal(18,6)")]
        public decimal Tasa { get; set; }

        public DateTime FechaRegistro { get; set; } = DateTime.Now;

        [ForeignKey(nameof(IdEmpresa))]
        public virtual Empresa Empresa { get; set; }

        [ForeignKey(nameof(IdMoneda))]
        public virtual Moneda Moneda { get; set; }
    }
}
