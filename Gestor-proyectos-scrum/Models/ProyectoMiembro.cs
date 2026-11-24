namespace Gestor_proyectos_scrum.Models;

/// <summary>Entidad de la tabla ProyectoMiembros.</summary>
public class ProyectoMiembro
{
    public int IdProyectoMiembro { get; set; }
    public int IdProyecto { get; set; }
    public int IdUsuario { get; set; }
    public string RolProyecto { get; set; } = string.Empty;
    public DateTime FechaIngreso { get; set; }
}
