using Dapper;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Gestor_proyectos_scrum.Data;

namespace Gestor_proyectos_scrum.Controllers;
public class AccesoController : Controller
{
 private readonly ConexionDb _db;
 public AccesoController(ConexionDb db) => _db=db;
 [AllowAnonymous, HttpGet] public IActionResult Index(string? returnUrl=null)
 {
   if (User.Identity?.IsAuthenticated==true) return RedirectToAction("Index","Home");
   ViewBag.ReturnUrl=returnUrl;return View();
 }
 [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
 public async Task<IActionResult> Entrar(string correo,string contrasena,string? returnUrl)
 {
   if (string.IsNullOrWhiteSpace(correo)||string.IsNullOrWhiteSpace(contrasena))
   {TempData["Error"]="Debes indicar correo y contraseña.";return RedirectToAction("Index");}
   try {
   await using var c=await _db.CrearConexionAbiertaAsync();
   var usuario=await c.QuerySingleOrDefaultAsync<UsuarioLogin>(@"select u.IdUsuario,u.Nombre,u.Apellidos,u.ContrasenaHash,u.Activo,r.Nombre as Rol
       from Usuarios u join Roles r on r.IdRol=u.IdRol where u.Correo=@correo",new{correo=correo.Trim()});
   var comprobador=new PasswordHasher<UsuarioLogin>();
   // Las claves de ejemplo PENDIENTE-HU01 deben sustituirse con scripts/preparar_clave.py.
   if (usuario==null || !usuario.Activo || !usuario.ContrasenaHash.StartsWith("AQAAAA",StringComparison.Ordinal))
   {TempData["Error"]="Credenciales incorrectas o cuenta no habilitada.";return RedirectToAction("Index");}
   PasswordVerificationResult ok;
   try { ok=comprobador.VerifyHashedPassword(usuario,usuario.ContrasenaHash,contrasena); }
   catch { ok=PasswordVerificationResult.Failed; }
   if (ok==PasswordVerificationResult.Failed) {TempData["Error"]="Credenciales incorrectas.";return RedirectToAction("Index");}
   var claims=new[]{new Claim(ClaimTypes.NameIdentifier,usuario.IdUsuario.ToString()),new Claim(ClaimTypes.Name,usuario.Nombre+" "+usuario.Apellidos),new Claim(ClaimTypes.Role,usuario.Rol)};
   await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(claims,CookieAuthenticationDefaults.AuthenticationScheme)));
   await c.ExecuteAsync("update Usuarios set UltimoAcceso=now() where IdUsuario=@id",new{id=usuario.IdUsuario});
   await c.ExecuteAsync("insert into Auditoria (IdUsuario,Accion,Entidad,IdRegistro,Detalle) values (@id,'LOGIN','Usuarios',@id,'Inicio de sesión')",new{id=usuario.IdUsuario});
   if (!string.IsNullOrWhiteSpace(returnUrl)&&Url.IsLocalUrl(returnUrl)) return LocalRedirect(returnUrl);
   return RedirectToAction("Index","Home");
   }catch(Exception){TempData["Error"]="No se pudo acceder al servidor de base de datos. Revisa la conexión.";return RedirectToAction("Index");}
 }
 [Authorize, HttpPost, ValidateAntiForgeryToken]
 public async Task<IActionResult> Salir() {
   await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
   return RedirectToAction("Index");
 }
 [AllowAnonymous] public IActionResult Denegado() => View();
 private class UsuarioLogin {public int IdUsuario{get;set;}public string Nombre{get;set;}="";public string Apellidos{get;set;}="";public string ContrasenaHash{get;set;}="";public bool Activo{get;set;}public string Rol{get;set;}="";}
}
