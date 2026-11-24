using System.ComponentModel.DataAnnotations;

namespace Gestor_proyectos_scrum.Models;

// =========================================================================
// RESULTADO ESTÁNDAR DE CONSUMO DE LA MINIMAL API
// =========================================================================

public class ApiResult<T>
{
    public bool Exito { get; set; }
    public int StatusCode { get; set; }
    public T? Datos { get; set; }
    public string? Error { get; set; }

    public static ApiResult<T> Ok(T? datos, int statusCode = 200) => new()
    {
        Exito = true,
        StatusCode = statusCode,
        Datos = datos
    };

    public static ApiResult<T> Fallo(string error, int statusCode = 400) => new()
    {
        Exito = false,
        StatusCode = statusCode,
        Error = error
    };
}

public class ApiErrorDto
{
    public string? Error { get; set; }
    public string? Detalle { get; set; }
}

public class ApiMessageDto
{
    public string Mensaje { get; set; } = string.Empty;
    public int? Id { get; set; }
}

public class PerfilActivoViewModel
{
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;

    public bool EsAdministrador => string.Equals(Rol, "Administrador", StringComparison.OrdinalIgnoreCase);
    public bool EsProductOwner => string.Equals(Rol, "Product Owner", StringComparison.OrdinalIgnoreCase);
    public bool EsScrumMaster => string.Equals(Rol, "Scrum Master", StringComparison.OrdinalIgnoreCase);
    public bool EsMiembroEquipo => string.Equals(Rol, "Miembro del Equipo", StringComparison.OrdinalIgnoreCase);

    // Sprints y Tareas: Admin y SM gestionan (crean/editan/asignan/aprueban/eliminan); PO solo ve.
    public bool PuedeGestionarSprints => EsAdministrador || EsScrumMaster;
    public bool PuedeGestionarTareas => EsAdministrador || EsScrumMaster;
    public bool PuedeAprobarTareas => EsAdministrador || EsScrumMaster;
    public bool PuedeEliminarTareas => EsAdministrador || EsScrumMaster;
}

// =========================================================================
// HU-02: GESTIÓN DE USUARIOS, ROLES Y PERMISOS
// =========================================================================

