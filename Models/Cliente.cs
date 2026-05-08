using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacionUI.Models
{
    public class Cliente
    {
        [Key]
        public int Idcliente { get; set; }

        public string Nombre { get; set; }
        public string Telefono { get; set; }
        public string Direccion { get; set; }

        public DateTime FechaRegistro { get; set; }
        public bool Activo { get; set; }
    }
}