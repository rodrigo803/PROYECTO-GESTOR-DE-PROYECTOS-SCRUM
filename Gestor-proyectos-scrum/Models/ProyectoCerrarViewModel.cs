using System.ComponentModel.DataAnnotations;

namespace Gestor_proyectos_scrum.Models;

/// <summary>ViewModel para Proyectos/Cerrar: resumen de cierre + motivo/observaciones.</summary>
public class ProyectoCerrarViewModel
{
    public int IdProyecto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }

    public int CantidadSprints { get; set; }
    public int SprintsCerrados { get; set; }

    /// <summary>Nombre del sprint Activo del proyecto, o null si no tiene ninguno.</summary>
    public string? SprintActivoNombre { get; set; }

    public int HistoriasPendientes { get; set; }
    public int ImpedimentosAbiertos { get; set; }
    public int CantidadMiembros { get; set; }

    [Required(ErrorMessage = "Debes seleccionar un motivo de cierre.")]
    [Display(Name = "Motivo de Cierre")]
    public string? Motivo { get; set; }

    [StringLength(250, ErrorMessage = "Las observaciones no pueden superar los 250 caracteres.")]
    [Display(Name = "Observaciones")]
    public string? Observaciones { get; set; }
}
