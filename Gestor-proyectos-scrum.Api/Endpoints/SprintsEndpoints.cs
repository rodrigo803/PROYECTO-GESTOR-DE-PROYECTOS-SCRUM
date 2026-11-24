using Dapper;
using Gestor_proyectos_scrum.Api.Data;
using Gestor_proyectos_scrum.Api.Models;
using Gestor_proyectos_scrum.Api.Services;

namespace Gestor_proyectos_scrum.Api.Endpoints;

public static class SprintsEndpoints
{
    public static void MapSprintsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/sprints").WithTags("HU-05 Sprints");

        // GET /api/sprints/proyectos
        group.MapGet("/proyectos", async (ConexionDbApi db) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();
            var proyectos = await conexion.QueryAsync<ProyectoResumenDto>(@"
                SELECT IdProyecto, Codigo, Nombre, FechaInicio, FechaFin, Estado
                FROM Proyectos
                ORDER BY
                    CASE Estado
                        WHEN 'Activo' THEN 1
                        WHEN 'Planificado' THEN 2
                        WHEN 'En pausa' THEN 3
                        ELSE 4
                    END,
                    Codigo ASC;");
            return Results.Ok(proyectos);
        })
        .RequireRoles();

        // GET /api/sprints?idProyecto=&estado=&buscar=
        group.MapGet("/", async (int? idProyecto, string? estado, string? buscar, ConexionDbApi db) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();

            var sql = @"
                SELECT
                    s.IdSprint,
                    s.IdProyecto,
                    p.Codigo AS ProyectoCodigo,
                    p.Nombre AS ProyectoNombre,
                    p.Estado AS ProyectoEstado,
                    p.FechaInicio AS ProyectoFechaInicio,
                    p.FechaFin AS ProyectoFechaFin,
                    s.Nombre,
                    s.Objetivo,
                    s.FechaInicio,
                    s.FechaFin,
                    s.CapacidadPuntos,
                    s.Estado,
                    GREATEST(1, DATEDIFF(s.FechaFin, s.FechaInicio)) AS DuracionDias,
                    CASE WHEN s.Estado = 'Activo' THEN GREATEST(0, DATEDIFF(s.FechaFin, CURDATE())) ELSE 0 END AS DiasRestantes,
                    COALESCE(sh_res.TotalHistorias, 0) AS TotalHistorias,
                    COALESCE(sh_res.HistoriasTerminadas, 0) AS HistoriasTerminadas,
                    COALESCE(sh_res.PuntosComprometidos, 0) AS PuntosComprometidos,
                    COALESCE(v.TotalTareas, 0) AS TotalTareas,
                    COALESCE(v.Pendientes, 0) AS Pendientes,
                    COALESCE(v.EnProgreso, 0) AS EnProgreso,
                    COALESCE(v.EnRevision, 0) AS EnRevision,
                    COALESCE(v.Finalizadas, 0) AS Finalizadas,
                    COALESCE(v.AvancePonderado, 0) AS AvancePonderado,
                    COALESCE(v.HorasEstimadas, 0) AS HorasEstimadas,
                    COALESCE(v.HorasReales, 0) AS HorasReales
                FROM Sprints s
                INNER JOIN Proyectos p ON p.IdProyecto = s.IdProyecto
                LEFT JOIN vw_AvanceSprint v ON v.IdSprint = s.IdSprint
                LEFT JOIN (
                    SELECT
                        sh.IdSprint,
                        COUNT(sh.IdSprintHistoria) AS TotalHistorias,
                        SUM(CASE WHEN sh.Resultado = 'Terminada' THEN 1 ELSE 0 END) AS HistoriasTerminadas,
                        COALESCE(SUM(h.Estimacion), 0) AS PuntosComprometidos
                    FROM SprintHistorias sh
                    INNER JOIN HistoriasUsuario h ON h.IdHistoria = sh.IdHistoria
                    GROUP BY sh.IdSprint
                ) sh_res ON sh_res.IdSprint = s.IdSprint
                WHERE 1 = 1";

            var parametros = new DynamicParameters();

            if (idProyecto.HasValue && idProyecto.Value > 0)
            {
                sql += " AND s.IdProyecto = @IdProyecto";
                parametros.Add("IdProyecto", idProyecto.Value);
            }

            if (!string.IsNullOrWhiteSpace(estado))
            {
                sql += " AND s.Estado = @Estado";
                parametros.Add("Estado", estado.Trim());
            }

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                sql += " AND (s.Nombre LIKE @Buscar OR s.Objetivo LIKE @Buscar OR p.Nombre LIKE @Buscar OR p.Codigo LIKE @Buscar)";
                parametros.Add("Buscar", $"%{buscar.Trim()}%");
            }

