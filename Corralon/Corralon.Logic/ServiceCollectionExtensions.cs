using Corralon.Logic.DTOs;
using Corralon.Logic.Models.Interfaces;
using Corralon.Logic.Repositories;
using Corralon.Logic.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Corralon.Logic
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>Registra el Unit of Work y todos los servicios de la lógica de negocio.</summary>
        public static IServiceCollection AddCorralonLogic(this IServiceCollection services)
        {
            services.AddScoped<IUnitOfWork, UnitOfWork>();

            // ABMC de soporte y datos maestros
            services.AddScoped<ICrudService<FormaPagoDto>, FormaPagoService>();
            services.AddScoped<ICrudService<CategoriaDto>, CategoriaService>();
            services.AddScoped<ICrudService<ProveedorDto>, ProveedorService>();
            services.AddScoped<ICrudService<VehiculoDto>, VehiculoService>();
            services.AddScoped<ICrudService<ChoferDto>, ChoferService>();
            services.AddScoped<ICrudService<ClienteDto>, ClienteService>();
            services.AddScoped<IArticuloService, ArticuloService>();
            services.AddScoped<ICatalogoService, CatalogoService>();

            // Transacciones y resto de la lógica
            services.AddScoped<IFacturaService, FacturaService>();
            services.AddScoped<IRepartoService, RepartoService>();
            services.AddScoped<IDashboardService, DashboardService>();
            services.AddScoped<IAuthService, AuthService>();

            return services;
        }
    }
}
