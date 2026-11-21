using Microsoft.AspNetCore.Http;

namespace Gestor_proyectos_scrum.Services;

/// <summary>
/// Usuario autenticado de la sesión actual.
/// Lee el perfil activo guardado en sesión por PerfilPruebasController (el mismo que
/// ScrumApiClient manda como header X-User-Id a la Api), para que todo el sistema
/// use una sola fuente del usuario actual. Si la sesión no tiene un perfil elegido,
/// cae a un usuario fijo de desarrollo.
/// Cuando exista el login de HU-01 (Jesús), este es el único lugar que hay que cambiar.
/// </summary>
public class UsuarioActual
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UsuarioActual(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ISession? Sesion => _httpContextAccessor.HttpContext?.Session;

    public int IdUsuario => Sesion?.GetInt32(ScrumApiClient.SessionDevUserId) ?? 2;

    public string Nombre => Sesion?.GetString(ScrumApiClient.SessionDevUserNombre) ?? "Valeria Castro";

    public string Rol => Sesion?.GetString(ScrumApiClient.SessionDevUserRol) ?? "Product Owner";

    public bool PuedeGestionarProyectos() => Rol is "Administrador" or "Product Owner";

    public bool PuedePlanificarSprint() => Rol is "Administrador" or "Product Owner" or "Scrum Master";

    public bool PuedeAprobarHistorias() => Rol is "Administrador" or "Product Owner";
}
