namespace Gestor_proyectos_scrum.Api.Models;

// DTOs de HU-03 (Gestión de Proyectos). Los nombres de propiedades coinciden a propósito
// con los Models/ViewModels del MVC (Gestor_proyectos_scrum.Models) para que
// ScrumApiClient pueda deserializar la respuesta directamente en ellos.

public class MiembroAvatarDto
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string Iniciales { get; set; } = string.Empty;
}

public class ProyectoListaItemDto
{
    public int IdProyecto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? ProductOwner { get; set; }
    public string? ScrumMaster { get; set; }
    public int CantidadMiembros { get; set; }
    public List<MiembroAvatarDto> Miembros { get; set; } = new();
    public decimal? Avance { get; set; }
}

public class ProyectoMetricasDto
{
    public int Total { get; set; }
    public int Activos { get; set; }
    public int Planificados { get; set; }
    public int Cerrados { get; set; }
}

public class UsuarioOpcionDto
{
    public int IdUsuario { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
}

public class ProyectoMiembroDto
{
    public int IdUsuario { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string RolProyecto { get; set; } = string.Empty;
    public DateTime FechaIngreso { get; set; }
}

public class SprintResumenDto
{
    public int IdSprint { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Estado { get; set; } = string.Empty;
}

public class AuditoriaDto
{
    public string Usuario { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public DateTime Fecha { get; set; }
}

public class ProyectoDetalleDto
{
    public int IdProyecto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Estado { get; set; } = string.Empty;
    public decimal? Avance { get; set; }
    public int CantidadSprints { get; set; }
    public int CantidadHistorias { get; set; }
    public int CantidadMiembros { get; set; }
    public List<SprintResumenDto> Sprints { get; set; } = new();
    public List<ProyectoMiembroDto> Miembros { get; set; } = new();
    public List<AuditoriaDto> Historial { get; set; } = new();
    public List<UsuarioOpcionDto> UsuariosDisponibles { get; set; } = new();
}

public class CrearActualizarProyectoRequest
{
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int? IdProductOwner { get; set; }
    public int? IdScrumMaster { get; set; }
    public List<int> IdsDevelopers { get; set; } = new();
}

public class AgregarMiembroRequest
{
    public int IdUsuario { get; set; }
    public string RolProyecto { get; set; } = string.Empty;
}

public class CambiarRolMiembroRequest
{
    public string NuevoRol { get; set; } = string.Empty;
}

public class ProyectoCierreResumenDto
{
    public int IdProyecto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public int CantidadSprints { get; set; }
    public int SprintsCerrados { get; set; }
    public string? SprintActivoNombre { get; set; }
    public int HistoriasPendientes { get; set; }
    public int ImpedimentosAbiertos { get; set; }
    public int CantidadMiembros { get; set; }
}

public class CerrarProyectoRequest
{
    public string? Motivo { get; set; }
    public string? Observaciones { get; set; }
}

public class ProyectoClienteItemDto
{
    public int IdProyecto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string MiRol { get; set; } = string.Empty;
    public string? ProductOwner { get; set; }
    public string? ScrumMaster { get; set; }
    public decimal? Avance { get; set; }
    public string? SprintActivoNombre { get; set; }
    public List<MiembroAvatarDto> Miembros { get; set; } = new();
}

public class ProyectoClienteDetalleDto
{
    public int IdProyecto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string? ProductOwner { get; set; }
    public string? ScrumMaster { get; set; }
    public decimal? Avance { get; set; }
    public string MiRol { get; set; } = string.Empty;
    public int IdUsuarioActual { get; set; }
    public List<SprintResumenDto> Sprints { get; set; } = new();
    public List<ProyectoMiembroDto> Miembros { get; set; } = new();
}
