namespace Gestor_proyectos_scrum.Models;

/// <summary>Entidad de la tabla SprintHistorias (HU-06): historias comprometidas en cada Sprint.</summary>
public class SprintHistoria
{
    public int IdSprintHistoria { get; set; }
    public int IdSprint { get; set; }
    public int IdHistoria { get; set; }
    public DateTime FechaCompromiso { get; set; }
    public string Resultado { get; set; } = string.Empty;
}
