using Gestor_proyectos_scrum.Data;
using Gestor_proyectos_scrum.Models;
using Microsoft.AspNetCore.Mvc;

namespace Gestor_proyectos_scrum.Controllers;

public class PlanificacionController : Controller
{
    private readonly PlanificacionRepositorio _planificacionRepositorio;

    public PlanificacionController(PlanificacionRepositorio planificacionRepositorio)
    {
        _planificacionRepositorio = planificacionRepositorio;
    }

    public async Task<IActionResult> Index(int? idProyecto, int? idSprint)
    {
        var proyectos = await _planificacionRepositorio.ListarProyectosAbiertosAsync();

        var modelo = new PlanificacionIndexViewModel { Proyectos = proyectos.ToList() };

        if (!proyectos.Any())
        {
            return View(modelo);
        }

        if (idProyecto is null || !proyectos.Any(p => p.IdProyecto == idProyecto))
        {
            idProyecto = proyectos.FirstOrDefault(p => p.Estado == "Activo")?.IdProyecto
                ?? proyectos.First().IdProyecto;
        }

        modelo.IdProyecto = idProyecto;

        var sprints = await _planificacionRepositorio.ListarSprintsPlanificablesAsync(idProyecto.Value);
        modelo.Sprints = sprints.ToList();

        if (idSprint is null || !sprints.Any(s => s.IdSprint == idSprint))
        {
            idSprint = sprints.FirstOrDefault(s => s.Estado == "Planificado")?.IdSprint
                ?? sprints.FirstOrDefault(s => s.Estado == "Activo")?.IdSprint;
        }

        modelo.IdSprint = idSprint;

        if (idSprint is null)
        {
            return View(modelo);
        }

        modelo.Sprint = await _planificacionRepositorio.ObtenerSprintAsync(idSprint.Value);
        modelo.Candidatas = (await _planificacionRepositorio.ListarCandidatasAsync(idProyecto.Value)).ToList();
        modelo.Comprometidas = (await _planificacionRepositorio.ListarComprometidasAsync(idSprint.Value)).ToList();
        modelo.PuntosComprometidos = modelo.Comprometidas.Sum(c => c.Estimacion ?? 0);

        return View(modelo);
    }
}
