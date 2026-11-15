using Corralon.Logic.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Corralon.Api.Infrastructure
{
    /// <summary>
    /// Traduce las excepciones de la lógica de negocio a respuestas HTTP con un mensaje claro
    /// para que el front pueda mostrarlo: 404, 400, 401, 409 o 500.
    /// </summary>
    public class ErrorHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlingMiddleware> _logger;

        public ErrorHandlingMiddleware(RequestDelegate next, ILogger<ErrorHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (NotFoundException ex)
            {
                await Responder(context, StatusCodes.Status404NotFound, "No encontrado", ex.Message);
            }
            catch (BusinessException ex)
            {
                await Responder(context, StatusCodes.Status400BadRequest, "Regla de negocio", ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                await Responder(context, StatusCodes.Status401Unauthorized, "No autorizado", ex.Message);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Error al guardar en la base de datos");
                await Responder(context, StatusCodes.Status409Conflict, "Conflicto con la base de datos",
                    "No se pudo guardar: el dato está duplicado o lo usan otros registros.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error no controlado");
                await Responder(context, StatusCodes.Status500InternalServerError, "Error interno",
                    "Ocurrió un error inesperado en el servidor.");
            }
        }

        private static async Task Responder(HttpContext context, int estado, string titulo, string detalle)
        {
            if (context.Response.HasStarted)
            {
                return;
            }

            context.Response.StatusCode = estado;
            await context.Response.WriteAsJsonAsync(new ProblemDetails
            {
                Status = estado,
                Title = titulo,
                Detail = detalle
            });
        }
    }
}
