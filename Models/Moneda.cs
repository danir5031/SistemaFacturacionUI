
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacionUI.Models
{
    public class Moneda
    {
        [Key]
        public int IdMoneda { get; set; }

        [Required]
        [StringLength(3)]
        public string Codigo { get; set; }

        [Required]
        [StringLength(80)]
        public string Nombre { get; set; }

        [Required]
        [StringLength(10)]
        public string Simbolo { get; set; }

        public byte Decimales { get; set; } = 2;

        public bool Activa { get; set; } = true;

        public virtual ICollection<EmpresaMoneda> EmpresaMonedas { get; set; }
            = new List<EmpresaMoneda>();
    }
}
