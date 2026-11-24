namespace Gestor_proyectos_scrum.Models;

/// <summary>Miembro de un proyecto para la pestaña Miembros de Proyectos/Detalle.</summary>
public class ProyectoMiembroItem
{
    public int IdUsuario { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
    public string RolProyecto { get; set; } = string.Empty;
    public DateTime FechaIngreso { get; set; }
}
