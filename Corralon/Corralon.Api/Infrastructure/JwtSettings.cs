namespace Corralon.Api.Infrastructure
{
    /// <summary>Configuración del token JWT (sección "Jwt" de appsettings.json).</summary>
    public class JwtSettings
    {
        public string Key { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpiraMinutos { get; set; } = 120;
    }
}
