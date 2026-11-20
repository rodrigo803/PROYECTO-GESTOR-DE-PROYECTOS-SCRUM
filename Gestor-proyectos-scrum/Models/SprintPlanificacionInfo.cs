namespace Gestor_proyectos_scrum.Models;

/// <summary>Datos del Sprint elegido para la tarjeta resumen de Planificacion/Index.</summary>
public class SprintPlanificacionInfo
{
    public int IdSprint { get; set; }
    public int IdProyecto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Objetivo { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int CapacidadPuntos { get; set; }
}
