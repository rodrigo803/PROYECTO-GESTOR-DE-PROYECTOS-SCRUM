using Gestor_proyectos_scrum.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gestor_proyectos_scrum.Controllers;

/// <summary>
/// Permite cambiar el perfil de usuario activo en sesión durante las pruebas en entorno Development
/// para evaluar los 4 roles de SCRUM sin alterar _Layout.cshtml ni UsuarioActual.cs.
/// </summary>
public class PerfilPruebasController : Controller
{
    private readonly IWebHostEnvironment _env;
    private readonly ScrumApiClient _apiClient;

    public PerfilPruebasController(IWebHostEnvironment env, ScrumApiClient apiClient)
    {
        _env = env;
        _apiClient = apiClient;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cambiar(int idUsuario, string? returnUrl)
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }

        var resultado = await _apiClient.ObtenerUsuarioPorIdAsync(idUsuario);
        if (resultado.Exito && resultado.Datos is not null)
        {
            var u = resultado.Datos;
            HttpContext.Session.SetInt32(ScrumApiClient.SessionDevUserId, u.IdUsuario);
            HttpContext.Session.SetString(ScrumApiClient.SessionDevUserNombre, u.NombreCompleto);
            HttpContext.Session.SetString(ScrumApiClient.SessionDevUserRol, u.RolNombre);

            TempData["Mensaje"] = $"Perfil de pruebas cambiado a: {u.NombreCompleto} ({u.RolNombre}).";
        }
        else
        {
            // Fallback por si la API aún no responde pero se quiere cambiar el ID en sesión
            var (nombreFallback, rolFallback) = idUsuario switch
            {
                1 => ("Admin Sistema", "Administrador"),
                2 => ("Valeria Castro", "Product Owner"),
                3 => ("Rodrigo Morales", "Scrum Master"),
                4 => ("Carlos Ruiz", "Miembro del Equipo"),
                _ => ($"Usuario #{idUsuario}", "Miembro del Equipo")
            };

            HttpContext.Session.SetInt32(ScrumApiClient.SessionDevUserId, idUsuario);
            HttpContext.Session.SetString(ScrumApiClient.SessionDevUserNombre, nombreFallback);
            HttpContext.Session.SetString(ScrumApiClient.SessionDevUserRol, rolFallback);

            TempData["Mensaje"] = $"Perfil de pruebas establecido en: {nombreFallback} ({rolFallback}).";
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Usuarios");
    }
}
