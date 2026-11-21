using System.Data;
using Dapper;
using Gestor_proyectos_scrum.Api.Data;
using Gestor_proyectos_scrum.Api.Endpoints;
using Gestor_proyectos_scrum.Api.Models;
using Gestor_proyectos_scrum.Api.Services;
using Microsoft.AspNetCore.Diagnostics;
using MySqlConnector;

// Compatibilidad Dapper con columnas DATE devueltas como DateOnly por MySqlConnector 3.0
SqlMapper.AddTypeHandler(new DateTimeFromDateOnlyHandler());

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddScoped<ConexionDbApi>();
builder.Services.AddScoped<AuditoriaService>();

var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
    ?? ["http://localhost:5265"];

builder.Services.AddCors(options =>
{
    options.AddPolicy("MvcCors", policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// Middleware global de manejo de errores para traducir MySqlException a respuestas JSON legibles
app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        context.Response.ContentType = "application/json; charset=utf-8";
        var exceptionFeature = context.Features.Get<IExceptionHandlerFeature>();
        var ex = exceptionFeature?.Error;

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
            app.Environment.IsDevelopment() ? ex?.Message : null));
    });
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("MvcCors");

// GET /api/health
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

app.Run();

static (int StatusCode, string Mensaje) TraducirMySqlException(MySqlException ex)
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
