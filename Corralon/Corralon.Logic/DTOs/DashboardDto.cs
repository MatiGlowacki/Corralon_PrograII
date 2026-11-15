namespace Corralon.Logic.DTOs
{
    public class VentaFormaPagoDto
    {
        public string FormaPago { get; set; } = string.Empty;
        public int CantidadFacturas { get; set; }
        public decimal Total { get; set; }
    }

    public class ArticuloVendidoDto
    {
        public int IdArticulo { get; set; }
        public string Articulo { get; set; } = string.Empty;
        public decimal CantidadVendida { get; set; }
        public decimal Total { get; set; }
    }

    public class ArticuloStockBajoDto
    {
        public int IdArticulo { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Articulo { get; set; } = string.Empty;
        public string UnidadMedida { get; set; } = string.Empty;
        public decimal StockActual { get; set; }
        public decimal StockMinimo { get; set; }
    }

    public class VentaDiaDto
    {
        public DateTime Fecha { get; set; }
        public decimal Total { get; set; }
    }

    /// <summary>Información resumida para el dashboard (mes en curso).</summary>
    public class DashboardDto
    {
        public int FacturasEmitidasMes { get; set; }
        public int FacturasAnuladasMes { get; set; }
        public decimal TotalVendidoMes { get; set; }
        public decimal TicketPromedio { get; set; }
        public int RepartosPendientes { get; set; }
        public int EntregasPendientes { get; set; }
        public List<VentaFormaPagoDto> VentasPorFormaPago { get; set; } = new();
        public List<ArticuloVendidoDto> ArticulosMasVendidos { get; set; } = new();
        public List<ArticuloStockBajoDto> ArticulosStockBajo { get; set; } = new();
        public List<VentaDiaDto> VentasUltimos30Dias { get; set; } = new();
    }
}
