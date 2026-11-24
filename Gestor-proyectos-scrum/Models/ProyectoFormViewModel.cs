using System.ComponentModel.DataAnnotations;

namespace Gestor_proyectos_scrum.Models;

/// <summary>ViewModel para Proyectos/Crear y Proyectos/Editar.</summary>
public class ProyectoFormViewModel
{
    public int? IdProyecto { get; set; }

    public string? Codigo { get; set; }

    [Required(ErrorMessage = "El nombre del proyecto es obligatorio.")]
    [StringLength(100, ErrorMessage = "El nombre no puede superar los 100 caracteres.")]
    [Display(Name = "Nombre del Proyecto")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "La descripción es obligatoria.")]
    [StringLength(500, ErrorMessage = "La descripción no puede superar los 500 caracteres.")]
    [Display(Name = "Descripción")]
    public string Descripcion { get; set; } = string.Empty;

    [Required(ErrorMessage = "La fecha de inicio es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de Inicio")]
    public DateTime FechaInicio { get; set; } = DateTime.Today;

    [Required(ErrorMessage = "La fecha de finalización es obligatoria.")]
    [DataType(DataType.Date)]
    [Display(Name = "Fecha de Finalización")]
    public DateTime FechaFin { get; set; } = DateTime.Today.AddMonths(3);

    [Required]
    [Display(Name = "Estado")]
    public string Estado { get; set; } = "Planificado";

    [Required(ErrorMessage = "Debes seleccionar un Product Owner.")]
    [Display(Name = "Product Owner")]
    public int? IdProductOwner { get; set; }

    [Display(Name = "Scrum Master")]
    public int? IdScrumMaster { get; set; }

    [Display(Name = "Developers")]
    public List<int> IdsDevelopers { get; set; } = new();

    /// <summary>Usuarios activos disponibles para los selects. Se llena en el controller.</summary>
    public List<UsuarioOpcion> Usuarios { get; set; } = new();
}
