using Dapper;
using Gestor_proyectos_scrum.Data;
using Gestor_proyectos_scrum.Models;
using Gestor_proyectos_scrum.Services;

namespace Gestor_proyectos_scrum.Endpoints;

public static class HomeEndpoints
{
    public static void Mapear(WebApplication app)
    {
        var grupo = app.MapGroup("/Home").WithTags("Navegación");
        grupo.MapGet("/Index", (HttpContext ctx, RazorVista vistas) =>
            vistas.Mostrar(ctx, "Home", "Index")).RequireAuthorization();
        grupo.MapGet("/InicioCliente", (HttpContext ctx, RazorVista vistas) =>
            vistas.Mostrar(ctx, "Home", "InicioCliente")).RequireAuthorization();
        grupo.MapGet("/Error", (HttpContext ctx, RazorVista vistas) =>
            vistas.Mostrar(ctx, "Shared", "Error", new ErrorViewModel { RequestId = ctx.TraceIdentifier })).AllowAnonymous();
        grupo.MapGet("/EstadoBd", async (HttpContext ctx, RazorVista vistas, ConexionDb db,
            IWebHostEnvironment entorno) =>
        {
            if (!entorno.IsDevelopment()) return Results.NotFound();
            var m = new EstadoBdViewModel();
            try
            {
                await using var c = await db.CrearConexionAbiertaAsync();
                m.Conectado = true;
                m.Tablas = await c.ExecuteScalarAsync<int>("select count(*) from information_schema.tables where table_schema=database() and table_type='BASE TABLE'");
                m.Vistas = await c.ExecuteScalarAsync<int>("select count(*) from information_schema.views where table_schema=database()");
                m.Triggers = await c.ExecuteScalarAsync<int>("select count(*) from information_schema.triggers where trigger_schema=database()");
                m.RegistrosProyectos = await c.ExecuteScalarAsync<int>("select count(*) from Proyectos");
            }
            catch (Exception ex) { m.Conectado = false; m.Error = ex.Message; }
            return await vistas.Mostrar(ctx, "Home", "EstadoBd", m);
        }).RequireAuthorization();
    }
}
