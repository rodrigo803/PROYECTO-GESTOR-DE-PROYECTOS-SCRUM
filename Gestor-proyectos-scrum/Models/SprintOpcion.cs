namespace Gestor_proyectos_scrum.Models;

/// <summary>Sprint Planificado o Activo de un proyecto, para el select de Planificacion/Index.</summary>
public class SprintOpcion
{
    public int IdSprint { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}
