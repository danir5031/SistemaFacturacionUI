using System;
using System.ComponentModel.DataAnnotations;

namespace SistemaFacturacionUI.Models
{
    public class Producto
    {

        [Key]
        public int IdProducto { get; set; }

        public string Nombre { get; set; }
        public string Categoria { get; set; }
        public string Descripcion { get; set; }

        public decimal PrecioVenta { get; set; }
        public decimal Costo { get; set; }

        public int Stock { get; set; }

        public string ImagenUrl { get; set; }

        // 🔥 NUEVO
        public byte[] Imagen { get; set; }
        public string ImagenMime { get; set; }

        public bool Disponible { get; set; }
        public bool Activo { get; set; } = true;
        public DateTime FechaRegistro { get; set; }
    }
}
