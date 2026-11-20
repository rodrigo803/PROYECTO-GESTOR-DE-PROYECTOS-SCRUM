using System.Net.Mail;
using Dapper;
using Gestor_proyectos_scrum.Api.Data;
using Gestor_proyectos_scrum.Api.Models;
using Gestor_proyectos_scrum.Api.Services;

namespace Gestor_proyectos_scrum.Api.Endpoints;

public static class UsuariosEndpoints
{
    public static void MapUsuariosEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/usuarios").WithTags("HU-02 Usuarios");

        // GET /api/usuarios/roles
        group.MapGet("/roles", async (ConexionDbApi db) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();
            var roles = await conexion.QueryAsync<RolDto>(
                "SELECT IdRol, Nombre FROM Roles ORDER BY IdRol;");
            return Results.Ok(roles);
        });

        // GET /api/usuarios?buscar=&idRol=&activo=
        group.MapGet("/", async (string? buscar, int? idRol, bool? activo, ConexionDbApi db) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();

            var sql = @"
                SELECT
                    u.IdUsuario,
                    u.Nombre,
                    u.Apellidos,
                    u.Correo,
                    u.IdRol,
                    r.Nombre AS RolNombre,
                    u.Activo,
                    u.FechaRegistro,
                    u.UltimoAcceso,
                    COALESCE(t.TareasActivas, 0) AS TareasAsignadas,
                    COALESCE(t.TareasActivas, 0) AS TareasActivas,
                    COALESCE(t.HorasActivas, 0) AS HorasAsignadas,
                    COALESCE(t.HorasActivas, 0) AS HorasEstimadasAsignadas,
                    COALESCE(pm.ProyectosParticipa, 0) AS ProyectosParticipa
                FROM Usuarios u
                INNER JOIN Roles r ON r.IdRol = u.IdRol
                LEFT JOIN (
                    SELECT
                        IdResponsable,
                        SUM(CASE WHEN Estado <> 'Finalizado' THEN 1 ELSE 0 END) AS TareasActivas,
                        COALESCE(SUM(CASE WHEN Estado <> 'Finalizado' THEN HorasEstimadas ELSE 0 END), 0) AS HorasActivas
                    FROM Tareas
                    WHERE IdResponsable IS NOT NULL
                    GROUP BY IdResponsable
                ) t ON t.IdResponsable = u.IdUsuario
                LEFT JOIN (
                    SELECT IdUsuario, COUNT(*) AS ProyectosParticipa
                    FROM ProyectoMiembros
                    GROUP BY IdUsuario
                ) pm ON pm.IdUsuario = u.IdUsuario
                WHERE 1 = 1";

            var parametros = new DynamicParameters();

            if (!string.IsNullOrWhiteSpace(buscar))
            {
                sql += @" AND (
                    u.Nombre LIKE @Buscar
                    OR u.Apellidos LIKE @Buscar
                    OR CONCAT(u.Nombre, ' ', u.Apellidos) LIKE @Buscar
                    OR u.Correo LIKE @Buscar
                )";
                parametros.Add("Buscar", $"%{buscar.Trim()}%");
            }

            if (idRol.HasValue && idRol.Value > 0)
            {
                sql += " AND u.IdRol = @IdRol";
                parametros.Add("IdRol", idRol.Value);
            }

            if (activo.HasValue)
            {
                sql += " AND u.Activo = @Activo";
                parametros.Add("Activo", activo.Value);
            }

            sql += " ORDER BY u.Activo DESC, u.IdRol ASC, u.Nombre ASC, u.Apellidos ASC;";

            var usuarios = await conexion.QueryAsync<UsuarioDto>(sql, parametros);
            return Results.Ok(usuarios);
        });

        // GET /api/usuarios/{id:int}
        group.MapGet("/{id:int}", async (int id, ConexionDbApi db) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();

            const string sql = @"
                SELECT
                    u.IdUsuario,
                    u.Nombre,
                    u.Apellidos,
                    u.Correo,
                    u.IdRol,
                    r.Nombre AS RolNombre,
                    u.Activo,
                    u.FechaRegistro,
                    u.UltimoAcceso,
                    (SELECT COUNT(*) FROM Tareas t WHERE t.IdResponsable = u.IdUsuario AND t.Estado <> 'Finalizado') AS TareasAsignadas,
                    (SELECT COUNT(*) FROM Tareas t WHERE t.IdResponsable = u.IdUsuario AND t.Estado <> 'Finalizado') AS TareasActivas,
                    (SELECT COALESCE(SUM(t.HorasEstimadas), 0) FROM Tareas t WHERE t.IdResponsable = u.IdUsuario AND t.Estado <> 'Finalizado') AS HorasAsignadas,
                    (SELECT COALESCE(SUM(t.HorasEstimadas), 0) FROM Tareas t WHERE t.IdResponsable = u.IdUsuario AND t.Estado <> 'Finalizado') AS HorasEstimadasAsignadas,
                    (SELECT COUNT(*) FROM ProyectoMiembros pm WHERE pm.IdUsuario = u.IdUsuario) AS ProyectosParticipa
                FROM Usuarios u
                INNER JOIN Roles r ON r.IdRol = u.IdRol
                WHERE u.IdUsuario = @IdUsuario;";

            var usuario = await conexion.QueryFirstOrDefaultAsync<UsuarioDto>(sql, new { IdUsuario = id });
            if (usuario is null)
            {
                return Results.NotFound(new ApiErrorResponse($"No se encontró el usuario con ID {id}."));
            }

            return Results.Ok(usuario);
        });

        // POST /api/usuarios (Requiere rol: Administrador)
        group.MapPost("/", async (
            CrearUsuarioRequest req,
            HttpContext httpContext,
            ConexionDbApi db,
            AuditoriaService auditoria) =>
        {
            var errorValidacion = ValidarDatosBase(req.Nombre, req.Apellidos, req.Correo);
            if (errorValidacion is not null)
            {
                return Results.BadRequest(new ApiErrorResponse(errorValidacion));
            }

            if (string.IsNullOrWhiteSpace(req.Contrasena) || req.Contrasena.Length < 8)
            {
                return Results.BadRequest(new ApiErrorResponse("La contraseña es obligatoria y debe tener al menos 8 caracteres."));
            }

            using var conexion = await db.CrearConexionAbiertaAsync();

            var rolNombre = await conexion.ExecuteScalarAsync<string?>(
                "SELECT Nombre FROM Roles WHERE IdRol = @IdRol;",
                new { req.IdRol });

            if (string.IsNullOrEmpty(rolNombre))
            {
                return Results.BadRequest(new ApiErrorResponse("El rol seleccionado no es válido."));
            }

            var correoNormalizado = req.Correo.Trim().ToLowerInvariant();
            var existeCorreo = await conexion.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Usuarios WHERE LOWER(Correo) = @Correo;",
                new { Correo = correoNormalizado });

            if (existeCorreo > 0)
            {
                return Results.Conflict(new ApiErrorResponse($"Ya existe un usuario registrado con el correo '{correoNormalizado}'."));
            }

            var hash = BCrypt.Net.BCrypt.HashPassword(req.Contrasena);

            const string insertSql = @"
                INSERT INTO Usuarios (Nombre, Apellidos, Correo, ContrasenaHash, IdRol, Activo, FechaRegistro)
                VALUES (@Nombre, @Apellidos, @Correo, @ContrasenaHash, @IdRol, @Activo, NOW());
                SELECT LAST_INSERT_ID();";

            var nuevoId = await conexion.ExecuteScalarAsync<int>(insertSql, new
            {
                Nombre = req.Nombre.Trim(),
                Apellidos = req.Apellidos.Trim(),
                Correo = correoNormalizado,
                ContrasenaHash = hash,
                IdRol = req.IdRol,
                Activo = req.Activo
            });

            var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);
            await auditoria.RegistrarAsync(
                conexion,
                idActor,
                "Crear",
                "Usuarios",
                nuevoId,
                $"Usuario {req.Nombre.Trim()} {req.Apellidos.Trim()} ({correoNormalizado}) creado con rol '{rolNombre}'.");

            return Results.Created($"/api/usuarios/{nuevoId}", new ApiMessageResponse("Usuario creado correctamente.", nuevoId));
        })
        .RequireRoles("Administrador");

        // PUT /api/usuarios/{id:int} (Requiere rol: Administrador)
        group.MapPut("/{id:int}", async (
            int id,
            ActualizarUsuarioRequest req,
            HttpContext httpContext,
            ConexionDbApi db,
            AuditoriaService auditoria) =>
        {
            var errorValidacion = ValidarDatosBase(req.Nombre, req.Apellidos, req.Correo);
            if (errorValidacion is not null)
            {
                return Results.BadRequest(new ApiErrorResponse(errorValidacion));
            }

            if (!string.IsNullOrWhiteSpace(req.NuevaContrasena) && req.NuevaContrasena.Length < 8)
            {
                return Results.BadRequest(new ApiErrorResponse("La nueva contraseña debe tener al menos 8 caracteres."));
            }

            using var conexion = await db.CrearConexionAbiertaAsync();

            var actual = await conexion.QueryFirstOrDefaultAsync<UsuarioDto>(@"
                SELECT u.IdUsuario, u.Nombre, u.Apellidos, u.Correo, u.IdRol, r.Nombre AS RolNombre, u.Activo
                FROM Usuarios u
                INNER JOIN Roles r ON r.IdRol = u.IdRol
                WHERE u.IdUsuario = @IdUsuario;",
                new { IdUsuario = id });

            if (actual is null)
            {
                return Results.NotFound(new ApiErrorResponse($"No se encontró el usuario con ID {id}."));
            }

            var nuevoRolNombre = await conexion.ExecuteScalarAsync<string?>(
                "SELECT Nombre FROM Roles WHERE IdRol = @IdRol;",
                new { req.IdRol });

            if (string.IsNullOrEmpty(nuevoRolNombre))
            {
                return Results.BadRequest(new ApiErrorResponse("El rol seleccionado no es válido."));
            }

            var correoNormalizado = req.Correo.Trim().ToLowerInvariant();
            var existeCorreo = await conexion.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Usuarios WHERE LOWER(Correo) = @Correo AND IdUsuario <> @IdUsuario;",
                new { Correo = correoNormalizado, IdUsuario = id });

            if (existeCorreo > 0)
            {
                return Results.Conflict(new ApiErrorResponse($"El correo '{correoNormalizado}' ya está siendo utilizado por otro usuario."));
            }

            // Regla de seguridad: impedir desactivar o cambiar de rol al último Administrador activo
            var esAdminActivoActual = string.Equals(actual.RolNombre, "Administrador", StringComparison.OrdinalIgnoreCase) && actual.Activo;
            var dejaDeSerAdminActivo = !string.Equals(nuevoRolNombre, "Administrador", StringComparison.OrdinalIgnoreCase) || !req.Activo;

            if (esAdminActivoActual && dejaDeSerAdminActivo)
            {
                var otrosAdminsActivos = await ContarOtrosAdministradoresActivosAsync(conexion, id);
                if (otrosAdminsActivos == 0)
                {
                    return Results.BadRequest(new ApiErrorResponse(
                        "Operación bloqueada por seguridad: no se puede desactivar ni cambiar el rol del último Administrador activo del sistema."));
                }
            }

            if (!string.IsNullOrWhiteSpace(req.NuevaContrasena))
            {
                var nuevoHash = BCrypt.Net.BCrypt.HashPassword(req.NuevaContrasena);
                await conexion.ExecuteAsync(@"
                    UPDATE Usuarios
                    SET Nombre = @Nombre,
                        Apellidos = @Apellidos,
                        Correo = @Correo,
                        IdRol = @IdRol,
                        Activo = @Activo,
                        ContrasenaHash = @ContrasenaHash
                    WHERE IdUsuario = @IdUsuario;",
                    new
                    {
                        IdUsuario = id,
                        Nombre = req.Nombre.Trim(),
                        Apellidos = req.Apellidos.Trim(),
                        Correo = correoNormalizado,
                        IdRol = req.IdRol,
                        Activo = req.Activo,
                        ContrasenaHash = nuevoHash
                    });
            }
            else
            {
                await conexion.ExecuteAsync(@"
                    UPDATE Usuarios
                    SET Nombre = @Nombre,
                        Apellidos = @Apellidos,
                        Correo = @Correo,
                        IdRol = @IdRol,
                        Activo = @Activo
                    WHERE IdUsuario = @IdUsuario;",
                    new
                    {
                        IdUsuario = id,
                        Nombre = req.Nombre.Trim(),
                        Apellidos = req.Apellidos.Trim(),
                        Correo = correoNormalizado,
                        IdRol = req.IdRol,
                        Activo = req.Activo
                    });
            }

            var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);
            await auditoria.RegistrarAsync(
                conexion,
                idActor,
                "Actualizar",
                "Usuarios",
                id,
                $"Usuario {req.Nombre.Trim()} {req.Apellidos.Trim()} actualizado (Rol: {nuevoRolNombre}, Activo: {req.Activo}).");

            return Results.Ok(new ApiMessageResponse("Usuario actualizado correctamente.", id));
        })
        .RequireRoles("Administrador");

        // PATCH /api/usuarios/{id:int}/rol (Requiere rol: Administrador)
        group.MapPatch("/{id:int}/rol", async (
            int id,
            CambiarRolUsuarioRequest req,
            HttpContext httpContext,
            ConexionDbApi db,
            AuditoriaService auditoria) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();

            var actual = await conexion.QueryFirstOrDefaultAsync<UsuarioDto>(@"
                SELECT u.IdUsuario, u.Nombre, u.Apellidos, u.Correo, u.IdRol, r.Nombre AS RolNombre, u.Activo
                FROM Usuarios u
                INNER JOIN Roles r ON r.IdRol = u.IdRol
                WHERE u.IdUsuario = @IdUsuario;",
                new { IdUsuario = id });

            if (actual is null)
            {
                return Results.NotFound(new ApiErrorResponse($"No se encontró el usuario con ID {id}."));
            }

            var nuevoRolNombre = await conexion.ExecuteScalarAsync<string?>(
                "SELECT Nombre FROM Roles WHERE IdRol = @IdRol;",
                new { req.IdRol });

            if (string.IsNullOrEmpty(nuevoRolNombre))
            {
                return Results.BadRequest(new ApiErrorResponse("El rol seleccionado no es válido."));
            }

            var esAdminActivoActual = string.Equals(actual.RolNombre, "Administrador", StringComparison.OrdinalIgnoreCase) && actual.Activo;
            var dejaDeSerAdmin = !string.Equals(nuevoRolNombre, "Administrador", StringComparison.OrdinalIgnoreCase);

            if (esAdminActivoActual && dejaDeSerAdmin)
            {
                var otrosAdminsActivos = await ContarOtrosAdministradoresActivosAsync(conexion, id);
                if (otrosAdminsActivos == 0)
                {
                    return Results.BadRequest(new ApiErrorResponse(
                        "No se puede cambiar el rol del último Administrador activo del sistema."));
                }
            }

            await conexion.ExecuteAsync(
                "UPDATE Usuarios SET IdRol = @IdRol WHERE IdUsuario = @IdUsuario;",
                new { IdRol = req.IdRol, IdUsuario = id });

            var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);
            await auditoria.RegistrarAsync(
                conexion,
                idActor,
                "Cambiar Rol",
                "Usuarios",
                id,
                $"Rol de {actual.NombreCompleto} cambiado de '{actual.RolNombre}' a '{nuevoRolNombre}'.");

            return Results.Ok(new ApiMessageResponse($"Rol actualizado a '{nuevoRolNombre}'.", id));
        })
        .RequireRoles("Administrador");

        // PATCH /api/usuarios/{id:int}/estado (Requiere rol: Administrador)
        group.MapPatch("/{id:int}/estado", async (
            int id,
            CambiarEstadoUsuarioRequest req,
            HttpContext httpContext,
            ConexionDbApi db,
            AuditoriaService auditoria) =>
        {
            using var conexion = await db.CrearConexionAbiertaAsync();

            var actual = await conexion.QueryFirstOrDefaultAsync<UsuarioDto>(@"
                SELECT u.IdUsuario, u.Nombre, u.Apellidos, u.Correo, u.IdRol, r.Nombre AS RolNombre, u.Activo
                FROM Usuarios u
                INNER JOIN Roles r ON r.IdRol = u.IdRol
                WHERE u.IdUsuario = @IdUsuario;",
                new { IdUsuario = id });

            if (actual is null)
            {
                return Results.NotFound(new ApiErrorResponse($"No se encontró el usuario con ID {id}."));
            }

            var esAdminActivoActual = string.Equals(actual.RolNombre, "Administrador", StringComparison.OrdinalIgnoreCase) && actual.Activo;
            if (esAdminActivoActual && !req.Activo)
            {
                var otrosAdminsActivos = await ContarOtrosAdministradoresActivosAsync(conexion, id);
                if (otrosAdminsActivos == 0)
                {
                    return Results.BadRequest(new ApiErrorResponse(
                        "No se puede desactivar al último Administrador activo del sistema."));
                }
            }

            await conexion.ExecuteAsync(
                "UPDATE Usuarios SET Activo = @Activo WHERE IdUsuario = @IdUsuario;",
                new { Activo = req.Activo, IdUsuario = id });

            var estadoTexto = req.Activo ? "Activado" : "Desactivado";
            var idActor = RequireRolesFilter.ObtenerIdUsuarioActual(httpContext);
            await auditoria.RegistrarAsync(
                conexion,
                idActor,
                estadoTexto,
                "Usuarios",
                id,
                $"Usuario {actual.NombreCompleto} ({actual.Correo}) fue {estadoTexto.ToLowerInvariant()}.");

            return Results.Ok(new ApiMessageResponse($"Usuario {estadoTexto.ToLowerInvariant()} correctamente.", id));
        })
        .RequireRoles("Administrador");
    }

    private static string? ValidarDatosBase(string? nombre, string? apellidos, string? correo)
    {
        if (string.IsNullOrWhiteSpace(nombre) || nombre.Trim().Length > 50)
        {
            return "El nombre es obligatorio y no debe exceder 50 caracteres.";
        }

        if (string.IsNullOrWhiteSpace(apellidos) || apellidos.Trim().Length > 80)
        {
            return "Los apellidos son obligatorios y no deben exceder 80 caracteres.";
        }

        if (string.IsNullOrWhiteSpace(correo) || correo.Trim().Length > 120)
        {
            return "El correo electrónico es obligatorio y no debe exceder 120 caracteres.";
        }

        try
        {
            var addr = new MailAddress(correo.Trim());
            if (!string.Equals(addr.Address, correo.Trim(), StringComparison.OrdinalIgnoreCase) || !correo.Contains('.'))
            {
                return "El formato del correo electrónico no es válido.";
            }
        }
        catch
        {
            return "El formato del correo electrónico no es válido.";
        }

        return null;
    }

    private static async Task<int> ContarOtrosAdministradoresActivosAsync(MySqlConnector.MySqlConnection conexion, int idUsuarioExcluir)
    {
        const string sql = @"
            SELECT COUNT(*)
            FROM Usuarios u
            INNER JOIN Roles r ON r.IdRol = u.IdRol
            WHERE r.Nombre = 'Administrador'
              AND u.Activo = 1
              AND u.IdUsuario <> @IdUsuario;";

        return await conexion.ExecuteScalarAsync<int>(sql, new { IdUsuario = idUsuarioExcluir });
    }
}
