namespace Gestor_proyectos_scrum.Models;

/// <summary>Historia ya comprometida en el Sprint elegido, para el panel "Compromiso del Sprint".</summary>
public class HistoriaComprometidaItem
{
    public int IdHistoria { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Quiero { get; set; } = string.Empty;
    public int? Estimacion { get; set; }
    public string Resultado { get; set; } = string.Empty;
    public int CantidadTareas { get; set; }
}
