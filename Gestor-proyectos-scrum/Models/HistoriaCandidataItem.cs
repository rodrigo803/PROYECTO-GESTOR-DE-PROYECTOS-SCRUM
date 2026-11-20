namespace Gestor_proyectos_scrum.Models;

/// <summary>Historia del Product Backlog candidata a entrar al Sprint elegido (Planificacion/Index).</summary>
public class HistoriaCandidataItem
{
    public int IdHistoria { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Como { get; set; } = string.Empty;
    public string Quiero { get; set; } = string.Empty;
    public string Para { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public int OrdenPrioridad { get; set; }
    public int? Estimacion { get; set; }
    public int CantidadCriterios { get; set; }

    /// <summary>True si ya está comprometida en el Sprint elegido (checkbox marcado).</summary>
    public bool YaComprometida { get; set; }

    /// <summary>Motivos por los que no puede entrar al Sprint; vacío si es elegible.</summary>
    public List<string> Motivos { get; set; } = new();

    public bool EsElegible => Motivos.Count == 0;
}
