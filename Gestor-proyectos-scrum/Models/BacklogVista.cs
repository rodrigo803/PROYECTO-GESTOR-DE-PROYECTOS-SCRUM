namespace Gestor_proyectos_scrum.Models;

/// <summary>ViewModel de Backlog/Index (HU-04).</summary>
public class BacklogVista
{
    public int ProyectoId { get; set; }
    public List<ProyectoOpcion> Proyectos { get; set; } = new();
    public List<HistoriaDto> Historias { get; set; } = new();
}
