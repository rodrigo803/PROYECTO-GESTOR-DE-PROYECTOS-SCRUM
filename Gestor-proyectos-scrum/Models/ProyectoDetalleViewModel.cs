namespace Gestor_proyectos_scrum.Models;

/// <summary>ViewModel de pantalla para Proyectos/Detalle.</summary>
public class ProyectoDetalleViewModel
{
    public int IdProyecto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Estado { get; set; } = string.Empty;

    /// <summary>Null cuando no hay puntos estimados de los que calcular el avance ("Sin historias").</summary>
    public decimal? Avance { get; set; }

    public int CantidadSprints { get; set; }
    public int CantidadHistorias { get; set; }
    public int CantidadMiembros { get; set; }

    public List<SprintResumenItem> Sprints { get; set; } = new();
    public List<ProyectoMiembroItem> Miembros { get; set; } = new();
    public List<AuditoriaItem> Historial { get; set; } = new();

    /// <summary>Usuarios activos que todavía no son miembros del proyecto, para el select de "agregar miembro".</summary>
    public List<UsuarioOpcion> UsuariosDisponibles { get; set; } = new();

    /// <summary>Se puede gestionar miembros: el usuario actual tiene permiso y el proyecto no está cerrado.</summary>
    public bool PuedeGestionar { get; set; }
}
