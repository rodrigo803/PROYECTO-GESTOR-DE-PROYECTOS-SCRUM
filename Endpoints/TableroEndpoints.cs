using Dapper;
using Gestor_proyectos_scrum.Data;
using Gestor_proyectos_scrum.Models;
using Gestor_proyectos_scrum.Services;
using Microsoft.AspNetCore.Antiforgery;
using MySqlConnector;

namespace Gestor_proyectos_scrum.Endpoints;

/// <summary>HU-08: Kanban con Minimal APIs y persistencia en MySQL.</summary>
public static class TableroEndpoints
{
    private static readonly string[] Estados = { "Pendiente", "En progreso", "En revisión", "Finalizado" };

    public static void Mapear(WebApplication app)
    {
        var g = app.MapGroup("/Tablero").RequireAuthorization().WithTags("HU-08 Tablero Kanban");
        g.MapGet("/Index", Index);
        g.MapPost("/Asignar", Asignar);
        g.MapPost("/Mover", Mover);
    }

    private static async Task<bool> AccesoTarea(HttpContext ctx, MySqlConnection c, int id, MySqlTransaction? tx = null) =>
        ctx.User.IsInRole("Administrador") || await c.ExecuteScalarAsync<int>(@"
            select count(*) from Tareas t
            join SprintHistorias sh on sh.IdSprintHistoria=t.IdSprintHistoria
            join Sprints s on s.IdSprint=sh.IdSprint
            join ProyectoMiembros pm on pm.IdProyecto=s.IdProyecto and pm.IdUsuario=@u
            where t.IdTarea=@id", new { u = ApiComun.UsuarioId(ctx), id }, tx) > 0;

    private static async Task<IResult> Index(HttpContext ctx, RazorVista vistas, ConexionDb db, int? sprintId)
    {
        await using var c = await db.CrearConexionAbiertaAsync();
        var vm = new TableroVista();
        vm.Sprints = (await c.QueryAsync<SprintOpcion>(@"
            select s.IdSprint,s.IdProyecto,concat(p.Nombre,' · ',s.Nombre) as Nombre,
                s.Estado,s.FechaInicio,s.FechaFin from Sprints s
            join Proyectos p on p.IdProyecto=s.IdProyecto
            where @admin=1 or exists(select 1 from ProyectoMiembros pm
                where pm.IdProyecto=s.IdProyecto and pm.IdUsuario=@u)
            order by (s.Estado='Activo') desc,s.FechaInicio desc",
            new { admin = ctx.User.IsInRole("Administrador") ? 1 : 0, u = ApiComun.UsuarioId(ctx) })).ToList();
        vm.SprintId = vm.Sprints.Any(s => s.IdSprint == sprintId) ? sprintId!.Value : vm.Sprints.FirstOrDefault()?.IdSprint ?? 0;
        if (vm.SprintId > 0)
        {
            vm.Tareas = (await c.QueryAsync<TareaKanban>(@"
                select t.*,sh.IdSprintHistoria,h.IdHistoria,h.Codigo,h.Como,h.Quiero,h.Para,
                    h.Prioridad,h.Estimacion,concat(u.Nombre,' ',u.Apellidos) as Responsable,
                    (select count(*) from Tareas sub where sub.IdSprintHistoria=t.IdSprintHistoria) as TotalTareas,
                    (select count(*) from Tareas sub where sub.IdSprintHistoria=t.IdSprintHistoria
                        and sub.Estado='Finalizado') as TareasTerminadas
                from Tareas t join SprintHistorias sh on sh.IdSprintHistoria=t.IdSprintHistoria
                join HistoriasUsuario h on h.IdHistoria=sh.IdHistoria
                left join Usuarios u on u.IdUsuario=t.IdResponsable
                where sh.IdSprint=@s order by h.OrdenPrioridad,t.IdTarea", new { s = vm.SprintId })).ToList();
            var proyectoId = vm.Sprints.Single(x => x.IdSprint == vm.SprintId).IdProyecto;
            vm.Responsables = (await c.QueryAsync<ResponsableOpcion>(@"
                select distinct u.IdUsuario,concat(u.Nombre,' ',u.Apellidos) as Nombre
                from Usuarios u join ProyectoMiembros pm on pm.IdUsuario=u.IdUsuario
                where pm.IdProyecto=@p and u.Activo=1 order by Nombre", new { p = proyectoId })).ToList();
        }
        return await vistas.Mostrar(ctx, "Tablero", "Index", vm);
    }

    private static async Task<IResult> Asignar(HttpContext ctx, ConexionDb db, IAntiforgery antiforgery)
    {
        if (!await ApiComun.TokenValido(ctx, antiforgery)) return Results.BadRequest(new { ok = false, mensaje = "Token de seguridad inválido." });
        var form = await ctx.Request.ReadFormAsync();
        var id = ApiComun.Numero(form, "IdTarea");
        var responsable = ApiComun.NumeroOpcional(form, "IdResponsable");
        if (id <= 0 || responsable is <= 0)
            return Results.BadRequest(new { ok = false, mensaje = "Datos de la tarea inválidos." });
        await using var c = await db.CrearConexionAbiertaAsync();
        if (!await AccesoTarea(ctx, c, id)) return Results.Forbid();
        if (responsable.HasValue)
        {
            var miembro = await c.ExecuteScalarAsync<int>(@"
                select count(*) from Tareas t
                join SprintHistorias sh on sh.IdSprintHistoria=t.IdSprintHistoria
                join Sprints s on s.IdSprint=sh.IdSprint
                join ProyectoMiembros pm on pm.IdProyecto=s.IdProyecto and pm.IdUsuario=@responsable
                join Usuarios u on u.IdUsuario=pm.IdUsuario and u.Activo=1
                where t.IdTarea=@id", new { responsable, id });
            if (miembro == 0)
                return Results.BadRequest(new { ok = false, mensaje = "El responsable debe pertenecer al proyecto." });
        }
        await using var tx = await c.BeginTransactionAsync();
        try
        {
            var n = await c.ExecuteAsync(@"update Tareas set IdResponsable=@responsable where IdTarea=@id",
                new { responsable, id }, tx);
            if (n == 0) return Results.NotFound(new { ok = false, mensaje = "Tarea no encontrada." });
            await c.ExecuteAsync(@"
                insert into Auditoria (IdUsuario,Accion,Entidad,IdRegistro,Detalle)
                values (@u,'ASIGNAR','Tareas',@id,'Asignación o retiro de responsable')",
                new { u = ApiComun.UsuarioId(ctx), id }, tx);
            await tx.CommitAsync();
            return Results.Json(new { ok = true });
        }
        catch
        {
            await tx.RollbackAsync();
            return Results.Problem("No se pudo asignar la tarea.", statusCode: 500);
        }
    }

    private static async Task<IResult> Mover(HttpContext ctx, ConexionDb db, IAntiforgery antiforgery)
    {
        if (!await ApiComun.TokenValido(ctx, antiforgery))
            return Results.BadRequest(new { ok = false, mensaje = "Token de seguridad inválido." });
        var f = await ctx.Request.ReadFormAsync();
        var dto = new MoverTareaDto
        {
            IdTarea = ApiComun.Numero(f, "IdTarea"),
            EstadoNuevo = ApiComun.Campo(f, "EstadoNuevo"),
            PorcentajeNuevo = ApiComun.Numero(f, "PorcentajeNuevo"),
            HorasTrabajadas = ApiComun.Decimal(f, "HorasTrabajadas"),
            Justificacion = ApiComun.Campo(f, "Justificacion")
        };
        var (valido, _) = ApiComun.Validar(dto);
        if (!valido || !Estados.Contains(dto.EstadoNuevo))
            return Results.BadRequest(new { ok = false, mensaje = "Estado, porcentaje u horas no válidos." });
        if (dto.EstadoNuevo == "Finalizado" && dto.PorcentajeNuevo != 100)
            return Results.BadRequest(new { ok = false, mensaje = "Para finalizar, el avance debe ser del 100%." });
        await using var c = await db.CrearConexionAbiertaAsync();
        if (!await AccesoTarea(ctx, c, dto.IdTarea)) return Results.Forbid();
        await using var tx = await c.BeginTransactionAsync();
        try
        {
            var actual = await c.QuerySingleOrDefaultAsync<EstadoActual>(@"
                select PorcentajeAvance,Estado from Tareas where IdTarea=@id for update", new { id = dto.IdTarea }, tx);
            if (actual == null)
                return Results.NotFound(new { ok = false, mensaje = "Tarea no encontrada." });
            if (dto.PorcentajeNuevo < actual.PorcentajeAvance && string.IsNullOrWhiteSpace(dto.Justificacion))
                return Results.BadRequest(new { ok = false, mensaje = "Debes justificar un retroceso de porcentaje." });
            if (dto.PorcentajeNuevo == actual.PorcentajeAvance && dto.EstadoNuevo == actual.Estado && dto.HorasTrabajadas == 0)
                return Results.Json(new { ok = true });
            await c.ExecuteAsync(@"
                insert into AvanceTareas
                (IdTarea,IdUsuario,PorcentajeAnterior,PorcentajeNuevo,HorasTrabajadas,EstadoAnterior,EstadoNuevo,Justificacion)
                values (@id,@usuario,@anterior,@nuevo,@horas,@estado,@estadoNuevo,@justificacion)",
                new
                {
                    id = dto.IdTarea, usuario = ApiComun.UsuarioId(ctx), anterior = actual.PorcentajeAvance,
                    nuevo = dto.PorcentajeNuevo, horas = dto.HorasTrabajadas, estado = actual.Estado,
                    estadoNuevo = dto.EstadoNuevo,
                    justificacion = string.IsNullOrWhiteSpace(dto.Justificacion) ? null : dto.Justificacion
                }, tx);
            await c.ExecuteAsync(@"
                insert into Auditoria (IdUsuario,Accion,Entidad,IdRegistro,Detalle)
                values (@u,'AVANCE','Tareas',@id,@detalle)",
                new
                {
                    u = ApiComun.UsuarioId(ctx), id = dto.IdTarea,
                    detalle = $"{actual.Estado} → {dto.EstadoNuevo} ({actual.PorcentajeAvance}% → {dto.PorcentajeNuevo}%)"
                }, tx);
            await tx.CommitAsync();
            return Results.Json(new { ok = true });
        }
        catch
        {
            await tx.RollbackAsync();
            return Results.Json(new { ok = false, mensaje = "No se pudo registrar el movimiento en MySQL." }, statusCode: 500);
        }
    }

    private sealed class EstadoActual
    {
        public int PorcentajeAvance { get; set; }
        public string Estado { get; set; } = "";
    }
}
