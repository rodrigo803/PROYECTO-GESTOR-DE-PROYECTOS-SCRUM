using Gestor_proyectos_scrum.Api;
using Gestor_proyectos_scrum.Data;
using Gestor_proyectos_scrum.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc.Authorization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// [Authorize] global: todo el MVC requiere sesión iniciada salvo lo marcado [AllowAnonymous]
// (Acceso y, en Development, PerfilPruebas). Ver Controllers/AccesoController.cs (HU-01).
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add(new AuthorizeFilter());
});

builder.Services.AddScoped<ConexionDb>();
builder.Services.AddScoped<UsuarioActual>();

// La Api (Gestor-proyectos-scrum.Api) se monta dentro de esta misma app para que
// Plesk solo tenga que publicar un sitio; sus servicios (ConexionDbApi, AuditoriaService)
// toman ConnectionStrings:ScrumDb de esta misma configuración del MVC.
builder.Services.AddScrumApi();

builder.Services.AddHttpContextAccessor();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Acceso/Index";
        options.AccessDeniedPath = "/Acceso/Denegado";
        options.Cookie.Name = "ScrumManager.Sesion";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(3);
        options.SlidingExpiration = true;
    });

// Con la Api montada en el mismo proceso, ApiSettings:BaseUrl en local apunta a la
// propia URL del MVC (ver launchSettings.json); en Plesk apunta al mismo dominio público.
var apiBaseUrl = builder.Configuration["ApiSettings:BaseUrl"] ?? "http://localhost:5265";
builder.Services.AddHttpClient<ScrumApiClient>(client =>
{
    client.BaseAddress = new Uri(apiBaseUrl.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(30);
});

var app = builder.Build();

// Manejo de errores acotado con UseWhen para que el handler JSON de la Api (/api/*)
// y el handler HTML del MVC (el resto de rutas) nunca se pisen entre sí.
app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api"),
    apiApp => apiApp.UseScrumApiExceptionHandler());

app.UseWhen(
    ctx => !ctx.Request.Path.StartsWithSegments("/api"),
    mvcApp =>
    {
        if (!app.Environment.IsDevelopment())
        {
            mvcApp.UseExceptionHandler("/Home/Error");
        }
    });

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapScrumApi();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
