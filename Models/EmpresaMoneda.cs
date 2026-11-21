
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaFacturacionUI.Models
{
    public class EmpresaMoneda
    {
        [Key]
        public int IdEmpresaMoneda { get; set; }

        public int IdEmpresa { get; set; }

        public int IdMoneda { get; set; }

        public bool Activa { get; set; } = true;

        [ForeignKey(nameof(IdEmpresa))]
        public virtual Empresa Empresa { get; set; }

        [ForeignKey(nameof(IdMoneda))]
        public virtual Moneda Moneda { get; set; }
    }
}
