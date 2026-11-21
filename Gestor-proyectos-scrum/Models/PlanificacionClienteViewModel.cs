namespace Gestor_proyectos_scrum.Models;

/// <summary>ViewModel de pantalla para Planificacion/Cliente.</summary>
public class PlanificacionClienteViewModel
{
    public List<ProyectoOpcion> Proyectos { get; set; } = new();
    public List<SprintOpcion> Sprints { get; set; } = new();

    public int? IdProyecto { get; set; }
    public int? IdSprint { get; set; }

    public SprintPlanificacionInfo? Sprint { get; set; }

    public List<HistoriaComprometidaClienteItem> Comprometidas { get; set; } = new();

    public int PuntosComprometidos { get; set; }
}
