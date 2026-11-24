namespace Gestor_proyectos_scrum.Models;

/// <summary>Tarjeta de un proyecto en Proyectos/Cliente (proyectos donde el usuario actual es miembro).</summary>
public class ProyectoClienteItem
{
    public int IdProyecto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }

    /// <summary>Rol del usuario actual dentro de este proyecto.</summary>
    public string MiRol { get; set; } = string.Empty;

    public string? ProductOwner { get; set; }
    public string? ScrumMaster { get; set; }

    /// <summary>Null cuando no hay puntos estimados de los que calcular el avance ("Sin historias").</summary>
    public decimal? Avance { get; set; }

    /// <summary>Nombre del sprint Activo del proyecto, o null si no tiene ninguno.</summary>
    public string? SprintActivoNombre { get; set; }

    public List<MiembroAvatar> Miembros { get; set; } = new();
}
