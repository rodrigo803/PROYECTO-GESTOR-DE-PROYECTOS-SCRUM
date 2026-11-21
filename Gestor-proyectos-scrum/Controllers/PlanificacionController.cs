using Gestor_proyectos_scrum.Models;
using Gestor_proyectos_scrum.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gestor_proyectos_scrum.Controllers;

/// <summary>
/// Controlador MVC para HU-06: Sprint Backlog / Planificación.
/// Consume la Minimal API (Gestor-proyectos-scrum.Api) mediante ScrumApiClient;
/// no accede directamente a MySQL.
/// </summary>
public class PlanificacionController : Controller
{
    private readonly ScrumApiClient _apiClient;
    private readonly UsuarioActual _usuarioActual;

    public PlanificacionController(ScrumApiClient apiClient, UsuarioActual usuarioActual)
    {
        _apiClient = apiClient;
        _usuarioActual = usuarioActual;
    }

    public async Task<IActionResult> Index(int? idProyecto, int? idSprint)
    {
        var proyectosRes = await _apiClient.ObtenerProyectosAbiertosAsync();
        var proyectos = proyectosRes.Datos ?? new List<ProyectoOpcion>();

        var modelo = new PlanificacionIndexViewModel { Proyectos = proyectos };

        if (proyectos.Count == 0)
        {
            return View(modelo);
        }

        if (idProyecto is null || !proyectos.Any(p => p.IdProyecto == idProyecto))
        {
            idProyecto = proyectos.FirstOrDefault(p => p.Estado == "Activo")?.IdProyecto
                ?? proyectos.First().IdProyecto;
        }

        modelo.IdProyecto = idProyecto;

        var sprints = (await _apiClient.ObtenerSprintsPlanificablesAsync(idProyecto.Value)).Datos ?? new List<SprintOpcion>();
        modelo.Sprints = sprints;

        if (idSprint is null || !sprints.Any(s => s.IdSprint == idSprint))
        {
            idSprint = sprints.FirstOrDefault(s => s.Estado == "Planificado")?.IdSprint
                ?? sprints.FirstOrDefault(s => s.Estado == "Activo")?.IdSprint;
        }

        modelo.IdSprint = idSprint;
        modelo.HistoriasNoTerminadas = (await _apiClient.ObtenerHistoriasNoTerminadasAsync(idProyecto.Value)).Datos ?? new List<HistoriaNoTerminadaItem>();

        if (idSprint is null)
        {
            return View(modelo);
        }

        modelo.Sprint = (await _apiClient.ObtenerSprintPlanificacionAsync(idSprint.Value)).Datos;
        modelo.Candidatas = (await _apiClient.ObtenerCandidatasAsync(idProyecto.Value)).Datos ?? new List<HistoriaCandidataItem>();
        modelo.Comprometidas = (await _apiClient.ObtenerComprometidasAsync(idSprint.Value)).Datos ?? new List<HistoriaComprometidaItem>();
        modelo.PuntosComprometidos = modelo.Comprometidas.Sum(c => c.Estimacion ?? 0);

        return View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> Cliente(int? idProyecto, int? idSprint)
    {
        var proyectos = (await _apiClient.ObtenerProyectosDeMiembroAsync()).Datos ?? new List<ProyectoOpcion>();
        var modelo = new PlanificacionClienteViewModel { Proyectos = proyectos };

        if (proyectos.Count == 0)
        {
            return View(modelo);
        }

        if (idProyecto is not null && !proyectos.Any(p => p.IdProyecto == idProyecto))
        {
            TempData["Error"] = "No tienes acceso a ese proyecto.";
            return RedirectToAction(nameof(Cliente));
        }

        idProyecto ??= proyectos.First().IdProyecto;
        modelo.IdProyecto = idProyecto;

        var sprints = (await _apiClient.ObtenerSprintsDelProyectoClienteAsync(idProyecto.Value)).Datos ?? new List<SprintOpcion>();
        modelo.Sprints = sprints;

        if (idSprint is not null && !sprints.Any(s => s.IdSprint == idSprint))
        {
            TempData["Error"] = "Ese Sprint no pertenece a este proyecto.";
            return RedirectToAction(nameof(Cliente), new { idProyecto });
        }

        idSprint ??= sprints.FirstOrDefault()?.IdSprint;
        modelo.IdSprint = idSprint;

        if (idSprint is null)
        {
            return View(modelo);
        }

        modelo.Sprint = (await _apiClient.ObtenerSprintClienteAsync(idSprint.Value)).Datos;
        modelo.Comprometidas = (await _apiClient.ObtenerComprometidasClienteAsync(idSprint.Value)).Datos ?? new List<HistoriaComprometidaClienteItem>();
        modelo.PuntosComprometidos = modelo.Comprometidas.Sum(c => c.Estimacion ?? 0);

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Comprometer(int idProyecto, int idSprint, List<int>? idHistoria, bool confirmo)
    {
        if (!_usuarioActual.PuedePlanificarSprint())
        {
            TempData["Error"] = "No tienes permisos para planificar el Sprint.";
            return RedirectToAction(nameof(Index), new { idProyecto, idSprint });
        }

        var idsSeleccionadas = (idHistoria ?? new List<int>()).Distinct().ToList();
        if (idsSeleccionadas.Count == 0)
        {
            TempData["Error"] = "Debes seleccionar al menos una historia.";
            return RedirectToAction(nameof(Index), new { idProyecto, idSprint });
        }

        if (!confirmo)
        {
            TempData["Error"] = "Debes confirmar que las historias cumplen los criterios de aceptación y estimación.";
            return RedirectToAction(nameof(Index), new { idProyecto, idSprint });
        }

        var resultado = await _apiClient.ComprometerHistoriasAsync(idSprint, idsSeleccionadas);
        TempData[resultado.Exito ? "Mensaje" : "Error"] = resultado.Exito
            ? resultado.Datos?.Mensaje ?? "Historias comprometidas correctamente."
            : resultado.Error ?? "No fue posible comprometer las historias.";

        return RedirectToAction(nameof(Index), new { idProyecto, idSprint });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuitarHistoria(int idProyecto, int idSprint, int idHistoria)
    {
        if (!_usuarioActual.PuedePlanificarSprint())
        {
            TempData["Error"] = "No tienes permisos para planificar el Sprint.";
            return RedirectToAction(nameof(Index), new { idProyecto, idSprint });
        }

        var resultado = await _apiClient.QuitarHistoriaAsync(idSprint, idHistoria);
        TempData[resultado.Exito ? "Mensaje" : "Error"] = resultado.Exito
            ? resultado.Datos?.Mensaje ?? "La historia se quitó del Sprint."
            : resultado.Error ?? "No fue posible quitar la historia.";

        return RedirectToAction(nameof(Index), new { idProyecto, idSprint });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AceptarHistoria(int idProyecto, int idSprint, int idHistoria)
    {
        if (!_usuarioActual.PuedeAprobarHistorias())
        {
            TempData["Error"] = "No tienes permisos para aceptar o rechazar historias.";
            return RedirectToAction(nameof(Index), new { idProyecto, idSprint });
        }

        var resultado = await _apiClient.AceptarHistoriaAsync(idSprint, idHistoria);
        TempData[resultado.Exito ? "Mensaje" : "Error"] = resultado.Exito
            ? resultado.Datos?.Mensaje ?? "La historia se aceptó en la revisión del Sprint."
            : resultado.Error ?? "No fue posible aceptar la historia.";

        return RedirectToAction(nameof(Index), new { idProyecto, idSprint });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RechazarHistoria(int idProyecto, int idSprint, int idHistoria, string comentario)
    {
        if (!_usuarioActual.PuedeAprobarHistorias())
        {
            TempData["Error"] = "No tienes permisos para aceptar o rechazar historias.";
            return RedirectToAction(nameof(Index), new { idProyecto, idSprint });
        }

        if (string.IsNullOrWhiteSpace(comentario))
        {
            TempData["Error"] = "Debes escribir un comentario para rechazar la historia.";
            return RedirectToAction(nameof(Index), new { idProyecto, idSprint });
        }

        var resultado = await _apiClient.RechazarHistoriaAsync(idSprint, idHistoria, comentario.Trim());
        TempData[resultado.Exito ? "Mensaje" : "Error"] = resultado.Exito
            ? resultado.Datos?.Mensaje ?? "La historia se rechazó en la revisión del Sprint."
            : resultado.Error ?? "No fue posible rechazar la historia.";

        return RedirectToAction(nameof(Index), new { idProyecto, idSprint });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RegresarAlBacklog(int idProyecto, int idSprint, int idSprintHistoria)
    {
        if (!_usuarioActual.PuedePlanificarSprint())
        {
            TempData["Error"] = "No tienes permisos para planificar el Sprint.";
            return RedirectToAction(nameof(Index), new { idProyecto, idSprint });
        }

        var resultado = await _apiClient.RegresarAlBacklogAsync(idSprintHistoria);
        TempData[resultado.Exito ? "Mensaje" : "Error"] = resultado.Exito
            ? resultado.Datos?.Mensaje ?? "La historia volvió al Product Backlog."
            : resultado.Error ?? "No fue posible regresar la historia al Backlog.";

        return RedirectToAction(nameof(Index), new { idProyecto, idSprint });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Replanificar(int idProyecto, int idSprint, int idSprintHistoria, int idSprintDestino)
    {
        if (!_usuarioActual.PuedePlanificarSprint())
        {
            TempData["Error"] = "No tienes permisos para planificar el Sprint.";
            return RedirectToAction(nameof(Index), new { idProyecto, idSprint });
        }

        var resultado = await _apiClient.ReplanificarAsync(idSprintHistoria, idSprintDestino);
        TempData[resultado.Exito ? "Mensaje" : "Error"] = resultado.Exito
            ? resultado.Datos?.Mensaje ?? "La historia se replanificó al nuevo Sprint."
            : resultado.Error ?? "No fue posible replanificar la historia.";

        return RedirectToAction(nameof(Index), new { idProyecto, idSprint });
    }
}
