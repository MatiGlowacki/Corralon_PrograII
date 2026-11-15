using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Corralon.Api.Controllers
{
    public abstract class ApiControllerBase : ControllerBase
    {
        /// <summary>Id del usuario logueado, tomado del token JWT.</summary>
        protected int UsuarioId
        {
            get
            {
                var valor = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(valor, out var id))
                {
                    throw new UnauthorizedAccessException("No se pudo identificar al usuario.");
                }
                return id;
            }
        }
    }
}
