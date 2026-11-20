using System.ComponentModel.DataAnnotations;

namespace Gestor_proyectos_scrum.Models;
public class HistoriaDto
{
  public int IdHistoria {get;set;}
  public int IdProyecto {get;set;}
  [Required, StringLength(10), RegularExpression(@"HU-\d{2,7}",ErrorMessage="Código: HU-01, HU-02, etc.")]
  public string Codigo {get;set;}="";
  [Required, StringLength(100)] public string Como {get;set;}="";
  [Required, StringLength(300)] public string Quiero {get;set;}="";
  [Required, StringLength(300)] public string Para {get;set;}="";
  [Required] public string Prioridad {get;set;}="Media";
  [Range(1,99999)] public int OrdenPrioridad {get;set;}=1;
  [Range(1,999)] public int? Estimacion {get;set;}
  [Required] public string Estado {get;set;}="Pendiente";
  public string CriteriosTexto {get;set;}="";
  public int TotalCriterios {get;set;}
  public string? NombreProyecto {get;set;}
}
public class ProyectoOpcion {public int IdProyecto {get;set;} public string Nombre {get;set;}="";}
public class SprintOpcion { public int IdSprint {get;set;} public int IdProyecto {get;set;} public string Nombre {get;set;}=""; public string Estado {get;set;}="";public DateTime FechaInicio {get;set;} public DateTime FechaFin {get;set;} }
public class ResponsableOpcion { public int IdUsuario {get;set;} public string Nombre {get;set;}=""; }
public class BacklogVista {public int ProyectoId {get;set;} public List<ProyectoOpcion> Proyectos {get;set;}=new(); public List<HistoriaDto> Historias {get;set;}=new();}
public class TareaKanban
{
 public int IdTarea {get;set;} public int IdSprintHistoria {get;set;} public int IdHistoria {get;set;} public string Codigo {get;set;}="";
 public string Como {get;set;}=""; public string Quiero {get;set;}=""; public string Para {get;set;}="";
 public string Titulo {get;set;}=""; public string? Descripcion {get;set;} public string Estado {get;set;}="Pendiente";
 public string Prioridad {get;set;}="Media"; public int? Estimacion {get;set;} public int PuntosEsfuerzo {get;set;}
 public int PorcentajeAvance {get;set;} public decimal HorasEstimadas {get;set;} public decimal HorasReales {get;set;}
 public int? IdResponsable {get;set;} public string? Responsable {get;set;}
 public int TotalTareas {get;set;} public int TareasTerminadas {get;set;}
}
public class TableroVista { public int SprintId {get;set;} public List<SprintOpcion> Sprints {get;set;}=new();public List<TareaKanban> Tareas {get;set;}=new();public List<ResponsableOpcion> Responsables {get;set;}=new();}
public class MoverTareaDto
{
 [Range(1,int.MaxValue)] public int IdTarea {get;set;}
 [Required] public string EstadoNuevo {get;set;}="";
 [Range(0,100)] public int PorcentajeNuevo {get;set;}
 [Range(0,99999)] public decimal HorasTrabajadas {get;set;}
 [StringLength(300)] public string? Justificacion {get;set;}
}
public class AsignarTareaDto {public int IdTarea {get;set;} public int? IdResponsable {get;set;}}
public class RetroEntrada
{
 public int IdSprint {get;set;}
 [DataType(DataType.Date)] public DateTime Fecha {get;set;}=DateTime.Today;
 [Required, StringLength(300)] public string Positivo {get;set;}="";
 [Required, StringLength(300)] public string Mejora {get;set;}="";
 [Required, StringLength(300)] public string Accion {get;set;}="";
 public int? IdResponsable {get;set;}
 [DataType(DataType.Date)] public DateTime? FechaCompromiso {get;set;}
 public string Estado {get;set;}="Pendiente";
}
public class RetroListado {public int IdRetrospectiva {get;set;} public int IdSprint {get;set;} public string Nombre {get;set;}="";public DateTime Fecha {get;set;} public string Facilitador {get;set;}=""; public string? Positivo {get;set;}public string? Mejora {get;set;}public string? Accion {get;set;}public string? EstadoAccion {get;set;} }
public class RetroVista { public RetroEntrada Formulario {get;set;}=new();public List<SprintOpcion> Sprints {get;set;}=new(); public List<ResponsableOpcion> Responsables {get;set;}=new();public List<RetroListado> Anteriores {get;set;}=new();}
