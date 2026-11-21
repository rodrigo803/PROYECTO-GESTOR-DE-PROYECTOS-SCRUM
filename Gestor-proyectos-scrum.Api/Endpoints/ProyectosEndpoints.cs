using Dapper;
using Gestor_proyectos_scrum.Api.Data;
using Gestor_proyectos_scrum.Api.Models;
using Gestor_proyectos_scrum.Api.Services;
using MySqlConnector;

namespace Gestor_proyectos_scrum.Api.Endpoints;

/// <summary>HU-03: Gestión de Proyectos (migrado desde Gestor-proyectos-scrum/Data/ProyectoRepositorio.cs).</summary>
public static class ProyectosEndpoints
{
    private static readonly string[] RolesGestion = { "Administrador", "Product Owner" };
    private static readonly string[] RolesProyectoValidos = { "Product Owner", "Scrum Master", "Developer", "Stakeholder" };
    private static readonly string[] MotivosCierreValidos =
    {
        "Proyecto completado",
        "Cancelado por el cliente",
        "Reemplazado por otro proyecto"
    };

    private const string SqlMiembrosProyecto = @"
        SELECT pm.IdUsuario, CONCAT(u.Nombre, ' ', u.Apellidos) AS NombreCompleto, pm.RolProyecto, pm.FechaIngreso
        FROM ProyectoMiembros pm
        JOIN Usuarios u ON u.IdUsuario = pm.IdUsuario
        WHERE pm.IdProyecto = @IdProyecto
        ORDER BY CASE pm.RolProyecto
                     WHEN 'Product Owner' THEN 1
                     WHEN 'Scrum Master' THEN 2
                     WHEN 'Developer' THEN 3
                     ELSE 4
                 END, u.Nombre;";

    public static void MapProyectosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/proyectos").WithTags("HU-03 Proyectos");

        // GET /api/proyectos?buscar=&estado=&orden=
        group.MapGet("/", async (string? buscar, string? estado, string? orden, ConexionDbApi db) =>
        {
            const string sql = @"
                SELECT
                    p.IdProyecto, p.Codigo, p.Nombre, p.FechaInicio, p.FechaFin, p.Estado,
                    MAX(CASE WHEN pm.RolProyecto = 'Product Owner' THEN CONCAT(u.Nombre, ' ', u.Apellidos) END) AS ProductOwner,
                    MAX(CASE WHEN pm.RolProyecto = 'Scrum Master' THEN CONCAT(u.Nombre, ' ', u.Apellidos) END) AS ScrumMaster,
                    COUNT(pm.IdProyectoMiembro) AS CantidadMiembros,
                    GROUP_CONCAT(CONCAT(u.Nombre, ' ', u.Apellidos, ':', UPPER(LEFT(u.Nombre, 1)), UPPER(LEFT(u.Apellidos, 1))) ORDER BY pm.FechaIngreso SEPARATOR '|') AS MiembrosCsv,
                    av.Avance AS Avance
                FROM Proyectos p
                LEFT JOIN ProyectoMiembros pm ON pm.IdProyecto = p.IdProyecto
                LEFT JOIN Usuarios u ON u.IdUsuario = pm.IdUsuario
                LEFT JOIN (
                    SELECT IdProyecto,
                           CASE WHEN COALESCE(SUM(Estimacion), 0) = 0 THEN NULL
                                ELSE SUM(CASE WHEN Estado = 'Terminada' THEN Estimacion ELSE 0 END) * 100.0 / SUM(Estimacion)
                           END AS Avance
                    FROM HistoriasUsuario
                    GROUP BY IdProyecto
                ) av ON av.IdProyecto = p.IdProyecto
                WHERE (@Buscar IS NULL OR @Buscar = '' OR p.Nombre LIKE CONCAT('%', @Buscar, '%') OR p.Codigo LIKE CONCAT('%', @Buscar, '%'))
                  AND (@Estado IS NULL OR @Estado = '' OR p.Estado = @Estado)
                GROUP BY p.IdProyecto, p.Codigo, p.Nombre, p.FechaInicio, p.FechaFin, p.Estado, av.Avance
                ORDER BY
                    CASE WHEN @Orden = 'inicio' THEN p.FechaInicio END ASC,
                    CASE WHEN @Orden = 'avance' THEN COALESCE(av.Avance, 0) END DESC,
                    CASE WHEN @Orden = 'estado' THEN p.Estado END ASC,
                    p.Nombre ASC;";

            using var conexion = await db.CrearConexionAbiertaAsync();
            var filas = await conexion.QueryAsync<ProyectoListaFila>(sql, new { Buscar = buscar, Estado = estado, Orden = orden });
            return Results.Ok(filas.Select(MapearListaFila).ToList());
        })
        .RequireRoles();

        // GET /api/proyectos/metricas
        group.MapGet("/metricas", async (ConexionDbApi db) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();
            var metricas = await conexion.QuerySingleAsync<ProyectoMetricasDto>(@"
                SELECT
                    COUNT(*) AS Total,
                    COALESCE(SUM(CASE WHEN Estado = 'Activo' THEN 1 ELSE 0 END), 0) AS Activos,
                    COALESCE(SUM(CASE WHEN Estado = 'Planificado' THEN 1 ELSE 0 END), 0) AS Planificados,
                    COALESCE(SUM(CASE WHEN Estado = 'Cerrado' THEN 1 ELSE 0 END), 0) AS Cerrados
                FROM Proyectos;");
            return Results.Ok(metricas);
        })
        .RequireRoles();

