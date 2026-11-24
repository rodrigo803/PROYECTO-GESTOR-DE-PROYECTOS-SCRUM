using Gestor_proyectos_scrum.Models;

namespace Gestor_proyectos_scrum.Services;

public partial class ScrumApiClient
{
    // =========================================================================
    // HU-01: ENDPOINTS DE ACCESO (LOGIN)
    // =========================================================================

    /// <summary>Valida las credenciales contra la Api. No manda X-User-Id significativo
    /// (todavía no hay nadie autenticado); el login es público en la Api.</summary>
    public Task<ApiResult<LoginResultViewModel>> IniciarSesionAsync(string correo, string contrasena) =>
        EnviarAsync<LoginResultViewModel>(HttpMethod.Post, "api/acceso/login", new { Correo = correo, Contrasena = contrasena });
}
