namespace Gestor_proyectos_scrum.Models;

/// <summary>Fila de la lista de sprints de un proyecto, para la pestaña Resumen de Proyectos/Detalle.</summary>
public class SprintResumenItem
{
    public int IdSprint { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Estado { get; set; } = string.Empty;
}