public class RolItemViewModel
{
    public int IdRol { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class UsuarioItemViewModel
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

public class UsuariosIndexViewModel
{
    public List<UsuarioItemViewModel> Usuarios { get; set; } = new();
    public List<RolItemViewModel> Roles { get; set; } = new();
    public string? Buscar { get; set; }
    public int? IdRolFiltro { get; set; }
    public bool? ActivoFiltro { get; set; }

    public int TotalUsuarios { get; set; }
    public int UsuariosActivos { get; set; }
    public int UsuariosInactivos { get; set; }
    public int TotalAdministradores { get; set; }

    public PerfilActivoViewModel PerfilActual { get; set; } = new();
}

public class CrearUsuarioViewModel
{
    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre no debe exceder 50 caracteres.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos son obligatorios.")]
    [StringLength(80, ErrorMessage = "Los apellidos no deben exceder 80 caracteres.")]
    [Display(Name = "Apellidos")]
    public string Apellidos { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo electrónico con formato válido.")]
    [StringLength(120, ErrorMessage = "El correo no debe exceder 120 caracteres.")]
    [Display(Name = "Correo Electrónico")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria.")]
    [StringLength(100, MinimumLength = 8, ErrorMessage = "La contraseña debe tener al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Contraseña Inicial")]
    public string Contrasena { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe seleccionar un rol SCRUM.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un rol válido.")]
    [Display(Name = "Rol del Sistema")]
    public int IdRol { get; set; }

    [Display(Name = "Usuario activo")]
    public bool Activo { get; set; } = true;

    public List<RolItemViewModel> RolesDisponibles { get; set; } = new();
}

public class EditarUsuarioViewModel
{
    public int IdUsuario { get; set; }

    [Required(ErrorMessage = "El nombre es obligatorio.")]
    [StringLength(50, ErrorMessage = "El nombre no debe exceder 50 caracteres.")]
    [Display(Name = "Nombre")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos son obligatorios.")]
    [StringLength(80, ErrorMessage = "Los apellidos no deben exceder 80 caracteres.")]
    [Display(Name = "Apellidos")]
    public string Apellidos { get; set; } = string.Empty;

    [Required(ErrorMessage = "El correo electrónico es obligatorio.")]
    [EmailAddress(ErrorMessage = "Ingrese un correo electrónico con formato válido.")]
    [StringLength(120, ErrorMessage = "El correo no debe exceder 120 caracteres.")]
    [Display(Name = "Correo Electrónico")]
    public string Correo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Debe seleccionar un rol SCRUM.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un rol válido.")]
    [Display(Name = "Rol del Sistema")]
    public int IdRol { get; set; }

    [Display(Name = "Usuario activo")]
    public bool Activo { get; set; } = true;

    [StringLength(100, MinimumLength = 8, ErrorMessage = "Si ingresa una nueva contraseña, debe tener al menos 8 caracteres.")]
    [DataType(DataType.Password)]
    [Display(Name = "Nueva Contraseña (opcional)")]
    public string? NuevaContrasena { get; set; }

    public List<RolItemViewModel> RolesDisponibles { get; set; } = new();
}

// =========================================================================
// HU-05: GESTIÓN DE SPRINTS
// =========================================================================

public class ProyectoItemViewModel
{
    public int IdProyecto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Estado { get; set; } = string.Empty;
}

public class SprintItemViewModel
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

    private int _duracionDias;
    public int DuracionDias
    {
        get => _duracionDias > 0 ? _duracionDias : Math.Max(1, (FechaFin.Date - FechaInicio.Date).Days);
        set => _duracionDias = value;
    }

    private int _diasRestantes;
    public int DiasRestantes
    {
        get => _diasRestantes > 0 ? _diasRestantes : Math.Max(0, (FechaFin.Date - DateTime.Today).Days);
        set => _diasRestantes = value;
    }
}

public class SprintHistoriaDetalleViewModel
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

public class SprintDetalleApiDto
{
    public SprintItemViewModel Sprint { get; set; } = new();
    public List<SprintHistoriaDetalleViewModel> Historias { get; set; } = new();
}

public class SprintsIndexViewModel
{
    public List<SprintItemViewModel> Sprints { get; set; } = new();
    public List<ProyectoItemViewModel> Proyectos { get; set; } = new();
    public int? IdProyectoFiltro { get; set; }
    public string? EstadoFiltro { get; set; }
    public string? Buscar { get; set; }

    // 4 tarjetas de resumen como en prototipo/sprints.html
    public int TotalSprints { get; set; }
    public int SprintsActivosCount { get; set; }
    public string NombreSprintActivoDestacado { get; set; } = "Ninguno";
    public string SubtituloSprintActivo { get; set; } = "Sin sprint en curso";
    public int TotalPuntosComprometidos { get; set; }
    public int TotalCapacidadPuntos { get; set; }
    public decimal TotalHorasEstimadas { get; set; }
    public decimal AvancePonderadoPromedio { get; set; }

    public PerfilActivoViewModel PerfilActual { get; set; } = new();
}

public class SprintDetailsViewModel
{
    public SprintItemViewModel Sprint { get; set; } = new();
    public List<SprintHistoriaDetalleViewModel> Historias { get; set; } = new();
    public PerfilActivoViewModel PerfilActual { get; set; } = new();
}

public class CrearSprintViewModel
{
    [Required(ErrorMessage = "Debe seleccionar un proyecto.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione un proyecto válido.")]
    [Display(Name = "Proyecto")]
    public int IdProyecto { get; set; }

    [Required(ErrorMessage = "El nombre del Sprint es obligatorio.")]
    [StringLength(60, ErrorMessage = "El nombre no debe exceder 60 caracteres.")]
    [Display(Name = "Nombre del Sprint")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El objetivo del Sprint es obligatorio.")]
    [StringLength(300, ErrorMessage = "El objetivo no debe exceder 300 caracteres.")]
    [Display(Name = "Objetivo del Sprint (Sprint Goal)")]
    public string Objetivo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de Inicio")]
    public DateTime FechaInicio { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "La fecha de finalización es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de Finalización")]
    public DateTime FechaFin { get; set; } = DateTime.Today.AddDays(14);

    [Required(ErrorMessage = "La capacidad en puntos es obligatoria.")]
    [Range(1, 1000, ErrorMessage = "La capacidad en puntos debe ser mayor que 0.")]
    [Display(Name = "Capacidad Estimada (Story Points)")]
    public int CapacidadPuntos { get; set; } = 30;

    public List<ProyectoItemViewModel> ProyectosDisponibles { get; set; } = new();
}

public class EditarSprintViewModel
{
    public int IdSprint { get; set; }
    public int IdProyecto { get; set; }
    public string ProyectoCodigo { get; set; } = string.Empty;
    public string ProyectoNombre { get; set; } = string.Empty;
    public DateTime ProyectoFechaInicio { get; set; }
    public DateTime ProyectoFechaFin { get; set; }
    public string Estado { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre del Sprint es obligatorio.")]
    [StringLength(60, ErrorMessage = "El nombre no debe exceder 60 caracteres.")]
    [Display(Name = "Nombre del Sprint")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El objetivo del Sprint es obligatorio.")]
    [StringLength(300, ErrorMessage = "El objetivo no debe exceder 300 caracteres.")]
    [Display(Name = "Objetivo del Sprint (Sprint Goal)")]
    public string Objetivo { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de Inicio")]
    public DateTime FechaInicio { get; set; }

    [Required(ErrorMessage = "La fecha de finalización es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de Finalización")]
    public DateTime FechaFin { get; set; }

    [Required(ErrorMessage = "La capacidad en puntos es obligatoria.")]
    [Range(1, 1000, ErrorMessage = "La capacidad en puntos debe ser mayor que 0.")]
    [Display(Name = "Capacidad Estimada (Story Points)")]
    public int CapacidadPuntos { get; set; }
}

// =========================================================================
// HU-07: GESTIÓN DE TAREAS, CHECKPOINTS INMUTABLES Y APROBACIONES
// =========================================================================

public class SprintHistoriaCatalogoViewModel
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

    public string EtiquetaSelector => $"{ProyectoCodigo} · {SprintNombre} ({SprintEstado}) — {HistoriaCodigo}: {HistoriaQuiero}";
}

public class ResponsableCatalogoViewModel
{
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string RolNombre { get; set; } = string.Empty;
    public string NombreCompleto => $"{Nombre} {Apellidos}".Trim();
}

public class ImpedimentoResumenViewModel
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

public class SprintFiltroCatalogoViewModel
{
    public int IdSprint { get; set; }
    public int IdProyecto { get; set; }
    public string ProyectoCodigo { get; set; } = string.Empty;
    public string ProyectoNombre { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;

    public string Etiqueta => $"{ProyectoCodigo} — {Nombre} ({Estado})";
}

public class TareaCatalogosViewModel
{
    public List<SprintHistoriaCatalogoViewModel> SprintHistorias { get; set; } = new();
    public List<ResponsableCatalogoViewModel> Responsables { get; set; } = new();
    public List<ImpedimentoResumenViewModel> ImpedimentosActivos { get; set; } = new();
    public List<SprintFiltroCatalogoViewModel> Sprints { get; set; } = new();
}

public class CriterioAceptacionViewModel
{
    public int IdCriterio { get; set; }
    public int IdHistoria { get; set; }
    public string Descripcion { get; set; } = string.Empty;
}

public class TareaItemViewModel
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

    public string InicialesResponsable
    {
        get
        {
            if (string.IsNullOrWhiteSpace(ResponsableNombre)) return "SA";
            var partes = ResponsableNombre.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (partes.Length == 1) return partes[0][0].ToString().ToUpperInvariant();
            return $"{partes[0][0]}{partes[^1][0]}".ToUpperInvariant();
        }
    }
}

public class SprintBacklogHistoriaViewModel
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
    public List<CriterioAceptacionViewModel> CriteriosAceptacion { get; set; } = new();
    public List<TareaItemViewModel> Tareas { get; set; } = new();
}

public class SprintBacklogApiDto
{
    public List<SprintFiltroCatalogoViewModel> SprintsDisponibles { get; set; } = new();
    public List<ResponsableCatalogoViewModel> ResponsablesDisponibles { get; set; } = new();
    public List<SprintHistoriaCatalogoViewModel> SprintHistoriasElegibles { get; set; } = new();
    public List<ImpedimentoResumenViewModel> ImpedimentosActivos { get; set; } = new();

    public SprintItemViewModel? SprintSeleccionado { get; set; }
    public List<SprintBacklogHistoriaViewModel> Historias { get; set; } = new();

    public int CountPendientes { get; set; }
    public decimal HorasPendientes { get; set; }
    public int CountEnProgreso { get; set; }
    public decimal HorasEnProgreso { get; set; }
    public int CountEnRevision { get; set; }
    public decimal HorasEnRevision { get; set; }
    public int CountFinalizadas { get; set; }
    public decimal HorasFinalizadas { get; set; }
}

public class AvanceTareaItemViewModel
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

public class AprobacionTareaItemViewModel
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

public class TareaDetalleApiDto
{
    public TareaItemViewModel Tarea { get; set; } = new();
    public List<AvanceTareaItemViewModel> Avances { get; set; } = new();
    public List<AprobacionTareaItemViewModel> Aprobaciones { get; set; } = new();
    public List<ImpedimentoResumenViewModel> ImpedimentosSprint { get; set; } = new();
}

public class TareasIndexViewModel
{
    public SprintItemViewModel? SprintSeleccionado { get; set; }
    public List<SprintBacklogHistoriaViewModel> Historias { get; set; } = new();
    public List<TareaItemViewModel> Tareas { get; set; } = new();

    public List<SprintFiltroCatalogoViewModel> Sprints { get; set; } = new();
    public List<ResponsableCatalogoViewModel> Responsables { get; set; } = new();
    public List<SprintHistoriaCatalogoViewModel> SprintHistoriasElegibles { get; set; } = new();
    public List<ImpedimentoResumenViewModel> ImpedimentosActivos { get; set; } = new();

    public int? IdSprintFiltro { get; set; }
    public int? IdResponsableFiltro { get; set; }
    public string? EstadoFiltro { get; set; }
    public string? Buscar { get; set; }

    // 4 tarjetas de métricas por estado (Pendiente, En progreso, En revisión, Finalizado)
    public int CountPendientes { get; set; }
    public decimal HorasPendientes { get; set; }
    public int CountEnProgreso { get; set; }
    public decimal HorasEnProgreso { get; set; }
    public int CountEnRevision { get; set; }
    public decimal HorasEnRevision { get; set; }
    public int CountFinalizadas { get; set; }
    public decimal HorasFinalizadas { get; set; }

    public decimal AvanceGlobalPonderado { get; set; }
    public int TotalPuntosEsfuerzo { get; set; }

    public PerfilActivoViewModel PerfilActual { get; set; } = new();
}

public class CrearTareaViewModel
{
    [Required(ErrorMessage = "Debe seleccionar una Historia de Usuario comprometida en un Sprint.")]
    [Range(1, int.MaxValue, ErrorMessage = "Seleccione una Historia de Usuario válida.")]
    [Display(Name = "Historia Comprometida (Sprint Backlog)")]
    public int IdSprintHistoria { get; set; }

    [Required(ErrorMessage = "El título de la tarea es obligatorio.")]
    [StringLength(150, ErrorMessage = "El título no debe exceder 150 caracteres.")]
    [Display(Name = "Título de la Tarea")]
    public string Titulo { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripción no debe exceder 500 caracteres.")]
    [Display(Name = "Descripción Técnica")]
    public string? Descripcion { get; set; }

    [Display(Name = "Responsable Asignado")]
    public int? IdResponsable { get; set; }

    [Required(ErrorMessage = "Los puntos de esfuerzo son obligatorios.")]
    [Range(1, 100, ErrorMessage = "Los puntos de esfuerzo deben estar entre 1 y 100.")]
    [Display(Name = "Puntos de Esfuerzo (Peso)")]
    public int PuntosEsfuerzo { get; set; } = 3;

    [Required(ErrorMessage = "Las horas estimadas son obligatorias.")]
    [Range(0.5, 500, ErrorMessage = "Las horas estimadas deben ser mayores que 0.")]
    [Display(Name = "Horas Estimadas")]
    public decimal HorasEstimadas { get; set; } = 6;

    [Required(ErrorMessage = "La fecha de entrega estimada es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de Entrega Estimada")]
    public DateTime FechaEntregaEstimada { get; set; } = DateTime.Today.AddDays(5);

    public List<SprintHistoriaCatalogoViewModel> SprintHistoriasDisponibles { get; set; } = new();
    public List<ResponsableCatalogoViewModel> ResponsablesDisponibles { get; set; } = new();
}

public class EditarTareaViewModel
{
    public int IdTarea { get; set; }
    public int IdSprintHistoria { get; set; }
    public string HistoriaEtiqueta { get; set; } = string.Empty;
    public string SprintNombre { get; set; } = string.Empty;
    public string SprintEstado { get; set; } = string.Empty;
    public int PorcentajeAvanceActual { get; set; }
    public string EstadoActual { get; set; } = string.Empty;
    public decimal HorasRealesActuales { get; set; }

    [Required(ErrorMessage = "El título de la tarea es obligatorio.")]
    [StringLength(150, ErrorMessage = "El título no debe exceder 150 caracteres.")]
    [Display(Name = "Título de la Tarea")]
    public string Titulo { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "La descripción no debe exceder 500 caracteres.")]
    [Display(Name = "Descripción Técnica")]
    public string? Descripcion { get; set; }

    [Display(Name = "Responsable Asignado")]
    public int? IdResponsable { get; set; }

    [Required(ErrorMessage = "Los puntos de esfuerzo son obligatorios.")]
    [Range(1, 100, ErrorMessage = "Los puntos de esfuerzo deben estar entre 1 y 100.")]
    [Display(Name = "Puntos de Esfuerzo (Peso)")]
    public int PuntosEsfuerzo { get; set; }

    [Required(ErrorMessage = "Las horas estimadas son obligatorias.")]
    [Range(0.5, 500, ErrorMessage = "Las horas estimadas deben ser mayores que 0.")]
    [Display(Name = "Horas Estimadas")]
    public decimal HorasEstimadas { get; set; }

    [Required(ErrorMessage = "La fecha de entrega estimada es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de Entrega Estimada")]
    public DateTime FechaEntregaEstimada { get; set; }

    public List<ResponsableCatalogoViewModel> ResponsablesDisponibles { get; set; } = new();
}

public class RegistrarCheckpointViewModel
{
    public int IdTarea { get; set; }

    [Required(ErrorMessage = "El nuevo porcentaje de avance es obligatorio.")]
    [Range(0, 100, ErrorMessage = "El porcentaje debe estar entre 0 y 100.")]
    [Display(Name = "Nuevo Porcentaje de Avance (%)")]
    public int PorcentajeNuevo { get; set; }

    [Required(ErrorMessage = "Las horas trabajadas son obligatorias.")]
    [Range(0, 500, ErrorMessage = "Las horas trabajadas no pueden ser negativas.")]
    [Display(Name = "Horas Trabajadas en esta sesión")]
    public decimal HorasTrabajadas { get; set; }

    [Required(ErrorMessage = "Debe seleccionar el estado nuevo.")]
    [Display(Name = "Estado de la Tarea")]
    public string EstadoNuevo { get; set; } = "En progreso";

    [StringLength(300, ErrorMessage = "La justificación no debe superar 300 caracteres.")]
    [Display(Name = "Justificación / Notas del Checkpoint")]
    public string? Justificacion { get; set; }

    [Display(Name = "Impedimento Relacionado (opcional)")]
    public int? IdImpedimento { get; set; }
}

public class RegistrarAprobacionViewModel
{
    public int IdTarea { get; set; }

    [Required(ErrorMessage = "Debe seleccionar el dictamen (Aprobado o Rechazado).")]
    [Display(Name = "Dictamen de Revisión")]
    public string Resultado { get; set; } = "Aprobado";

    [StringLength(500, ErrorMessage = "El comentario no debe superar 500 caracteres.")]
    [Display(Name = "Comentario / Observaciones")]
    public string? Comentario { get; set; }
}

public class MisTareasViewModel
{
    public List<MisTareasProyectoViewModel> Proyectos { get; set; } = new();
}

public class MisTareasProyectoViewModel
{
    public int IdProyecto { get; set; }
    public string ProyectoCodigo { get; set; } = string.Empty;
    public string ProyectoNombre { get; set; } = string.Empty;
    public List<MisTareasSprintViewModel> Sprints { get; set; } = new();
}

public class MisTareasSprintViewModel
{
    public int IdSprint { get; set; }
    public string SprintNombre { get; set; } = string.Empty;
    public string SprintEstado { get; set; } = string.Empty;
    public List<TareaItemViewModel> Tareas { get; set; } = new();
}

public class TareaDetailsViewModel
{
    public TareaItemViewModel Tarea { get; set; } = new();
    public List<AvanceTareaItemViewModel> Avances { get; set; } = new();
    public List<AprobacionTareaItemViewModel> Aprobaciones { get; set; } = new();
    public List<ImpedimentoResumenViewModel> ImpedimentosSprint { get; set; } = new();

    public RegistrarCheckpointViewModel NuevoCheckpoint { get; set; } = new();
    public RegistrarAprobacionViewModel NuevaAprobacion { get; set; } = new();

    public PerfilActivoViewModel PerfilActual { get; set; } = new();
}
