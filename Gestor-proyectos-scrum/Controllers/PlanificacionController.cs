using Gestor_proyectos_scrum.Data;
using Gestor_proyectos_scrum.Models;
using Gestor_proyectos_scrum.Services;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace Gestor_proyectos_scrum.Controllers;

public class PlanificacionController : Controller
{
    private readonly PlanificacionRepositorio _planificacionRepositorio;
    private readonly UsuarioActual _usuarioActual;

    public PlanificacionController(PlanificacionRepositorio planificacionRepositorio, UsuarioActual usuarioActual)
    {
        _planificacionRepositorio = planificacionRepositorio;
        _usuarioActual = usuarioActual;
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
        modelo.HistoriasNoTerminadas = (await _planificacionRepositorio.ListarHistoriasNoTerminadasAsync(idProyecto.Value)).ToList();

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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Comprometer(int idProyecto, int idSprint, List<int>? idHistoria, bool confirmo)
    {
        if (!_usuarioActual.PuedePlanificarSprint())
        {
            TempData["Error"] = "No tienes permisos para planificar el Sprint.";
            return RedirectToAction(nameof(Index), new { idProyecto, idSprint });
        }

        var sprint = await _planificacionRepositorio.ObtenerSprintAsync(idSprint);
        if (sprint is null)
        {
            return NotFound();
        }

        if (sprint.IdProyecto != idProyecto)
        {
            return BadRequest();
        }

        if (sprint.Estado is not ("Planificado" or "Activo"))
        {
            TempData["Error"] = "El Sprint debe estar Planificado o Activo para comprometer historias.";
            return RedirectToAction(nameof(Index), new { idProyecto, idSprint });
        }

        var proyectosAbiertos = await _planificacionRepositorio.ListarProyectosAbiertosAsync();
        if (!proyectosAbiertos.Any(p => p.IdProyecto == idProyecto))
        {
            TempData["Error"] = "El proyecto está Cerrado.";
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

        var candidatas = (await _planificacionRepositorio.ListarCandidatasAsync(idProyecto))
            .ToDictionary(c => c.IdHistoria);

        var invalidas = idsSeleccionadas
            .Where(id => !candidatas.TryGetValue(id, out var historia) || !historia.EsElegible)
            .ToList();

        if (invalidas.Count > 0)
        {
            TempData["Error"] = "Una o más historias seleccionadas ya no están disponibles para este Sprint. Vuelve a intentarlo.";
            return RedirectToAction(nameof(Index), new { idProyecto, idSprint });
        }

        var comprometidasActuales = await _planificacionRepositorio.ListarComprometidasAsync(idSprint);
        var puntosYaComprometidos = comprometidasActuales.Sum(c => c.Estimacion ?? 0);
        var puntosNuevos = idsSeleccionadas.Sum(id => candidatas[id].Estimacion ?? 0);
        var disponibles = sprint.CapacidadPuntos - puntosYaComprometidos;

        if (puntosNuevos > disponibles)
        {
            var exceso = puntosNuevos - disponibles;
            TempData["Error"] = $"Las historias seleccionadas suman {puntosNuevos} pts, pero solo quedan {disponibles} pts disponibles de capacidad (sobran {exceso} pts).";
            return RedirectToAction(nameof(Index), new { idProyecto, idSprint });
        }

        try
        {
            await _planificacionRepositorio.ComprometerAsync(idSprint, idsSeleccionadas, _usuarioActual.IdUsuario);
            var codigos = string.Join(", ", idsSeleccionadas.Select(id => candidatas[id].Codigo));
            TempData["Mensaje"] = $"Se comprometieron {idsSeleccionadas.Count} historia(s) al Sprint \"{sprint.Nombre}\": {codigos}.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            TempData["Error"] = "Una de las historias ya fue comprometida por otro usuario. Vuelve a intentarlo.";
        }

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

        try
        {
            await _planificacionRepositorio.QuitarHistoriaAsync(idSprint, idHistoria, _usuarioActual.IdUsuario);
            TempData["Mensaje"] = "La historia se quitó del Sprint y volvió al Product Backlog.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

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

        try
        {
            await _planificacionRepositorio.AceptarHistoriaAsync(idSprint, idHistoria, _usuarioActual.IdUsuario);
            TempData["Mensaje"] = "La historia se aceptó en la revisión del Sprint.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

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

        try
        {
            await _planificacionRepositorio.RechazarHistoriaAsync(idSprint, idHistoria, comentario.Trim(), _usuarioActual.IdUsuario);
            TempData["Mensaje"] = "La historia se rechazó en la revisión del Sprint. Sigue comprometida.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

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

        try
        {
            await _planificacionRepositorio.RegresarAlBacklogAsync(idSprintHistoria, _usuarioActual.IdUsuario);
            TempData["Mensaje"] = "La historia volvió al Product Backlog como 'Lista'.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

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

        try
        {
            await _planificacionRepositorio.ReplanificarAsync(idSprintHistoria, idSprintDestino, _usuarioActual.IdUsuario);
            TempData["Mensaje"] = "La historia se replanificó al nuevo Sprint junto con sus tareas pendientes.";
        }
        catch (InvalidOperationException ex)
        {
            TempData["Error"] = ex.Message;
        }

        return RedirectToAction(nameof(Index), new { idProyecto, idSprint });
    }
}
