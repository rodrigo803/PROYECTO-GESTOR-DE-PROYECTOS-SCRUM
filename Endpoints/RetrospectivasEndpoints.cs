using Dapper;
using Gestor_proyectos_scrum.Data;
using Gestor_proyectos_scrum.Models;
using Gestor_proyectos_scrum.Services;
using Microsoft.AspNetCore.Antiforgery;

namespace Gestor_proyectos_scrum.Endpoints;

/// <summary>HU-11: retrospectivas y acuerdos mediante Minimal APIs.</summary>
public static class RetrospectivasEndpoints
{
    public static void Mapear(WebApplication app)
    {
        var g = app.MapGroup("/Retrospectivas").RequireAuthorization().WithTags("HU-11 Retrospectivas");
        g.MapGet("/Index", Index);
        g.MapPost("/Guardar", Guardar);
    }

    private static bool PuedeRegistrar(HttpContext ctx) =>
        ctx.User.IsInRole("Administrador") || ctx.User.IsInRole("Scrum Master");

    private static async Task<IResult> Index(HttpContext ctx, RazorVista vistas, ConexionDb db, int? sprintId)
    {
        await using var c = await db.CrearConexionAbiertaAsync();
        var vm = new RetroVista();
        vm.Sprints = (await c.QueryAsync<SprintOpcion>(@"
            select s.* from Sprints s where @admin=1
            or exists(select 1 from ProyectoMiembros pm where pm.IdProyecto=s.IdProyecto and pm.IdUsuario=@u)
            order by s.FechaInicio desc", new { admin = ctx.User.IsInRole("Administrador") ? 1 : 0, u = ApiComun.UsuarioId(ctx) })).ToList();
        vm.Formulario.IdSprint = vm.Sprints.Any(x => x.IdSprint == sprintId)
            ? sprintId!.Value : vm.Sprints.FirstOrDefault()?.IdSprint ?? 0;
        // Lista de responsables disponible únicamente dentro del proyecto del Sprint seleccionado.
        var proyectoId = vm.Sprints.FirstOrDefault(s => s.IdSprint == vm.Formulario.IdSprint)?.IdProyecto ?? 0;
        vm.Responsables = (await c.QueryAsync<ResponsableOpcion>(@"
            select distinct u.IdUsuario,concat(u.Nombre,' ',u.Apellidos) as Nombre
            from Usuarios u join ProyectoMiembros pm on pm.IdUsuario=u.IdUsuario
            where pm.IdProyecto=@p and u.Activo=1 order by Nombre", new { p = proyectoId })).ToList();
        vm.Anteriores = (await c.QueryAsync<RetroListado>(@"
            select r.IdRetrospectiva,r.IdSprint,s.Nombre,r.Fecha,
                concat(u.Nombre,' ',u.Apellidos) as Facilitador,
                (select group_concat(Descripcion separator ' / ') from ElementosRetrospectiva e
                    where e.IdRetrospectiva=r.IdRetrospectiva and e.Tipo='Positivo') as Positivo,
                (select group_concat(Descripcion separator ' / ') from ElementosRetrospectiva e
                    where e.IdRetrospectiva=r.IdRetrospectiva and e.Tipo='Mejora') as Mejora,
                (select group_concat(Descripcion separator ' / ') from ElementosRetrospectiva e
                    where e.IdRetrospectiva=r.IdRetrospectiva and e.Tipo='Acción') as Accion,
                (select max(Estado) from ElementosRetrospectiva e
                    where e.IdRetrospectiva=r.IdRetrospectiva and e.Tipo='Acción') as EstadoAccion
            from Retrospectivas r join Sprints s on s.IdSprint=r.IdSprint
            join Usuarios u on u.IdUsuario=r.IdFacilitador
            where @admin=1 or exists(select 1 from ProyectoMiembros pm
                where pm.IdProyecto=s.IdProyecto and pm.IdUsuario=@u)
            order by r.Fecha desc", new { admin = ctx.User.IsInRole("Administrador") ? 1 : 0, u = ApiComun.UsuarioId(ctx) })).ToList();
        return await vistas.Mostrar(ctx, "Retrospectivas", "Index", vm,
            new Dictionary<string, object?> { ["PuedeRegistrar"] = PuedeRegistrar(ctx) });
    }

    private static async Task<IResult> Guardar(HttpContext ctx, ConexionDb db, IAntiforgery antiforgery)
    {
        if (!await ApiComun.TokenValido(ctx, antiforgery)) return ApiComun.ErrorToken();
        if (!PuedeRegistrar(ctx)) return Results.Forbid();
        var f = await ctx.Request.ReadFormAsync();
        const string prefijo = "Formulario.";
        var entrada = new RetroEntrada
        {
            IdSprint = ApiComun.Numero(f, "IdSprint", prefijo),
            Fecha = ApiComun.Fecha(f, "Fecha", prefijo),
            Positivo = ApiComun.Campo(f, "Positivo", prefijo),
            Mejora = ApiComun.Campo(f, "Mejora", prefijo),
            Accion = ApiComun.Campo(f, "Accion", prefijo),
            IdResponsable = ApiComun.NumeroOpcional(f, "IdResponsable", prefijo),
            FechaCompromiso = ApiComun.FechaOpcional(f, "FechaCompromiso", prefijo),
            Estado = ApiComun.Campo(f, "Estado", prefijo)
        };
        var redirigir = $"/Retrospectivas/Index?sprintId={entrada.IdSprint}";
        var (valido, _) = ApiComun.Validar(entrada);
        if (!valido || entrada.IdSprint < 1 || entrada.Fecha == default ||
            !new[] { "Pendiente", "En curso", "Cumplida" }.Contains(entrada.Estado))
        {
            ApiComun.Mensaje(ctx, "Error", "Completa los campos y elige un estado válido (máximo 300 caracteres por campo).");
            return Results.Redirect(redirigir);
        }
        await using var c = await db.CrearConexionAbiertaAsync();
        var acceso = await c.ExecuteScalarAsync<int>(@"
            select count(*) from Sprints s
            where s.IdSprint=@s and s.Estado='Cerrado' and
            (@admin=1 or exists(select 1 from ProyectoMiembros pm
                where pm.IdProyecto=s.IdProyecto and pm.IdUsuario=@u and pm.RolProyecto='Scrum Master'))",
            new { s = entrada.IdSprint, admin = ctx.User.IsInRole("Administrador") ? 1 : 0, u = ApiComun.UsuarioId(ctx) });
        if (acceso == 0)
        {
            ApiComun.Mensaje(ctx, "Error", "Solo el Scrum Master asignado o el Administrador pueden registrar retrospectivas de Sprints cerrados.");
            return Results.Redirect(redirigir);
        }
        if (entrada.IdResponsable.HasValue)
        {
            var miembro = await c.ExecuteScalarAsync<int>(@"
                select count(*) from ProyectoMiembros pm
                join Sprints s on s.IdProyecto=pm.IdProyecto
                join Usuarios u on u.IdUsuario=pm.IdUsuario and u.Activo=1
                where s.IdSprint=@s and pm.IdUsuario=@r",
                new { s = entrada.IdSprint, r = entrada.IdResponsable });
            if (miembro == 0)
            {
                ApiComun.Mensaje(ctx, "Error", "El responsable debe pertenecer al proyecto.");
                return Results.Redirect(redirigir);
            }
        }
        await using var tx = await c.BeginTransactionAsync();
        try
        {
            var existente = await c.ExecuteScalarAsync<int?>(@"
                select IdRetrospectiva from Retrospectivas where IdSprint=@s for update",
                new { s = entrada.IdSprint }, tx);
            int id;
            if (existente.HasValue)
            {
                id = existente.Value;
                await c.ExecuteAsync(@"update Retrospectivas set Fecha=@fecha where IdRetrospectiva=@id",
                    new { fecha = entrada.Fecha, id }, tx);
                await c.ExecuteAsync("delete from ElementosRetrospectiva where IdRetrospectiva=@id", new { id }, tx);
            }
            else
            {
                await c.ExecuteAsync(@"
                    insert into Retrospectivas (IdSprint,Fecha,IdFacilitador) values (@s,@fecha,@usuario)",
                    new { s = entrada.IdSprint, fecha = entrada.Fecha, usuario = ApiComun.UsuarioId(ctx) }, tx);
                id = await c.ExecuteScalarAsync<int>("select last_insert_id()", transaction: tx);
            }
            await c.ExecuteAsync(@"
                insert into ElementosRetrospectiva (IdRetrospectiva,Tipo,Descripcion)
                values (@id,'Positivo',@texto)", new { id, texto = entrada.Positivo }, tx);
            await c.ExecuteAsync(@"
                insert into ElementosRetrospectiva (IdRetrospectiva,Tipo,Descripcion)
                values (@id,'Mejora',@texto)", new { id, texto = entrada.Mejora }, tx);
            await c.ExecuteAsync(@"
                insert into ElementosRetrospectiva
                (IdRetrospectiva,Tipo,Descripcion,IdResponsable,FechaCompromiso,Estado)
                values (@id,'Acción',@texto,@responsable,@fecha,@estado)",
                new
                {
                    id, texto = entrada.Accion, responsable = entrada.IdResponsable,
                    fecha = entrada.FechaCompromiso, estado = entrada.Estado
                }, tx);
            await c.ExecuteAsync(@"
                insert into Auditoria (IdUsuario,Accion,Entidad,IdRegistro,Detalle)
                values (@usuario,'GUARDAR','Retrospectivas',@id,'Registro de retrospectiva y acuerdos')",
                new { usuario = ApiComun.UsuarioId(ctx), id }, tx);
            await tx.CommitAsync();
            ApiComun.Mensaje(ctx, "Mensaje", "Retrospectiva guardada en MySQL.");
        }
        catch
        {
            await tx.RollbackAsync();
            ApiComun.Mensaje(ctx, "Error", "No se pudo guardar la retrospectiva. Revisa los datos.");
        }
        return Results.Redirect(redirigir);
    }
}
