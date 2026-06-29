using System.Collections.Generic;

public class FacturaDTO
{
    public string Telefono { get; set; }
    public string Direccion { get; set; }
    public string Horario { get; set; }
    public string HoraExacta { get; set; }

    public decimal Subtotal { get; set; }
    public decimal Envio { get; set; }
    public string? Tienda { get; set; }
    public decimal Descuento { get; set; }
    public decimal Total { get; set; }

    public string MetodoPago { get; set; }
    public string Estado { get; set; }

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