using System.Security.Claims;
namespace Gestor_proyectos_scrum.Services;
public class UsuarioActual
{
    private readonly IHttpContextAccessor? _acceso;
    public UsuarioActual(IHttpContextAccessor acceso) { _acceso = acceso; }
    public int IdUsuario => int.TryParse(_acceso?.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier),out var id) ? id : 0;
    public string Nombre => _acceso?.HttpContext?.User.Identity?.Name ?? "Invitado";
}
