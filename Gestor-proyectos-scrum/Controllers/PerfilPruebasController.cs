using System.Security.Claims;
using Gestor_proyectos_scrum.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestor_proyectos_scrum.Controllers;

/// <summary>
/// Acceso directo (sin contraseña) a los 4 perfiles de prueba, solo en Development, para
/// evaluar los distintos roles sin reescribir credenciales a cada rato. Con HU-01 ya no
/// guarda el perfil en sesión: inicia la MISMA cookie de autenticación que usa el login
/// real (AccesoController), para que todo el sistema siga leyendo un solo origen de verdad
/// (los claims) y no haya dos mecanismos de identidad conviviendo.
/// </summary>
[AllowAnonymous]
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
        if (!resultado.Exito || resultado.Datos is null)
        {
            TempData["Error"] = resultado.Error ?? "No se pudo cargar ese usuario de prueba.";
            return RedirectToAction("Index", "Usuarios");
        }

        var u = resultado.Datos;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, u.IdUsuario.ToString()),
            new Claim(ClaimTypes.Name, u.NombreCompleto),
            new Claim(ClaimTypes.Role, u.RolNombre)
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));

        TempData["Mensaje"] = $"Perfil de pruebas: sesión iniciada como {u.NombreCompleto} ({u.RolNombre}).";

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Usuarios");
    }
}
