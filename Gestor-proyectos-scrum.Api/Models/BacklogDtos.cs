namespace Gestor_proyectos_scrum.Api.Models;

// =========================================================================
// HU-04: PRODUCT BACKLOG (migrado desde la propuesta de Jesús en feature/jesus)
// =========================================================================

public class ProyectoBacklogOpcionDto
{
    public int IdProyecto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}

public class HistoriaBacklogDto
{
    public int IdHistoria { get; set; }
    public int IdProyecto { get; set; }
    public string? NombreProyecto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Como { get; set; } = string.Empty;
    public string Quiero { get; set; } = string.Empty;
    public string Para { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public int OrdenPrioridad { get; set; }
    public int? Estimacion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int TotalCriterios { get; set; }
    public string? CriteriosTexto { get; set; }
}

public class GuardarHistoriaRequest
{
    public int IdProyecto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Como { get; set; } = string.Empty;
    public string Quiero { get; set; } = string.Empty;
    public string Para { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public int OrdenPrioridad { get; set; }
    public int? Estimacion { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? CriteriosTexto { get; set; }
}
