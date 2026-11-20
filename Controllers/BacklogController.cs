using Dapper;
using Gestor_proyectos_scrum.Data;
using Gestor_proyectos_scrum.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Gestor_proyectos_scrum.Controllers;
[Authorize]
public class BacklogController : Controller
{
 private readonly ConexionDb _db;
 public BacklogController(ConexionDb db)=>_db=db;
 private int UsuarioId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 private bool PuedeEditar => User.IsInRole("Administrador")||User.IsInRole("Product Owner");
 private async Task<bool> PuedeAcceder(MySqlConnector.MySqlConnection c,int proyectoId) => User.IsInRole("Administrador") || await c.ExecuteScalarAsync<int>("select count(*) from ProyectoMiembros where IdProyecto=@proyectoId and IdUsuario=@u",new{proyectoId,u=UsuarioId})>0;
 [HttpGet]
 public async Task<IActionResult> Index(int? proyectoId)
 {
   await using var c=await _db.CrearConexionAbiertaAsync();
   var vm=new BacklogVista();
   vm.Proyectos=(await c.QueryAsync<ProyectoOpcion>(@"select p.IdProyecto,p.Nombre from Proyectos p where @admin=1 or exists(select 1 from ProyectoMiembros pm where pm.IdProyecto=p.IdProyecto and pm.IdUsuario=@u) order by p.Nombre",new{admin=User.IsInRole("Administrador")?1:0,u=UsuarioId})).ToList();
   vm.ProyectoId=vm.Proyectos.Any(x=>x.IdProyecto==proyectoId)?proyectoId!.Value:vm.Proyectos.FirstOrDefault()?.IdProyecto??0;
   if (vm.ProyectoId!=0) {
      vm.Historias=(await c.QueryAsync<HistoriaDto>(@"select h.*,p.Nombre as NombreProyecto,(select count(*) from CriteriosAceptacion ca where ca.IdHistoria=h.IdHistoria) as TotalCriterios,
      (select group_concat(ca.Descripcion separator '\n') from CriteriosAceptacion ca where ca.IdHistoria=h.IdHistoria) as CriteriosTexto
      from HistoriasUsuario h join Proyectos p on p.IdProyecto=h.IdProyecto where h.IdProyecto=@id order by h.OrdenPrioridad,h.IdHistoria",new{id=vm.ProyectoId})).ToList();
   }
   ViewBag.PuedeEditar=PuedeEditar;
   return View(vm);
 }
 [HttpPost, ValidateAntiForgeryToken]
 public async Task<IActionResult> Guardar(HistoriaDto h)
 {
   if (!PuedeEditar) return Forbid();
   if (!new[]{"Alta","Media","Baja"}.Contains(h.Prioridad) || !new[]{"Pendiente","Lista","En Sprint","Terminada"}.Contains(h.Estado))
      ModelState.AddModelError("", "Prioridad o estado no válido.");
   var criterios=(h.CriteriosTexto??"").Split('\n',StringSplitOptions.RemoveEmptyEntries|StringSplitOptions.TrimEntries).Distinct().ToList();
   if (criterios.Any(x=>x.Length>300)) ModelState.AddModelError("","Cada criterio permite hasta 300 caracteres.");
   if ((h.Estado=="Lista"||h.Estado=="En Sprint")&&(criterios.Count==0||h.Estimacion==null)) ModelState.AddModelError("","Para marcar como Lista o En Sprint se requieren estimación y criterios.");
   if (!ModelState.IsValid) {TempData["Error"]=string.Join(" ", ModelState.Values.SelectMany(x=>x.Errors).Select(e=>e.ErrorMessage));return RedirectToAction("Index",new{proyectoId=h.IdProyecto});}
   await using var c=await _db.CrearConexionAbiertaAsync();
   if (!await PuedeAcceder(c,h.IdProyecto)) return Forbid();
   await using var tx=await c.BeginTransactionAsync();
   try {
     if(h.IdHistoria!=0) {
       var existe=await c.ExecuteScalarAsync<int>("select count(*) from HistoriasUsuario where IdHistoria=@id and IdProyecto=@p",new{id=h.IdHistoria,p=h.IdProyecto},tx);
       if(existe==0) {TempData["Error"]="Historia no encontrada.";return RedirectToAction("Index",new{proyectoId=h.IdProyecto});}
       var actuales=(await c.QueryAsync<string>("select Descripcion from CriteriosAceptacion where IdHistoria=@id order by IdCriterio",new{id=h.IdHistoria},tx)).ToList();
       if(!actuales.SequenceEqual(criterios)) {
          var aprobados=await c.ExecuteScalarAsync<int>("select count(*) from Aprobaciones a join CriteriosAceptacion ca on ca.IdCriterio=a.IdCriterio where ca.IdHistoria=@id",new{id=h.IdHistoria},tx);
          if(aprobados>0) throw new InvalidOperationException("No se pueden sustituir criterios que ya tienen aprobaciones registradas.");
          await c.ExecuteAsync("delete from CriteriosAceptacion where IdHistoria=@id",new{id=h.IdHistoria},tx);
       } else criterios.Clear();
       await c.ExecuteAsync(@"update HistoriasUsuario set Codigo=@Codigo,Como=@Como,Quiero=@Quiero,Para=@Para,Prioridad=@Prioridad,OrdenPrioridad=@OrdenPrioridad,Estimacion=@Estimacion,Estado=@Estado where IdHistoria=@IdHistoria and IdProyecto=@IdProyecto",h,tx);
     } else {
       await c.ExecuteAsync(@"insert into HistoriasUsuario (IdProyecto,Codigo,Como,Quiero,Para,Prioridad,OrdenPrioridad,Estimacion,Estado) values (@IdProyecto,@Codigo,@Como,@Quiero,@Para,@Prioridad,@OrdenPrioridad,@Estimacion,@Estado)",h,tx);
       h.IdHistoria=await c.ExecuteScalarAsync<int>("select last_insert_id()", transaction:tx);
     }
     foreach(var cr in criterios) await c.ExecuteAsync("insert into CriteriosAceptacion (IdHistoria,Descripcion) values (@id,@cr)",new{id=h.IdHistoria,cr},tx);
     await c.ExecuteAsync("insert into Auditoria (IdUsuario,Accion,Entidad,IdRegistro,Detalle) values (@u,@accion,'HistoriasUsuario',@id,@codigo)",new{u=UsuarioId,accion=h.IdHistoria>0?"GUARDAR":"CREAR",id=h.IdHistoria,codigo=h.Codigo},tx);
     await tx.CommitAsync(); TempData["Mensaje"]="Historia guardada en MySQL.";
   } catch(Exception ex) {await tx.RollbackAsync();TempData["Error"]=ex is MySqlConnector.MySqlException ? "No se pudo guardar: verifica código duplicado y relaciones de la historia." : ex.Message;}
   return RedirectToAction("Index",new{proyectoId=h.IdProyecto});
 }
 [HttpPost, ValidateAntiForgeryToken]
 public async Task<IActionResult> Eliminar(int id,int proyectoId) {
   if (!PuedeEditar) return Forbid();
   await using var c=await _db.CrearConexionAbiertaAsync();
   if (!await PuedeAcceder(c,proyectoId)) return Forbid();
   await using var tx=await c.BeginTransactionAsync();
   try {
     var vinculada=await c.ExecuteScalarAsync<int>(@"select (select count(*) from SprintHistorias where IdHistoria=@id)+(select count(*) from Aprobaciones where IdHistoria=@id)",new{id},tx);
     if(vinculada>0) throw new InvalidOperationException("La historia tiene Sprints o aprobaciones; no se puede eliminar.");
     await c.ExecuteAsync("delete from CriteriosAceptacion where IdHistoria=@id",new{id},tx);
     var n=await c.ExecuteAsync("delete from HistoriasUsuario where IdHistoria=@id and IdProyecto=@proyectoId",new{id,proyectoId},tx);
     if(n==0) throw new InvalidOperationException("Historia no encontrada.");
     await c.ExecuteAsync("insert into Auditoria (IdUsuario,Accion,Entidad,IdRegistro,Detalle) values (@u,'ELIMINAR','HistoriasUsuario',@id,'Eliminación de historia sin dependencias')",new{u=UsuarioId,id},tx);
     await tx.CommitAsync();TempData["Mensaje"]="Historia eliminada.";
   }catch(Exception ex){await tx.RollbackAsync();TempData["Error"]=ex.Message;}
   return RedirectToAction("Index",new{proyectoId});
 }
}
