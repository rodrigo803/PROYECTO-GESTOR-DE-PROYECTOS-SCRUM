namespace Gestor_proyectos_scrum.Models;

/// <summary>Entidad de la tabla Proyectos.</summary>
public class Proyecto
{
    public int IdProyecto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? MotivoCierre { get; set; }
    public DateTime? FechaCierre { get; set; }
}
