using System;
using System.Collections.Generic;

namespace SistemaFacturacionUI.Models
{
    public class FacturaDTO
    {
        public int IdFactura { get; set; }

        public Guid? IdSolicitud { get; set; }

        public string NombreCliente { get; set; }

        public string Telefono { get; set; }

        public string Direccion { get; set; }

        public string? Departamento { get; set; }

        public string? Municipio { get; set; }

        public string Comentario { get; set; }

        public string Horario { get; set; }

        public string? Tienda { get; set; }

        public string HoraExacta { get; set; }

        public DateTime? FechaEnvio { get; set; }

        public decimal Subtotal { get; set; }

        public decimal Envio { get; set; }

        public decimal Descuento { get; set; }

        public decimal Total { get; set; }

        public string MetodoPago { get; set; }

        public string Estado { get; set; }

        // NUEVOS CAMPOS
        public string? Referencia { get; set; }

        //public string? TipoEnvio { get; set; }

        public List<DetalleDTO> Detalles { get; set; }
    }

    public class DetalleDTO
    {
        public int IdProducto { get; set; }

        public int Cantidad { get; set; }

        public decimal Precio { get; set; }

        public int? IdVariante { get; set; }

        public string Variante { get; set; }
    }
}