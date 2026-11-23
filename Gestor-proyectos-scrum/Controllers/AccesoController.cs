using System.Security.Claims;
using Gestor_proyectos_scrum.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestor_proyectos_scrum.Controllers;

/// <summary>
/// Controlador MVC para HU-01: Acceso (login). Valida credenciales contra la Api
/// (POST /api/acceso/login, que verifica con BCrypt) y, si son correctas, abre la cookie
/// de autenticación del navegador con los claims NameIdentifier, Name y Role.
/// </summary>
public class AccesoController : Controller
{
    private readonly ScrumApiClient _apiClient;

    public AccesoController(ScrumApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Index(string? returnUrl)
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            return RedirectToInicio(User.FindFirst(ClaimTypes.Role)?.Value ?? string.Empty);
        }

        ViewBag.ReturnUrl = returnUrl;
        return View();
    }

    [HttpPost]
    [AllowAnonymous]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Entrar(string correo, string contrasena, string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contrasena))
        {
            TempData["Error"] = "Debes indicar correo y contraseña.";
            return RedirectToAction(nameof(Index), new { returnUrl });
        }

        var resultado = await _apiClient.IniciarSesionAsync(correo.Trim(), contrasena);
        if (!resultado.Exito || resultado.Datos is null)
        {
            TempData["Error"] = resultado.Error ?? "Credenciales incorrectas.";
            return RedirectToAction(nameof(Index), new { returnUrl });
        }

        var usuario = resultado.Datos;
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
            new Claim(ClaimTypes.Name, usuario.NombreCompleto),
            new Claim(ClaimTypes.Role, usuario.RolNombre)
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl);
        }

        return RedirectToInicio(usuario.RolNombre);
    }

    /// <summary>La vista depende del rol: Miembro del Equipo va directo a su inicio cliente, el resto a Home/Index.</summary>
    private IActionResult RedirectToInicio(string rol) =>
        Permisos.EsVistaCliente(rol)
            ? RedirectToAction("InicioCliente", "Home")
            : RedirectToAction("Index", "Home");

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Salir()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [AllowAnonymous]
    public IActionResult Denegado()
    {
        return View();
    }
}
