using System.ComponentModel.DataAnnotations;

namespace Corralon.Logic.DTOs
{
    /// <summary>Un renglón que se envía al crear o modificar una factura.</summary>
    public class DetalleFacturaItemDto
    {
        public int IdArticulo { get; set; }

        [Range(0.001, 999999999)]
        public decimal Cantidad { get; set; }
    }

    /// <summary>Cuerpo del POST (alta) y del PUT (modificación) de una factura.</summary>
    public class FacturaRequestDto
    {
        public int IdCliente { get; set; }
        public int IdFormaPago { get; set; }

        [StringLength(300)]
        public string? Observaciones { get; set; }

        [Required, MinLength(1, ErrorMessage = "La factura debe tener al menos un artículo.")]
        public List<DetalleFacturaItemDto> Detalles { get; set; } = new();
    }

    public class DetalleFacturaDto
    {
        public int IdDetalle { get; set; }
        public int IdArticulo { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Articulo { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }

    public class FacturaDto
    {
        public int IdFactura { get; set; }
        public int NroFactura { get; set; }
        public DateTime Fecha { get; set; }
        public int IdCliente { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public int IdFormaPago { get; set; }
        public string FormaPago { get; set; } = string.Empty;
        public int IdUsuario { get; set; }
        public string Vendedor { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaAnulacion { get; set; }
        public string? MotivoAnulacion { get; set; }

        /// <summary>Calculado al responder: suma de cantidad x precio de los detalles.</summary>
        public decimal Total { get; set; }

        public List<DetalleFacturaDto> Detalles { get; set; } = new();
    }
}
