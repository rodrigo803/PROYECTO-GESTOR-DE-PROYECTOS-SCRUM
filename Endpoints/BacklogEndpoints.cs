using Dapper;
using Gestor_proyectos_scrum.Data;
using Gestor_proyectos_scrum.Models;
using Gestor_proyectos_scrum.Services;
using Microsoft.AspNetCore.Antiforgery;
using MySqlConnector;

namespace Gestor_proyectos_scrum.Endpoints;

/// <summary>HU-04: CRUD de historias y criterios de aceptación con Minimal APIs.</summary>
public static class BacklogEndpoints
{
    public static void Mapear(WebApplication app)
    {
        var g = app.MapGroup("/Backlog").RequireAuthorization().WithTags("HU-04 Product Backlog");
        g.MapGet("/Index", Index);
        g.MapPost("/Guardar", Guardar);
        g.MapPost("/Eliminar", Eliminar);
    }

    private static bool PuedeEditar(HttpContext c) =>
        c.User.IsInRole("Administrador") || c.User.IsInRole("Product Owner");

    private static async Task<bool> PuedeAcceder(HttpContext ctx, MySqlConnection c, int idProyecto) =>
        ctx.User.IsInRole("Administrador") ||
        await c.ExecuteScalarAsync<int>(@"select count(*) from ProyectoMiembros
            where IdProyecto=@idProyecto and IdUsuario=@u", new { idProyecto, u = ApiComun.UsuarioId(ctx) }) > 0;

    private static async Task<IResult> Index(HttpContext ctx, RazorVista vistas, ConexionDb db, int? proyectoId)
    {
        await using var c = await db.CrearConexionAbiertaAsync();
        var vm = new BacklogVista();
        vm.Proyectos = (await c.QueryAsync<ProyectoOpcion>(@"
            select p.IdProyecto,p.Nombre from Proyectos p where @admin=1
            or exists(select 1 from ProyectoMiembros pm where pm.IdProyecto=p.IdProyecto and pm.IdUsuario=@u)
            order by p.Nombre", new { admin = ctx.User.IsInRole("Administrador") ? 1 : 0, u = ApiComun.UsuarioId(ctx) })).ToList();
        vm.ProyectoId = vm.Proyectos.Any(x => x.IdProyecto == proyectoId)
            ? proyectoId!.Value : vm.Proyectos.FirstOrDefault()?.IdProyecto ?? 0;
        if (vm.ProyectoId > 0)
        {
            vm.Historias = (await c.QueryAsync<HistoriaDto>(@"
                select h.*,p.Nombre as NombreProyecto,
                (select count(*) from CriteriosAceptacion ca where ca.IdHistoria=h.IdHistoria) as TotalCriterios,
                (select group_concat(ca.Descripcion separator '\n') from CriteriosAceptacion ca where ca.IdHistoria=h.IdHistoria) as CriteriosTexto
                from HistoriasUsuario h join Proyectos p on p.IdProyecto=h.IdProyecto
                where h.IdProyecto=@id order by h.OrdenPrioridad,h.IdHistoria", new { id = vm.ProyectoId })).ToList();
        }
        return await vistas.Mostrar(ctx, "Backlog", "Index", vm, new Dictionary<string, object?>
        {
            ["PuedeEditar"] = PuedeEditar(ctx)
        });
    }

    private static async Task<IResult> Guardar(HttpContext ctx, ConexionDb db, IAntiforgery antiforgery)
    {
        if (!await ApiComun.TokenValido(ctx, antiforgery)) return ApiComun.ErrorToken();
        if (!PuedeEditar(ctx)) return Results.Forbid();
        var f = await ctx.Request.ReadFormAsync();
        var h = new HistoriaDto
        {
            IdHistoria = ApiComun.Numero(f, "IdHistoria"),
            IdProyecto = ApiComun.Numero(f, "IdProyecto"),
            Codigo = ApiComun.Campo(f, "Codigo"),
            Como = ApiComun.Campo(f, "Como"),
            Quiero = ApiComun.Campo(f, "Quiero"),
            Para = ApiComun.Campo(f, "Para"),
            Prioridad = ApiComun.Campo(f, "Prioridad"),
            OrdenPrioridad = ApiComun.Numero(f, "OrdenPrioridad"),
            Estimacion = ApiComun.NumeroOpcional(f, "Estimacion"),
            Estado = ApiComun.Campo(f, "Estado"),
            CriteriosTexto = ApiComun.Campo(f, "CriteriosTexto")
        };
        var redirigir = $"/Backlog/Index?proyectoId={h.IdProyecto}";
        var (valido, error) = ApiComun.Validar(h);
        var criterios = h.CriteriosTexto.Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();
        if (h.IdProyecto < 1 || h.IdHistoria < 0 ||
            !new[] { "Alta", "Media", "Baja" }.Contains(h.Prioridad) ||
            !new[] { "Pendiente", "Lista", "En Sprint", "Terminada" }.Contains(h.Estado) ||
            criterios.Any(x => x.Length > 300) ||
            ((h.Estado == "Lista" || h.Estado == "En Sprint") && (criterios.Count == 0 || h.Estimacion == null)))
        {
            valido = false;
            error = "Revisa prioridad, estado, proyecto, estimación y criterios de aceptación (máximo 300 caracteres).";
        }
        if (!valido)
        {
            ApiComun.Mensaje(ctx, "Error", error);
            return Results.Redirect(redirigir);
        }

        await using var c = await db.CrearConexionAbiertaAsync();
        if (!await PuedeAcceder(ctx, c, h.IdProyecto)) return Results.Forbid();
        await using var tx = await c.BeginTransactionAsync();
        try
        {
            bool nuevo = h.IdHistoria == 0;
            if (!nuevo)
            {
                var existe = await c.ExecuteScalarAsync<int>(@"
                    select count(*) from HistoriasUsuario where IdHistoria=@id and IdProyecto=@p",
                    new { id = h.IdHistoria, p = h.IdProyecto }, tx);
                if (existe == 0)
                {
                    ApiComun.Mensaje(ctx, "Error", "Historia no encontrada.");
                    return Results.Redirect(redirigir);
                }
                var actuales = (await c.QueryAsync<string>(@"
                    select Descripcion from CriteriosAceptacion where IdHistoria=@id order by IdCriterio",
                    new { id = h.IdHistoria }, tx)).ToList();
                if (!actuales.SequenceEqual(criterios))
                {
                    var aprobados = await c.ExecuteScalarAsync<int>(@"
                        select count(*) from Aprobaciones a
                        join CriteriosAceptacion ca on ca.IdCriterio=a.IdCriterio
                        where ca.IdHistoria=@id", new { id = h.IdHistoria }, tx);
                    if (aprobados > 0)
                        throw new InvalidOperationException("No se pueden sustituir criterios con aprobaciones registradas.");
                    await c.ExecuteAsync("delete from CriteriosAceptacion where IdHistoria=@id", new { id = h.IdHistoria }, tx);
                }
                else criterios.Clear();
                await c.ExecuteAsync(@"
                    update HistoriasUsuario set Codigo=@Codigo,Como=@Como,Quiero=@Quiero,Para=@Para,
                    Prioridad=@Prioridad,OrdenPrioridad=@OrdenPrioridad,Estimacion=@Estimacion,Estado=@Estado
                    where IdHistoria=@IdHistoria and IdProyecto=@IdProyecto", h, tx);
            }
            else
            {
                await c.ExecuteAsync(@"
                    insert into HistoriasUsuario
                    (IdProyecto,Codigo,Como,Quiero,Para,Prioridad,OrdenPrioridad,Estimacion,Estado)
                    values (@IdProyecto,@Codigo,@Como,@Quiero,@Para,@Prioridad,@OrdenPrioridad,@Estimacion,@Estado)", h, tx);
                h.IdHistoria = await c.ExecuteScalarAsync<int>("select last_insert_id()", transaction: tx);
            }
            foreach (var criterio in criterios)
                await c.ExecuteAsync(@"insert into CriteriosAceptacion (IdHistoria,Descripcion) values (@id,@texto)",
                    new { id = h.IdHistoria, texto = criterio }, tx);
            await c.ExecuteAsync(@"
                insert into Auditoria (IdUsuario,Accion,Entidad,IdRegistro,Detalle)
                values (@u,@accion,'HistoriasUsuario',@id,@codigo)",
                new { u = ApiComun.UsuarioId(ctx), accion = nuevo ? "CREAR" : "EDITAR", id = h.IdHistoria, codigo = h.Codigo }, tx);
            await tx.CommitAsync();
            ApiComun.Mensaje(ctx, "Mensaje", "Historia guardada en MySQL.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            ApiComun.Mensaje(ctx, "Error", ex is MySqlException
                ? "No se pudo guardar: revisa código duplicado o relaciones en MySQL." : ex.Message);
        }
        return Results.Redirect(redirigir);
    }

    private static async Task<IResult> Eliminar(HttpContext ctx, ConexionDb db, IAntiforgery antiforgery)
    {
        if (!await ApiComun.TokenValido(ctx, antiforgery)) return ApiComun.ErrorToken();
        if (!PuedeEditar(ctx)) return Results.Forbid();
        var f = await ctx.Request.ReadFormAsync();
        var id = ApiComun.Numero(f, "id");
        var proyectoId = ApiComun.Numero(f, "proyectoId");
        if (id < 1 || proyectoId < 1) return Results.BadRequest();
        var redirigir = $"/Backlog/Index?proyectoId={proyectoId}";
        await using var c = await db.CrearConexionAbiertaAsync();
        if (!await PuedeAcceder(ctx, c, proyectoId)) return Results.Forbid();
        await using var tx = await c.BeginTransactionAsync();
        try
        {
            var vinculada = await c.ExecuteScalarAsync<int>(@"
                select (select count(*) from SprintHistorias where IdHistoria=@id)
                + (select count(*) from Aprobaciones where IdHistoria=@id)", new { id }, tx);
            if (vinculada > 0)
                throw new InvalidOperationException("La historia tiene Sprints o aprobaciones; no se puede eliminar.");
            // Verificar el proyecto ANTES de borrar sus criterios.
            var pertenece = await c.ExecuteScalarAsync<int>(@"
                select count(*) from HistoriasUsuario where IdHistoria=@id and IdProyecto=@proyectoId",
                new { id, proyectoId }, tx);
            if (pertenece == 0) throw new InvalidOperationException("Historia no encontrada.");
            await c.ExecuteAsync("delete from CriteriosAceptacion where IdHistoria=@id", new { id }, tx);
            await c.ExecuteAsync(@"delete from HistoriasUsuario where IdHistoria=@id and IdProyecto=@proyectoId",
                new { id, proyectoId }, tx);
            await c.ExecuteAsync(@"
                insert into Auditoria (IdUsuario,Accion,Entidad,IdRegistro,Detalle)
                values (@u,'ELIMINAR','HistoriasUsuario',@id,'Eliminación de historia sin dependencias')",
                new { u = ApiComun.UsuarioId(ctx), id }, tx);
            await tx.CommitAsync();
            ApiComun.Mensaje(ctx, "Mensaje", "Historia eliminada.");
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync();
            ApiComun.Mensaje(ctx, "Error", ex.Message);
        }
        return Results.Redirect(redirigir);
    }
}