            sql += @"
                ORDER BY
                    CASE s.Estado
                        WHEN 'Activo' THEN 1
                        WHEN 'Planificado' THEN 2
                        ELSE 3
                    END,
                    s.FechaInicio DESC,
                    s.IdSprint DESC;";

            var sprints = await conexion.QueryAsync<SprintDto>(sql, parametros);
            return Results.Ok(sprints);
        })
        .RequireRoles();

        // GET /api/sprints/{id:int}
        group.MapGet("/{id:int}", async (int id, ConexionDbApi db) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();

            const string sqlSprint = @"
                SELECT
                    s.IdSprint,
                    s.IdProyecto,
                    p.Codigo AS ProyectoCodigo,
                    p.Nombre AS ProyectoNombre,
                    p.Estado AS ProyectoEstado,
                    p.FechaInicio AS ProyectoFechaInicio,
                    p.FechaFin AS ProyectoFechaFin,
                    s.Nombre,
                    s.Objetivo,
                    s.FechaInicio,
                    s.FechaFin,
                    s.CapacidadPuntos,
                    s.Estado,
                    GREATEST(1, DATEDIFF(s.FechaFin, s.FechaInicio)) AS DuracionDias,
                    CASE WHEN s.Estado = 'Activo' THEN GREATEST(0, DATEDIFF(s.FechaFin, CURDATE())) ELSE 0 END AS DiasRestantes,
                    COALESCE(sh_res.TotalHistorias, 0) AS TotalHistorias,
                    COALESCE(sh_res.HistoriasTerminadas, 0) AS HistoriasTerminadas,
                    COALESCE(sh_res.PuntosComprometidos, 0) AS PuntosComprometidos,
                    COALESCE(v.TotalTareas, 0) AS TotalTareas,
                    COALESCE(v.Pendientes, 0) AS Pendientes,
                    COALESCE(v.EnProgreso, 0) AS EnProgreso,
                    COALESCE(v.EnRevision, 0) AS EnRevision,
                    COALESCE(v.Finalizadas, 0) AS Finalizadas,
                    COALESCE(v.AvancePonderado, 0) AS AvancePonderado,
                    COALESCE(v.HorasEstimadas, 0) AS HorasEstimadas,
                    COALESCE(v.HorasReales, 0) AS HorasReales
                FROM Sprints s
                INNER JOIN Proyectos p ON p.IdProyecto = s.IdProyecto
                LEFT JOIN vw_AvanceSprint v ON v.IdSprint = s.IdSprint
                LEFT JOIN (
                    SELECT
                        sh.IdSprint,
                        COUNT(sh.IdSprintHistoria) AS TotalHistorias,
                        SUM(CASE WHEN sh.Resultado = 'Terminada' THEN 1 ELSE 0 END) AS HistoriasTerminadas,
                        COALESCE(SUM(h.Estimacion), 0) AS PuntosComprometidos
                    FROM SprintHistorias sh
                    INNER JOIN HistoriasUsuario h ON h.IdHistoria = sh.IdHistoria
                    GROUP BY sh.IdSprint
                ) sh_res ON sh_res.IdSprint = s.IdSprint
                WHERE s.IdSprint = @IdSprint;";

            var sprint = await conexion.QueryFirstOrDefaultAsync<SprintDto>(sqlSprint, new { IdSprint = id });
            if (sprint is null)
            {
                return Results.NotFound(new ApiErrorResponse($"No se encontró el Sprint con ID {id}."));
            }

            const string sqlHistorias = @"
                SELECT
                    sh.IdSprintHistoria,
                    sh.IdSprint,
                    h.IdHistoria,
                    h.Codigo,
                    h.Como,
                    h.Quiero,
                    h.Para,
                    h.Prioridad,
                    h.Estimacion,
                    h.Estado AS EstadoHistoria,
                    sh.Resultado,
                    sh.FechaCompromiso,
                    COALESCE(vah.TotalTareas, 0) AS TotalTareas,
                    COALESCE(vah.PuntosTotales, 0) AS PuntosTotales,
                    COALESCE(vah.AvancePonderado, 0) AS AvancePonderado,
                    COALESCE(vah.AvanceSimple, 0) AS AvanceSimple,
                    COALESCE(vah.HorasEstimadas, 0) AS HorasEstimadas,
                    COALESCE(vah.HorasReales, 0) AS HorasReales
                FROM SprintHistorias sh
                INNER JOIN HistoriasUsuario h ON h.IdHistoria = sh.IdHistoria
                LEFT JOIN vw_AvanceHistoria vah ON vah.IdSprintHistoria = sh.IdSprintHistoria
                WHERE sh.IdSprint = @IdSprint
                ORDER BY h.OrdenPrioridad ASC, h.Codigo ASC;";

            var historias = (await conexion.QueryAsync<SprintHistoriaItemDto>(sqlHistorias, new { IdSprint = id })).ToList();

            return Results.Ok(new SprintDetalleDto
            {
                Sprint = sprint,
                Historias = historias
            });
        })
        .RequireRoles();

        // POST /api/sprints (Requiere rol: Scrum Master, Administrador)
        group.MapPost("/", async (
            CrearSprintRequest req,
            HttpContext httpContext,
            ConexionDbApi db,
            AuditoriaService auditoria) =>
        {
            var errorBase = ValidarCamposSprint(req.Nombre, req.Objetivo, req.FechaInicio, req.FechaFin, req.CapacidadPuntos);
            if (errorBase is not null)
            {
                return Results.BadRequest(new ApiErrorResponse(errorBase));
            }

            using var conexion = await db.CrearConexionAbiertaAsync();

            var proyecto = await conexion.QueryFirstOrDefaultAsync<ProyectoResumenDto>(
                "SELECT IdProyecto, Codigo, Nombre, FechaInicio, FechaFin, Estado FROM Proyectos WHERE IdProyecto = @IdProyecto;",
                new { req.IdProyecto });

            if (proyecto is null)
            {
                return Results.BadRequest(new ApiErrorResponse("El proyecto seleccionado no existe."));
            }

            if (string.Equals(proyecto.Estado, "Cerrado", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new ApiErrorResponse($"No se pueden crear Sprints en el proyecto '{proyecto.Nombre}' porque se encuentra Cerrado."));
            }

            if (req.FechaInicio.Date < proyecto.FechaInicio.Date || req.FechaFin.Date > proyecto.FechaFin.Date)
            {
                return Results.BadRequest(new ApiErrorResponse(
                    $"Las fechas del Sprint ({req.FechaInicio:yyyy-MM-dd} a {req.FechaFin:yyyy-MM-dd}) deben estar dentro de la vigencia del proyecto {proyecto.Codigo} ({proyecto.FechaInicio:yyyy-MM-dd} a {proyecto.FechaFin:yyyy-MM-dd})."));
            }

            const string insertSql = @"
                INSERT INTO Sprints (IdProyecto, Nombre, Objetivo, FechaInicio, FechaFin, CapacidadPuntos, Estado)
                VALUES (@IdProyecto, @Nombre, @Objetivo, @FechaInicio, @FechaFin, @CapacidadPuntos, 'Planificado');
                SELECT LAST_INSERT_ID();";

            var nuevoId = await conexion.ExecuteScalarAsync<int>(insertSql, new
            {
                req.IdProyecto,
                Nombre = req.Nombre.Trim(),
                Objetivo = req.Objetivo.Trim(),
                FechaInicio = req.FechaInicio.Date,
                FechaFin = req.FechaFin.Date,
                req.CapacidadPuntos
            });

            var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);
            await auditoria.RegistrarAsync(
                conexion,
                idActor,
                "Crear",
                "Sprints",
                nuevoId,
                $"Sprint '{req.Nombre.Trim()}' creado en estado Planificado para el proyecto {proyecto.Codigo}.");

            return Results.Created($"/api/sprints/{nuevoId}", new ApiMessageResponse("Sprint creado correctamente en estado Planificado.", nuevoId));
        })
        .RequireRoles("Scrum Master", "Administrador");

        // PUT /api/sprints/{id:int} (Requiere rol: Scrum Master, Administrador)
        group.MapPut("/{id:int}", async (
            int id,
            ActualizarSprintRequest req,
            HttpContext httpContext,
            ConexionDbApi db,
            AuditoriaService auditoria) =>
        {
            var errorBase = ValidarCamposSprint(req.Nombre, req.Objetivo, req.FechaInicio, req.FechaFin, req.CapacidadPuntos);
            if (errorBase is not null)
            {
                return Results.BadRequest(new ApiErrorResponse(errorBase));
            }

            using var conexion = await db.CrearConexionAbiertaAsync();

            var sprintActual = await conexion.QueryFirstOrDefaultAsync<SprintDto>(@"
                SELECT s.IdSprint, s.IdProyecto, s.Nombre, s.Estado,
                       p.Codigo AS ProyectoCodigo, p.Nombre AS ProyectoNombre,
                       p.FechaInicio AS ProyectoFechaInicio, p.FechaFin AS ProyectoFechaFin
                FROM Sprints s
                INNER JOIN Proyectos p ON p.IdProyecto = s.IdProyecto
                WHERE s.IdSprint = @IdSprint;",
                new { IdSprint = id });

            if (sprintActual is null)
            {
                return Results.NotFound(new ApiErrorResponse($"No se encontró el Sprint con ID {id}."));
            }

            if (string.Equals(sprintActual.Estado, "Cerrado", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new ApiErrorResponse("No se puede editar un Sprint que ya se encuentra Cerrado."));
            }

            if (req.FechaInicio.Date < sprintActual.ProyectoFechaInicio.Date || req.FechaFin.Date > sprintActual.ProyectoFechaFin.Date)
            {
                return Results.BadRequest(new ApiErrorResponse(
                    $"Las fechas del Sprint deben encontrarse dentro del rango del proyecto {sprintActual.ProyectoCodigo} ({sprintActual.ProyectoFechaInicio:yyyy-MM-dd} a {sprintActual.ProyectoFechaFin:yyyy-MM-dd})."));
            }

            await conexion.ExecuteAsync(@"
                UPDATE Sprints
                SET Nombre = @Nombre,
                    Objetivo = @Objetivo,
                    FechaInicio = @FechaInicio,
                    FechaFin = @FechaFin,
                    CapacidadPuntos = @CapacidadPuntos
                WHERE IdSprint = @IdSprint;",
                new
                {
                    IdSprint = id,
                    Nombre = req.Nombre.Trim(),
                    Objetivo = req.Objetivo.Trim(),
                    FechaInicio = req.FechaInicio.Date,
                    FechaFin = req.FechaFin.Date,
                    req.CapacidadPuntos
                });

            var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);
            await auditoria.RegistrarAsync(
                conexion,
                idActor,
                "Actualizar",
                "Sprints",
                id,
                $"Sprint '{req.Nombre.Trim()}' del proyecto {sprintActual.ProyectoCodigo} actualizado.");

            return Results.Ok(new ApiMessageResponse("Sprint actualizado correctamente.", id));
        })
        .RequireRoles("Scrum Master", "Administrador");

        // PATCH /api/sprints/{id:int}/estado (Requiere rol: Scrum Master, Administrador)
        group.MapPatch("/{id:int}/estado", async (
            int id,
            CambiarEstadoSprintRequest req,
            HttpContext httpContext,
            ConexionDbApi db,
            AuditoriaService auditoria) =>
        {
            if (string.IsNullOrWhiteSpace(req.NuevoEstado))
            {
                return Results.BadRequest(new ApiErrorResponse("Debe indicar el nuevo estado del Sprint."));
            }

            var nuevoEstado = req.NuevoEstado.Trim();

            using var conexion = await db.CrearConexionAbiertaAsync();
            using var transaccion = await conexion.BeginTransactionAsync();

            var sprint = await conexion.QueryFirstOrDefaultAsync<SprintDto>(@"
                SELECT s.IdSprint, s.IdProyecto, s.Nombre, s.Estado,
                       p.Codigo AS ProyectoCodigo, p.Nombre AS ProyectoNombre, p.Estado AS ProyectoEstado
                FROM Sprints s
                INNER JOIN Proyectos p ON p.IdProyecto = s.IdProyecto
                WHERE s.IdSprint = @IdSprint
                FOR UPDATE;",
                new { IdSprint = id },
                transaction: transaccion);

            if (sprint is null)
            {
                await transaccion.RollbackAsync();
                return Results.NotFound(new ApiErrorResponse($"No se encontró el Sprint con ID {id}."));
            }

            var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);

            // Transición 1: Planificado -> Activo
            if (string.Equals(sprint.Estado, "Planificado", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(nuevoEstado, "Activo", StringComparison.OrdinalIgnoreCase))
            {
                if (string.Equals(sprint.ProyectoEstado, "Cerrado", StringComparison.OrdinalIgnoreCase))
                {
                    await transaccion.RollbackAsync();
                    return Results.BadRequest(new ApiErrorResponse("No se puede iniciar un Sprint de un proyecto Cerrado."));
                }

                var sprintActivoExistente = await conexion.QueryFirstOrDefaultAsync<string?>(@"
                    SELECT Nombre
                    FROM Sprints
                    WHERE IdProyecto = @IdProyecto
                      AND Estado = 'Activo'
                      AND IdSprint <> @IdSprint;",
                    new { sprint.IdProyecto, IdSprint = id },
                    transaction: transaccion);

                if (!string.IsNullOrEmpty(sprintActivoExistente))
                {
                    await transaccion.RollbackAsync();
                    return Results.Conflict(new ApiErrorResponse(
                        $"El proyecto {sprint.ProyectoCodigo} ya tiene un Sprint activo ('{sprintActivoExistente}'). Debe cerrarlo antes de iniciar otro."));
                }

                await conexion.ExecuteAsync(
                    "UPDATE Sprints SET Estado = 'Activo' WHERE IdSprint = @IdSprint;",
                    new { IdSprint = id },
                    transaction: transaccion);

                await auditoria.RegistrarAsync(
                    conexion,
                    idActor,
                    "Iniciar",
                    "Sprints",
                    id,
                    $"Sprint '{sprint.Nombre}' del proyecto {sprint.ProyectoCodigo} iniciado (Estado: Activo).",
                    transaccion);

                await transaccion.CommitAsync();
                return Results.Ok(new ApiMessageResponse($"El Sprint '{sprint.Nombre}' ha sido iniciado y ahora está Activo.", id));
            }

            // Transición 2: Activo -> Cerrado
            if (string.Equals(sprint.Estado, "Activo", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(nuevoEstado, "Cerrado", StringComparison.OrdinalIgnoreCase))
            {
                var destinoIncompletas = string.Equals(req.DestinoIncompletas?.Trim(), "Replanificada", StringComparison.OrdinalIgnoreCase)
                    ? "Replanificada"
                    : "Devuelta";

                var historiasSprint = (await conexion.QueryAsync<HistoriaCierreEvaluacionRow>(@"
                    SELECT
                        sh.IdSprintHistoria,
                        sh.IdHistoria,
                        COUNT(t.IdTarea) AS TotalTareas,
                        SUM(CASE WHEN t.Estado = 'Finalizado' AND t.PorcentajeAvance = 100 THEN 1 ELSE 0 END) AS TareasCompletadas100
                    FROM SprintHistorias sh
                    LEFT JOIN Tareas t ON t.IdSprintHistoria = sh.IdSprintHistoria
                    WHERE sh.IdSprint = @IdSprint
                      AND sh.Resultado = 'Comprometida'
                    GROUP BY sh.IdSprintHistoria, sh.IdHistoria;",
                    new { IdSprint = id },
                    transaction: transaccion)).ToList();

                var idsSprintHistoriaTerminadas = new List<int>();
                var idsHistoriaTerminadas = new List<int>();
                var idsSprintHistoriaIncompletas = new List<int>();
                var idsHistoriaIncompletas = new List<int>();

                foreach (var h in historiasSprint)
                {
                    if (h.TotalTareas > 0 && h.TotalTareas == h.TareasCompletadas100)
                    {
                        idsSprintHistoriaTerminadas.Add(h.IdSprintHistoria);
                        idsHistoriaTerminadas.Add(h.IdHistoria);
                    }
                    else
                    {
                        idsSprintHistoriaIncompletas.Add(h.IdSprintHistoria);
                        idsHistoriaIncompletas.Add(h.IdHistoria);
                    }
                }

                if (idsSprintHistoriaTerminadas.Count > 0)
                {
                    await conexion.ExecuteAsync(@"
                        UPDATE SprintHistorias
                        SET Resultado = 'Terminada'
                        WHERE IdSprintHistoria IN @Ids;",
                        new { Ids = idsSprintHistoriaTerminadas },
                        transaction: transaccion);

                    await conexion.ExecuteAsync(@"
                        UPDATE HistoriasUsuario
                        SET Estado = 'Terminada'
                        WHERE IdHistoria IN @Ids;",
                        new { Ids = idsHistoriaTerminadas },
                        transaction: transaccion);
                }

                if (idsSprintHistoriaIncompletas.Count > 0)
                {
                    await conexion.ExecuteAsync(@"
                        UPDATE SprintHistorias
                        SET Resultado = @Destino
                        WHERE IdSprintHistoria IN @Ids;",
                        new { Destino = destinoIncompletas, Ids = idsSprintHistoriaIncompletas },
                        transaction: transaccion);

                    await conexion.ExecuteAsync(@"
                        UPDATE HistoriasUsuario
                        SET Estado = 'Lista'
                        WHERE IdHistoria IN @Ids;",
                        new { Ids = idsHistoriaIncompletas },
                        transaction: transaccion);
                }

                await conexion.ExecuteAsync(
                    "UPDATE Sprints SET Estado = 'Cerrado' WHERE IdSprint = @IdSprint;",
                    new { IdSprint = id },
                    transaction: transaccion);

                var resumenCierre = $"Sprint '{sprint.Nombre}' ({sprint.ProyectoCodigo}) cerrado. Historias terminadas: {idsHistoriaTerminadas.Count}. Historias incompletas ({destinoIncompletas} al Product Backlog en estado Lista): {idsHistoriaIncompletas.Count}.";

                await auditoria.RegistrarAsync(
                    conexion,
                    idActor,
                    "Cerrar",
                    "Sprints",
                    id,
                    resumenCierre,
                    transaccion);

                await transaccion.CommitAsync();
                return Results.Ok(new ApiMessageResponse(resumenCierre, id));
            }

            await transaccion.RollbackAsync();
            return Results.BadRequest(new ApiErrorResponse(
                $"Transición de estado no permitida: no se puede pasar de '{sprint.Estado}' a '{nuevoEstado}'. Solo se permite Planificado -> Activo y Activo -> Cerrado."));
        })
        .RequireRoles("Scrum Master", "Administrador");

        // DELETE /api/sprints/{id:int} (Requiere rol: Scrum Master, Administrador)
        group.MapDelete("/{id:int}", async (
            int id,
            HttpContext httpContext,
            ConexionDbApi db,
            AuditoriaService auditoria) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();

            var sprint = await conexion.QueryFirstOrDefaultAsync<SprintDto>(@"
                SELECT s.IdSprint, s.Nombre, s.Estado, p.Codigo AS ProyectoCodigo
                FROM Sprints s
                INNER JOIN Proyectos p ON p.IdProyecto = s.IdProyecto
                WHERE s.IdSprint = @IdSprint;",
                new { IdSprint = id });

            if (sprint is null)
            {
                return Results.NotFound(new ApiErrorResponse($"No se encontró el Sprint con ID {id}."));
            }

            if (!string.Equals(sprint.Estado, "Planificado", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new ApiErrorResponse(
                    $"Solo se pueden eliminar Sprints en estado 'Planificado'. El Sprint '{sprint.Nombre}' está '{sprint.Estado}'."));
            }

            var totalHistorias = await conexion.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM SprintHistorias WHERE IdSprint = @IdSprint;",
                new { IdSprint = id });

            if (totalHistorias > 0)
            {
                return Results.Conflict(new ApiErrorResponse(
                    $"No se puede eliminar el Sprint '{sprint.Nombre}' porque ya tiene {totalHistorias} historia(s) de usuario comprometida(s)."));
            }

            var totalImpedimentos = await conexion.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Impedimentos WHERE IdSprint = @IdSprint;",
                new { IdSprint = id });

            if (totalImpedimentos > 0)
            {
                return Results.Conflict(new ApiErrorResponse(
                    $"No se puede eliminar el Sprint '{sprint.Nombre}' porque tiene impedimentos asociados."));
            }

            await conexion.ExecuteAsync("DELETE FROM Sprints WHERE IdSprint = @IdSprint;", new { IdSprint = id });

            var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);
            await auditoria.RegistrarAsync(
                conexion,
                idActor,
                "Eliminar",
                "Sprints",
                id,
                $"Sprint Planificado '{sprint.Nombre}' del proyecto {sprint.ProyectoCodigo} eliminado.");

            return Results.Ok(new ApiMessageResponse($"Sprint '{sprint.Nombre}' eliminado correctamente.", id));
        })
        .RequireRoles("Scrum Master", "Administrador");
    }

    private static string? ValidarCamposSprint(
        string? nombre,
        string? objetivo,
        DateTime fechaInicio,
        DateTime fechaFin,
        int capacidadPuntos)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 60)
        {
            return "El nombre del Sprint es obligatorio y no debe superar los 60 caracteres.";
        }

        if (string.IsNullOrWhiteSpace(objetivo) || objetivo.Trim().Length > 300)
        {
            return "El objetivo del Sprint es obligatorio y no debe superar los 300 caracteres.";
        }

        if (capacidadPuntos <= 0)
        {
            return "La capacidad en puntos del Sprint debe ser mayor que cero.";
        }

        if (fechaInicio.Date >= fechaFin.Date)
        {
            return "La fecha de inicio debe ser estrictamente anterior a la fecha de finalización.";
        }

        return null;
    }

    private sealed class HistoriaCierreEvaluacionRow
    {
        public int IdSprintHistoria { get; set; }
        public int IdHistoria { get; set; }
        public int TotalTareas { get; set; }
        public int TareasCompletadas100 { get; set; }
    }
}
