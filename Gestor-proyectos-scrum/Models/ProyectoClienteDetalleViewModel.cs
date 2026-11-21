namespace Gestor_proyectos_scrum.Models;

/// <summary>ViewModel de solo lectura para Proyectos/DetalleCliente.</summary>
public class ProyectoClienteDetalleViewModel
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

    /// <summary>Null cuando no hay puntos estimados de los que calcular el avance ("Sin historias").</summary>
    public decimal? Avance { get; set; }

    /// <summary>Rol del usuario actual dentro de este proyecto.</summary>
    public string MiRol { get; set; } = string.Empty;

    /// <summary>IdUsuario del usuario actual, para resaltarlo en la lista de Equipo con la etiqueta "Tú".</summary>
    public int IdUsuarioActual { get; set; }

    public List<SprintResumenItem> Sprints { get; set; } = new();
    public List<ProyectoMiembroItem> Miembros { get; set; } = new();
}
