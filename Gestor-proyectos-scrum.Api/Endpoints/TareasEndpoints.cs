using Dapper;
using Gestor_proyectos_scrum.Api.Data;
using Gestor_proyectos_scrum.Api.Models;
using Gestor_proyectos_scrum.Api.Services;

namespace Gestor_proyectos_scrum.Api.Endpoints;

public static class TareasEndpoints
{
    private static readonly string[] EstadosValidosTarea = ["Pendiente", "En progreso", "En revisión", "Finalizado"];
    private static readonly string[] ResultadosValidosAprobacion = ["Aprobado", "Rechazado"];

    public static void MapTareasEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tareas").WithTags("HU-07 Tareas, Checkpoints y Aprobaciones");

        // GET /api/tareas/catalogos
        group.MapGet("/catalogos", async (ConexionDbApi db) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();
            var dto = await CargarCatalogosInternoAsync(conexion);
            return Results.Ok(dto);
        })
        .RequireRoles();

        // GET /api/tareas/sprint-backlog?idSprint=&idResponsable=&estado=&buscar=
        group.MapGet("/sprint-backlog", async (int? idSprint, int? idResponsable, string? estado, string? buscar, ConexionDbApi db) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();

            var catalogos = await CargarCatalogosInternoAsync(conexion);
            var viewDto = new SprintBacklogViewDto
            {
                SprintsDisponibles = catalogos.Sprints,
                ResponsablesDisponibles = catalogos.Responsables,
                SprintHistoriasElegibles = catalogos.SprintHistorias,
                ImpedimentosActivos = catalogos.ImpedimentosActivos
            };

            if (catalogos.Sprints.Count == 0)
            {
                return Results.Ok(viewDto);
            }

            // Determinar Sprint seleccionado (por defecto el Sprint 'Activo' más reciente o el primero)
            var idSprintTarget = idSprint.HasValue && catalogos.Sprints.Any(s => s.IdSprint == idSprint.Value)
                ? idSprint.Value
                : (catalogos.Sprints.FirstOrDefault(s => string.Equals(s.Estado, "Activo", StringComparison.OrdinalIgnoreCase))?.IdSprint
                   ?? catalogos.Sprints.First().IdSprint);

            // Cargar resumen completo del Sprint seleccionado
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

            viewDto.SprintSeleccionado = await conexion.QueryFirstOrDefaultAsync<SprintDto>(sqlSprint, new { IdSprint = idSprintTarget });

            // Cargar historias comprometidas del Sprint seleccionado
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
                    h.OrdenPrioridad,
                    h.Estimacion,
                    h.Estado AS EstadoHistoria,
                    sh.Resultado,
                    COALESCE(vah.AvancePonderado, 0) AS AvancePonderado,
                    COALESCE(vah.TotalTareas, 0) AS TotalTareas,
                    COALESCE(vah.PuntosTotales, 0) AS PuntosTotales,
                    COALESCE(vah.HorasEstimadas, 0) AS HorasEstimadas,
                    COALESCE(vah.HorasReales, 0) AS HorasReales
                FROM SprintHistorias sh
                INNER JOIN HistoriasUsuario h ON h.IdHistoria = sh.IdHistoria
                LEFT JOIN vw_AvanceHistoria vah ON vah.IdSprintHistoria = sh.IdSprintHistoria
                WHERE sh.IdSprint = @IdSprint
                ORDER BY h.OrdenPrioridad ASC, h.Codigo ASC;";

            var historias = (await conexion.QueryAsync<SprintBacklogHistoriaDto>(sqlHistorias, new { IdSprint = idSprintTarget })).ToList();

            if (historias.Count > 0)
            {
                var idsHistorias = historias.Select(h => h.IdHistoria).Distinct().ToList();

                // Cargar CriteriosAceptacion de las historias del Sprint
                var criterios = (await conexion.QueryAsync<CriterioAceptacionDto>(@"
                    SELECT IdCriterio, IdHistoria, Descripcion
                    FROM CriteriosAceptacion
                    WHERE IdHistoria IN @IdsHistorias
                    ORDER BY IdCriterio ASC;",
                    new { IdsHistorias = idsHistorias })).ToList();

                // Cargar todas las tareas del Sprint para las tarjetas KPI y luego aplicar filtros de tabla
                var todasTareasSprint = (await ConsultarTareasInternoAsync(conexion, idSprintTarget, null, null, null)).ToList();

                viewDto.CountPendientes = todasTareasSprint.Count(t => t.Estado == "Pendiente");
                viewDto.HorasPendientes = todasTareasSprint.Where(t => t.Estado == "Pendiente").Sum(t => t.HorasEstimadas);
                viewDto.CountEnProgreso = todasTareasSprint.Count(t => t.Estado == "En progreso");
                viewDto.HorasEnProgreso = todasTareasSprint.Where(t => t.Estado == "En progreso").Sum(t => t.HorasEstimadas);
                viewDto.CountEnRevision = todasTareasSprint.Count(t => t.Estado == "En revisión");
                viewDto.HorasEnRevision = todasTareasSprint.Where(t => t.Estado == "En revisión").Sum(t => t.HorasEstimadas);
                viewDto.CountFinalizadas = todasTareasSprint.Count(t => t.Estado == "Finalizado");
                viewDto.HorasFinalizadas = todasTareasSprint.Where(t => t.Estado == "Finalizado").Sum(t => t.HorasReales > 0 ? t.HorasReales : t.HorasEstimadas);

                var tareasFiltradas = (idResponsable.HasValue || !string.IsNullOrWhiteSpace(estado) || !string.IsNullOrWhiteSpace(buscar))
                    ? (await ConsultarTareasInternoAsync(conexion, idSprintTarget, idResponsable, estado, buscar)).ToList()
                    : todasTareasSprint;

                foreach (var h in historias)
                {
                    h.CriteriosAceptacion = criterios.Where(c => c.IdHistoria == h.IdHistoria).ToList();
                    h.Tareas = tareasFiltradas.Where(t => t.IdSprintHistoria == h.IdSprintHistoria).ToList();
                }
            }

            viewDto.Historias = historias;
            return Results.Ok(viewDto);
        })
        .RequireRoles();

        // GET /api/tareas?idSprint=&idResponsable=&estado=&buscar=
        // Miembro del Equipo solo puede ver sus propias tareas: se fuerza idResponsable a su Id,
        // sin importar lo que pida el parámetro (evita que consulte tareas de otro usuario).
        group.MapGet("/", async (int? idSprint, int? idResponsable, string? estado, string? buscar, HttpContext httpContext, ConexionDbApi db) =>
        {
            var rolActor = RequireRolesFilter.ObtenerRolUsuarioActual(httpContext);
            if (string.Equals(rolActor, "Miembro del Equipo", StringComparison.OrdinalIgnoreCase))
            {
                idResponsable = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);
            }

            using var conexion = await db.CrearConexionAbiertaAsync();
            var tareas = await ConsultarTareasInternoAsync(conexion, idSprint, idResponsable, estado, buscar);
            return Results.Ok(tareas);
        })
        .RequireRoles();

        // GET /api/tareas/{id:int}
        group.MapGet("/{id:int}", async (int id, ConexionDbApi db) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();

            var tarea = await ObtenerTareaPorIdAsync(conexion, id);
            if (tarea is null)
            {
                return Results.NotFound(new ApiErrorResponse($"No se encontró la tarea con ID {id}."));
            }

            const string sqlAvances = @"
                SELECT
                    a.IdAvance,
                    a.IdTarea,
                    a.IdUsuario,
                    CONCAT(u.Nombre, ' ', u.Apellidos) AS UsuarioNombre,
                    r.Nombre AS UsuarioRol,
                    a.Fecha,
                    a.PorcentajeAnterior,
                    a.PorcentajeNuevo,
                    a.HorasTrabajadas,
                    a.EstadoAnterior,
                    a.EstadoNuevo,
                    a.Justificacion,
                    a.IdImpedimento,
                    i.Titulo AS ImpedimentoTitulo,
                    i.Severidad AS ImpedimentoSeveridad
                FROM AvanceTareas a
                INNER JOIN Usuarios u ON u.IdUsuario = a.IdUsuario
                INNER JOIN Roles r ON r.IdRol = u.IdRol
                LEFT JOIN Impedimentos i ON i.IdImpedimento = a.IdImpedimento
                WHERE a.IdTarea = @IdTarea
                ORDER BY a.Fecha DESC, a.IdAvance DESC;";

            var avances = (await conexion.QueryAsync<AvanceTareaDto>(sqlAvances, new { IdTarea = id })).ToList();

            const string sqlAprobaciones = @"
                SELECT
                    ap.IdAprobacion,
                    ap.IdHistoria,
                    ap.IdCriterio,
                    ap.IdTarea,
                    ap.IdAprobador,
                    CONCAT(u.Nombre, ' ', u.Apellidos) AS AprobadorNombre,
                    r.Nombre AS AprobadorRol,
                    ap.Resultado,
                    ap.Comentario,
                    ap.Fecha
                FROM Aprobaciones ap
                INNER JOIN Usuarios u ON u.IdUsuario = ap.IdAprobador
                INNER JOIN Roles r ON r.IdRol = u.IdRol
                WHERE ap.IdTarea = @IdTarea
                ORDER BY ap.Fecha DESC, ap.IdAprobacion DESC;";

            var aprobaciones = (await conexion.QueryAsync<AprobacionTareaDto>(sqlAprobaciones, new { IdTarea = id })).ToList();

            const string sqlImpedimentosSprint = @"
                SELECT
                    i.IdImpedimento,
                    i.IdSprint,
                    s.Nombre AS SprintNombre,
                    i.IdHistoria,
                    i.IdTarea,
                    i.Titulo,
                    i.Descripcion,
                    i.Severidad,
                    i.Estado,
                    i.FechaReporte
                FROM Impedimentos i
                INNER JOIN Sprints s ON s.IdSprint = i.IdSprint
                WHERE i.IdSprint = @IdSprint
                  AND i.Estado <> 'Resuelto'
                ORDER BY
                    CASE WHEN i.IdTarea = @IdTarea THEN 0 ELSE 1 END,
                    i.FechaReporte DESC;";

            var impedimentosSprint = (await conexion.QueryAsync<ImpedimentoResumenDto>(
                sqlImpedimentosSprint,
                new { tarea.IdSprint, IdTarea = id })).ToList();

            return Results.Ok(new TareaDetalleDto
            {
                Tarea = tarea,
                Avances = avances,
                Aprobaciones = aprobaciones,
                ImpedimentosSprint = impedimentosSprint
            });
        });

        // POST /api/tareas (Requiere rol: Administrador, Scrum Master)
        group.MapPost("/", async (
            CrearTareaRequest req,
            HttpContext httpContext,
            ConexionDbApi db,
            AuditoriaService auditoria) =>
        {
            var errorBase = ValidarMetadatosTarea(req.Titulo, req.Descripcion, req.PuntosEsfuerzo, req.HorasEstimadas, req.FechaEntregaEstimada);
            if (errorBase is not null)
            {
                return Results.BadRequest(new ApiErrorResponse(errorBase));
            }

            using var conexion = await db.CrearConexionAbiertaAsync();

            var shInfo = await conexion.QueryFirstOrDefaultAsync<SprintHistoriaValidacionRow>(@"
                SELECT sh.IdSprintHistoria, sh.IdSprint, sh.IdHistoria, sh.Resultado,
                       s.Nombre AS SprintNombre, s.Estado AS SprintEstado,
                       h.Codigo AS HistoriaCodigo
                FROM SprintHistorias sh
                INNER JOIN Sprints s ON s.IdSprint = sh.IdSprint
                INNER JOIN HistoriasUsuario h ON h.IdHistoria = sh.IdHistoria
                WHERE sh.IdSprintHistoria = @IdSprintHistoria;",
                new { req.IdSprintHistoria });

            if (shInfo is null)
            {
                return Results.BadRequest(new ApiErrorResponse("La historia de usuario comprometida en el Sprint no existe."));
            }

            if (string.Equals(shInfo.SprintEstado, "Cerrado", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new ApiErrorResponse($"No se pueden registrar tareas en el Sprint '{shInfo.SprintNombre}' porque está Cerrado."));
            }

            if (req.IdResponsable.HasValue && req.IdResponsable.Value > 0)
            {
                var responsableActivo = await conexion.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Usuarios WHERE IdUsuario = @IdUsuario AND Activo = 1;",
                    new { IdUsuario = req.IdResponsable.Value });

                if (responsableActivo == 0)
                {
                    return Results.BadRequest(new ApiErrorResponse("El responsable seleccionado no existe o se encuentra inactivo."));
                }
            }

            var idResponsableParam = (req.IdResponsable.HasValue && req.IdResponsable.Value > 0) ? req.IdResponsable : null;
            var descripcionLimpia = string.IsNullOrWhiteSpace(req.Descripcion) ? null : req.Descripcion.Trim();

            const string insertSql = @"
                INSERT INTO Tareas (
                    IdSprintHistoria, Titulo, Descripcion, IdResponsable,
                    PuntosEsfuerzo, HorasEstimadas, HorasReales,
                    PorcentajeAvance, Estado, FechaRegistro, FechaEntregaEstimada
                )
                VALUES (
                    @IdSprintHistoria, @Titulo, @Descripcion, @IdResponsable,
                    @PuntosEsfuerzo, @HorasEstimadas, 0,
                    0, 'Pendiente', NOW(), @FechaEntregaEstimada
                );
                SELECT LAST_INSERT_ID();";

            var nuevoId = await conexion.ExecuteScalarAsync<int>(insertSql, new
            {
                req.IdSprintHistoria,
                Titulo = req.Titulo.Trim(),
                Descripcion = descripcionLimpia,
                IdResponsable = idResponsableParam,
                req.PuntosEsfuerzo,
                req.HorasEstimadas,
                FechaEntregaEstimada = req.FechaEntregaEstimada.Date
            });

            var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);
            await auditoria.RegistrarAsync(
                conexion,
                idActor,
                "Crear",
                "Tareas",
                nuevoId,
                $"Tarea '{req.Titulo.Trim()}' creada en {shInfo.HistoriaCodigo} ({shInfo.SprintNombre}) con {req.PuntosEsfuerzo} pts y {req.HorasEstimadas} hrs.");

            return Results.Created($"/api/tareas/{nuevoId}", new ApiMessageResponse("Tarea creada correctamente en estado Pendiente (0%).", nuevoId));
        })
        .RequireRoles("Administrador", "Scrum Master");

        // PUT /api/tareas/{id:int}
        group.MapPut("/{id:int}", async (
            int id,
            ActualizarTareaRequest req,
            HttpContext httpContext,
            ConexionDbApi db,
            AuditoriaService auditoria) =>
        {
            var errorBase = ValidarMetadatosTarea(req.Titulo, req.Descripcion, req.PuntosEsfuerzo, req.HorasEstimadas, req.FechaEntregaEstimada);
            if (errorBase is not null)
            {
                return Results.BadRequest(new ApiErrorResponse(errorBase));
            }

            using var conexion = await db.CrearConexionAbiertaAsync();

            var tareaActual = await ObtenerTareaPorIdAsync(conexion, id);
            if (tareaActual is null)
            {
                return Results.NotFound(new ApiErrorResponse($"No se encontró la tarea con ID {id}."));
            }

            if (string.Equals(tareaActual.SprintEstado, "Cerrado", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new ApiErrorResponse($"No se puede editar la tarea porque el Sprint '{tareaActual.SprintNombre}' está Cerrado."));
            }

            if (req.IdResponsable.HasValue && req.IdResponsable.Value > 0)
            {
                var responsableActivo = await conexion.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Usuarios WHERE IdUsuario = @IdUsuario AND Activo = 1;",
                    new { IdUsuario = req.IdResponsable.Value });

                if (responsableActivo == 0)
                {
                    return Results.BadRequest(new ApiErrorResponse("El responsable seleccionado no existe o se encuentra inactivo."));
                }
            }

            var idResponsableParam = (req.IdResponsable.HasValue && req.IdResponsable.Value > 0) ? req.IdResponsable : null;
            var descripcionLimpia = string.IsNullOrWhiteSpace(req.Descripcion) ? null : req.Descripcion.Trim();

            await conexion.ExecuteAsync(@"
                UPDATE Tareas
                SET Titulo = @Titulo,
                    Descripcion = @Descripcion,
                    IdResponsable = @IdResponsable,
                    PuntosEsfuerzo = @PuntosEsfuerzo,
                    HorasEstimadas = @HorasEstimadas,
                    FechaEntregaEstimada = @FechaEntregaEstimada
                WHERE IdTarea = @IdTarea;",
                new
                {
                    IdTarea = id,
                    Titulo = req.Titulo.Trim(),
                    Descripcion = descripcionLimpia,
                    IdResponsable = idResponsableParam,
                    req.PuntosEsfuerzo,
                    req.HorasEstimadas,
                    FechaEntregaEstimada = req.FechaEntregaEstimada.Date
                });

            var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);
            await auditoria.RegistrarAsync(
                conexion,
                idActor,
                "Actualizar",
                "Tareas",
                id,
                $"Metadatos de tarea '{req.Titulo.Trim()}' actualizados ({req.PuntosEsfuerzo} pts, {req.HorasEstimadas} hrs est.).");

            return Results.Ok(new ApiMessageResponse("Metadatos de la tarea actualizados correctamente.", id));
        })
        .RequireRoles("Administrador", "Scrum Master");

        // POST /api/tareas/{id:int}/avances (Checkpoints inmutables en AvanceTareas)
        // Admin y Scrum Master en cualquier tarea; Miembro del Equipo solo en sus propias tareas asignadas.
        group.MapPost("/{id:int}/avances", async (
            int id,
            RegistrarAvanceTareaRequest req,
            HttpContext httpContext,
            ConexionDbApi db,
            AuditoriaService auditoria) =>
        {
            if (req.PorcentajeNuevo < 0 || req.PorcentajeNuevo > 100)
            {
                return Results.BadRequest(new ApiErrorResponse("El nuevo porcentaje de avance debe estar entre 0 y 100."));
            }

            if (req.HorasTrabajadas < 0)
            {
                return Results.BadRequest(new ApiErrorResponse("Las horas trabajadas en este checkpoint no pueden ser negativas."));
            }

            if (string.IsNullOrWhiteSpace(req.EstadoNuevo) ||
                !EstadosValidosTarea.Any(e => string.Equals(e, req.EstadoNuevo.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return Results.BadRequest(new ApiErrorResponse("El estado nuevo debe ser 'Pendiente', 'En progreso', 'En revisión' o 'Finalizado'."));
            }

            var estadoNuevoNormalizado = EstadosValidosTarea.First(e =>
                string.Equals(e, req.EstadoNuevo.Trim(), StringComparison.OrdinalIgnoreCase));

            // Regla ck_Tareas_FinalizadaAl100: si EstadoNuevo == "Finalizado", exigir PorcentajeNuevo == 100
            if (string.Equals(estadoNuevoNormalizado, "Finalizado", StringComparison.OrdinalIgnoreCase) && req.PorcentajeNuevo != 100)
            {
                return Results.BadRequest(new ApiErrorResponse(
                    "Para marcar la tarea como 'Finalizado', el porcentaje de avance debe ser exactamente 100%."));
            }

            if (!string.IsNullOrWhiteSpace(req.Justificacion) && req.Justificacion.Trim().Length > 300)
            {
                return Results.BadRequest(new ApiErrorResponse("La justificación no debe exceder 300 caracteres."));
            }

            using var conexion = await db.CrearConexionAbiertaAsync();

            var tareaActual = await ObtenerTareaPorIdAsync(conexion, id);
            if (tareaActual is null)
            {
                return Results.NotFound(new ApiErrorResponse($"No se encontró la tarea con ID {id}."));
            }

            if (string.Equals(tareaActual.SprintEstado, "Cerrado", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new ApiErrorResponse(
                    $"No se pueden registrar checkpoints de avance porque el Sprint '{tareaActual.SprintNombre}' está Cerrado."));
            }

            var rolActor = RequireRolesFilter.ObtenerRolUsuarioActual(httpContext);
            if (string.Equals(rolActor, "Miembro del Equipo", StringComparison.OrdinalIgnoreCase) &&
                tareaActual.IdResponsable != RequireRolesFilter.ObtenerIdUsuarioActual(httpContext))
            {
                return Results.Json(
                    new ApiErrorResponse("No puedes registrar avance en una tarea que no tienes asignada."),
                    statusCode: StatusCodes.Status403Forbidden);
            }

            var porcentajeAnterior = tareaActual.PorcentajeAvance;
            var estadoAnterior = tareaActual.Estado;

            var justificacionLimpia = string.IsNullOrWhiteSpace(req.Justificacion) ? null : req.Justificacion.Trim();
            var idImpedimentoParam = (req.IdImpedimento.HasValue && req.IdImpedimento.Value > 0) ? req.IdImpedimento : null;

            // Regla ck_AvanceTareas_RetrocesoJustificado: si PorcentajeNuevo < PorcentajeAnterior, exigir Justificacion o IdImpedimento
            if (req.PorcentajeNuevo < porcentajeAnterior &&
                justificacionLimpia is null &&
                !idImpedimentoParam.HasValue)
            {
                return Results.BadRequest(new ApiErrorResponse(
                    $"Cuando el porcentaje disminuye ({porcentajeAnterior}% -> {req.PorcentajeNuevo}%), es obligatorio indicar una justificación o vincular un impedimento activo."));
            }

            if (idImpedimentoParam.HasValue)
            {
                var existeImpedimento = await conexion.ExecuteScalarAsync<int>(
                    "SELECT COUNT(*) FROM Impedimentos WHERE IdImpedimento = @IdImpedimento;",
                    new { IdImpedimento = idImpedimentoParam.Value });

                if (existeImpedimento == 0)
                {
                    return Results.BadRequest(new ApiErrorResponse("El impedimento seleccionado no existe."));
                }
            }

            var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);

            const string insertAvanceSql = @"
                INSERT INTO AvanceTareas (
                    IdTarea, IdUsuario, Fecha,
                    PorcentajeAnterior, PorcentajeNuevo,
                    HorasTrabajadas, EstadoAnterior, EstadoNuevo,
                    Justificacion, IdImpedimento
                )
                VALUES (
                    @IdTarea, @IdUsuario, NOW(),
                    @PorcentajeAnterior, @PorcentajeNuevo,
                    @HorasTrabajadas, @EstadoAnterior, @EstadoNuevo,
                    @Justificacion, @IdImpedimento
                );
                SELECT LAST_INSERT_ID();";

            var idAvance = await conexion.ExecuteScalarAsync<int>(insertAvanceSql, new
            {
                IdTarea = id,
                IdUsuario = idActor,
                PorcentajeAnterior = porcentajeAnterior,
                PorcentajeNuevo = req.PorcentajeNuevo,
                HorasTrabajadas = req.HorasTrabajadas,
                EstadoAnterior = estadoAnterior,
                EstadoNuevo = estadoNuevoNormalizado,
                Justificacion = justificacionLimpia,
                IdImpedimento = idImpedimentoParam
            });

            // Si en la BD local no se creó el trigger tr_AvanceTareas_Registrar al importar el script,
            // sincroniza de forma idempotente la fila de Tareas sin duplicar horas.
            await ConexionDbApi.SincronizarTareasConAvancesAsync(conexion, id);

            await auditoria.RegistrarAsync(
                conexion,
                idActor,
                "Registrar Avance",
                "AvanceTareas",
                idAvance,
                $"Checkpoint en tarea #{id} '{tareaActual.Titulo}': {porcentajeAnterior}% -> {req.PorcentajeNuevo}% ({estadoAnterior} -> {estadoNuevoNormalizado}, +{req.HorasTrabajadas} hrs).");

            return Results.Created(
                $"/api/tareas/{id}",
                new ApiMessageResponse($"Checkpoint registrado: {porcentajeAnterior}% → {req.PorcentajeNuevo}% ({estadoNuevoNormalizado}).", idAvance));
        })
        .RequireRoles("Administrador", "Scrum Master", "Miembro del Equipo");

        // POST /api/tareas/{id:int}/aprobaciones
        group.MapPost("/{id:int}/aprobaciones", async (
            int id,
            RegistrarAprobacionTareaRequest req,
            HttpContext httpContext,
            ConexionDbApi db,
            AuditoriaService auditoria) =>
        {
            if (string.IsNullOrWhiteSpace(req.Resultado) ||
                !ResultadosValidosAprobacion.Any(r => string.Equals(r, req.Resultado.Trim(), StringComparison.OrdinalIgnoreCase)))
            {
                return Results.BadRequest(new ApiErrorResponse("El resultado de la revisión debe ser 'Aprobado' o 'Rechazado'."));
            }

            var resultadoNormalizado = ResultadosValidosAprobacion.First(r =>
                string.Equals(r, req.Resultado.Trim(), StringComparison.OrdinalIgnoreCase));

            var comentarioLimpio = string.IsNullOrWhiteSpace(req.Comentario) ? null : req.Comentario.Trim();

            if (string.Equals(resultadoNormalizado, "Rechazado", StringComparison.OrdinalIgnoreCase) && comentarioLimpio is null)
            {
                return Results.BadRequest(new ApiErrorResponse("Cuando el resultado es 'Rechazado', es obligatorio ingresar un comentario explicando el motivo."));
            }

            if (comentarioLimpio is not null && comentarioLimpio.Length > 500)
            {
                return Results.BadRequest(new ApiErrorResponse("El comentario no debe superar los 500 caracteres."));
            }

            using var conexion = await db.CrearConexionAbiertaAsync();

            var tareaActual = await ObtenerTareaPorIdAsync(conexion, id);
            if (tareaActual is null)
            {
                return Results.NotFound(new ApiErrorResponse($"No se encontró la tarea con ID {id}."));
            }

            var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);

            const string insertAprobacionSql = @"
                INSERT INTO Aprobaciones (IdHistoria, IdCriterio, IdTarea, IdAprobador, Resultado, Comentario, Fecha)
                VALUES (@IdHistoria, NULL, @IdTarea, @IdAprobador, @Resultado, @Comentario, NOW());
                SELECT LAST_INSERT_ID();";

            var idAprobacion = await conexion.ExecuteScalarAsync<int>(insertAprobacionSql, new
            {
                IdHistoria = tareaActual.IdHistoria,
                IdTarea = id,
                IdAprobador = idActor,
                Resultado = resultadoNormalizado,
                Comentario = comentarioLimpio
            });

            await auditoria.RegistrarAsync(
                conexion,
                idActor,
                resultadoNormalizado,
                "Aprobaciones",
                idAprobacion,
                $"Revisión formal de tarea #{id} '{tareaActual.Titulo}' ({tareaActual.HistoriaCodigo}): {resultadoNormalizado}.");

            return Results.Created(
                $"/api/tareas/{id}",
                new ApiMessageResponse($"Revisión registrada como '{resultadoNormalizado}' correctamente.", idAprobacion));
        })
        .RequireRoles("Administrador", "Scrum Master");

        // DELETE /api/tareas/{id:int}
        group.MapDelete("/{id:int}", async (
            int id,
            HttpContext httpContext,
            ConexionDbApi db,
            AuditoriaService auditoria) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();

            var tareaActual = await ObtenerTareaPorIdAsync(conexion, id);
            if (tareaActual is null)
            {
                return Results.NotFound(new ApiErrorResponse($"No se encontró la tarea con ID {id}."));
            }

            if (string.Equals(tareaActual.SprintEstado, "Cerrado", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new ApiErrorResponse(
                    $"No se puede eliminar la tarea porque el Sprint '{tareaActual.SprintNombre}' está Cerrado."));
            }

            var tieneAvances = await conexion.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM AvanceTareas WHERE IdTarea = @IdTarea;",
                new { IdTarea = id });

            if (tieneAvances > 0)
            {
                return Results.Conflict(new ApiErrorResponse(
                    $"No se puede eliminar la tarea '{tareaActual.Titulo}' porque ya tiene {tieneAvances} checkpoint(s) inmutable(s) en AvanceTareas."));
            }

            var tieneAprobaciones = await conexion.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Aprobaciones WHERE IdTarea = @IdTarea;",
                new { IdTarea = id });

            if (tieneAprobaciones > 0)
            {
                return Results.Conflict(new ApiErrorResponse(
                    $"No se puede eliminar la tarea '{tareaActual.Titulo}' porque ya cuenta con registros de aprobación/rechazo."));
            }

            var tieneImpedimentos = await conexion.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Impedimentos WHERE IdTarea = @IdTarea;",
                new { IdTarea = id });

            if (tieneImpedimentos > 0)
            {
                return Results.Conflict(new ApiErrorResponse(
                    $"No se puede eliminar la tarea '{tareaActual.Titulo}' porque tiene impedimentos vinculados."));
            }

            await conexion.ExecuteAsync("DELETE FROM Tareas WHERE IdTarea = @IdTarea;", new { IdTarea = id });

            var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);
            await auditoria.RegistrarAsync(
                conexion,
                idActor,
                "Eliminar",
                "Tareas",
                id,
                $"Tarea '{tareaActual.Titulo}' ({tareaActual.HistoriaCodigo}) eliminada.");

            return Results.Ok(new ApiMessageResponse($"Tarea '{tareaActual.Titulo}' eliminada correctamente.", id));
        })
        .RequireRoles("Administrador", "Scrum Master");
    }

    private static async Task<TareaCatalogosDto> CargarCatalogosInternoAsync(MySqlConnector.MySqlConnection conexion)
    {
        var sprintHistorias = (await conexion.QueryAsync<SprintHistoriaCatalogoDto>(@"
            SELECT
                sh.IdSprintHistoria,
                sh.IdSprint,
                s.Nombre AS SprintNombre,
                s.Estado AS SprintEstado,
                s.FechaInicio AS SprintFechaInicio,
                s.FechaFin AS SprintFechaFin,
                p.IdProyecto,
                p.Codigo AS ProyectoCodigo,
                p.Nombre AS ProyectoNombre,
                h.IdHistoria,
                h.Codigo AS HistoriaCodigo,
                h.Quiero AS HistoriaQuiero,
                h.Prioridad,
                h.Estimacion
            FROM SprintHistorias sh
            INNER JOIN Sprints s ON s.IdSprint = sh.IdSprint
            INNER JOIN Proyectos p ON p.IdProyecto = s.IdProyecto
            INNER JOIN HistoriasUsuario h ON h.IdHistoria = sh.IdHistoria
            WHERE s.Estado <> 'Cerrado'
              AND sh.Resultado = 'Comprometida'
            ORDER BY
                CASE s.Estado WHEN 'Activo' THEN 1 ELSE 2 END,
                p.Codigo ASC,
                s.IdSprint ASC,
                h.OrdenPrioridad ASC;")).ToList();

        var responsables = (await conexion.QueryAsync<ResponsableCatalogoDto>(@"
            SELECT
                u.IdUsuario,
                u.Nombre,
                u.Apellidos,
                u.Correo,
                r.Nombre AS RolNombre
            FROM Usuarios u
            INNER JOIN Roles r ON r.IdRol = u.IdRol
            WHERE u.Activo = 1
            ORDER BY u.Nombre ASC, u.Apellidos ASC;")).ToList();

        var impedimentosActivos = (await conexion.QueryAsync<ImpedimentoResumenDto>(@"
            SELECT
                i.IdImpedimento,
                i.IdSprint,
                s.Nombre AS SprintNombre,
                i.IdHistoria,
                i.IdTarea,
                i.Titulo,
                i.Descripcion,
                i.Severidad,
                i.Estado,
                i.FechaReporte
            FROM Impedimentos i
            INNER JOIN Sprints s ON s.IdSprint = i.IdSprint
            WHERE i.Estado <> 'Resuelto'
            ORDER BY
                CASE i.Severidad
                    WHEN 'Crítica' THEN 1
                    WHEN 'Alta' THEN 2
                    WHEN 'Media' THEN 3
                    ELSE 4
                END,
                i.FechaReporte DESC;")).ToList();

        var sprints = (await conexion.QueryAsync<SprintFiltroCatalogoDto>(@"
            SELECT
                s.IdSprint,
                s.IdProyecto,
                p.Codigo AS ProyectoCodigo,
                p.Nombre AS ProyectoNombre,
                s.Nombre,
                s.Estado
            FROM Sprints s
            INNER JOIN Proyectos p ON p.IdProyecto = s.IdProyecto
            ORDER BY
                CASE s.Estado
                    WHEN 'Activo' THEN 1
                    WHEN 'Planificado' THEN 2
                    ELSE 3
                END,
                s.FechaInicio DESC,
                s.IdSprint DESC;")).ToList();

        return new TareaCatalogosDto
        {
            SprintHistorias = sprintHistorias,
            Responsables = responsables,
            ImpedimentosActivos = impedimentosActivos,
            Sprints = sprints
        };
    }

    private static async Task<IEnumerable<TareaDto>> ConsultarTareasInternoAsync(
        MySqlConnector.MySqlConnection conexion,
        int? idSprint,
        int? idResponsable,
        string? estado,
        string? buscar)
    {
        var sql = @"
            SELECT
                t.IdTarea,
                t.IdSprintHistoria,
                s.IdSprint,
                s.Nombre AS SprintNombre,
                s.Estado AS SprintEstado,
                p.IdProyecto,
                p.Codigo AS ProyectoCodigo,
                p.Nombre AS ProyectoNombre,
                h.IdHistoria,
                h.Codigo AS HistoriaCodigo,
                h.Quiero AS HistoriaQuiero,
                h.Prioridad AS HistoriaPrioridad,
                t.Titulo,
                t.Descripcion,
                t.IdResponsable,
                CASE WHEN u.IdUsuario IS NULL THEN NULL ELSE CONCAT(u.Nombre, ' ', u.Apellidos) END AS ResponsableNombre,
                u.Correo AS ResponsableCorreo,
                r.Nombre AS ResponsableRol,
                t.PuntosEsfuerzo,
                t.HorasEstimadas,
                t.HorasReales,
                t.PorcentajeAvance,
                t.Estado,
                t.FechaRegistro,
                t.FechaEntregaEstimada,
                t.FechaEntregaReal,
                CASE WHEN t.Estado <> 'Finalizado' AND t.FechaEntregaEstimada < CURDATE() THEN 1 ELSE 0 END AS Atrasada,
                CASE WHEN EXISTS (
                    SELECT 1 FROM Impedimentos i
                    WHERE i.IdTarea = t.IdTarea AND i.Estado <> 'Resuelto'
                ) THEN 1 ELSE 0 END AS Bloqueada,
                (SELECT COUNT(*) FROM AvanceTareas a WHERE a.IdTarea = t.IdTarea) AS TotalCheckpoints,
                (SELECT COUNT(*) FROM Aprobaciones ap WHERE ap.IdTarea = t.IdTarea) AS TotalAprobaciones
            FROM Tareas t
            INNER JOIN SprintHistorias sh ON sh.IdSprintHistoria = t.IdSprintHistoria
            INNER JOIN Sprints s ON s.IdSprint = sh.IdSprint
            INNER JOIN Proyectos p ON p.IdProyecto = s.IdProyecto
            INNER JOIN HistoriasUsuario h ON h.IdHistoria = sh.IdHistoria
            LEFT JOIN Usuarios u ON u.IdUsuario = t.IdResponsable
            LEFT JOIN Roles r ON r.IdRol = u.IdRol
            WHERE 1 = 1";

        var parametros = new DynamicParameters();

        if (idSprint.HasValue && idSprint.Value > 0)
        {
            sql += " AND s.IdSprint = @IdSprint";
            parametros.Add("IdSprint", idSprint.Value);
        }

        if (idResponsable.HasValue && idResponsable.Value > 0)
        {
            sql += " AND t.IdResponsable = @IdResponsable";
            parametros.Add("IdResponsable", idResponsable.Value);
        }

        if (!string.IsNullOrWhiteSpace(estado))
        {
            sql += " AND t.Estado = @Estado";
            parametros.Add("Estado", estado.Trim());
        }

        if (!string.IsNullOrWhiteSpace(buscar))
        {
            sql += @" AND (
                t.Titulo LIKE @Buscar
                OR t.Descripcion LIKE @Buscar
                OR h.Codigo LIKE @Buscar
                OR h.Quiero LIKE @Buscar
                OR p.Codigo LIKE @Buscar
            )";
            parametros.Add("Buscar", $"%{buscar.Trim()}%");
        }

        sql += @"
            ORDER BY
                CASE s.Estado
                    WHEN 'Activo' THEN 1
                    WHEN 'Planificado' THEN 2
                    ELSE 3
                END,
                s.IdSprint DESC,
                h.OrdenPrioridad ASC,
                t.FechaEntregaEstimada ASC,
                t.IdTarea ASC;";

        return await conexion.QueryAsync<TareaDto>(sql, parametros);
    }

    private static async Task<TareaDto?> ObtenerTareaPorIdAsync(MySqlConnector.MySqlConnection conexion, int idTarea)
    {
        const string sql = @"
            SELECT
                t.IdTarea,
                t.IdSprintHistoria,
                s.IdSprint,
                s.Nombre AS SprintNombre,
                s.Estado AS SprintEstado,
                p.IdProyecto,
                p.Codigo AS ProyectoCodigo,
                p.Nombre AS ProyectoNombre,
                h.IdHistoria,
                h.Codigo AS HistoriaCodigo,
                h.Quiero AS HistoriaQuiero,
                h.Prioridad AS HistoriaPrioridad,
                t.Titulo,
                t.Descripcion,
                t.IdResponsable,
                CASE WHEN u.IdUsuario IS NULL THEN NULL ELSE CONCAT(u.Nombre, ' ', u.Apellidos) END AS ResponsableNombre,
                u.Correo AS ResponsableCorreo,
                r.Nombre AS ResponsableRol,
                t.PuntosEsfuerzo,
                t.HorasEstimadas,
                t.HorasReales,
                t.PorcentajeAvance,
                t.Estado,
                t.FechaRegistro,
                t.FechaEntregaEstimada,
                t.FechaEntregaReal,
                CASE WHEN t.Estado <> 'Finalizado' AND t.FechaEntregaEstimada < CURDATE() THEN 1 ELSE 0 END AS Atrasada,
                CASE WHEN EXISTS (
                    SELECT 1 FROM Impedimentos i
                    WHERE i.IdTarea = t.IdTarea AND i.Estado <> 'Resuelto'
                ) THEN 1 ELSE 0 END AS Bloqueada,
                (SELECT COUNT(*) FROM AvanceTareas a WHERE a.IdTarea = t.IdTarea) AS TotalCheckpoints,
                (SELECT COUNT(*) FROM Aprobaciones ap WHERE ap.IdTarea = t.IdTarea) AS TotalAprobaciones
            FROM Tareas t
            INNER JOIN SprintHistorias sh ON sh.IdSprintHistoria = t.IdSprintHistoria
            INNER JOIN Sprints s ON s.IdSprint = sh.IdSprint
            INNER JOIN Proyectos p ON p.IdProyecto = s.IdProyecto
            INNER JOIN HistoriasUsuario h ON h.IdHistoria = sh.IdHistoria
            LEFT JOIN Usuarios u ON u.IdUsuario = t.IdResponsable
            LEFT JOIN Roles r ON r.IdRol = u.IdRol
            WHERE t.IdTarea = @IdTarea;";

        return await conexion.QueryFirstOrDefaultAsync<TareaDto>(sql, new { IdTarea = idTarea });
    }

    private static string? ValidarMetadatosTarea(
        string? titulo,
        string? descripcion,
        int puntosEsfuerzo,
        decimal horasEstimadas,
        DateTime fechaEntregaEstimada)
    {
        if (string.IsNullOrWhiteSpace(titulo) || titulo.Trim().Length > 150)
        {
            return "El título de la tarea es obligatorio y no debe superar los 150 caracteres.";
        }

        if (!string.IsNullOrWhiteSpace(descripcion) && descripcion.Trim().Length > 500)
        {
            return "La descripción de la tarea no debe superar los 500 caracteres.";
        }

        if (puntosEsfuerzo <= 0)
        {
            return "Los puntos de esfuerzo deben ser mayores que cero.";
        }

        if (horasEstimadas <= 0 || horasEstimadas > 9999.9m)
        {
            return "Las horas estimadas deben ser mayores que cero.";
        }

        if (fechaEntregaEstimada == default)
        {
            return "La fecha de entrega estimada es obligatoria.";
        }

        return null;
    }

    private sealed class SprintHistoriaValidacionRow
    {
        public int IdSprintHistoria { get; set; }
        public int IdSprint { get; set; }
        public int IdHistoria { get; set; }
        public string Resultado { get; set; } = string.Empty;
        public string SprintNombre { get; set; } = string.Empty;
        public string SprintEstado { get; set; } = string.Empty;
        public string HistoriaCodigo { get; set; } = string.Empty;
    }
}
