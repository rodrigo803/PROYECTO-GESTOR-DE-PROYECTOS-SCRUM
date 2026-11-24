namespace Gestor_proyectos_scrum.Api.Models;

public class ApiErrorResponse
{
    public string Error { get; set; } = string.Empty;
    public string? Detalle { get; set; }

    public ApiErrorResponse() { }

    public ApiErrorResponse(string error, string? detalle = null)
    {
        Error = error;
        Detalle = detalle;
    }
}

public class ApiMessageResponse
{
    public string Mensaje { get; set; } = string.Empty;
    public int? Id { get; set; }

    public ApiMessageResponse() { }

    public ApiMessageResponse(string mensaje, int? id = null)
    {
        Mensaje = mensaje;
        Id = id;
    }
}

// =========================================================================
// HU-02: USUARIOS, ROLES Y PERMISOS
// =========================================================================

public class RolDto
{
    public int IdRol { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class UsuarioDto
{
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public int IdRol { get; set; }
    public string RolNombre { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public DateTime FechaRegistro { get; set; }
    public DateTime? UltimoAcceso { get; set; }
    public int TareasAsignadas { get; set; }
    public int TareasActivas { get; set; }
    public decimal HorasAsignadas { get; set; }
    public decimal HorasEstimadasAsignadas { get; set; }
    public int ProyectosParticipa { get; set; }

    public string NombreCompleto => $"{Nombre} {Apellidos}".Trim();

    public string Iniciales
    {
        get
        {
            var n = !string.IsNullOrWhiteSpace(Nombre) ? Nombre.Trim()[0].ToString() : "";
            var a = !string.IsNullOrWhiteSpace(Apellidos) ? Apellidos.Trim()[0].ToString() : "";
            var res = (n + a).ToUpperInvariant();
            return string.IsNullOrEmpty(res) ? "U" : res;
        }
    }
}

public class CrearUsuarioRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
    public int IdRol { get; set; }
    public bool Activo { get; set; } = true;
}

public class ActualizarUsuarioRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public int IdRol { get; set; }
    public bool Activo { get; set; } = true;
    public string? NuevaContrasena { get; set; }
}

public class CambiarRolUsuarioRequest
{
    public int IdRol { get; set; }
}

public class CambiarEstadoUsuarioRequest
{
    public bool Activo { get; set; }
}

// =========================================================================
// HU-05: GESTIÓN DE SPRINTS
// =========================================================================

public class ProyectoResumenDto
{
    public int IdProyecto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Estado { get; set; } = string.Empty;
}

public class SprintDto
{
    public int IdSprint { get; set; }
    public int IdProyecto { get; set; }
    public string ProyectoCodigo { get; set; } = string.Empty;
    public string ProyectoNombre { get; set; } = string.Empty;
    public string ProyectoEstado { get; set; } = string.Empty;
    public DateTime ProyectoFechaInicio { get; set; }
    public DateTime ProyectoFechaFin { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Objetivo { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public int CapacidadPuntos { get; set; }
    public string Estado { get; set; } = string.Empty;

    // Métricas desde SprintHistorias y vw_AvanceSprint
    public int TotalHistorias { get; set; }
    public int HistoriasTerminadas { get; set; }
    public int PuntosComprometidos { get; set; }
    public int TotalTareas { get; set; }
    public int Pendientes { get; set; }
    public int EnProgreso { get; set; }
    public int EnRevision { get; set; }
    public int Finalizadas { get; set; }
    public decimal AvancePonderado { get; set; }
    public decimal HorasEstimadas { get; set; }
    public decimal HorasReales { get; set; }
    public int DuracionDias { get; set; }
    public int DiasRestantes { get; set; }
}

public class SprintHistoriaItemDto
{
    public int IdSprintHistoria { get; set; }
    public int IdSprint { get; set; }
    public int IdHistoria { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Como { get; set; } = string.Empty;
    public string Quiero { get; set; } = string.Empty;
    public string Para { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public int? Estimacion { get; set; }
    public string EstadoHistoria { get; set; } = string.Empty;
    public string Resultado { get; set; } = string.Empty;
    public DateTime FechaCompromiso { get; set; }
    public int TotalTareas { get; set; }
    public int PuntosTotales { get; set; }
    public decimal AvancePonderado { get; set; }
    public decimal AvanceSimple { get; set; }
    public decimal HorasEstimadas { get; set; }
    public decimal HorasReales { get; set; }
}

public class SprintDetalleDto
{
    public SprintDto Sprint { get; set; } = new();
    public List<SprintHistoriaItemDto> Historias { get; set; } = new();
}

public class CrearSprintRequest
{
    public int IdProyecto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Objetivo { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public int CapacidadPuntos { get; set; }
}

public class ActualizarSprintRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Objetivo { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public int CapacidadPuntos { get; set; }
}

public class CambiarEstadoSprintRequest
{
    public string NuevoEstado { get; set; } = string.Empty;
    public string? DestinoIncompletas { get; set; }
}

// =========================================================================
// HU-07: TAREAS, CHECKPOINTS INMUTABLES (AvanceTareas) Y APROBACIONES
// =========================================================================

public class SprintHistoriaCatalogoDto
{
    public int IdSprintHistoria { get; set; }
    public int IdSprint { get; set; }
    public string SprintNombre { get; set; } = string.Empty;
    public string SprintEstado { get; set; } = string.Empty;
    public DateTime SprintFechaInicio { get; set; }
    public DateTime SprintFechaFin { get; set; }
    public int IdProyecto { get; set; }
    public string ProyectoCodigo { get; set; } = string.Empty;
    public string ProyectoNombre { get; set; } = string.Empty;
    public int IdHistoria { get; set; }
    public string HistoriaCodigo { get; set; } = string.Empty;
    public string HistoriaQuiero { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public int? Estimacion { get; set; }
}

public class ResponsableCatalogoDto
{
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string RolNombre { get; set; } = string.Empty;
    public string NombreCompleto => $"{Nombre} {Apellidos}".Trim();
}

public class ImpedimentoResumenDto
{
    public int IdImpedimento { get; set; }
    public int IdSprint { get; set; }
    public string SprintNombre { get; set; } = string.Empty;
    public int? IdHistoria { get; set; }
    public int? IdTarea { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Severidad { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaReporte { get; set; }
}

public class SprintFiltroCatalogoDto
{
    public int IdSprint { get; set; }
    public int IdProyecto { get; set; }
    public string ProyectoCodigo { get; set; } = string.Empty;
    public string ProyectoNombre { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}

public class TareaCatalogosDto
{
    public List<SprintHistoriaCatalogoDto> SprintHistorias { get; set; } = new();
    public List<ResponsableCatalogoDto> Responsables { get; set; } = new();
    public List<ImpedimentoResumenDto> ImpedimentosActivos { get; set; } = new();
    public List<SprintFiltroCatalogoDto> Sprints { get; set; } = new();
}

public class CriterioAceptacionDto
{
    public int IdCriterio { get; set; }
    public int IdHistoria { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}

public class TareaDto
{
    public int IdTarea { get; set; }
    public int IdSprintHistoria { get; set; }
    public int IdSprint { get; set; }
    public string SprintNombre { get; set; } = string.Empty;
    public string SprintEstado { get; set; } = string.Empty;
    public int IdProyecto { get; set; }
    public string ProyectoCodigo { get; set; } = string.Empty;
    public string ProyectoNombre { get; set; } = string.Empty;
    public int IdHistoria { get; set; }
    public string HistoriaCodigo { get; set; } = string.Empty;
    public string HistoriaQuiero { get; set; } = string.Empty;
    public string HistoriaPrioridad { get; set; } = string.Empty;
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? IdResponsable { get; set; }
    public string? ResponsableNombre { get; set; }
    public string? ResponsableCorreo { get; set; }
    public string? ResponsableRol { get; set; }
    public int PuntosEsfuerzo { get; set; }
    public decimal HorasEstimadas { get; set; }
    public decimal HorasReales { get; set; }
    public int PorcentajeAvance { get; set; }
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaRegistro { get; set; }
    public DateTime FechaEntregaEstimada { get; set; }
    public DateTime? FechaEntregaReal { get; set; }
    public bool Atrasada { get; set; }
    public bool Bloqueada { get; set; }
    public int TotalCheckpoints { get; set; }
    public int TotalAprobaciones { get; set; }
}

public class SprintBacklogHistoriaDto
{
    public int IdSprintHistoria { get; set; }
    public int IdSprint { get; set; }
    public int IdHistoria { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Como { get; set; } = string.Empty;
    public string Quiero { get; set; } = string.Empty;
    public string Para { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public int OrdenPrioridad { get; set; }
    public int? Estimacion { get; set; }
    public string EstadoHistoria { get; set; } = string.Empty;
    public string Resultado { get; set; } = string.Empty;
    public decimal AvancePonderado { get; set; }
    public int TotalTareas { get; set; }
    public int PuntosTotales { get; set; }
    public decimal HorasEstimadas { get; set; }
    public decimal HorasReales { get; set; }
    public List<CriterioAceptacionDto> CriteriosAceptacion { get; set; } = new();
    public List<TareaDto> Tareas { get; set; } = new();
}

public class SprintBacklogViewDto
{
    public List<SprintFiltroCatalogoDto> SprintsDisponibles { get; set; } = new();
    public List<ResponsableCatalogoDto> ResponsablesDisponibles { get; set; } = new();
    public List<SprintHistoriaCatalogoDto> SprintHistoriasElegibles { get; set; } = new();
    public List<ImpedimentoResumenDto> ImpedimentosActivos { get; set; } = new();

    public SprintDto? SprintSeleccionado { get; set; }
    public List<SprintBacklogHistoriaDto> Historias { get; set; } = new();

    public int CountPendientes { get; set; }
    public decimal HorasPendientes { get; set; }
    public int CountEnProgreso { get; set; }
    public decimal HorasEnProgreso { get; set; }
    public int CountEnRevision { get; set; }
    public decimal HorasEnRevision { get; set; }
    public int CountFinalizadas { get; set; }
    public decimal HorasFinalizadas { get; set; }
}

public class AvanceTareaDto
{
    public int IdAvance { get; set; }
    public int IdTarea { get; set; }
    public int IdUsuario { get; set; }
    public string UsuarioNombre { get; set; } = string.Empty;
    public string UsuarioRol { get; set; } = string.Empty;
    public DateTime Fecha { get; set; }
    public int PorcentajeAnterior { get; set; }
    public int PorcentajeNuevo { get; set; }
    public decimal HorasTrabajadas { get; set; }
    public string? EstadoAnterior { get; set; }
    public string EstadoNuevo { get; set; } = string.Empty;
    public string? Justificacion { get; set; }
    public int? IdImpedimento { get; set; }
    public string? ImpedimentoTitulo { get; set; }
    public string? ImpedimentoSeveridad { get; set; }
}

public class AprobacionTareaDto
{
    public int IdAprobacion { get; set; }
    public int IdHistoria { get; set; }
    public int? IdCriterio { get; set; }
    public int? IdTarea { get; set; }
    public int IdAprobador { get; set; }
    public string AprobadorNombre { get; set; } = string.Empty;
    public string AprobadorRol { get; set; } = string.Empty;
    public string Resultado { get; set; } = string.Empty;
    public string? Comentario { get; set; }
    public DateTime Fecha { get; set; }
}

public class TareaDetalleDto
{
    public TareaDto Tarea { get; set; } = new();
    public List<AvanceTareaDto> Avances { get; set; } = new();
    public List<AprobacionTareaDto> Aprobaciones { get; set; } = new();
    public List<ImpedimentoResumenDto> ImpedimentosSprint { get; set; } = new();
}

public class CrearTareaRequest
{
    public int IdSprintHistoria { get; set; }
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? IdResponsable { get; set; }
    public int PuntosEsfuerzo { get; set; }
    public decimal HorasEstimadas { get; set; }
    public DateTime FechaEntregaEstimada { get; set; }
}

public class ActualizarTareaRequest
{
    public string Titulo { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public int? IdResponsable { get; set; }
    public int PuntosEsfuerzo { get; set; }
    public decimal HorasEstimadas { get; set; }
    public DateTime FechaEntregaEstimada { get; set; }
}

public class RegistrarAvanceTareaRequest
{
    public int PorcentajeNuevo { get; set; }
    public decimal HorasTrabajadas { get; set; }
    public string EstadoNuevo { get; set; } = string.Empty;
    public string? Justificacion { get; set; }
    public int? IdImpedimento { get; set; }
}

public class RegistrarAprobacionTareaRequest
{
    public string Resultado { get; set; } = string.Empty;
    public string? Comentario { get; set; }
}
