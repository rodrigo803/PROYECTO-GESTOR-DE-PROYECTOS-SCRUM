namespace Gestor_proyectos_scrum.Models;

/// <summary>Historia comprometida en un Sprint ya Cerrado que nunca se Terminó ni se resolvió.</summary>
public class HistoriaNoTerminadaItem
{
    public int IdSprintHistoria { get; set; }
    public int IdHistoria { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Quiero { get; set; } = string.Empty;
    public int? Estimacion { get; set; }
    public string NombreSprint { get; set; } = string.Empty;
}
