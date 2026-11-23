using System.Text.RegularExpressions;
using Dapper;
using Gestor_proyectos_scrum.Api.Data;
using Gestor_proyectos_scrum.Api.Models;
using Gestor_proyectos_scrum.Api.Services;
using MySqlConnector;

namespace Gestor_proyectos_scrum.Api.Endpoints;

/// <summary>
/// HU-04: Product Backlog (historias de usuario y sus criterios de aceptación).
/// Migrado desde la propuesta de Jesús en feature/jesus (Endpoints/BacklogEndpoints.cs,
/// pensado para Minimal APIs con autorización por cookie) al patrón de la Api: Dapper +
/// RequireRolesFilter (X-User-Id) + transacciones + AuditoriaService.
/// Las reglas de negocio (prioridad/estado válidos, estimación y criterios para marcar
/// Lista/En Sprint, criterios con aprobaciones no se sustituyen, no borrar historias con
/// Sprints o aprobaciones) son las mismas que tenía su código original.
/// </summary>
public static class BacklogEndpoints
{
    private static readonly string[] RolesEdicion = { "Administrador", "Product Owner" };
    private static readonly string[] PrioridadesValidas = { "Alta", "Media", "Baja" };
    private static readonly string[] EstadosValidos = { "Pendiente", "Lista", "En Sprint", "Terminada" };
    private static readonly Regex CodigoHistoriaRegex = new(@"^HU-\d{2,7}$");

    public static void MapBacklogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/backlog").WithTags("HU-04 Product Backlog");

