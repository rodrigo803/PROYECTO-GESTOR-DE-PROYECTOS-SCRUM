using System.Data;
using Dapper;
using Gestor_proyectos_scrum.Api.Data;
using Gestor_proyectos_scrum.Api.Endpoints;
using Gestor_proyectos_scrum.Api.Models;
using Gestor_proyectos_scrum.Api.Services;
using Microsoft.AspNetCore.Diagnostics;
using MySqlConnector;

namespace Gestor_proyectos_scrum.Api;

/// <summary>
/// Punto único de registro de la API para que pueda correr sola (su propio Program.cs,
/// puerto 5100, con Swagger) o montada dentro del Program.cs del MVC (una sola app para Plesk).
/// </summary>
public static class ScrumApiExtensions
{
    /// <summary>Registra los servicios de la API (conexión a MySQL y auditoría).</summary>
    public static IServiceCollection AddScrumApi(this IServiceCollection services)
    {
        // Compatibilidad Dapper con columnas DATE devueltas como DateOnly por MySqlConnector 3.0
        SqlMapper.AddTypeHandler(new DateTimeFromDateOnlyHandler());

        services.AddScoped<ConexionDbApi>();
        services.AddScoped<AuditoriaService>();

        return services;
    }

    /// <summary>
    /// Middleware que traduce MySqlException a JSON. Se registra envuelto en un UseWhen
    /// (en el Program.cs que lo use) para que solo afecte a las rutas /api/*, nunca a las
    /// páginas del MVC.
    /// </summary>
    public static IApplicationBuilder UseScrumApiExceptionHandler(this IApplicationBuilder app)
    {
        app.UseExceptionHandler(errorApp =>
        {
            errorApp.Run(async context =>
            {
                context.Response.ContentType = "application/json; charset=utf-8";
                var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
                var ex = exceptionFeature?.Error;
                var env = context.RequestServices.GetRequiredService<IWebHostEnvironment>();

                if (ex is MySqlException mysqlEx)
                {
                    var (statusCode, mensaje) = TraducirMySqlException(mysqlEx);
                    context.Response.StatusCode = statusCode;
                    await context.Response.WriteAsJsonAsync(new ApiErrorResponse(mensaje, mysqlEx.Message));
                    return;
                }

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                await context.Response.WriteAsJsonAsync(new ApiErrorResponse(
                    "Ocurrió un error interno al procesar la solicitud en la API.",
                    env.IsDevelopment() ? ex?.Message : null));
            });
        });

        return app;
    }

    /// <summary>Mapea /api/health y todos los Endpoints/*Endpoints.cs directo sobre la raíz
    /// (cada grupo ya define su propio prefijo /api/..., así que no hay que anteponer /api otra vez).</summary>
    public static IEndpointRouteBuilder MapScrumApi(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/health", async (ConexionDbApi db) =>
        {
            try
            {
                using var conexion = await db.CrearConexionAbiertaAsync();
                var bdActual = await conexion.ExecuteScalarAsync<string?>("SELECT DATABASE();");
                return Results.Ok(new
                {
                    Estado = "OK",
                    BaseDatos = bdActual,
                    FechaServidor = DateTime.Now
                });
            }
            catch (Exception ex)
            {
                return Results.Json(
                    new ApiErrorResponse("No se pudo conectar a la base de datos MySQL.", ex.Message),
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }
        }).WithTags("Diagnóstico");

        app.MapUsuariosEndpoints();
        app.MapSprintsEndpoints();
        app.MapTareasEndpoints();
        app.MapProyectosEndpoints();
        app.MapPlanificacionEndpoints();

        return app;
    }

    private static (int StatusCode, string Mensaje) TraducirMySqlException(MySqlException ex)
    {
        // Triggers personalizados del script BDScrumManager_MySQL.sql (SIGNAL SQLSTATE '45000')
        if (ex.SqlState == "45000")
        {
            return (StatusCodes.Status400BadRequest, ex.Message);
        }

        return ex.Number switch
        {
            1062 => (StatusCodes.Status409Conflict, "Ya existe un registro con esos datos únicos en la base de datos."),
            1451 => (StatusCodes.Status409Conflict, "No se puede eliminar o modificar el registro porque está siendo referenciado por otros datos del sistema."),
            1452 => (StatusCodes.Status400BadRequest, "Uno de los identificadores relacionados no existe en la base de datos."),
            3819 or 4025 => (StatusCodes.Status400BadRequest, $"Se incumplió una regla de integridad (CHECK) de la base de datos: {ex.Message}"),
            _ => (StatusCodes.Status500InternalServerError, $"Error de base de datos MySQL ({ex.Number}): {ex.Message}")
        };
    }
}

internal sealed class DateTimeFromDateOnlyHandler : SqlMapper.TypeHandler<DateTime>
{
    public override void SetValue(IDbDataParameter parameter, DateTime value)
    {
        parameter.Value = value;
    }

    public override DateTime Parse(object value)
    {
        return value switch
        {
            DateTime dt => dt,
            DateOnly d => d.ToDateTime(TimeOnly.MinValue),
            string s when DateTime.TryParse(s, out var parsed) => parsed,
            _ => Convert.ToDateTime(value)
        };
    }
}
