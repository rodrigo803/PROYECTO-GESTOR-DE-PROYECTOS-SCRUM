namespace Gestor_proyectos_scrum.Models;

/// <summary>Fila del listado de proyectos (Proyectos/Index).</summary>
public class ProyectoListaItem
{
    public int IdProyecto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? ProductOwner { get; set; }
    public string? ScrumMaster { get; set; }
    public int CantidadMiembros { get; set; }
    public List<string> InicialesMiembros { get; set; } = new();
    public decimal Avance { get; set; }
}