        // GET /api/backlog/proyectos (los que el usuario puede ver: todos si es Administrador, si no solo donde es miembro)
        group.MapGet("/proyectos", async (HttpContext ctx, ConexionDbApi db) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();
            var proyectos = await ListarProyectosVisiblesAsync(ctx, conexion);
            return Results.Ok(proyectos);
        })
        .RequireRoles();

        // GET /api/backlog/proyectos/{idProyecto}/historias
        group.MapGet("/proyectos/{idProyecto:int}/historias", async (int idProyecto, HttpContext ctx, ConexionDbApi db) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();

            if (!await PuedeAccederAsync(ctx, conexion, idProyecto))
            {
                return Results.Json(
                    new ApiErrorResponse("No tienes acceso a este proyecto."),
                    statusCode: StatusCodes.Status403Forbidden);
            }

            var historias = await ListarHistoriasAsync(conexion, idProyecto);
            return Results.Ok(historias);
        })
        .RequireRoles();

        // POST /api/backlog/historias (crear)
        group.MapPost("/historias", async (GuardarHistoriaRequest req, HttpContext ctx, ConexionDbApi db, AuditoriaService auditoria) =>
        {
            var (valido, error, criterios) = ValidarHistoria(req);
            if (!valido)
            {
                return Results.BadRequest(new ApiErrorResponse(error));
            }

            using var conexion = await db.CrearConexionAbiertaAsync();

            if (!await PuedeAccederAsync(ctx, conexion, req.IdProyecto))
            {
                return Results.Json(
                    new ApiErrorResponse("No tienes acceso a este proyecto."),
                    statusCode: StatusCodes.Status403Forbidden);
            }

            using var transaccion = await conexion.BeginTransactionAsync();
            try
            {
                await conexion.ExecuteAsync(@"
                    INSERT INTO HistoriasUsuario (IdProyecto, Codigo, Como, Quiero, Para, Prioridad, OrdenPrioridad, Estimacion, Estado)
                    VALUES (@IdProyecto, @Codigo, @Como, @Quiero, @Para, @Prioridad, @OrdenPrioridad, @Estimacion, @Estado);",
                    req, transaccion);

                var idHistoria = await conexion.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID();", transaction: transaccion);

                await InsertarCriteriosAsync(conexion, idHistoria, criterios, transaccion);

                var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(ctx);
                await auditoria.RegistrarAsync(conexion, idActor, "Crear", "HistoriasUsuario", idHistoria, req.Codigo, transaccion);

                await transaccion.CommitAsync();
                return Results.Ok(new ApiMessageResponse("Historia guardada en MySQL.", idHistoria));
            }
            catch
            {
                await transaccion.RollbackAsync();
                throw;
            }
        })
        .RequireRoles(RolesEdicion);

        // PUT /api/backlog/historias/{id} (editar)
        group.MapPut("/historias/{id:int}", async (int id, GuardarHistoriaRequest req, HttpContext ctx, ConexionDbApi db, AuditoriaService auditoria) =>
        {
            var (valido, error, criteriosNuevos) = ValidarHistoria(req);
            if (!valido)
            {
                return Results.BadRequest(new ApiErrorResponse(error));
            }

            using var conexion = await db.CrearConexionAbiertaAsync();

            if (!await PuedeAccederAsync(ctx, conexion, req.IdProyecto))
            {
                return Results.Json(
                    new ApiErrorResponse("No tienes acceso a este proyecto."),
                    statusCode: StatusCodes.Status403Forbidden);
            }

            using var transaccion = await conexion.BeginTransactionAsync();
            try
            {
                var existe = await conexion.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM HistoriasUsuario WHERE IdHistoria = @Id AND IdProyecto = @IdProyecto;",
                    new { Id = id, req.IdProyecto }, transaccion);
                if (existe == 0)
                {
                    throw new InvalidOperationException("Historia no encontrada.");
                }

                var criteriosActuales = (await conexion.QueryAsync<string>(
                    "SELECT Descripcion FROM CriteriosAceptacion WHERE IdHistoria = @Id ORDER BY IdCriterio;",
                    new { Id = id }, transaccion)).ToList();

                if (!criteriosActuales.SequenceEqual(criteriosNuevos))
                {
                    var aprobados = await conexion.ExecuteScalarAsync<int>(@"
                        SELECT COUNT(*) FROM Aprobaciones a
                        JOIN CriteriosAceptacion ca ON ca.IdCriterio = a.IdCriterio
                        WHERE ca.IdHistoria = @Id;",
                        new { Id = id }, transaccion);
                    if (aprobados > 0)
                    {
                        throw new InvalidOperationException("No se pueden sustituir criterios con aprobaciones registradas.");
                    }

                    await conexion.ExecuteAsync("DELETE FROM CriteriosAceptacion WHERE IdHistoria = @Id;", new { Id = id }, transaccion);
                    await InsertarCriteriosAsync(conexion, id, criteriosNuevos, transaccion);
                }

                await conexion.ExecuteAsync(@"
                    UPDATE HistoriasUsuario
                    SET Codigo = @Codigo, Como = @Como, Quiero = @Quiero, Para = @Para, Prioridad = @Prioridad,
                        OrdenPrioridad = @OrdenPrioridad, Estimacion = @Estimacion, Estado = @Estado
                    WHERE IdHistoria = @Id AND IdProyecto = @IdProyecto;",
                    new
                    {
                        req.Codigo, req.Como, req.Quiero, req.Para, req.Prioridad,
                        req.OrdenPrioridad, req.Estimacion, req.Estado, Id = id, req.IdProyecto
                    },
                    transaccion);

                var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(ctx);
                await auditoria.RegistrarAsync(conexion, idActor, "Editar", "HistoriasUsuario", id, req.Codigo, transaccion);

                await transaccion.CommitAsync();
                return Results.Ok(new ApiMessageResponse("Historia guardada en MySQL.", id));
            }
            catch (InvalidOperationException ex)
            {
                await transaccion.RollbackAsync();
                return Results.BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch
            {
                await transaccion.RollbackAsync();
                throw;
            }
        })
        .RequireRoles(RolesEdicion);

        // DELETE /api/backlog/historias/{id}
        group.MapDelete("/historias/{id:int}", async (int id, HttpContext ctx, ConexionDbApi db, AuditoriaService auditoria) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();

            var historia = await conexion.QueryFirstOrDefaultAsync<HistoriaIdentificacionFila>(
                "SELECT IdHistoria, IdProyecto, Codigo FROM HistoriasUsuario WHERE IdHistoria = @Id;",
                new { Id = id });
            if (historia is null)
            {
                return Results.BadRequest(new ApiErrorResponse("Historia no encontrada."));
            }

            if (!await PuedeAccederAsync(ctx, conexion, historia.IdProyecto))
            {
                return Results.Json(
                    new ApiErrorResponse("No tienes acceso a este proyecto."),
                    statusCode: StatusCodes.Status403Forbidden);
            }

            using var transaccion = await conexion.BeginTransactionAsync();
            try
            {
                var vinculada = await conexion.ExecuteScalarAsync<int>(@"
                    SELECT (SELECT COUNT(*) FROM SprintHistorias WHERE IdHistoria = @Id)
                         + (SELECT COUNT(*) FROM Aprobaciones WHERE IdHistoria = @Id);",
                    new { Id = id }, transaccion);
                if (vinculada > 0)
                {
                    throw new InvalidOperationException("La historia tiene Sprints o aprobaciones registradas; no se puede eliminar.");
                }

                await conexion.ExecuteAsync("DELETE FROM CriteriosAceptacion WHERE IdHistoria = @Id;", new { Id = id }, transaccion);
                await conexion.ExecuteAsync("DELETE FROM HistoriasUsuario WHERE IdHistoria = @Id;", new { Id = id }, transaccion);

                var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(ctx);
                await auditoria.RegistrarAsync(conexion, idActor, "Eliminar", "HistoriasUsuario", id,
                    $"Eliminó la historia {historia.Codigo} (sin dependencias).", transaccion);

                await transaccion.CommitAsync();
                return Results.Ok(new ApiMessageResponse("Historia eliminada.", id));
            }
            catch (InvalidOperationException ex)
            {
                await transaccion.RollbackAsync();
                return Results.BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch
            {
                await transaccion.RollbackAsync();
                throw;
            }
        })
        .RequireRoles(RolesEdicion);
    }

    private static bool EsAdmin(HttpContext ctx) =>
        string.Equals(ctx.Items[RequireRolesFilter.CurrentUserRoleKey] as string, "Administrador", StringComparison.OrdinalIgnoreCase);

    private static async Task<bool> PuedeAccederAsync(HttpContext ctx, MySqlConnection conexion, int idProyecto)
    {
        if (EsAdmin(ctx))
        {
            return true;
        }

        var idUsuario = RequireRolesFilter.ObtenerIdUsuarioActual(ctx);
        var cantidad = await conexion.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM ProyectoMiembros WHERE IdProyecto = @IdProyecto AND IdUsuario = @IdUsuario;",
            new { IdProyecto = idProyecto, IdUsuario = idUsuario });
        return cantidad > 0;
    }

    private static async Task<List<ProyectoBacklogOpcionDto>> ListarProyectosVisiblesAsync(HttpContext ctx, MySqlConnection conexion)
    {
        var esAdmin = EsAdmin(ctx);
        var idUsuario = RequireRolesFilter.ObtenerIdUsuarioActual(ctx);

        var proyectos = await conexion.QueryAsync<ProyectoBacklogOpcionDto>(@"
            SELECT p.IdProyecto, p.Codigo, p.Nombre, p.Estado
            FROM Proyectos p
            WHERE @EsAdmin = 1 OR EXISTS (
                SELECT 1 FROM ProyectoMiembros pm WHERE pm.IdProyecto = p.IdProyecto AND pm.IdUsuario = @IdUsuario
            )
            ORDER BY p.Nombre;",
            new { EsAdmin = esAdmin ? 1 : 0, IdUsuario = idUsuario });

        return proyectos.ToList();
    }

    private static async Task<List<HistoriaBacklogDto>> ListarHistoriasAsync(MySqlConnection conexion, int idProyecto)
    {
        var historias = await conexion.QueryAsync<HistoriaBacklogDto>(@"
            SELECT h.*, p.Nombre AS NombreProyecto,
                   (SELECT COUNT(*) FROM CriteriosAceptacion ca WHERE ca.IdHistoria = h.IdHistoria) AS TotalCriterios,
                   (SELECT GROUP_CONCAT(ca.Descripcion SEPARATOR '\n') FROM CriteriosAceptacion ca WHERE ca.IdHistoria = h.IdHistoria) AS CriteriosTexto
            FROM HistoriasUsuario h
            JOIN Proyectos p ON p.IdProyecto = h.IdProyecto
            WHERE h.IdProyecto = @IdProyecto
            ORDER BY h.OrdenPrioridad, h.IdHistoria;",
            new { IdProyecto = idProyecto });

        return historias.ToList();
    }

    private static async Task InsertarCriteriosAsync(MySqlConnection conexion, int idHistoria, List<string> criterios, MySqlTransaction transaccion)
    {
        foreach (var criterio in criterios)
        {
            await conexion.ExecuteAsync(
                "INSERT INTO CriteriosAceptacion (IdHistoria, Descripcion) VALUES (@IdHistoria, @Descripcion);",
                new { IdHistoria = idHistoria, Descripcion = criterio }, transaccion);
        }
    }

    /// <summary>Valida las reglas de negocio del formulario (las de formato/longitud que no son
    /// un check de la base de datos) y separa el texto de criterios en líneas únicas no vacías.</summary>
    private static (bool Valido, string Error, List<string> Criterios) ValidarHistoria(GuardarHistoriaRequest req)
    {
        var criterios = (req.CriteriosTexto ?? string.Empty)
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct()
            .ToList();

        if (req.IdProyecto < 1)
        {
            return (false, "Debes indicar el proyecto.", criterios);
        }

        if (string.IsNullOrWhiteSpace(req.Codigo) || !CodigoHistoriaRegex.IsMatch(req.Codigo))
        {
            return (false, "Código inválido: usa el formato HU-01, HU-02, etc.", criterios);
        }

        if (string.IsNullOrWhiteSpace(req.Como) || req.Como.Length > 100)
        {
            return (false, "El campo \"Como\" es obligatorio (máximo 100 caracteres).", criterios);
        }

        if (string.IsNullOrWhiteSpace(req.Quiero) || req.Quiero.Length > 300)
        {
            return (false, "El campo \"Quiero\" es obligatorio (máximo 300 caracteres).", criterios);
        }

        if (string.IsNullOrWhiteSpace(req.Para) || req.Para.Length > 300)
        {
            return (false, "El campo \"Para\" es obligatorio (máximo 300 caracteres).", criterios);
        }

        if (!PrioridadesValidas.Contains(req.Prioridad))
        {
            return (false, "La prioridad debe ser Alta, Media o Baja.", criterios);
        }

        if (!EstadosValidos.Contains(req.Estado))
        {
            return (false, "Estado inválido.", criterios);
        }

        if (req.OrdenPrioridad < 1)
        {
            return (false, "El orden de prioridad debe ser mayor a 0.", criterios);
        }

        if (req.Estimacion is <= 0)
        {
            return (false, "La estimación debe ser mayor a 0 puntos.", criterios);
        }

        if (criterios.Any(c => c.Length > 300))
        {
            return (false, "Cada criterio de aceptación debe tener máximo 300 caracteres.", criterios);
        }

        if ((req.Estado is "Lista" or "En Sprint") && (criterios.Count == 0 || req.Estimacion is null))
        {
            return (false, "Para marcar la historia como Lista o En Sprint necesitas estimación y al menos un criterio de aceptación.", criterios);
        }

        return (true, string.Empty, criterios);
    }

    private sealed class HistoriaIdentificacionFila
    {
        public int IdHistoria { get; set; }
        public int IdProyecto { get; set; }
        public string Codigo { get; set; } = string.Empty;
    }
}
