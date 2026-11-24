namespace Gestor_proyectos_scrum.Models;

/// <summary>Proyecto no cerrado, para el select de Planificacion/Index.</summary>
public class ProyectoOpcion
{
    public int IdProyecto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}
