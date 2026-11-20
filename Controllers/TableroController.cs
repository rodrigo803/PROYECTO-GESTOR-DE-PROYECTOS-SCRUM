using Dapper;
using Gestor_proyectos_scrum.Data;
using Gestor_proyectos_scrum.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gestor_proyectos_scrum.Controllers;
[Authorize]
public class TableroController:Controller
{
 private readonly ConexionDb _db;
 public TableroController(ConexionDb db)=>_db=db;
 private int UsuarioId=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 private bool EsAdmin=>User.IsInRole("Administrador");
 private async Task<bool> AccesoTarea(MySqlConnector.MySqlConnection c,int id,MySqlConnector.MySqlTransaction? tx=null)
 => EsAdmin || await c.ExecuteScalarAsync<int>(@"select count(*) from Tareas t join SprintHistorias sh on sh.IdSprintHistoria=t.IdSprintHistoria
 join Sprints s on s.IdSprint=sh.IdSprint join ProyectoMiembros pm on pm.IdProyecto=s.IdProyecto and pm.IdUsuario=@u where t.IdTarea=@id",new{u=UsuarioId,id},tx)>0;
 [HttpGet]
 public async Task<IActionResult> Index(int? sprintId)
 {
  await using var c=await _db.CrearConexionAbiertaAsync();
  var m=new TableroVista();
  m.Sprints=(await c.QueryAsync<SprintOpcion>(@"select s.IdSprint,s.IdProyecto,concat(p.Nombre,' · ',s.Nombre) as Nombre,s.Estado,s.FechaInicio,s.FechaFin from Sprints s join Proyectos p on p.IdProyecto=s.IdProyecto
  where @admin=1 or exists(select 1 from ProyectoMiembros pm where pm.IdProyecto=s.IdProyecto and pm.IdUsuario=@u) order by (s.Estado='Activo') desc,s.FechaInicio desc",new{admin=EsAdmin?1:0,u=UsuarioId})).ToList();
  m.SprintId=m.Sprints.Any(x=>x.IdSprint==sprintId)?sprintId!.Value:m.Sprints.FirstOrDefault()?.IdSprint??0;
  if (m.SprintId>0){
    m.Tareas=(await c.QueryAsync<TareaKanban>(@"select t.*,sh.IdSprintHistoria,h.IdHistoria,h.Codigo,h.Como,h.Quiero,h.Para,h.Prioridad,h.Estimacion,
       concat(u.Nombre,' ',u.Apellidos) as Responsable,
       (select count(*) from Tareas sub where sub.IdSprintHistoria=t.IdSprintHistoria) as TotalTareas,
       (select count(*) from Tareas sub where sub.IdSprintHistoria=t.IdSprintHistoria and sub.Estado='Finalizado') as TareasTerminadas
       from Tareas t join SprintHistorias sh on sh.IdSprintHistoria=t.IdSprintHistoria
       join HistoriasUsuario h on h.IdHistoria=sh.IdHistoria
       left join Usuarios u on u.IdUsuario=t.IdResponsable
       where sh.IdSprint=@s order by h.OrdenPrioridad,t.IdTarea",new{s=m.SprintId})).ToList();
    var projectId=m.Sprints.Single(x=>x.IdSprint==m.SprintId).IdProyecto;
    m.Responsables=(await c.QueryAsync<ResponsableOpcion>(@"select distinct u.IdUsuario,concat(u.Nombre,' ',u.Apellidos) as Nombre from Usuarios u
         join ProyectoMiembros pm on pm.IdUsuario=u.IdUsuario where pm.IdProyecto=@p and u.Activo=1 order by Nombre",new{p=projectId})).ToList();
  }
  return View(m);
 }
 [HttpPost, ValidateAntiForgeryToken]
 public async Task<IActionResult> Asignar([FromForm] AsignarTareaDto dto)
 {
  if(dto.IdTarea<=0) return BadRequest(new{ok=false,mensaje="Tarea inválida."});
  await using var c=await _db.CrearConexionAbiertaAsync();
  if(!await AccesoTarea(c,dto.IdTarea)) return Forbid();
  if(dto.IdResponsable.HasValue){
   var valido=await c.ExecuteScalarAsync<int>(@"select count(*) from Tareas t join SprintHistorias sh on sh.IdSprintHistoria=t.IdSprintHistoria
        join Sprints s on s.IdSprint=sh.IdSprint join ProyectoMiembros pm on pm.IdProyecto=s.IdProyecto and pm.IdUsuario=@responsable
        join Usuarios u on u.IdUsuario=pm.IdUsuario and u.Activo=1 where t.IdTarea=@t",new{responsable=dto.IdResponsable,t=dto.IdTarea});
   if(valido==0) return BadRequest(new{ok=false,mensaje="El responsable debe pertenecer al proyecto."});
  }
  await c.ExecuteAsync("update Tareas set IdResponsable=@responsable where IdTarea=@id",new{responsable=dto.IdResponsable,id=dto.IdTarea});
  await c.ExecuteAsync("insert into Auditoria (IdUsuario,Accion,Entidad,IdRegistro,Detalle) values (@u,'ASIGNAR','Tareas',@id,'Asignación o retiro de responsable')",new{u=UsuarioId,id=dto.IdTarea});
  return Json(new{ok=true});
 }
 [HttpPost, ValidateAntiForgeryToken]
 public async Task<IActionResult> Mover([FromForm] MoverTareaDto dto)
 {
  if(!ModelState.IsValid || !new[]{"Pendiente","En progreso","En revisión","Finalizado"}.Contains(dto.EstadoNuevo))
      return BadRequest(new{ok=false,mensaje="Estado, porcentaje u horas no válidos."});
  if(dto.EstadoNuevo=="Finalizado" && dto.PorcentajeNuevo!=100)
      return BadRequest(new{ok=false,mensaje="Para finalizar una tarea, su avance debe ser 100%."});
  await using var c=await _db.CrearConexionAbiertaAsync();
  await using var tx=await c.BeginTransactionAsync();
  try {
    if(!await AccesoTarea(c,dto.IdTarea,tx)) return Forbid();
    var actual=await c.QuerySingleOrDefaultAsync<(int PorcentajeAvance,string Estado)>("select PorcentajeAvance,Estado from Tareas where IdTarea=@id for update",new{id=dto.IdTarea},tx);
    if(string.IsNullOrEmpty(actual.Estado)) return NotFound(new{ok=false,mensaje="Tarea no encontrada."});
    if(dto.PorcentajeNuevo<actual.PorcentajeAvance && string.IsNullOrWhiteSpace(dto.Justificacion))
      return BadRequest(new{ok=false,mensaje="Debes justificar un retroceso de porcentaje."});
    if(dto.PorcentajeNuevo==actual.PorcentajeAvance && dto.EstadoNuevo==actual.Estado && dto.HorasTrabajadas==0)
      return Json(new{ok=true});
    await c.ExecuteAsync(@"insert into AvanceTareas (IdTarea,IdUsuario,PorcentajeAnterior,PorcentajeNuevo,HorasTrabajadas,EstadoAnterior,EstadoNuevo,Justificacion)
      values (@id,@usuario,@anterior,@nuevo,@horas,@estado,@estadoNuevo,@justificacion)",new{id=dto.IdTarea,usuario=UsuarioId,anterior=actual.PorcentajeAvance,nuevo=dto.PorcentajeNuevo,horas=dto.HorasTrabajadas,estado=actual.Estado,estadoNuevo=dto.EstadoNuevo,justificacion=string.IsNullOrWhiteSpace(dto.Justificacion)?null:dto.Justificacion},tx);
    await c.ExecuteAsync("insert into Auditoria (IdUsuario,Accion,Entidad,IdRegistro,Detalle) values (@u,'AVANCE','Tareas',@id,@detalle)",new{u=UsuarioId,id=dto.IdTarea,detalle=$"{actual.Estado} → {dto.EstadoNuevo} ({actual.PorcentajeAvance}% → {dto.PorcentajeNuevo}%)"},tx);
    await tx.CommitAsync();return Json(new{ok=true});
  }catch(Exception){await tx.RollbackAsync();return StatusCode(500,new{ok=false,mensaje="No se pudo registrar el movimiento en MySQL."});}
 }
}
