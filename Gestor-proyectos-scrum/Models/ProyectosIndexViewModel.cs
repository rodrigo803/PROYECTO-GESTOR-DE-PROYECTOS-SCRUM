namespace Gestor_proyectos_scrum.Models;

/// <summary>ViewModel de pantalla para Proyectos/Index: listado, métricas y filtros aplicados.</summary>
public class ProyectosIndexViewModel
{
    public IReadOnlyList<ProyectoListaItem> Proyectos { get; set; } = new List<ProyectoListaItem>();
    public ProyectoMetricas Metricas { get; set; } = new();

    public string? Buscar { get; set; }
    public string? Estado { get; set; }
    public string? Orden { get; set; }
}