        // GET /api/proyectos/usuarios-activos
        group.MapGet("/usuarios-activos", async (ConexionDbApi db) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();
            var usuarios = await conexion.QueryAsync<UsuarioOpcionDto>(@"
                SELECT IdUsuario, CONCAT(Nombre, ' ', Apellidos) AS NombreCompleto
                FROM Usuarios
                WHERE Activo = 1
                ORDER BY Nombre, Apellidos;");
            return Results.Ok(usuarios.ToList());
        })
        .RequireRoles();

        // GET /api/proyectos/{id:int}
        group.MapGet("/{id:int}", async (int id, ConexionDbApi db) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();
            var detalle = await ObtenerDetalleAsync(conexion, id);
            if (detalle is null)
            {
                return Results.NotFound(new ApiErrorResponse($"No se encontró el proyecto con ID {id}."));
            }

            return Results.Ok(detalle);
        })
        .RequireRoles();

        // GET /api/proyectos/{id:int}/editar
        group.MapGet("/{id:int}/editar", async (int id, ConexionDbApi db) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();

            var proyecto = await conexion.QueryFirstOrDefaultAsync<ProyectoRow>(
                "SELECT IdProyecto, Codigo, Nombre, Descripcion, FechaInicio, FechaFin, Estado FROM Proyectos WHERE IdProyecto = @IdProyecto;",
                new { IdProyecto = id });

            if (proyecto is null)
            {
                return Results.NotFound(new ApiErrorResponse($"No se encontró el proyecto con ID {id}."));
            }

            var miembros = (await conexion.QueryAsync<(int IdUsuario, string RolProyecto)>(
                "SELECT IdUsuario, RolProyecto FROM ProyectoMiembros WHERE IdProyecto = @IdProyecto;",
                new { IdProyecto = id })).ToList();

            return Results.Ok(new
            {
                proyecto.IdProyecto,
                proyecto.Codigo,
                proyecto.Nombre,
                proyecto.Descripcion,
                proyecto.FechaInicio,
                proyecto.FechaFin,
                proyecto.Estado,
                IdProductOwner = miembros.Where(m => m.RolProyecto == "Product Owner").Select(m => (int?)m.IdUsuario).FirstOrDefault(),
                IdScrumMaster = miembros.Where(m => m.RolProyecto == "Scrum Master").Select(m => (int?)m.IdUsuario).FirstOrDefault(),
                IdsDevelopers = miembros.Where(m => m.RolProyecto == "Developer").Select(m => m.IdUsuario).ToList()
            });
        })
        .RequireRoles();

        // POST /api/proyectos (Requiere rol: Product Owner, Administrador)
        group.MapPost("/", async (
            CrearActualizarProyectoRequest req,
            HttpContext httpContext,
            ConexionDbApi db,
            AuditoriaService auditoria) =>
        {
            var error = ValidarCamposProyecto(req, esEdicion: false);
            if (error is not null)
            {
                return Results.BadRequest(new ApiErrorResponse(error));
            }

            using var conexion = await db.CrearConexionAbiertaAsync();
            using var transaccion = await conexion.BeginTransactionAsync();

            try
            {
                var codigo = await GenerarSiguienteCodigoAsync(conexion, transaccion);

                await conexion.ExecuteAsync(@"
                    INSERT INTO Proyectos (Codigo, Nombre, Descripcion, FechaInicio, FechaFin, Estado)
                    VALUES (@Codigo, @Nombre, @Descripcion, @FechaInicio, @FechaFin, @Estado);",
                    new { Codigo = codigo, req.Nombre, req.Descripcion, req.FechaInicio, req.FechaFin, req.Estado },
                    transaccion);

                var idProyecto = await conexion.ExecuteScalarAsync<int>("SELECT LAST_INSERT_ID();", transaction: transaccion);

                await InsertarMiembrosAsync(conexion, transaccion, idProyecto, req);

                var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);
                await auditoria.RegistrarAsync(conexion, idActor, "Crear", "Proyectos", idProyecto,
                    $"Creó el proyecto {codigo} - {req.Nombre}.", transaccion);

                await transaccion.CommitAsync();
                return Results.Created($"/api/proyectos/{idProyecto}", new ApiMessageResponse($"Proyecto \"{req.Nombre}\" creado correctamente.", idProyecto));
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                await transaccion.RollbackAsync();
                return Results.Conflict(new ApiErrorResponse("Ya existe un proyecto con ese nombre."));
            }
        })
        .RequireRoles(RolesGestion);

        // PUT /api/proyectos/{id:int} (Requiere rol: Product Owner, Administrador)
        group.MapPut("/{id:int}", async (
            int id,
            CrearActualizarProyectoRequest req,
            HttpContext httpContext,
            ConexionDbApi db,
            AuditoriaService auditoria) =>
        {
            var error = ValidarCamposProyecto(req, esEdicion: true);
            if (error is not null)
            {
                return Results.BadRequest(new ApiErrorResponse(error));
            }

            using var conexion = await db.CrearConexionAbiertaAsync();
            using var transaccion = await conexion.BeginTransactionAsync();

            var proyectoActual = await conexion.QueryFirstOrDefaultAsync<ProyectoRow>(
                "SELECT IdProyecto, Codigo, Estado FROM Proyectos WHERE IdProyecto = @IdProyecto FOR UPDATE;",
                new { IdProyecto = id }, transaccion);

            if (proyectoActual is null)
            {
                await transaccion.RollbackAsync();
                return Results.NotFound(new ApiErrorResponse($"No se encontró el proyecto con ID {id}."));
            }

            if (string.Equals(proyectoActual.Estado, "Cerrado", StringComparison.OrdinalIgnoreCase))
            {
                await transaccion.RollbackAsync();
                return Results.BadRequest(new ApiErrorResponse("No se puede editar un proyecto cerrado."));
            }

            try
            {
                await conexion.ExecuteAsync(@"
                    UPDATE Proyectos
                    SET Nombre = @Nombre, Descripcion = @Descripcion, FechaInicio = @FechaInicio,
                        FechaFin = @FechaFin, Estado = @Estado
                    WHERE IdProyecto = @IdProyecto;",
                    new { req.Nombre, req.Descripcion, req.FechaInicio, req.FechaFin, req.Estado, IdProyecto = id },
                    transaccion);

                var actuales = (await conexion.QueryAsync<(int IdUsuario, string RolProyecto)>(
                    "SELECT IdUsuario, RolProyecto FROM ProyectoMiembros WHERE IdProyecto = @IdProyecto;",
                    new { IdProyecto = id }, transaccion))
                    .ToDictionary(m => m.IdUsuario, m => m.RolProyecto);

                var deseados = ConstruirMiembrosDeseados(req);

                foreach (var idUsuario in actuales.Keys.Except(deseados.Keys).ToList())
                {
                    await conexion.ExecuteAsync(
                        "DELETE FROM ProyectoMiembros WHERE IdProyecto = @IdProyecto AND IdUsuario = @IdUsuario;",
                        new { IdProyecto = id, IdUsuario = idUsuario }, transaccion);
                }

                foreach (var par in deseados.Where(d => !actuales.ContainsKey(d.Key)))
                {
                    await conexion.ExecuteAsync(
                        "INSERT INTO ProyectoMiembros (IdProyecto, IdUsuario, RolProyecto) VALUES (@IdProyecto, @IdUsuario, @RolProyecto);",
                        new { IdProyecto = id, IdUsuario = par.Key, RolProyecto = par.Value }, transaccion);
                }

                foreach (var par in deseados.Where(d => actuales.ContainsKey(d.Key) && actuales[d.Key] != d.Value))
                {
                    await conexion.ExecuteAsync(
                        "UPDATE ProyectoMiembros SET RolProyecto = @RolProyecto WHERE IdProyecto = @IdProyecto AND IdUsuario = @IdUsuario;",
                        new { IdProyecto = id, IdUsuario = par.Key, RolProyecto = par.Value }, transaccion);
                }

                var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);
                await auditoria.RegistrarAsync(conexion, idActor, "Editar", "Proyectos", id,
                    $"Actualizó el proyecto {proyectoActual.Codigo} - {req.Nombre}.", transaccion);

                await transaccion.CommitAsync();
                return Results.Ok(new ApiMessageResponse($"Proyecto \"{req.Nombre}\" actualizado correctamente.", id));
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                await transaccion.RollbackAsync();
                return Results.Conflict(new ApiErrorResponse("Ya existe un proyecto con ese nombre."));
            }
        })
        .RequireRoles(RolesGestion);

        // POST /api/proyectos/{id:int}/miembros (Requiere rol: Product Owner, Administrador)
        group.MapPost("/{id:int}/miembros", async (
            int id,
            AgregarMiembroRequest req,
            HttpContext httpContext,
            ConexionDbApi db,
            AuditoriaService auditoria) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();

            var estado = await conexion.QueryFirstOrDefaultAsync<string>(
                "SELECT Estado FROM Proyectos WHERE IdProyecto = @IdProyecto;", new { IdProyecto = id });
            if (estado is null)
            {
                return Results.NotFound(new ApiErrorResponse($"No se encontró el proyecto con ID {id}."));
            }

            if (string.Equals(estado, "Cerrado", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new ApiErrorResponse("No se puede modificar un proyecto cerrado."));
            }

            if (!RolesProyectoValidos.Contains(req.RolProyecto))
            {
                return Results.BadRequest(new ApiErrorResponse("Rol no válido."));
            }

            var yaEsMiembro = await conexion.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM ProyectoMiembros WHERE IdProyecto = @IdProyecto AND IdUsuario = @IdUsuario;",
                new { IdProyecto = id, req.IdUsuario });
            if (yaEsMiembro > 0)
            {
                return Results.Conflict(new ApiErrorResponse("El usuario ya es miembro del proyecto."));
            }

            var usuario = await conexion.QueryFirstOrDefaultAsync<UsuarioOpcionDto>(
                "SELECT IdUsuario, CONCAT(Nombre, ' ', Apellidos) AS NombreCompleto FROM Usuarios WHERE IdUsuario = @IdUsuario AND Activo = 1;",
                new { req.IdUsuario });
            if (usuario is null)
            {
                return Results.BadRequest(new ApiErrorResponse("El usuario seleccionado no es válido."));
            }

            using var transaccion = await conexion.BeginTransactionAsync();

            await conexion.ExecuteAsync(
                "INSERT INTO ProyectoMiembros (IdProyecto, IdUsuario, RolProyecto) VALUES (@IdProyecto, @IdUsuario, @RolProyecto);",
                new { IdProyecto = id, req.IdUsuario, req.RolProyecto }, transaccion);

            var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);
            await auditoria.RegistrarAsync(conexion, idActor, "Agregar Miembro", "Proyectos", id,
                $"Agregó a {usuario.NombreCompleto} como {req.RolProyecto}.", transaccion);

            await transaccion.CommitAsync();
            return Results.Ok(new ApiMessageResponse("Miembro agregado correctamente.", id));
        })
        .RequireRoles(RolesGestion);

        // PUT /api/proyectos/{id:int}/miembros/{idUsuario:int} (Requiere rol: Product Owner, Administrador)
        group.MapPut("/{id:int}/miembros/{idUsuario:int}", async (
            int id,
            int idUsuario,
            CambiarRolMiembroRequest req,
            HttpContext httpContext,
            ConexionDbApi db,
            AuditoriaService auditoria) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();

            var estado = await conexion.QueryFirstOrDefaultAsync<string>(
                "SELECT Estado FROM Proyectos WHERE IdProyecto = @IdProyecto;", new { IdProyecto = id });
            if (estado is null)
            {
                return Results.NotFound(new ApiErrorResponse($"No se encontró el proyecto con ID {id}."));
            }

            if (string.Equals(estado, "Cerrado", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new ApiErrorResponse("No se puede modificar un proyecto cerrado."));
            }

            if (!RolesProyectoValidos.Contains(req.NuevoRol))
            {
                return Results.BadRequest(new ApiErrorResponse("Rol no válido."));
            }

            var miembros = (await conexion.QueryAsync<ProyectoMiembroDto>(SqlMiembrosProyecto, new { IdProyecto = id })).ToList();
            var miembro = miembros.FirstOrDefault(m => m.IdUsuario == idUsuario);
            if (miembro is null)
            {
                return Results.BadRequest(new ApiErrorResponse("El usuario no pertenece al proyecto."));
            }

            if (req.NuevoRol != miembro.RolProyecto && EsUnicoResponsableCritico(miembros, miembro) && estado is "Activo" or "En pausa")
            {
                return Results.Conflict(new ApiErrorResponse(
                    $"No se puede cambiar de rol al único {miembro.RolProyecto} mientras el proyecto está {estado}."));
            }

            using var transaccion = await conexion.BeginTransactionAsync();

            await conexion.ExecuteAsync(
                "UPDATE ProyectoMiembros SET RolProyecto = @RolProyecto WHERE IdProyecto = @IdProyecto AND IdUsuario = @IdUsuario;",
                new { RolProyecto = req.NuevoRol, IdProyecto = id, IdUsuario = idUsuario }, transaccion);

            var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);
            await auditoria.RegistrarAsync(conexion, idActor, "Cambiar Rol Miembro", "Proyectos", id,
                $"Cambió el rol de {miembro.NombreCompleto} de {miembro.RolProyecto} a {req.NuevoRol}.", transaccion);

            await transaccion.CommitAsync();
            return Results.Ok(new ApiMessageResponse("Rol actualizado correctamente.", id));
        })
        .RequireRoles(RolesGestion);

        // DELETE /api/proyectos/{id:int}/miembros/{idUsuario:int} (Requiere rol: Product Owner, Administrador)
        group.MapDelete("/{id:int}/miembros/{idUsuario:int}", async (
            int id,
            int idUsuario,
            HttpContext httpContext,
            ConexionDbApi db,
            AuditoriaService auditoria) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();

            var estado = await conexion.QueryFirstOrDefaultAsync<string>(
                "SELECT Estado FROM Proyectos WHERE IdProyecto = @IdProyecto;", new { IdProyecto = id });
            if (estado is null)
            {
                return Results.NotFound(new ApiErrorResponse($"No se encontró el proyecto con ID {id}."));
            }

            if (string.Equals(estado, "Cerrado", StringComparison.OrdinalIgnoreCase))
            {
                return Results.BadRequest(new ApiErrorResponse("No se puede modificar un proyecto cerrado."));
            }

            var miembros = (await conexion.QueryAsync<ProyectoMiembroDto>(SqlMiembrosProyecto, new { IdProyecto = id })).ToList();
            var miembro = miembros.FirstOrDefault(m => m.IdUsuario == idUsuario);
            if (miembro is null)
            {
                return Results.BadRequest(new ApiErrorResponse("El usuario no pertenece al proyecto."));
            }

            if (EsUnicoResponsableCritico(miembros, miembro) && estado is "Activo" or "En pausa")
            {
                return Results.Conflict(new ApiErrorResponse(
                    $"No se puede quitar al único {miembro.RolProyecto} mientras el proyecto está {estado}."));
            }

            using var transaccion = await conexion.BeginTransactionAsync();

            await conexion.ExecuteAsync(
                "DELETE FROM ProyectoMiembros WHERE IdProyecto = @IdProyecto AND IdUsuario = @IdUsuario;",
                new { IdProyecto = id, IdUsuario = idUsuario }, transaccion);

            var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);
            await auditoria.RegistrarAsync(conexion, idActor, "Quitar Miembro", "Proyectos", id,
                $"Quitó a {miembro.NombreCompleto} del proyecto.", transaccion);

            await transaccion.CommitAsync();
            return Results.Ok(new ApiMessageResponse("Miembro eliminado del proyecto.", id));
        })
        .RequireRoles(RolesGestion);

        // GET /api/proyectos/{id:int}/cierre-resumen (Requiere rol: Product Owner, Administrador)
        group.MapGet("/{id:int}/cierre-resumen", async (int id, ConexionDbApi db) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();
            var resumen = await ObtenerResumenCierreAsync(conexion, id);
            if (resumen is null)
            {
                return Results.NotFound(new ApiErrorResponse($"No se encontró el proyecto con ID {id}."));
            }

            return Results.Ok(resumen);
        })
        .RequireRoles(RolesGestion);

        // POST /api/proyectos/{id:int}/cerrar (Requiere rol: Product Owner, Administrador)
        group.MapPost("/{id:int}/cerrar", async (
            int id,
            CerrarProyectoRequest req,
            HttpContext httpContext,
            ConexionDbApi db,
            AuditoriaService auditoria) =>
        {
            if (string.IsNullOrWhiteSpace(req.Motivo) || !MotivosCierreValidos.Contains(req.Motivo))
            {
                return Results.BadRequest(new ApiErrorResponse("Debes seleccionar un motivo de cierre válido."));
            }

            if (!string.IsNullOrWhiteSpace(req.Observaciones) && req.Observaciones.Trim().Length > 250)
            {
                return Results.BadRequest(new ApiErrorResponse("Las observaciones no pueden superar los 250 caracteres."));
            }

            using var conexion = await db.CrearConexionAbiertaAsync();
            using var transaccion = await conexion.BeginTransactionAsync();

            var proyecto = await conexion.QueryFirstOrDefaultAsync<ProyectoRow>(
                "SELECT IdProyecto, Codigo, Nombre, Estado FROM Proyectos WHERE IdProyecto = @IdProyecto FOR UPDATE;",
                new { IdProyecto = id }, transaccion);

            if (proyecto is null)
            {
                await transaccion.RollbackAsync();
                return Results.NotFound(new ApiErrorResponse($"No se encontró el proyecto con ID {id}."));
            }

            if (string.Equals(proyecto.Estado, "Cerrado", StringComparison.OrdinalIgnoreCase))
            {
                await transaccion.RollbackAsync();
                return Results.BadRequest(new ApiErrorResponse("Este proyecto ya está cerrado."));
            }

            var sprintActivoNombre = await conexion.QueryFirstOrDefaultAsync<string>(
                "SELECT Nombre FROM Sprints WHERE IdProyecto = @IdProyecto AND Estado = 'Activo' LIMIT 1;",
                new { IdProyecto = id }, transaccion);

            if (!string.IsNullOrEmpty(sprintActivoNombre))
            {
                await transaccion.RollbackAsync();
                return Results.Conflict(new ApiErrorResponse(
                    $"No se puede cerrar el proyecto: el Sprint \"{sprintActivoNombre}\" está Activo."));
            }

            var motivoCompleto = string.IsNullOrWhiteSpace(req.Observaciones)
                ? req.Motivo!
                : $"{req.Motivo}. {req.Observaciones.Trim()}";

            await conexion.ExecuteAsync(@"
                UPDATE Proyectos
                SET Estado = 'Cerrado', FechaCierre = CURDATE(), MotivoCierre = @MotivoCierre
                WHERE IdProyecto = @IdProyecto;",
                new { MotivoCierre = motivoCompleto, IdProyecto = id }, transaccion);

            var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);
            await auditoria.RegistrarAsync(conexion, idActor, "Cerrar", "Proyectos", id,
                $"Cerró el proyecto. Motivo: {motivoCompleto}", transaccion);

            await transaccion.CommitAsync();
            return Results.Ok(new ApiMessageResponse($"Proyecto \"{proyecto.Nombre}\" cerrado correctamente.", id));
        })
        .RequireRoles(RolesGestion);

        // GET /api/proyectos/cliente (proyectos donde el usuario autenticado es miembro)
        group.MapGet("/cliente", async (HttpContext httpContext, ConexionDbApi db) =>
        {
            var idUsuario = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);

            const string sql = @"
                SELECT
                    p.IdProyecto, p.Codigo, p.Nombre, p.Descripcion, p.Estado, p.FechaInicio, p.FechaFin,
                    yo.RolProyecto AS MiRol,
                    MAX(CASE WHEN pm.RolProyecto = 'Product Owner' THEN CONCAT(u.Nombre, ' ', u.Apellidos) END) AS ProductOwner,
                    MAX(CASE WHEN pm.RolProyecto = 'Scrum Master' THEN CONCAT(u.Nombre, ' ', u.Apellidos) END) AS ScrumMaster,
                    (SELECT CASE WHEN COALESCE(SUM(h.Estimacion), 0) = 0 THEN NULL
                                 ELSE SUM(CASE WHEN h.Estado = 'Terminada' THEN h.Estimacion ELSE 0 END) * 100.0 / SUM(h.Estimacion)
                            END
                     FROM HistoriasUsuario h WHERE h.IdProyecto = p.IdProyecto) AS Avance,
                    (SELECT s.Nombre FROM Sprints s WHERE s.IdProyecto = p.IdProyecto AND s.Estado = 'Activo' LIMIT 1) AS SprintActivoNombre,
                    GROUP_CONCAT(CONCAT(u.Nombre, ' ', u.Apellidos, ':', UPPER(LEFT(u.Nombre, 1)), UPPER(LEFT(u.Apellidos, 1))) ORDER BY pm.FechaIngreso SEPARATOR '|') AS MiembrosCsv
                FROM Proyectos p
                JOIN ProyectoMiembros yo ON yo.IdProyecto = p.IdProyecto AND yo.IdUsuario = @IdUsuario
                LEFT JOIN ProyectoMiembros pm ON pm.IdProyecto = p.IdProyecto
                LEFT JOIN Usuarios u ON u.IdUsuario = pm.IdUsuario
                WHERE p.Estado <> 'Cerrado'
                GROUP BY p.IdProyecto, p.Codigo, p.Nombre, p.Descripcion, p.Estado, p.FechaInicio, p.FechaFin, yo.RolProyecto
                ORDER BY p.FechaInicio DESC;";

            using var conexion = await db.CrearConexionAbiertaAsync();
            var filas = await conexion.QueryAsync<ProyectoClienteFila>(sql, new { IdUsuario = idUsuario });

            var resultado = filas.Select(f => new ProyectoClienteItemDto
            {
                IdProyecto = f.IdProyecto,
                Codigo = f.Codigo,
                Nombre = f.Nombre,
                Descripcion = f.Descripcion,
                Estado = f.Estado,
                FechaInicio = f.FechaInicio,
                FechaFin = f.FechaFin,
                MiRol = f.MiRol,
                ProductOwner = f.ProductOwner,
                ScrumMaster = f.ScrumMaster,
                Avance = f.Avance,
                SprintActivoNombre = f.SprintActivoNombre,
                Miembros = MapearMiembrosCsv(f.MiembrosCsv)
            }).ToList();

            return Results.Ok(resultado);
        })
        .RequireRoles();

        // GET /api/proyectos/{id:int}/cliente (valida que el usuario autenticado sea miembro del proyecto)
        group.MapGet("/{id:int}/cliente", async (int id, HttpContext httpContext, ConexionDbApi db) =>
        {
            var idUsuario = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);
            using var conexion = await db.CrearConexionAbiertaAsync();

            var proyecto = await conexion.QueryFirstOrDefaultAsync<ProyectoRow>(
                "SELECT IdProyecto, Codigo, Nombre, Descripcion, FechaInicio, FechaFin, Estado FROM Proyectos WHERE IdProyecto = @IdProyecto;",
                new { IdProyecto = id });

            if (proyecto is null)
            {
                return Results.NotFound(new ApiErrorResponse($"No se encontró el proyecto con ID {id}."));
            }

            var miRol = await conexion.QueryFirstOrDefaultAsync<string>(
                "SELECT RolProyecto FROM ProyectoMiembros WHERE IdProyecto = @IdProyecto AND IdUsuario = @IdUsuario;",
                new { IdProyecto = id, IdUsuario = idUsuario });

            if (miRol is null)
            {
                return Results.Json(
                    new ApiErrorResponse("No tienes acceso a ese proyecto."),
                    statusCode: StatusCodes.Status403Forbidden);
            }

            var miembros = (await conexion.QueryAsync<ProyectoMiembroDto>(SqlMiembrosProyecto, new { IdProyecto = id })).ToList();

            var sprints = (await conexion.QueryAsync<SprintResumenDto>(
                "SELECT IdSprint, Nombre, FechaInicio, FechaFin, Estado FROM Sprints WHERE IdProyecto = @IdProyecto ORDER BY FechaInicio;",
                new { IdProyecto = id })).ToList();

            var avance = await conexion.QueryFirstOrDefaultAsync<decimal?>(@"
                SELECT CASE WHEN COALESCE(SUM(Estimacion), 0) = 0 THEN NULL
                            ELSE SUM(CASE WHEN Estado = 'Terminada' THEN Estimacion ELSE 0 END) * 100.0 / SUM(Estimacion)
                       END
                FROM HistoriasUsuario WHERE IdProyecto = @IdProyecto;",
                new { IdProyecto = id });

            return Results.Ok(new ProyectoClienteDetalleDto
            {
                IdProyecto = proyecto.IdProyecto,
                Codigo = proyecto.Codigo,
                Nombre = proyecto.Nombre,
                Descripcion = proyecto.Descripcion,
                FechaInicio = proyecto.FechaInicio,
                FechaFin = proyecto.FechaFin,
                Estado = proyecto.Estado,
                ProductOwner = miembros.Where(m => m.RolProyecto == "Product Owner").Select(m => m.NombreCompleto).FirstOrDefault(),
                ScrumMaster = miembros.Where(m => m.RolProyecto == "Scrum Master").Select(m => m.NombreCompleto).FirstOrDefault(),
                Avance = avance,
                MiRol = miRol,
                IdUsuarioActual = idUsuario,
                Sprints = sprints,
                Miembros = miembros
            });
        })
        .RequireRoles();
    }

    private static async Task<ProyectoDetalleDto?> ObtenerDetalleAsync(MySqlConnection conexion, int idProyecto)
    {
        var proyecto = await conexion.QueryFirstOrDefaultAsync<ProyectoRow>(
            "SELECT IdProyecto, Codigo, Nombre, Descripcion, FechaInicio, FechaFin, Estado FROM Proyectos WHERE IdProyecto = @IdProyecto;",
            new { IdProyecto = idProyecto });

        if (proyecto is null)
        {
            return null;
        }

        var miembros = (await conexion.QueryAsync<ProyectoMiembroDto>(SqlMiembrosProyecto, new { IdProyecto = idProyecto })).ToList();

        var sprints = (await conexion.QueryAsync<SprintResumenDto>(
            "SELECT IdSprint, Nombre, FechaInicio, FechaFin, Estado FROM Sprints WHERE IdProyecto = @IdProyecto ORDER BY FechaInicio;",
            new { IdProyecto = idProyecto })).ToList();

        var avanceInfo = await conexion.QuerySingleAsync<AvanceInfoRow>(@"
            SELECT
                CASE WHEN COALESCE(SUM(Estimacion), 0) = 0 THEN NULL
                     ELSE SUM(CASE WHEN Estado = 'Terminada' THEN Estimacion ELSE 0 END) * 100.0 / SUM(Estimacion)
                END AS Avance,
                COUNT(*) AS CantidadHistorias
            FROM HistoriasUsuario WHERE IdProyecto = @IdProyecto;",
            new { IdProyecto = idProyecto });

        var historial = (await conexion.QueryAsync<AuditoriaDto>(@"
            SELECT CONCAT(u.Nombre, ' ', u.Apellidos) AS Usuario, a.Accion, a.Detalle, a.Fecha
            FROM Auditoria a
            JOIN Usuarios u ON u.IdUsuario = a.IdUsuario
            WHERE a.Entidad = 'Proyectos' AND a.IdRegistro = @IdProyecto
            ORDER BY a.Fecha DESC;",
            new { IdProyecto = idProyecto })).ToList();

        var usuariosActivos = await conexion.QueryAsync<UsuarioOpcionDto>(@"
            SELECT IdUsuario, CONCAT(Nombre, ' ', Apellidos) AS NombreCompleto
            FROM Usuarios WHERE Activo = 1 ORDER BY Nombre, Apellidos;");

        var idsMiembros = miembros.Select(m => m.IdUsuario).ToHashSet();
        var disponibles = usuariosActivos.Where(u => !idsMiembros.Contains(u.IdUsuario)).ToList();

        return new ProyectoDetalleDto
        {
            IdProyecto = proyecto.IdProyecto,
            Codigo = proyecto.Codigo,
            Nombre = proyecto.Nombre,
            Descripcion = proyecto.Descripcion,
            FechaInicio = proyecto.FechaInicio,
            FechaFin = proyecto.FechaFin,
            Estado = proyecto.Estado,
            Avance = avanceInfo.Avance,
            CantidadHistorias = avanceInfo.CantidadHistorias,
            CantidadSprints = sprints.Count,
            CantidadMiembros = miembros.Count,
            Sprints = sprints,
            Miembros = miembros,
            Historial = historial,
            UsuariosDisponibles = disponibles
        };
    }

    private static async Task<ProyectoCierreResumenDto?> ObtenerResumenCierreAsync(MySqlConnection conexion, int idProyecto)
    {
        var proyecto = await conexion.QueryFirstOrDefaultAsync<ProyectoRow>(
            "SELECT IdProyecto, Codigo, Nombre, Descripcion, FechaInicio, FechaFin, Estado FROM Proyectos WHERE IdProyecto = @IdProyecto;",
            new { IdProyecto = idProyecto });

        if (proyecto is null)
        {
            return null;
        }

        var sprints = await conexion.QuerySingleAsync<SprintsResumenRow>(
            "SELECT COUNT(*) AS Total, COALESCE(SUM(CASE WHEN Estado = 'Cerrado' THEN 1 ELSE 0 END), 0) AS Cerrados FROM Sprints WHERE IdProyecto = @IdProyecto;",
            new { IdProyecto = idProyecto });

        var sprintActivo = await conexion.QueryFirstOrDefaultAsync<string>(
            "SELECT Nombre FROM Sprints WHERE IdProyecto = @IdProyecto AND Estado = 'Activo' LIMIT 1;",
            new { IdProyecto = idProyecto });

        var historiasPendientes = await conexion.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM HistoriasUsuario WHERE IdProyecto = @IdProyecto AND Estado <> 'Terminada';",
            new { IdProyecto = idProyecto });

        var impedimentosAbiertos = await conexion.ExecuteScalarAsync<int>(@"
            SELECT COUNT(*) FROM Impedimentos i
            JOIN Sprints s ON s.IdSprint = i.IdSprint
            WHERE s.IdProyecto = @IdProyecto AND i.Estado <> 'Resuelto';",
            new { IdProyecto = idProyecto });

        var cantidadMiembros = await conexion.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM ProyectoMiembros WHERE IdProyecto = @IdProyecto;",
            new { IdProyecto = idProyecto });

        return new ProyectoCierreResumenDto
        {
            IdProyecto = proyecto.IdProyecto,
            Codigo = proyecto.Codigo,
            Nombre = proyecto.Nombre,
            Estado = proyecto.Estado,
            FechaInicio = proyecto.FechaInicio,
            FechaFin = proyecto.FechaFin,
            CantidadSprints = sprints.Total,
            SprintsCerrados = sprints.Cerrados,
            SprintActivoNombre = sprintActivo,
            HistoriasPendientes = historiasPendientes,
            ImpedimentosAbiertos = impedimentosAbiertos,
            CantidadMiembros = cantidadMiembros
        };
    }

    private static string? ValidarCamposProyecto(CrearActualizarProyectoRequest req, bool esEdicion)
    {
        if (string.IsNullOrWhiteSpace(req.Nombre) || req.Nombre.Trim().Length > 100)
        {
            return "El nombre del proyecto es obligatorio y no debe superar los 100 caracteres.";
        }

        if (string.IsNullOrWhiteSpace(req.Descripcion) || req.Descripcion.Trim().Length > 500)
        {
            return "La descripción es obligatoria y no debe superar los 500 caracteres.";
        }

        if (req.FechaFin.Date <= req.FechaInicio.Date)
        {
            return "La fecha de finalización debe ser posterior a la fecha de inicio.";
        }

        var estadosPermitidos = esEdicion
            ? new[] { "Planificado", "Activo", "En pausa" }
            : new[] { "Planificado", "Activo" };

        if (!estadosPermitidos.Contains(req.Estado))
        {
            return "Estado no válido.";
        }

        if (req.Estado == "Activo" && (req.IdProductOwner is null || req.IdScrumMaster is null))
        {
            return "Un proyecto no puede pasar a \"Activo\" sin Product Owner y Scrum Master asignados.";
        }

        var idsAsignados = new List<int>();
        if (req.IdProductOwner.HasValue) idsAsignados.Add(req.IdProductOwner.Value);
        if (req.IdScrumMaster.HasValue) idsAsignados.Add(req.IdScrumMaster.Value);
        idsAsignados.AddRange(req.IdsDevelopers);

        if (idsAsignados.Count != idsAsignados.Distinct().Count())
        {
            return "Un mismo usuario no puede tener más de un rol en el proyecto.";
        }

        return null;
    }

    private static bool EsUnicoResponsableCritico(IReadOnlyList<ProyectoMiembroDto> miembros, ProyectoMiembroDto miembro)
    {
        if (miembro.RolProyecto is not ("Product Owner" or "Scrum Master"))
        {
            return false;
        }

        return miembros.Count(m => m.RolProyecto == miembro.RolProyecto) == 1;
    }

    private static Dictionary<int, string> ConstruirMiembrosDeseados(CrearActualizarProyectoRequest req)
    {
        var deseados = new Dictionary<int, string>();

        if (req.IdProductOwner.HasValue) deseados[req.IdProductOwner.Value] = "Product Owner";
        if (req.IdScrumMaster.HasValue) deseados[req.IdScrumMaster.Value] = "Scrum Master";

        foreach (var idDeveloper in req.IdsDevelopers.Distinct())
        {
            deseados[idDeveloper] = "Developer";
        }

        return deseados;
    }

    private static async Task InsertarMiembrosAsync(MySqlConnection conexion, MySqlTransaction transaccion, int idProyecto, CrearActualizarProyectoRequest req)
    {
        const string sql = @"
            INSERT INTO ProyectoMiembros (IdProyecto, IdUsuario, RolProyecto)
            VALUES (@IdProyecto, @IdUsuario, @RolProyecto);";

        foreach (var par in ConstruirMiembrosDeseados(req))
        {
            await conexion.ExecuteAsync(sql, new { IdProyecto = idProyecto, IdUsuario = par.Key, RolProyecto = par.Value }, transaccion);
        }
    }

    private static async Task<string> GenerarSiguienteCodigoAsync(MySqlConnection conexion, MySqlTransaction transaccion)
    {
        const string sql = @"
            SELECT COALESCE(MAX(CAST(SUBSTRING(Codigo, 5) AS UNSIGNED)), 0) + 1
            FROM Proyectos
            WHERE Codigo LIKE 'PRY-%';";

        var siguienteNumero = await conexion.ExecuteScalarAsync<int>(sql, transaction: transaccion);
        return $"PRY-{siguienteNumero:000}";
    }

    private static ProyectoListaItemDto MapearListaFila(ProyectoListaFila fila) => new()
    {
        IdProyecto = fila.IdProyecto,
        Codigo = fila.Codigo,
        Nombre = fila.Nombre,
        FechaInicio = fila.FechaInicio,
        FechaFin = fila.FechaFin,
        Estado = fila.Estado,
        ProductOwner = fila.ProductOwner,
        ScrumMaster = fila.ScrumMaster,
        CantidadMiembros = fila.CantidadMiembros,
        Miembros = MapearMiembrosCsv(fila.MiembrosCsv),
        Avance = fila.Avance
    };

    /// <summary>Convierte el "Nombre Apellido:AB|Nombre Apellido:CD" de GROUP_CONCAT en una lista de avatares.</summary>
    private static List<MiembroAvatarDto> MapearMiembrosCsv(string? miembrosCsv)
    {
        if (string.IsNullOrWhiteSpace(miembrosCsv))
        {
            return new List<MiembroAvatarDto>();
        }

        return miembrosCsv
            .Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(entrada =>
            {
                var partes = entrada.Split(':', 2);
                return new MiembroAvatarDto
                {
                    NombreCompleto = partes[0],
                    Iniciales = partes.Length > 1 ? partes[1] : string.Empty
                };
            })
            .ToList();
    }

    private sealed class ProyectoRow
    {
        public int IdProyecto { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string Estado { get; set; } = string.Empty;
    }

    private sealed class AvanceInfoRow
    {
        public decimal? Avance { get; set; }
        public int CantidadHistorias { get; set; }
    }

    private sealed class SprintsResumenRow
    {
        public int Total { get; set; }
        public int Cerrados { get; set; }
    }

    private sealed class ProyectoListaFila
    {
        public int IdProyecto { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? ProductOwner { get; set; }
        public string? ScrumMaster { get; set; }
        public int CantidadMiembros { get; set; }
        public string? MiembrosCsv { get; set; }
        public decimal? Avance { get; set; }
    }

    private sealed class ProyectoClienteFila
    {
        public int IdProyecto { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string MiRol { get; set; } = string.Empty;
        public string? ProductOwner { get; set; }
        public string? ScrumMaster { get; set; }
        public decimal? Avance { get; set; }
        public string? SprintActivoNombre { get; set; }
        public string? MiembrosCsv { get; set; }
    }
}
