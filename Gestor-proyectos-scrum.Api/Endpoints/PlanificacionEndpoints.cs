using Dapper;
using Gestor_proyectos_scrum.Api.Data;
using Gestor_proyectos_scrum.Api.Models;
using Gestor_proyectos_scrum.Api.Services;
using MySqlConnector;

namespace Gestor_proyectos_scrum.Api.Endpoints;

/// <summary>HU-06: Sprint Backlog / Planificación (migrado desde Gestor-proyectos-scrum/Data/PlanificacionRepositorio.cs).</summary>
public static class PlanificacionEndpoints
{
    private static readonly string[] RolesPlanificar = { "Administrador", "Product Owner", "Scrum Master" };
    private static readonly string[] RolesAprobar = { "Administrador", "Product Owner" };

    public static void MapPlanificacionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/planificacion").WithTags("HU-06 Planificación");

        // ---------- Lecturas (gestión) ----------

        group.MapGet("/proyectos", async (ConexionDbApi db) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();
            var filas = await conexion.QueryAsync<ProyectoOpcionDto>(
                "select IdProyecto, Codigo, Nombre, Estado from Proyectos where Estado <> 'Cerrado' order by Codigo;");
            return Results.Ok(filas.ToList());
        })
        .RequireRoles();

        group.MapGet("/proyectos/{idProyecto:int}/sprints", async (int idProyecto, ConexionDbApi db) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();
            var filas = await conexion.QueryAsync<SprintOpcionDto>(
                @"select IdSprint, Nombre, Estado from Sprints
                  where IdProyecto = @IdProyecto and Estado in ('Planificado', 'Activo')
                  order by FechaInicio;",
                new { IdProyecto = idProyecto });
            return Results.Ok(filas.ToList());
        })
        .RequireRoles();

        group.MapGet("/sprints/{idSprint:int}", async (int idSprint, ConexionDbApi db) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();
            var sprint = await ObtenerSprintAsync(conexion, idSprint);
            return sprint is null
                ? Results.NotFound(new ApiErrorResponse($"No se encontró el Sprint con ID {idSprint}."))
                : Results.Ok(sprint);
        })
        .RequireRoles();

        group.MapGet("/proyectos/{idProyecto:int}/historias-no-terminadas", async (int idProyecto, ConexionDbApi db) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();
            var filas = await conexion.QueryAsync<HistoriaNoTerminadaItemDto>(
                @"select sh.IdSprintHistoria, h.IdHistoria, h.Codigo, h.Quiero, h.Estimacion, s.Nombre as NombreSprint
                  from SprintHistorias sh
                  join HistoriasUsuario h on h.IdHistoria = sh.IdHistoria
                  join Sprints s on s.IdSprint = sh.IdSprint
                  where s.IdProyecto = @IdProyecto and s.Estado = 'Cerrado' and sh.Resultado = 'Comprometida'
                  order by s.FechaInicio, h.OrdenPrioridad;",
                new { IdProyecto = idProyecto });
            return Results.Ok(filas.ToList());
        })
        .RequireRoles();

        group.MapGet("/proyectos/{idProyecto:int}/candidatas", async (int idProyecto, ConexionDbApi db) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();
            var candidatas = await ListarCandidatasAsync(conexion, idProyecto);
            return Results.Ok(candidatas);
        })
        .RequireRoles();

        group.MapGet("/sprints/{idSprint:int}/comprometidas", async (int idSprint, ConexionDbApi db) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();
            var filas = await conexion.QueryAsync<HistoriaComprometidaItemDto>(
                @"select
                      h.IdHistoria, h.Codigo, h.Quiero, h.Estimacion, sh.Resultado,
                      (select count(*) from Tareas t where t.IdSprintHistoria = sh.IdSprintHistoria) as CantidadTareas
                  from SprintHistorias sh
                  join HistoriasUsuario h on h.IdHistoria = sh.IdHistoria
                  where sh.IdSprint = @IdSprint and sh.Resultado = 'Comprometida'
                  order by h.OrdenPrioridad;",
                new { IdSprint = idSprint });
            return Results.Ok(filas.ToList());
        })
        .RequireRoles();

        // ---------- Lecturas (vista cliente, con validación de membresía) ----------

        group.MapGet("/cliente/proyectos", async (HttpContext httpContext, ConexionDbApi db) =>
        {
            var idUsuario = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);
            using var conexion = await db.CrearConexionAbiertaAsync();
            var filas = await conexion.QueryAsync<ProyectoOpcionDto>(
                @"select p.IdProyecto, p.Codigo, p.Nombre, p.Estado
                  from Proyectos p
                  join ProyectoMiembros pm on pm.IdProyecto = p.IdProyecto and pm.IdUsuario = @IdUsuario
                  order by p.Codigo;",
                new { IdUsuario = idUsuario });
            return Results.Ok(filas.ToList());
        })
        .RequireRoles();

        group.MapGet("/cliente/proyectos/{idProyecto:int}/sprints", async (int idProyecto, HttpContext httpContext, ConexionDbApi db) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();

            if (!await EsMiembroDeProyectoAsync(conexion, idProyecto, RequireRolesFilter.ObtenerIdUsuarioActual(httpContext)))
            {
                return Results.Json(new ApiErrorResponse("No tienes acceso a ese proyecto."), statusCode: StatusCodes.Status403Forbidden);
            }

            var filas = await conexion.QueryAsync<SprintOpcionDto>(
                @"select IdSprint, Nombre, Estado from Sprints
                  where IdProyecto = @IdProyecto
                  order by case Estado when 'Activo' then 0 when 'Planificado' then 1 else 2 end, FechaInicio desc;",
                new { IdProyecto = idProyecto });
            return Results.Ok(filas.ToList());
        })
        .RequireRoles();

        group.MapGet("/cliente/sprints/{idSprint:int}", async (int idSprint, HttpContext httpContext, ConexionDbApi db) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();
            var sprint = await ObtenerSprintAsync(conexion, idSprint);
            if (sprint is null)
            {
                return Results.NotFound(new ApiErrorResponse($"No se encontró el Sprint con ID {idSprint}."));
            }

            if (!await EsMiembroDeProyectoAsync(conexion, sprint.IdProyecto, RequireRolesFilter.ObtenerIdUsuarioActual(httpContext)))
            {
                return Results.Json(new ApiErrorResponse("No tienes acceso a ese proyecto."), statusCode: StatusCodes.Status403Forbidden);
            }

            return Results.Ok(sprint);
        })
        .RequireRoles();

        group.MapGet("/cliente/sprints/{idSprint:int}/comprometidas", async (int idSprint, HttpContext httpContext, ConexionDbApi db) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();

            var idProyecto = await conexion.ExecuteScalarAsync<int?>(
                "select IdProyecto from Sprints where IdSprint = @IdSprint;", new { IdSprint = idSprint });
            if (idProyecto is null)
            {
                return Results.NotFound(new ApiErrorResponse($"No se encontró el Sprint con ID {idSprint}."));
            }

            if (!await EsMiembroDeProyectoAsync(conexion, idProyecto.Value, RequireRolesFilter.ObtenerIdUsuarioActual(httpContext)))
            {
                return Results.Json(new ApiErrorResponse("No tienes acceso a ese proyecto."), statusCode: StatusCodes.Status403Forbidden);
            }

            const string sql = @"
                select
                    h.IdHistoria, h.Codigo, h.Quiero, h.Estimacion, h.Prioridad, sh.Resultado,
                    (select count(*) from Tareas t where t.IdSprintHistoria = sh.IdSprintHistoria) as CantidadTareas,
                    exists (
                        select 1 from Aprobaciones a
                        where a.IdHistoria = h.IdHistoria and a.Resultado = 'Rechazado' and a.Fecha >= sh.FechaCompromiso
                    ) as TieneRechazo
                from SprintHistorias sh
                join HistoriasUsuario h on h.IdHistoria = sh.IdHistoria
                where sh.IdSprint = @IdSprint and sh.Resultado in ('Comprometida', 'Terminada')
                order by h.OrdenPrioridad;";

            var filas = await conexion.QueryAsync<HistoriaComprometidaClienteFila>(sql, new { IdSprint = idSprint });

            var resultado = filas.Select(f => new HistoriaComprometidaClienteItemDto
            {
                IdHistoria = f.IdHistoria,
                Codigo = f.Codigo,
                Quiero = f.Quiero,
                Estimacion = f.Estimacion,
                Prioridad = f.Prioridad,
                CantidadTareas = f.CantidadTareas,
                EstadoAprobacion = f.Resultado == "Terminada" ? "Aceptada" : (f.TieneRechazo ? "Rechazada" : "Pendiente")
            }).ToList();

            return Results.Ok(resultado);
        })
        .RequireRoles();

        // ---------- Mutaciones: planificar (Administrador, Product Owner, Scrum Master) ----------

        group.MapPost("/sprints/{idSprint:int}/comprometer", async (
            int idSprint, ComprometerRequest req, HttpContext httpContext, ConexionDbApi db, AuditoriaService auditoria) =>
        {
            var idsSeleccionadas = (req.IdsHistorias ?? new List<int>()).Distinct().ToList();
            if (idsSeleccionadas.Count == 0)
            {
                return Results.BadRequest(new ApiErrorResponse("Debes seleccionar al menos una historia."));
            }

            using var conexion = await db.CrearConexionAbiertaAsync();

            var estadoProyecto = await conexion.ExecuteScalarAsync<string?>(
                @"select p.Estado from Sprints s join Proyectos p on p.IdProyecto = s.IdProyecto where s.IdSprint = @IdSprint;",
                new { IdSprint = idSprint });

            if (estadoProyecto is null)
            {
                return Results.NotFound(new ApiErrorResponse("El Sprint no existe."));
            }

            if (estadoProyecto == "Cerrado")
            {
                return Results.BadRequest(new ApiErrorResponse("El proyecto está Cerrado."));
            }

            var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);

            try
            {
                var codigos = await ComprometerAsync(conexion, idSprint, idsSeleccionadas, idActor, auditoria);
                return Results.Ok(new ApiMessageResponse($"Se comprometieron {idsSeleccionadas.Count} historia(s) al Sprint: {codigos}.", idSprint));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ApiErrorResponse(ex.Message));
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                return Results.Conflict(new ApiErrorResponse("Una de las historias ya fue comprometida por otro usuario. Vuelve a intentarlo."));
            }
        })
        .RequireRoles(RolesPlanificar);

        group.MapPost("/sprints/{idSprint:int}/historias/{idHistoria:int}/quitar", async (
            int idSprint, int idHistoria, HttpContext httpContext, ConexionDbApi db, AuditoriaService auditoria) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();
            var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);

            try
            {
                await QuitarHistoriaAsync(conexion, idSprint, idHistoria, idActor, auditoria);
                return Results.Ok(new ApiMessageResponse("La historia se quitó del Sprint y volvió al Product Backlog.", idSprint));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ApiErrorResponse(ex.Message));
            }
        })
        .RequireRoles(RolesPlanificar);

        group.MapPost("/sprint-historias/{idSprintHistoria:int}/regresar-backlog", async (
            int idSprintHistoria, HttpContext httpContext, ConexionDbApi db, AuditoriaService auditoria) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();
            var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);

            try
            {
                await RegresarAlBacklogAsync(conexion, idSprintHistoria, idActor, auditoria);
                return Results.Ok(new ApiMessageResponse("La historia volvió al Product Backlog como 'Lista'.", idSprintHistoria));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ApiErrorResponse(ex.Message));
            }
        })
        .RequireRoles(RolesPlanificar);

        group.MapPost("/sprint-historias/{idSprintHistoria:int}/replanificar", async (
            int idSprintHistoria, ReplanificarRequest req, HttpContext httpContext, ConexionDbApi db, AuditoriaService auditoria) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();
            var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);

            try
            {
                await ReplanificarAsync(conexion, idSprintHistoria, req.IdSprintDestino, idActor, auditoria);
                return Results.Ok(new ApiMessageResponse("La historia se replanificó al nuevo Sprint junto con sus tareas pendientes.", idSprintHistoria));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ApiErrorResponse(ex.Message));
            }
        })
        .RequireRoles(RolesPlanificar);

        // ---------- Mutaciones: aprobar (Administrador, Product Owner) ----------

        group.MapPost("/sprints/{idSprint:int}/historias/{idHistoria:int}/aceptar", async (
            int idSprint, int idHistoria, HttpContext httpContext, ConexionDbApi db, AuditoriaService auditoria) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();
            var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);

            try
            {
                await AceptarHistoriaAsync(conexion, idSprint, idHistoria, idActor, auditoria);
                return Results.Ok(new ApiMessageResponse("La historia se aceptó en la revisión del Sprint.", idSprint));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ApiErrorResponse(ex.Message));
            }
        })
        .RequireRoles(RolesAprobar);

        group.MapPost("/sprints/{idSprint:int}/historias/{idHistoria:int}/rechazar", async (
            int idSprint, int idHistoria, RechazarHistoriaRequest req, HttpContext httpContext, ConexionDbApi db, AuditoriaService auditoria) =>
        {
            if (string.IsNullOrWhiteSpace(req.Comentario))
            {
                return Results.BadRequest(new ApiErrorResponse("Debes escribir un comentario para rechazar la historia."));
            }

            using var conexion = await db.CrearConexionAbiertaAsync();
            var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);

            try
            {
                await RechazarHistoriaAsync(conexion, idSprint, idHistoria, req.Comentario.Trim(), idActor, auditoria);
                return Results.Ok(new ApiMessageResponse("La historia se rechazó en la revisión del Sprint. Sigue comprometida.", idSprint));
            }
            catch (InvalidOperationException ex)
            {
                return Results.BadRequest(new ApiErrorResponse(ex.Message));
            }
        })
        .RequireRoles(RolesAprobar);
    }

    // ---------- Lógica migrada de PlanificacionRepositorio.cs ----------

    private static async Task<SprintPlanificacionInfoDto?> ObtenerSprintAsync(MySqlConnection conexion, int idSprint) =>
        await conexion.QueryFirstOrDefaultAsync<SprintPlanificacionInfoDto>(
            @"select IdSprint, IdProyecto, Nombre, Objetivo, FechaInicio, FechaFin, Estado, CapacidadPuntos
              from Sprints where IdSprint = @IdSprint;",
            new { IdSprint = idSprint });

    private static async Task<bool> EsMiembroDeProyectoAsync(MySqlConnection conexion, int idProyecto, int idUsuario)
    {
        var cantidad = await conexion.ExecuteScalarAsync<int>(
            "select count(*) from ProyectoMiembros where IdProyecto = @IdProyecto and IdUsuario = @IdUsuario;",
            new { IdProyecto = idProyecto, IdUsuario = idUsuario });
        return cantidad > 0;
    }

    private static async Task<List<HistoriaCandidataItemDto>> ListarCandidatasAsync(MySqlConnection conexion, int idProyecto)
    {
        const string sql = @"
            select
                h.IdHistoria, h.Codigo, h.Como, h.Quiero, h.Para, h.Prioridad, h.OrdenPrioridad,
                h.Estimacion, h.Estado, count(ca.IdCriterio) as CantidadCriterios
            from HistoriasUsuario h
            left join CriteriosAceptacion ca on ca.IdHistoria = h.IdHistoria
            where h.IdProyecto = @IdProyecto
              and h.Estado <> 'Terminada'
              and not exists (
                  select 1 from SprintHistorias sh
                  where sh.IdHistoria = h.IdHistoria and sh.Resultado = 'Comprometida'
              )
            group by h.IdHistoria, h.Codigo, h.Como, h.Quiero, h.Para, h.Prioridad, h.OrdenPrioridad, h.Estimacion, h.Estado
            order by h.OrdenPrioridad;";

        var filas = await conexion.QueryAsync<HistoriaCandidataFila>(sql, new { IdProyecto = idProyecto });
        return filas.Select(MapearCandidata).ToList();
    }

    private static HistoriaCandidataItemDto MapearCandidata(HistoriaCandidataFila fila)
    {
        var motivos = new List<string>();
        if (fila.Estado != "Lista") motivos.Add("No está en estado Lista");
        if (fila.Estimacion is null) motivos.Add("Sin estimación");
        if (fila.CantidadCriterios == 0) motivos.Add("Sin criterios de aceptación");

        return new HistoriaCandidataItemDto
        {
            IdHistoria = fila.IdHistoria,
            Codigo = fila.Codigo,
            Como = fila.Como,
            Quiero = fila.Quiero,
            Para = fila.Para,
            Prioridad = fila.Prioridad,
            OrdenPrioridad = fila.OrdenPrioridad,
            Estimacion = fila.Estimacion,
            CantidadCriterios = fila.CantidadCriterios,
            Motivos = motivos
        };
    }

    /// <summary>Comprometer historias a un Sprint, revalidando todo con "for update" dentro de la
    /// transacción para evitar que dos personas comprometan la misma historia, o superen la
    /// capacidad, al mismo tiempo.</summary>
    private static async Task<string> ComprometerAsync(MySqlConnection conexion, int idSprint, IReadOnlyList<int> idsHistorias, int idUsuarioActual, AuditoriaService auditoria)
    {
        using var transaccion = await conexion.BeginTransactionAsync();

        try
        {
            var sprint = await conexion.QueryFirstOrDefaultAsync<SprintParaComprometerFila>(
                "select IdProyecto, Estado, CapacidadPuntos from Sprints where IdSprint = @IdSprint for update;",
                new { IdSprint = idSprint }, transaccion);

            if (sprint is null)
            {
                throw new InvalidOperationException("El Sprint no existe.");
            }

            if (sprint.Estado is not ("Planificado" or "Activo"))
            {
                throw new InvalidOperationException("El Sprint ya no está Planificado ni Activo.");
            }

            const string sqlHistorias = @"
                select
                    h.IdHistoria, h.Codigo, h.IdProyecto, h.Estado, h.Estimacion,
                    (select count(*) from CriteriosAceptacion ca where ca.IdHistoria = h.IdHistoria) as CantidadCriterios,
                    exists (
                        select 1 from SprintHistorias sh
                        where sh.IdHistoria = h.IdHistoria and sh.Resultado = 'Comprometida'
                    ) as YaComprometida
                from HistoriasUsuario h
                where h.IdHistoria in @IdsHistorias
                for update;";

            var historias = (await conexion.QueryAsync<HistoriaParaComprometerFila>(
                sqlHistorias, new { IdsHistorias = idsHistorias }, transaccion)).ToList();

            foreach (var idHistoria in idsHistorias)
            {
                var historia = historias.FirstOrDefault(h => h.IdHistoria == idHistoria);
                if (historia is null)
                {
                    throw new InvalidOperationException("Una de las historias seleccionadas ya no existe.");
                }

                if (historia.IdProyecto != sprint.IdProyecto)
                {
                    throw new InvalidOperationException($"La historia {historia.Codigo} no pertenece al proyecto del Sprint.");
                }

                if (historia.YaComprometida)
                {
                    throw new InvalidOperationException($"La historia {historia.Codigo} ya fue comprometida en un Sprint.");
                }

                if (historia.Estado != "Lista" || historia.Estimacion is null || historia.CantidadCriterios == 0)
                {
                    throw new InvalidOperationException($"La historia {historia.Codigo} ya no cumple las reglas para entrar al Sprint.");
                }
            }

            var puntosYaComprometidos = await conexion.ExecuteScalarAsync<int>(
                @"select coalesce(sum(h2.Estimacion), 0)
                  from SprintHistorias sh2
                  join HistoriasUsuario h2 on h2.IdHistoria = sh2.IdHistoria
                  where sh2.IdSprint = @IdSprint and sh2.Resultado = 'Comprometida';",
                new { IdSprint = idSprint }, transaccion);

            var puntosNuevos = historias.Sum(h => h.Estimacion ?? 0);
            if (puntosYaComprometidos + puntosNuevos > sprint.CapacidadPuntos)
            {
                var disponibles = sprint.CapacidadPuntos - puntosYaComprometidos;
                throw new InvalidOperationException(
                    $"Superas la capacidad del Sprint: disponibles {disponibles} pts, se intentaron comprometer {puntosNuevos} pts.");
            }

            foreach (var historia in historias)
            {
                await conexion.ExecuteAsync(
                    "insert into SprintHistorias (IdSprint, IdHistoria, Resultado) values (@IdSprint, @IdHistoria, 'Comprometida');",
                    new { IdSprint = idSprint, historia.IdHistoria }, transaccion);

                await conexion.ExecuteAsync(
                    "update HistoriasUsuario set Estado = 'En Sprint' where IdHistoria = @IdHistoria;",
                    new { historia.IdHistoria }, transaccion);
            }

            var codigos = string.Join(", ", historias.Select(h => h.Codigo));
            await auditoria.RegistrarAsync(conexion, idUsuarioActual, "Comprometer historias", "Sprints", idSprint,
                $"Comprometió las historias {codigos} al Sprint.", transaccion);

            await transaccion.CommitAsync();
            return codigos;
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    /// <summary>Quita una historia comprometida del Sprint y la devuelve a 'Lista'. Falla si tiene tareas.</summary>
    private static async Task QuitarHistoriaAsync(MySqlConnection conexion, int idSprint, int idHistoria, int idUsuarioActual, AuditoriaService auditoria)
    {
        using var transaccion = await conexion.BeginTransactionAsync();

        try
        {
            var estadoSprint = await conexion.ExecuteScalarAsync<string?>(
                "select Estado from Sprints where IdSprint = @IdSprint for update;",
                new { IdSprint = idSprint }, transaccion);

            if (estadoSprint is null)
            {
                throw new InvalidOperationException("El Sprint no existe.");
            }

            if (estadoSprint == "Cerrado")
            {
                throw new InvalidOperationException("No se puede modificar un Sprint Cerrado.");
            }

            var sprintHistoria = await conexion.QueryFirstOrDefaultAsync<SprintHistoriaFila>(
                @"select sh.IdSprintHistoria, h.Codigo
                  from SprintHistorias sh
                  join HistoriasUsuario h on h.IdHistoria = sh.IdHistoria
                  where sh.IdSprint = @IdSprint and sh.IdHistoria = @IdHistoria and sh.Resultado = 'Comprometida'
                  for update;",
                new { IdSprint = idSprint, IdHistoria = idHistoria }, transaccion);

            if (sprintHistoria is null)
            {
                throw new InvalidOperationException("La historia no está comprometida en este Sprint.");
            }

            var cantidadTareas = await conexion.ExecuteScalarAsync<int>(
                "select count(*) from Tareas where IdSprintHistoria = @IdSprintHistoria;",
                new { sprintHistoria.IdSprintHistoria }, transaccion);

            if (cantidadTareas > 0)
            {
                throw new InvalidOperationException("Primero hay que eliminar o mover sus tareas.");
            }

            await conexion.ExecuteAsync(
                "delete from SprintHistorias where IdSprintHistoria = @IdSprintHistoria;",
                new { sprintHistoria.IdSprintHistoria }, transaccion);

            await conexion.ExecuteAsync(
                "update HistoriasUsuario set Estado = 'Lista' where IdHistoria = @IdHistoria;",
                new { IdHistoria = idHistoria }, transaccion);

            await auditoria.RegistrarAsync(conexion, idUsuarioActual, "Quitar historia", "Sprints", idSprint,
                $"Quitó la historia {sprintHistoria.Codigo} del Sprint.", transaccion);

            await transaccion.CommitAsync();
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    /// <summary>Aprueba (Sprint Review) una historia comprometida en un Sprint Activo: debe tener al
    /// menos un criterio y, si tiene tareas, todas deben estar Finalizadas.</summary>
    private static async Task AceptarHistoriaAsync(MySqlConnection conexion, int idSprint, int idHistoria, int idUsuarioActual, AuditoriaService auditoria)
    {
        using var transaccion = await conexion.BeginTransactionAsync();

        try
        {
            var sprintHistoria = await BloquearSprintHistoriaComprometidaAsync(conexion, transaccion, idSprint, idHistoria, "Activo");

            var cantidadCriterios = await conexion.ExecuteScalarAsync<int>(
                "select count(*) from CriteriosAceptacion where IdHistoria = @IdHistoria;",
                new { IdHistoria = idHistoria }, transaccion);

            if (cantidadCriterios == 0)
            {
                throw new InvalidOperationException($"La historia {sprintHistoria.Codigo} no tiene criterios de aceptación.");
            }

            var cantidadTareasSinFinalizar = await conexion.ExecuteScalarAsync<int>(
                "select count(*) from Tareas where IdSprintHistoria = @IdSprintHistoria and Estado <> 'Finalizado';",
                new { sprintHistoria.IdSprintHistoria }, transaccion);

            if (cantidadTareasSinFinalizar > 0)
            {
                throw new InvalidOperationException($"La historia {sprintHistoria.Codigo} tiene tareas sin Finalizar.");
            }

            await conexion.ExecuteAsync(
                "update SprintHistorias set Resultado = 'Terminada' where IdSprintHistoria = @IdSprintHistoria;",
                new { sprintHistoria.IdSprintHistoria }, transaccion);

            await conexion.ExecuteAsync(
                "update HistoriasUsuario set Estado = 'Terminada' where IdHistoria = @IdHistoria;",
                new { IdHistoria = idHistoria }, transaccion);

            await conexion.ExecuteAsync(
                "insert into Aprobaciones (IdHistoria, IdAprobador, Resultado) values (@IdHistoria, @IdAprobador, 'Aprobado');",
                new { IdHistoria = idHistoria, IdAprobador = idUsuarioActual }, transaccion);

            await auditoria.RegistrarAsync(conexion, idUsuarioActual, "Aceptar historia", "Sprints", idSprint,
                $"Aceptó la historia {sprintHistoria.Codigo} en la revisión del Sprint.", transaccion);

            await transaccion.CommitAsync();
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    /// <summary>Rechaza (Sprint Review) una historia comprometida en un Sprint Activo. La historia
    /// sigue comprometida; solo queda registrado el rechazo con su comentario.</summary>
    private static async Task RechazarHistoriaAsync(MySqlConnection conexion, int idSprint, int idHistoria, string comentario, int idUsuarioActual, AuditoriaService auditoria)
    {
        using var transaccion = await conexion.BeginTransactionAsync();

        try
        {
            var sprintHistoria = await BloquearSprintHistoriaComprometidaAsync(conexion, transaccion, idSprint, idHistoria, "Activo");

            await conexion.ExecuteAsync(
                "insert into Aprobaciones (IdHistoria, IdAprobador, Resultado, Comentario) values (@IdHistoria, @IdAprobador, 'Rechazado', @Comentario);",
                new { IdHistoria = idHistoria, IdAprobador = idUsuarioActual, Comentario = comentario }, transaccion);

            await auditoria.RegistrarAsync(conexion, idUsuarioActual, "Rechazar historia", "Sprints", idSprint,
                $"Rechazó la historia {sprintHistoria.Codigo} en la revisión del Sprint. Comentario: {comentario}", transaccion);

            await transaccion.CommitAsync();
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    /// <summary>Devuelve al Backlog una historia comprometida en un Sprint ya Cerrado que nunca se Terminó.</summary>
    private static async Task RegresarAlBacklogAsync(MySqlConnection conexion, int idSprintHistoria, int idUsuarioActual, AuditoriaService auditoria)
    {
        using var transaccion = await conexion.BeginTransactionAsync();

        try
        {
            var fila = await conexion.QueryFirstOrDefaultAsync<HistoriaNoTerminadaFila>(
                @"select sh.IdSprintHistoria, sh.IdHistoria, h.Codigo, s.Estado as EstadoSprint
                  from SprintHistorias sh
                  join HistoriasUsuario h on h.IdHistoria = sh.IdHistoria
                  join Sprints s on s.IdSprint = sh.IdSprint
                  where sh.IdSprintHistoria = @IdSprintHistoria and sh.Resultado = 'Comprometida'
                  for update;",
                new { IdSprintHistoria = idSprintHistoria }, transaccion);

            if (fila is null)
            {
                throw new InvalidOperationException("La historia ya no está comprometida en ese Sprint.");
            }

            if (fila.EstadoSprint != "Cerrado")
            {
                throw new InvalidOperationException("Esta acción solo aplica a historias de Sprints ya Cerrados.");
            }

            await conexion.ExecuteAsync(
                "update SprintHistorias set Resultado = 'Devuelta' where IdSprintHistoria = @IdSprintHistoria;",
                new { IdSprintHistoria = idSprintHistoria }, transaccion);

            await conexion.ExecuteAsync(
                "update HistoriasUsuario set Estado = 'Lista' where IdHistoria = @IdHistoria;",
                new { fila.IdHistoria }, transaccion);

            await auditoria.RegistrarAsync(conexion, idUsuarioActual, "Regresar al Backlog", "HistoriasUsuario", fila.IdHistoria,
                $"Devolvió la historia {fila.Codigo} al Product Backlog desde un Sprint Cerrado.", transaccion);

            await transaccion.CommitAsync();
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    /// <summary>Replanifica una historia no terminada de un Sprint Cerrado a un Sprint Planificado del
    /// mismo proyecto. Las tareas no Finalizadas se mueven a la nueva fila; las Finalizadas se quedan
    /// en el Sprint viejo.</summary>
    private static async Task ReplanificarAsync(MySqlConnection conexion, int idSprintHistoria, int idSprintDestino, int idUsuarioActual, AuditoriaService auditoria)
    {
        using var transaccion = await conexion.BeginTransactionAsync();

        try
        {
            var vieja = await conexion.QueryFirstOrDefaultAsync<ReplanificarOrigenFila>(
                @"select sh.IdSprintHistoria, sh.IdHistoria, h.Codigo, h.Estimacion, s.Estado as EstadoSprint, s.IdProyecto
                  from SprintHistorias sh
                  join HistoriasUsuario h on h.IdHistoria = sh.IdHistoria
                  join Sprints s on s.IdSprint = sh.IdSprint
                  where sh.IdSprintHistoria = @IdSprintHistoria and sh.Resultado = 'Comprometida'
                  for update;",
                new { IdSprintHistoria = idSprintHistoria }, transaccion);

            if (vieja is null)
            {
                throw new InvalidOperationException("La historia ya no está comprometida en ese Sprint.");
            }

            if (vieja.EstadoSprint != "Cerrado")
            {
                throw new InvalidOperationException("Esta acción solo aplica a historias de Sprints ya Cerrados.");
            }

            var destino = await conexion.QueryFirstOrDefaultAsync<SprintParaComprometerFila>(
                "select IdProyecto, Estado, CapacidadPuntos from Sprints where IdSprint = @IdSprintDestino for update;",
                new { IdSprintDestino = idSprintDestino }, transaccion);

            if (destino is null)
            {
                throw new InvalidOperationException("El Sprint destino no existe.");
            }

            if (destino.Estado != "Planificado")
            {
                throw new InvalidOperationException("El Sprint destino debe estar Planificado.");
            }

            if (destino.IdProyecto != vieja.IdProyecto)
            {
                throw new InvalidOperationException("El Sprint destino debe ser del mismo proyecto.");
            }

            var puntosYaComprometidos = await conexion.ExecuteScalarAsync<int>(
                @"select coalesce(sum(h2.Estimacion), 0)
                  from SprintHistorias sh2
                  join HistoriasUsuario h2 on h2.IdHistoria = sh2.IdHistoria
                  where sh2.IdSprint = @IdSprintDestino and sh2.Resultado = 'Comprometida';",
                new { IdSprintDestino = idSprintDestino }, transaccion);

            var puntosHistoria = vieja.Estimacion ?? 0;
            if (puntosYaComprometidos + puntosHistoria > destino.CapacidadPuntos)
            {
                var disponibles = destino.CapacidadPuntos - puntosYaComprometidos;
                throw new InvalidOperationException(
                    $"El Sprint destino no tiene capacidad: disponibles {disponibles} pts, la historia {vieja.Codigo} necesita {puntosHistoria} pts.");
            }

            await conexion.ExecuteAsync(
                "update SprintHistorias set Resultado = 'Replanificada' where IdSprintHistoria = @IdSprintHistoria;",
                new { IdSprintHistoria = idSprintHistoria }, transaccion);

            await conexion.ExecuteAsync(
                "insert into SprintHistorias (IdSprint, IdHistoria, Resultado) values (@IdSprint, @IdHistoria, 'Comprometida');",
                new { IdSprint = idSprintDestino, vieja.IdHistoria }, transaccion);

            var idSprintHistoriaNueva = await conexion.ExecuteScalarAsync<int>("select last_insert_id();", transaction: transaccion);

            await conexion.ExecuteAsync(
                "update Tareas set IdSprintHistoria = @Nueva where IdSprintHistoria = @Vieja and Estado <> 'Finalizado';",
                new { Nueva = idSprintHistoriaNueva, Vieja = idSprintHistoria }, transaccion);

            await auditoria.RegistrarAsync(conexion, idUsuarioActual, "Replanificar historia", "Sprints", idSprintDestino,
                $"Replanificó la historia {vieja.Codigo} al nuevo Sprint; sus tareas no Finalizadas se movieron con ella.", transaccion);

            await transaccion.CommitAsync();
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    private static async Task<SprintHistoriaFila> BloquearSprintHistoriaComprometidaAsync(
        MySqlConnection conexion, MySqlTransaction transaccion, int idSprint, int idHistoria, string estadoSprintRequerido)
    {
        var estadoSprint = await conexion.ExecuteScalarAsync<string?>(
            "select Estado from Sprints where IdSprint = @IdSprint for update;",
            new { IdSprint = idSprint }, transaccion);

        if (estadoSprint is null)
        {
            throw new InvalidOperationException("El Sprint no existe.");
        }

        if (estadoSprint != estadoSprintRequerido)
        {
            throw new InvalidOperationException($"El Sprint debe estar {estadoSprintRequerido} para esta acción.");
        }

        var sprintHistoria = await conexion.QueryFirstOrDefaultAsync<SprintHistoriaFila>(
            @"select sh.IdSprintHistoria, h.Codigo
              from SprintHistorias sh
              join HistoriasUsuario h on h.IdHistoria = sh.IdHistoria
              where sh.IdSprint = @IdSprint and sh.IdHistoria = @IdHistoria and sh.Resultado = 'Comprometida'
              for update;",
            new { IdSprint = idSprint, IdHistoria = idHistoria }, transaccion);

        if (sprintHistoria is null)
        {
            throw new InvalidOperationException("La historia no está comprometida en este Sprint.");
        }

        return sprintHistoria;
    }

    private sealed class HistoriaCandidataFila
    {
        public int IdHistoria { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Como { get; set; } = string.Empty;
        public string Quiero { get; set; } = string.Empty;
        public string Para { get; set; } = string.Empty;
        public string Prioridad { get; set; } = string.Empty;
        public int OrdenPrioridad { get; set; }
        public int? Estimacion { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int CantidadCriterios { get; set; }
    }

    private sealed class SprintParaComprometerFila
    {
        public int IdProyecto { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int CapacidadPuntos { get; set; }
    }

    private sealed class HistoriaParaComprometerFila
    {
        public int IdHistoria { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public int IdProyecto { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int? Estimacion { get; set; }
        public int CantidadCriterios { get; set; }
        public bool YaComprometida { get; set; }
    }

    private sealed class SprintHistoriaFila
    {
        public int IdSprintHistoria { get; set; }
        public string Codigo { get; set; } = string.Empty;
    }

    private sealed class HistoriaNoTerminadaFila
    {
        public int IdSprintHistoria { get; set; }
        public int IdHistoria { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string EstadoSprint { get; set; } = string.Empty;
    }

    private sealed class ReplanificarOrigenFila
    {
        public int IdSprintHistoria { get; set; }
        public int IdHistoria { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public int? Estimacion { get; set; }
        public string EstadoSprint { get; set; } = string.Empty;
        public int IdProyecto { get; set; }
    }

    private sealed class HistoriaComprometidaClienteFila
    {
        public int IdHistoria { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Quiero { get; set; } = string.Empty;
        public int? Estimacion { get; set; }
        public string Prioridad { get; set; } = string.Empty;
        public string Resultado { get; set; } = string.Empty;
        public int CantidadTareas { get; set; }
        public bool TieneRechazo { get; set; }
    }
}
