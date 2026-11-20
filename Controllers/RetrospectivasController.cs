using Dapper;
using Gestor_proyectos_scrum.Data;
using Gestor_proyectos_scrum.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gestor_proyectos_scrum.Controllers;
[Authorize]
public class RetrospectivasController : Controller
{
 private readonly ConexionDb _db;
 public RetrospectivasController(ConexionDb db)=>_db=db;
 private int UsuarioId=>int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 private bool PuedeRegistrar=>User.IsInRole("Administrador")||User.IsInRole("Scrum Master");
 [HttpGet] public async Task<IActionResult> Index(int? sprintId) {
   await using var c=await _db.CrearConexionAbiertaAsync();
   var vm=new RetroVista();
   vm.Sprints=(await c.QueryAsync<SprintOpcion>(@"select s.* from Sprints s where @admin=1 or exists
    (select 1 from ProyectoMiembros pm where pm.IdProyecto=s.IdProyecto and pm.IdUsuario=@u) order by s.FechaInicio desc",new{admin=User.IsInRole("Administrador")?1:0,u=UsuarioId})).ToList();
   vm.Formulario.IdSprint=vm.Sprints.Any(x=>x.IdSprint==sprintId)?sprintId!.Value:vm.Sprints.FirstOrDefault()?.IdSprint??0;
   vm.Responsables=(await c.QueryAsync<ResponsableOpcion>(@"select IdUsuario,concat(Nombre,' ',Apellidos) as Nombre from Usuarios where Activo=1 order by Nombre")).ToList();
   vm.Anteriores=(await c.QueryAsync<RetroListado>(@"select r.IdRetrospectiva,r.IdSprint,s.Nombre,r.Fecha,
   concat(u.Nombre,' ',u.Apellidos) as Facilitador,
   (select group_concat(Descripcion separator ' / ') from ElementosRetrospectiva e where e.IdRetrospectiva=r.IdRetrospectiva and e.Tipo='Positivo') as Positivo,
   (select group_concat(Descripcion separator ' / ') from ElementosRetrospectiva e where e.IdRetrospectiva=r.IdRetrospectiva and e.Tipo='Mejora') as Mejora,
   (select group_concat(Descripcion separator ' / ') from ElementosRetrospectiva e where e.IdRetrospectiva=r.IdRetrospectiva and e.Tipo='Acción') as Accion,
   (select max(Estado) from ElementosRetrospectiva e where e.IdRetrospectiva=r.IdRetrospectiva and e.Tipo='Acción') as EstadoAccion
   from Retrospectivas r join Sprints s on s.IdSprint=r.IdSprint join Usuarios u on u.IdUsuario=r.IdFacilitador
   where @admin=1 or exists(select 1 from ProyectoMiembros pm where pm.IdProyecto=s.IdProyecto and pm.IdUsuario=@u)
   order by r.Fecha desc",new{admin=User.IsInRole("Administrador")?1:0,u=UsuarioId})).ToList();
   ViewBag.PuedeRegistrar=PuedeRegistrar;
   return View(vm);
 }
 [HttpPost,ValidateAntiForgeryToken] public async Task<IActionResult> Guardar([Bind(Prefix="Formulario")]RetroEntrada entrada) {
   if(!PuedeRegistrar)return Forbid();
   if(!new[]{"Pendiente","En curso","Cumplida"}.Contains(entrada.Estado))ModelState.AddModelError("","Estado de acción no válido.");
   if(entrada.IdSprint<=0||entrada.Fecha==default)ModelState.AddModelError("","Selecciona un Sprint y fecha válida.");
   if(!ModelState.IsValid) {TempData["Error"]="Completa los tres campos de la retrospectiva (máximo 300 caracteres por campo).";return RedirectToAction("Index",new{sprintId=entrada.IdSprint});}
   await using var c=await _db.CrearConexionAbiertaAsync();
   var acceso=await c.ExecuteScalarAsync<int>(@"select count(*) from Sprints s where s.IdSprint=@s and s.Estado='Cerrado' and
      (@admin=1 or exists(select 1 from ProyectoMiembros pm where pm.IdProyecto=s.IdProyecto and pm.IdUsuario=@u and pm.RolProyecto='Scrum Master'))",new{s=entrada.IdSprint,admin=User.IsInRole("Administrador")?1:0,u=UsuarioId});
   if(acceso==0){TempData["Error"]="Solo el Scrum Master del proyecto o un Administrador puede registrar una retrospectiva de un Sprint cerrado.";return RedirectToAction("Index",new{sprintId=entrada.IdSprint});}
   if(entrada.IdResponsable.HasValue) {
      var esMiembro=await c.ExecuteScalarAsync<int>(@"select count(*) from ProyectoMiembros pm join Sprints s on s.IdProyecto=pm.IdProyecto where s.IdSprint=@s and pm.IdUsuario=@r",new{s=entrada.IdSprint,r=entrada.IdResponsable});
      if(esMiembro==0){TempData["Error"]="Selecciona un responsable que pertenezca al proyecto.";return RedirectToAction("Index",new{sprintId=entrada.IdSprint});}
   }
   await using var tx=await c.BeginTransactionAsync();
   try{
     var existe=await c.ExecuteScalarAsync<int?>("select IdRetrospectiva from Retrospectivas where IdSprint=@s for update",new{s=entrada.IdSprint},tx);
     int retroId;
     if(existe.HasValue){retroId=existe.Value;
        await c.ExecuteAsync("update Retrospectivas set Fecha=@fecha where IdRetrospectiva=@id",new{fecha=entrada.Fecha,id=retroId},tx);
        await c.ExecuteAsync("delete from ElementosRetrospectiva where IdRetrospectiva=@id",new{id=retroId},tx);
     } else {
        await c.ExecuteAsync("insert into Retrospectivas (IdSprint,Fecha,IdFacilitador) values (@s,@fecha,@u)",new{s=entrada.IdSprint,fecha=entrada.Fecha,u=UsuarioId},tx);
        retroId=await c.ExecuteScalarAsync<int>("select last_insert_id()",transaction:tx);
     }
     await c.ExecuteAsync("insert into ElementosRetrospectiva (IdRetrospectiva,Tipo,Descripcion) values (@id,'Positivo',@desc)",new{id=retroId,desc=entrada.Positivo.Trim()},tx);
     await c.ExecuteAsync("insert into ElementosRetrospectiva (IdRetrospectiva,Tipo,Descripcion) values (@id,'Mejora',@desc)",new{id=retroId,desc=entrada.Mejora.Trim()},tx);
     await c.ExecuteAsync(@"insert into ElementosRetrospectiva (IdRetrospectiva,Tipo,Descripcion,IdResponsable,FechaCompromiso,Estado)
       values (@id,'Acción',@desc,@responsable,@fecha,@estado)",new{id=retroId,desc=entrada.Accion.Trim(),responsable=entrada.IdResponsable,fecha=entrada.FechaCompromiso,estado=entrada.Estado},tx);
     await c.ExecuteAsync("insert into Auditoria (IdUsuario,Accion,Entidad,IdRegistro,Detalle) values (@u,'GUARDAR','Retrospectivas',@id,'Registro de retrospectiva y acuerdos')",new{u=UsuarioId,id=retroId},tx);
     await tx.CommitAsync(); TempData["Mensaje"]="Retrospectiva guardada en MySQL.";
   }catch(Exception){await tx.RollbackAsync();TempData["Error"]="No se pudo guardar la retrospectiva. Revisa los datos.";}
   return RedirectToAction("Index",new{sprintId=entrada.IdSprint});
 }
}
