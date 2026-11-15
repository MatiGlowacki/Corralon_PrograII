using System.ComponentModel.DataAnnotations;

namespace Corralon.Logic.DTOs
{
    public class LoginDto
    {
        [Required]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
    }

    public class RegistrarUsuarioDto
    {
        [Required, StringLength(50)]
        public string NombreUsuario { get; set; } = string.Empty;

        [Required, StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = string.Empty;

        [Required, StringLength(150)]
        public string NombreCompleto { get; set; } = string.Empty;

        [EmailAddress, StringLength(150)]
        public string? Email { get; set; }

        public int IdRol { get; set; }
    }

    /// <summary>Datos del usuario ya validado (los usa la API para armar el token).</summary>
    public class UsuarioAutenticadoDto
    {
        public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }

    public class LoginRespuestaDto
    {
        public string Token { get; set; } = string.Empty;
        public DateTime Expira { get; set; }
        public int IdUsuario { get; set; }
        public string NombreUsuario { get; set; } = string.Empty;
        public string NombreCompleto { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }
}
