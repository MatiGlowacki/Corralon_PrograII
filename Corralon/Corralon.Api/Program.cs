using System.Text;
using Corralon.Api.Infrastructure;
using Corralon.Logic;
using Corralon.Logic.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Corralon.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ---------------------------------------------------------- Servicios

            builder.Services.AddControllers();

            // Base de datos
            builder.Services.AddDbContext<CorralonDBContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("CorralonDB")));

            // Lógica de negocio (Unit of Work, repositorios y servicios)
            builder.Services.AddCorralonLogic();

            // JWT
            builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Jwt"));
            builder.Services.AddScoped<ITokenService, TokenService>();

            var jwt = builder.Configuration.GetSection("Jwt").Get<JwtSettings>()
                      ?? throw new InvalidOperationException("Falta la sección 'Jwt' en appsettings.json.");
            if (string.IsNullOrWhiteSpace(jwt.Key) || jwt.Key.Length < 32)
            {
                throw new InvalidOperationException("La clave 'Jwt:Key' debe tener al menos 32 caracteres.");
            }

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = jwt.Issuer,
                        ValidateAudience = true,
                        ValidAudience = jwt.Audience,
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.Zero
                    };
                });
            builder.Services.AddAuthorization();

            // CORS: permite que el front (otro puerto) llame a la API
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("Front", policy =>
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod());
            });

            // OpenAPI + botón "Authorize" para el token
            builder.Services.AddOpenApi(options =>
            {
                options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            });

            var app = builder.Build();

            // ---------------------------------------------------------- Pipeline

            app.UseMiddleware<ErrorHandlingMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();

                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/openapi/v1.json", "Corralon API v1");
                });
            }

            app.UseHttpsRedirection();

            app.UseCors("Front");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}
