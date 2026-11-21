using Gestor_proyectos_scrum.Data;
using Gestor_proyectos_scrum.Endpoints;
using Gestor_proyectos_scrum.Services;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Se utilizan vistas Razor para mantener exactamente el diseño actual.
// No hay controladores MVC: las rutas se declaran con MapGet y MapPost.
builder.Services.AddMvcCore().AddViews().AddRazorViewEngine();
builder.Services.AddAntiforgery();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Acceso/Index";
        o.AccessDeniedPath = "/Acceso/Denegado";
        o.Cookie.Name = "ScrumManager.Sesion";
        o.Cookie.HttpOnly = true;
        o.Cookie.SameSite = SameSiteMode.Lax;
        o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        o.ExpireTimeSpan = TimeSpan.FromHours(3);
        o.SlidingExpiration = true;
    });
builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ConexionDb>();
builder.Services.AddScoped<UsuarioActual>();
builder.Services.AddScoped<RazorVista>();

var app = builder.Build();
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapGet("/", (HttpContext ctx) => Results.Redirect(
    ctx.User.Identity?.IsAuthenticated == true ? "/Home/Index" : "/Acceso/Index"));

// HU-01, HU-04, HU-08 y HU-11 son Minimal APIs.
AccesoEndpoints.Mapear(app);
BacklogEndpoints.Mapear(app);
TableroEndpoints.Mapear(app);
RetrospectivasEndpoints.Mapear(app);
HomeEndpoints.Mapear(app);

app.Run();
