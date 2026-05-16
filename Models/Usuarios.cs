using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaFacturacionUI.Models
{
    public class Usuario
    {
        [Key]
        public int IdUsuario { get; set; }

        public string Nombre { get; set; }

        [Column("usuario")]
        public string Usuario1 { get; set; }

        public string Password { get; set; }

        public string Rol { get; set; }

        public bool Activo { get; set; }

        public DateTime FechaRegistro { get; set; }
    }
}
