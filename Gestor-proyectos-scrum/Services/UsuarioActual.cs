using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace Gestor_proyectos_scrum.Services;

/// <summary>
/// Usuario autenticado de la sesión actual. Lee IdUsuario, Nombre y Rol de los claims
/// de la cookie de autenticación que abre AccesoController tras validar la contraseña
/// contra la Api (HU-01). Con [Authorize] global en el MVC, cualquier acción que se
/// ejecute (salvo Acceso) ya tiene un usuario autenticado detrás de estos claims.
/// </summary>
public class UsuarioActual
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UsuarioActual(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? Usuario => _httpContextAccessor.HttpContext?.User;

    public int IdUsuario =>
        int.TryParse(Usuario?.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public string Nombre => Usuario?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    public string Rol => Usuario?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

    public bool PuedeGestionarProyectos() => Rol is "Administrador" or "Product Owner";

    public bool PuedePlanificarSprint() => Rol is "Administrador" or "Product Owner" or "Scrum Master";

    public bool PuedeAprobarHistorias() => Rol is "Administrador" or "Product Owner";
}
