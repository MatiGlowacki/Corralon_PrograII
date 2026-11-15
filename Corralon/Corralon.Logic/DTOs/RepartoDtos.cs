using System.ComponentModel.DataAnnotations;

namespace Corralon.Logic.DTOs
{
    public class EntregaRequestDto
    {
        public int IdFactura { get; set; }

        [Required, StringLength(200)]
        public string DomicilioEntrega { get; set; } = string.Empty;

        public int IdLocalidad { get; set; }

        [StringLength(30)]
        public string? TelefonoContacto { get; set; }
    }

    /// <summary>Cuerpo del POST (alta) y del PUT (modificación) de un reparto. El orden de la lista es el orden de las paradas.</summary>
    public class RepartoRequestDto
    {
        public DateOnly FechaProgramada { get; set; }
        public int IdVehiculo { get; set; }
        public int IdChofer { get; set; }

        [StringLength(300)]
        public string? Observaciones { get; set; }

        [Required, MinLength(1, ErrorMessage = "El reparto debe tener al menos una entrega.")]
        public List<EntregaRequestDto> Entregas { get; set; } = new();
    }

    /// <summary>Resultado de una entrega: Entregada o NoEntregada.</summary>
    public class ResultadoEntregaDto
    {
        [Required, RegularExpression("^(Entregada|NoEntregada)$", ErrorMessage = "El estado debe ser Entregada o NoEntregada.")]
        public string Estado { get; set; } = "Entregada";

        /// <summary>Obligatorio cuando no se pudo entregar (motivo).</summary>
        [StringLength(300)]
        public string? Observaciones { get; set; }
    }

    public class EntregaDto
    {
        public int IdDetalleReparto { get; set; }
        public int IdFactura { get; set; }
        public int NroFactura { get; set; }
        public string Cliente { get; set; } = string.Empty;
        public int OrdenEntrega { get; set; }
        public string DomicilioEntrega { get; set; } = string.Empty;
        public int IdLocalidad { get; set; }
        public string Localidad { get; set; } = string.Empty;
        public string? TelefonoContacto { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? FechaEntrega { get; set; }
        public string? Observaciones { get; set; }
    }

    public class RepartoDto
    {
        public int IdReparto { get; set; }
        public DateOnly FechaProgramada { get; set; }
        public int IdVehiculo { get; set; }
        public string Vehiculo { get; set; } = string.Empty;
        public int IdChofer { get; set; }
        public string Chofer { get; set; } = string.Empty;
        public int IdUsuario { get; set; }
        public string Estado { get; set; } = string.Empty;
        public DateTime? HoraSalida { get; set; }
        public DateTime? HoraRegreso { get; set; }
        public string? Observaciones { get; set; }
        public List<EntregaDto> Entregas { get; set; } = new();
    }
}
