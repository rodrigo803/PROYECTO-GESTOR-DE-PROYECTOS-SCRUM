using Dapper;
using Gestor_proyectos_scrum.Api.Data;
using Gestor_proyectos_scrum.Api.Models;

namespace Gestor_proyectos_scrum.Api.Services;

/// <summary>
/// Filtro de autorización por roles para Minimal APIs.
/// Lee el header X-User-Id, valida que el usuario exista en MySQL, esté activo
/// y posea uno de los roles permitidos.
/// </summary>
public class RequireRolesFilter : IEndpointFilter
{
    public const string CurrentUserIdKey = "CurrentUserId";
    public const string CurrentUserRoleKey = "CurrentUserRole";
    public const string CurrentUserNameKey = "CurrentUserName";

    private readonly string[] _allowedRoles;

    public RequireRolesFilter(params string[] allowedRoles)
    {
        _allowedRoles = allowedRoles;
    }

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var httpContext = context.HttpContext;

        if (!httpContext.Request.Headers.TryGetValue("X-User-Id", out var headerValues) ||
            !int.TryParse(headerValues.FirstOrDefault(), out var idUsuario) ||
            idUsuario <= 0)
        {
            return Results.Json(
                new ApiErrorResponse("Autenticación requerida: debe enviar un identificador válido en la cabecera X-User-Id."),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        var db = httpContext.RequestServices.GetRequiredService<ConexionDbApi>();
        using var conexion = await db.CrearConexionAbiertaAsync();

        const string sql = @"
            SELECT u.IdUsuario, u.Nombre, u.Apellidos, u.Activo, r.Nombre AS RolNombre
            FROM Usuarios u
            INNER JOIN Roles r ON r.IdRol = u.IdRol
            WHERE u.IdUsuario = @IdUsuario;";

        var usuario = await conexion.QueryFirstOrDefaultAsync<UsuarioAuthRow>(sql, new { IdUsuario = idUsuario });

        if (usuario is null)
        {
            return Results.Json(
                new ApiErrorResponse($"El usuario con ID {idUsuario} no existe en el sistema."),
                statusCode: StatusCodes.Status401Unauthorized);
        }

        if (!usuario.Activo)
        {
            return Results.Json(
                new ApiErrorResponse($"La cuenta de {usuario.Nombre} {usuario.Apellidos} se encuentra inactiva."),
                statusCode: StatusCodes.Status403Forbidden);
        }

        if (_allowedRoles.Length > 0 &&
            !_allowedRoles.Any(r => string.Equals(r, usuario.RolNombre, StringComparison.OrdinalIgnoreCase)))
        {
            var rolesTexto = string.Join(", ", _allowedRoles);
            return Results.Json(
                new ApiErrorResponse($"Permisos insuficientes. Tu rol actual es '{usuario.RolNombre}' y esta operación requiere: {rolesTexto}."),
                statusCode: StatusCodes.Status403Forbidden);
        }

        httpContext.Items[CurrentUserIdKey] = usuario.IdUsuario;
        httpContext.Items[CurrentUserRoleKey] = usuario.RolNombre;
        httpContext.Items[CurrentUserNameKey] = $"{usuario.Nombre} {usuario.Apellidos}".Trim();

        return await next(context);
    }

    public static int ObtenerIdUsuarioActual(HttpContext httpContext)
    {
        if (httpContext.Items.TryGetValue(CurrentUserIdKey, out var val) && val is int id && id > 0)
        {
            return id;
        }

        if (httpContext.Request.Headers.TryGetValue("X-User-Id", out var header) &&
            int.TryParse(header.FirstOrDefault(), out var parsedId) && parsedId > 0)
        {
            return parsedId;
        }

        return 1;
    }

    public static string? ObtenerRolUsuarioActual(HttpContext httpContext) =>
        httpContext.Items.TryGetValue(CurrentUserRoleKey, out var val) ? val as string : null;

    private sealed class UsuarioAuthRow
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellidos { get; set; } = string.Empty;
        public bool Activo { get; set; }
        public string RolNombre { get; set; } = string.Empty;
    }
}

public static class RoleAccessFilterExtensions
{
    public static TBuilder RequireRoles<TBuilder>(this TBuilder builder, params string[] allowedRoles)
        where TBuilder : IEndpointConventionBuilder
    {
        return builder.AddEndpointFilter(new RequireRolesFilter(allowedRoles));
    }
}
