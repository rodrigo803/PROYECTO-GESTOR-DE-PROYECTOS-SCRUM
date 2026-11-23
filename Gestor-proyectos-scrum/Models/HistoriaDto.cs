namespace Gestor_proyectos_scrum.Models;

/// <summary>Historia de usuario del Product Backlog (HU-04). Mismos nombres de propiedad
/// que la propuesta original de Jesús para poder reutilizar Views/Backlog/Index.cshtml tal cual.</summary>
public class HistoriaDto
{
    public int IdHistoria { get; set; }
    public int IdProyecto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Como { get; set; } = string.Empty;
    public string Quiero { get; set; } = string.Empty;
    public string Para { get; set; } = string.Empty;
    public string Prioridad { get; set; } = "Media";
    public int OrdenPrioridad { get; set; } = 1;
    public int? Estimacion { get; set; }
    public string Estado { get; set; } = "Pendiente";
    public string CriteriosTexto { get; set; } = string.Empty;
    public int TotalCriterios { get; set; }
    public string? NombreProyecto { get; set; }
}
