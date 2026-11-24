namespace Gestor_proyectos_scrum.Models;

/// <summary>Fila de Auditoria para la pestaña Historial de Proyectos/Detalle.</summary>
public class AuditoriaItem
{
    public string Usuario { get; set; } = string.Empty;
    public string Accion { get; set; } = string.Empty;
    public string? Detalle { get; set; }
    public DateTime Fecha { get; set; }
}
