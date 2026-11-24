using Dapper;
using Gestor_proyectos_scrum.Api.Data;
using Gestor_proyectos_scrum.Api.Models;
using Gestor_proyectos_scrum.Api.Services;

namespace Gestor_proyectos_scrum.Api.Endpoints;

/// <summary>
/// HU-01: Acceso (login). Valida credenciales con BCrypt, el mismo algoritmo con el que
/// HU-02 (Usuarios) genera y actualiza ContrasenaHash al crear o editar un usuario.
/// La Api es stateless y no emite cookies: solo valida y devuelve los datos del usuario;
/// el MVC (Controllers/AccesoController.cs) es quien abre la sesión del navegador.
/// Por eso este endpoint no lleva .RequireRoles(): todavía no hay nadie autenticado.
/// </summary>
public static class AccesoEndpoints
{
    public static void MapAccesoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/acceso").WithTags("HU-01 Acceso");

        // POST /api/acceso/login
        group.MapPost("/login", async (LoginRequest req, ConexionDbApi db, AuditoriaService auditoria) =>
        {
            if (string.IsNullOrWhiteSpace(req.Correo) || string.IsNullOrWhiteSpace(req.Contrasena))
            {
                return Results.Json(
                    new ApiErrorResponse("Debes indicar correo y contraseña."),
                    statusCode: StatusCodes.Status400BadRequest);
            }

            using var conexion = await db.CrearConexionAbiertaAsync();

            var usuario = await conexion.QueryFirstOrDefaultAsync<UsuarioAuthRow>(@"
                SELECT u.IdUsuario, u.Nombre, u.Apellidos, u.Correo, u.ContrasenaHash, u.Activo, r.Nombre AS RolNombre
                FROM Usuarios u
                JOIN Roles r ON r.IdRol = u.IdRol
                WHERE u.Correo = @Correo;",
                new { req.Correo });

            // Mensaje genérico si no existe la cuenta: no revelamos qué correos están registrados.
            if (usuario is null)
            {
                return Results.Json(
                    new ApiErrorResponse("Credenciales incorrectas."),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            if (!usuario.Activo)
            {
                return Results.Json(
                    new ApiErrorResponse($"La cuenta de {usuario.Nombre} {usuario.Apellidos} está inactiva."),
                    statusCode: StatusCodes.Status403Forbidden);
            }

            bool contrasenaValida;
            try
            {
                contrasenaValida = BCrypt.Net.BCrypt.Verify(req.Contrasena, usuario.ContrasenaHash);
            }
            catch
            {
                // ContrasenaHash con un formato que BCrypt no reconoce (p. ej. el marcador PENDIENTE-HU01).
                contrasenaValida = false;
            }

            if (!contrasenaValida)
            {
                return Results.Json(
                    new ApiErrorResponse("Credenciales incorrectas."),
                    statusCode: StatusCodes.Status401Unauthorized);
            }

            await conexion.ExecuteAsync(
                "UPDATE Usuarios SET UltimoAcceso = NOW() WHERE IdUsuario = @IdUsuario;",
                new { usuario.IdUsuario });
            await auditoria.RegistrarAsync(conexion, usuario.IdUsuario, "Iniciar sesión", "Usuarios",
                usuario.IdUsuario, "Inicio de sesión correcto.");

            return Results.Ok(new LoginResponseDto
            {
                IdUsuario = usuario.IdUsuario,
                Nombre = usuario.Nombre,
                Apellidos = usuario.Apellidos,
                Correo = usuario.Correo,
                RolNombre = usuario.RolNombre
            });
        });
    }

    private sealed class UsuarioAuthRow
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public string Correo { get; set; } = string.Empty;
        public string ContrasenaHash { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public string RolNombre { get; set; } = string.Empty;
    }
}
