using Dapper;
using System.Security.Claims;
using Gestor_proyectos_scrum.Data;
using Gestor_proyectos_scrum.Services;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;

namespace Gestor_proyectos_scrum.Endpoints;

public static class AccesoEndpoints
{
    public static void Mapear(WebApplication app)
    {
        var grupo = app.MapGroup("/Acceso").WithTags("HU-01 Acceso");
        grupo.MapGet("/Index", (HttpContext ctx, RazorVista vistas, string? returnUrl) =>
        {
            if (ctx.User.Identity?.IsAuthenticated == true)
                return Task.FromResult<IResult>(Results.Redirect("/Home/Index"));
            return vistas.Mostrar(ctx, "Acceso", "Index", opciones: new Dictionary<string, object?> { ["ReturnUrl"] = returnUrl });
        }).AllowAnonymous();

        grupo.MapPost("/Entrar", Entrar).AllowAnonymous();
        grupo.MapPost("/Salir", Salir).RequireAuthorization();
        grupo.MapGet("/Denegado", (HttpContext ctx, RazorVista vistas) =>
            vistas.Mostrar(ctx, "Acceso", "Denegado")).AllowAnonymous();
    }

    private static async Task<IResult> Entrar(HttpContext ctx, ConexionDb db, IAntiforgery antiforgery)
    {
        if (!await ApiComun.TokenValido(ctx, antiforgery)) return ApiComun.ErrorToken();
        var form = await ctx.Request.ReadFormAsync();
        var correo = ApiComun.Campo(form, "correo");
        var contrasena = form["contrasena"].ToString();
        var returnUrl = ApiComun.Campo(form, "returnUrl");
        if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(contrasena))
        {
            ApiComun.Mensaje(ctx, "Error", "Debes indicar correo y contraseña.");
            return Results.Redirect("/Acceso/Index");
        }
        try
        {
            await using var conexion = await db.CrearConexionAbiertaAsync();
            var usuario = await conexion.QuerySingleOrDefaultAsync<UsuarioLogin>(@"
                select u.IdUsuario,u.Nombre,u.Apellidos,u.ContrasenaHash,u.Activo,r.Nombre as Rol
                from Usuarios u join Roles r on r.IdRol=u.IdRol where u.Correo=@correo", new { correo });
            if (usuario == null || !usuario.Activo ||
                !usuario.ContrasenaHash.StartsWith("AQAAAA", StringComparison.Ordinal))
            {
                ApiComun.Mensaje(ctx, "Error", "Credenciales incorrectas o cuenta no habilitada.");
                return Results.Redirect("/Acceso/Index");
            }
            PasswordVerificationResult resultado;
            try { resultado = new PasswordHasher<UsuarioLogin>().VerifyHashedPassword(usuario, usuario.ContrasenaHash, contrasena); }
            catch { resultado = PasswordVerificationResult.Failed; }
            if (resultado == PasswordVerificationResult.Failed)
            {
                ApiComun.Mensaje(ctx, "Error", "Credenciales incorrectas.");
                return Results.Redirect("/Acceso/Index");
            }
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
                new Claim(ClaimTypes.Name, usuario.Nombre + " " + usuario.Apellidos),
                new Claim(ClaimTypes.Role, usuario.Rol)
            };
            await ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)));
            await conexion.ExecuteAsync("update Usuarios set UltimoAcceso=now() where IdUsuario=@id", new { id = usuario.IdUsuario });
            await conexion.ExecuteAsync(@"insert into Auditoria (IdUsuario,Accion,Entidad,IdRegistro,Detalle)
                values (@id,'LOGIN','Usuarios',@id,'Inicio de sesión')", new { id = usuario.IdUsuario });
            if (!string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith('/') &&
                !returnUrl.StartsWith("//") && !returnUrl.StartsWith("/\\"))
                return Results.LocalRedirect(returnUrl);
            return Results.Redirect("/Home/Index");
        }
        catch
        {
            ApiComun.Mensaje(ctx, "Error", "No se pudo acceder al servidor de base de datos. Revisa la conexión.");
            return Results.Redirect("/Acceso/Index");
        }
    }

    private static async Task<IResult> Salir(HttpContext ctx, IAntiforgery antiforgery)
    {
        if (!await ApiComun.TokenValido(ctx, antiforgery)) return ApiComun.ErrorToken();
        await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Results.Redirect("/Acceso/Index");
    }

    private sealed class UsuarioLogin
    {
        public int IdUsuario { get; set; }
        public string Nombre { get; set; } = "";
        public string Apellidos { get; set; } = "";
        public string ContrasenaHash { get; set; } = "";
        public bool Activo { get; set; }
        public string Rol { get; set; } = "";
    }
}
