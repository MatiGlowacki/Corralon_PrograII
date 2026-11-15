using System.ComponentModel.DataAnnotations;

namespace Corralon.Logic.DTOs
{
    public class FormaPagoDto
    {
        public int IdFormaPago { get; set; }

        [Required, StringLength(50)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Descripcion { get; set; }

        public bool Activo { get; set; } = true;
    }

    public class CategoriaDto
    {
        public int IdCategoria { get; set; }

        [Required, StringLength(80)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(200)]
        public string? Descripcion { get; set; }

        public bool Activo { get; set; } = true;
    }

    public class ProveedorDto
    {
        public int IdProveedor { get; set; }

        [Required, StringLength(150)]
        public string RazonSocial { get; set; } = string.Empty;

        [Required, StringLength(13)]
        public string Cuit { get; set; } = string.Empty;

        [StringLength(30)]
        public string? Telefono { get; set; }

        [EmailAddress, StringLength(150)]
        public string? Email { get; set; }

        [StringLength(200)]
        public string? Domicilio { get; set; }

        public int? IdLocalidad { get; set; }

        /// <summary>Solo lectura.</summary>
        public string? Localidad { get; set; }

        public bool Activo { get; set; } = true;
    }

    public class VehiculoDto
    {
        public int IdVehiculo { get; set; }

        [Required, StringLength(10)]
        public string Patente { get; set; } = string.Empty;

        [Required, StringLength(50)]
        public string Marca { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Modelo { get; set; }

        [Range(0.01, 1000000)]
        public decimal CapacidadKg { get; set; }

        public bool Activo { get; set; } = true;
    }

    public class ChoferDto
    {
        public int IdChofer { get; set; }

        [Required, StringLength(150)]
        public string NombreCompleto { get; set; } = string.Empty;

        [Required, StringLength(10)]
        public string Dni { get; set; } = string.Empty;

        [StringLength(30)]
        public string? Telefono { get; set; }

        public bool Activo { get; set; } = true;
    }

    public class ClienteDto
    {
        public int IdCliente { get; set; }

        [Required, StringLength(150)]
        public string NombreCompleto { get; set; } = string.Empty;

        /// <summary>DNI, CUIT o CUIL.</summary>
        [Required, RegularExpression("^(DNI|CUIT|CUIL)$", ErrorMessage = "El tipo de documento debe ser DNI, CUIT o CUIL.")]
        public string TipoDocumento { get; set; } = "DNI";

        [Required, StringLength(13)]
        public string NroDocumento { get; set; } = string.Empty;

        public int IdCondicionIva { get; set; }

        /// <summary>Solo lectura.</summary>
        public string? CondicionIva { get; set; }

        [StringLength(30)]
        public string? Telefono { get; set; }

        [EmailAddress, StringLength(150)]
        public string? Email { get; set; }

        [StringLength(200)]
        public string? Domicilio { get; set; }

        public int? IdLocalidad { get; set; }

        /// <summary>Solo lectura.</summary>
        public string? Localidad { get; set; }

        public bool Activo { get; set; } = true;
    }

    public class ArticuloDto
    {
        public int IdArticulo { get; set; }

        [Required, StringLength(30)]
        public string Codigo { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string Nombre { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Descripcion { get; set; }

        public int IdCategoria { get; set; }

        /// <summary>Solo lectura.</summary>
        public string? Categoria { get; set; }

        public int IdUnidadMedida { get; set; }

        /// <summary>Solo lectura.</summary>
        public string? UnidadMedida { get; set; }

        /// <summary>Solo lectura.</summary>
        public bool PermiteDecimales { get; set; }

        public int? IdProveedor { get; set; }

        /// <summary>Solo lectura.</summary>
        public string? Proveedor { get; set; }

        [Range(0, 999999999)]
        public decimal PrecioUnitario { get; set; }

        /// <summary>Al crear es el stock inicial. Al modificar se ignora: el stock cambia solo con ventas y ajustes.</summary>
        [Range(0, 999999999)]
        public decimal StockActual { get; set; }

        [Range(0, 999999999)]
        public decimal StockMinimo { get; set; }

        public bool Activo { get; set; } = true;

        /// <summary>Solo lectura: true si el stock está en el mínimo o por debajo.</summary>
        public bool StockBajo { get; set; }
    }

    public class AjusteStockDto
    {
        /// <summary>Entrada (suma) o Salida (resta).</summary>
        [Required, RegularExpression("^(Entrada|Salida)$", ErrorMessage = "El tipo debe ser Entrada o Salida.")]
        public string Tipo { get; set; } = "Entrada";

        /// <summary>Compra, Ajuste o Devolucion.</summary>
        [Required, RegularExpression("^(Compra|Ajuste|Devolucion)$", ErrorMessage = "El motivo debe ser Compra, Ajuste o Devolucion.")]
        public string Motivo { get; set; } = "Compra";

        [Range(0.001, 999999999)]
        public decimal Cantidad { get; set; }

        [StringLength(200)]
        public string? Observaciones { get; set; }
    }

    public class MovimientoStockDto
    {
        public int IdMovimiento { get; set; }
        public int IdArticulo { get; set; }
        public string Articulo { get; set; } = string.Empty;
        public DateTime Fecha { get; set; }
        public string Tipo { get; set; } = string.Empty;
        public string Motivo { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public int? IdFactura { get; set; }
        public int? NroFactura { get; set; }
        public string Usuario { get; set; } = string.Empty;
        public string? Observaciones { get; set; }
    }

    /// <summary>Elemento simple para listas desplegables.</summary>
    public class LookupDto
    {
        public int Id { get; set; }
        public string Nombre { get; set; } = string.Empty;
    }

    public class LocalidadDto
    {
        public int IdLocalidad { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string? CodigoPostal { get; set; }
        public int IdProvincia { get; set; }
        public string Provincia { get; set; } = string.Empty;
    }

    public class UnidadMedidaDto
    {
        public int IdUnidadMedida { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Abreviatura { get; set; } = string.Empty;
        public bool PermiteDecimales { get; set; }
    }
}
