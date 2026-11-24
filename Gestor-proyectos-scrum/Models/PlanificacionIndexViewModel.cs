namespace Gestor_proyectos_scrum.Models;

/// <summary>ViewModel de pantalla para Planificacion/Index.</summary>
public class PlanificacionIndexViewModel
{
    public List<ProyectoOpcion> Proyectos { get; set; } = new();
    public List<SprintOpcion> Sprints { get; set; } = new();

    public int? IdProyecto { get; set; }
    public int? IdSprint { get; set; }

    /// <summary>Null cuando el proyecto elegido no tiene Sprints Planificados ni Activos.</summary>
    public SprintPlanificacionInfo? Sprint { get; set; }

    public List<HistoriaCandidataItem> Candidatas { get; set; } = new();
    public List<HistoriaComprometidaItem> Comprometidas { get; set; } = new();

    public int PuntosComprometidos { get; set; }

    public List<HistoriaNoTerminadaItem> HistoriasNoTerminadas { get; set; } = new();
}
