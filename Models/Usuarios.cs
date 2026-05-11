using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaFacturacionUI.Models
{
    public class Usuario
    {
        [Key]
        public int IdUsuario { get; set; }  // 🔥 ESTO ES LO QUE TE FALT
        public string Nombre { get; set; }
        [Column("usuario")]
        public string Usuario1 { get; set; } // ojo si así viene de DB
        public string Password { get; set; }
        public string Rol { get; set; }
    }
}
