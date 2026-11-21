
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacionUI.Models
{
    public class Empresa
    {
        [Key]
        public int IdEmpresa { get; set; }

        [Required]
        [StringLength(150)]
        public string Nombre { get; set; }

        [StringLength(150)]
        public string NombreComercial { get; set; }

        [StringLength(30)]
        public string IdentificacionFiscal { get; set; }

        [StringLength(300)]
        public string Direccion { get; set; }

        [StringLength(30)]
        public string Telefono { get; set; }

        [StringLength(150)]
        public string Correo { get; set; }

        // Configuración contable
        [Required]
        [StringLength(20)]
        public string MetodoContable { get; set; } = "Devengado";

        public bool PermitirMultimoneda { get; set; } = true;

        public bool Activa { get; set; } = true;

        public DateTime FechaCreacion { get; set; } = DateTime.Now;

        // Relación: una empresa tiene muchas tiendas
        public virtual ICollection<Tienda> Tiendas { get; set; }
            = new List<Tienda>();

        public int? IdMonedaBase { get; set; }

        [System.ComponentModel.DataAnnotations.Schema.ForeignKey(nameof(IdMonedaBase))]
        public virtual Moneda MonedaBase { get; set; }
    }
}
