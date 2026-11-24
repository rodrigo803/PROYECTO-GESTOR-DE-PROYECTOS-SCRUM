namespace Gestor_proyectos_scrum.Models;

/// <summary>Historia comprometida en un Sprint, para la vista cliente de solo lectura.</summary>
public class HistoriaComprometidaClienteItem
{
    public int IdHistoria { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Quiero { get; set; } = string.Empty;
    public int? Estimacion { get; set; }
    public string Prioridad { get; set; } = string.Empty;
    public int CantidadTareas { get; set; }

    /// <summary>"Aceptada", "Rechazada" o "Pendiente".</summary>
    public string EstadoAprobacion { get; set; } = string.Empty;
}
