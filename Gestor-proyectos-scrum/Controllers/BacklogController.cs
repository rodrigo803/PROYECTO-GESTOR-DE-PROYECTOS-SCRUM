using Gestor_proyectos_scrum.Models;
using Gestor_proyectos_scrum.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gestor_proyectos_scrum.Controllers;

/// <summary>
/// Controlador MVC para HU-04: Product Backlog. Consume la Minimal API
/// (Endpoints/BacklogEndpoints.cs) mediante ScrumApiClient; no accede a MySQL directo.
/// Migrado desde la propuesta de Jesús en feature/jesus (pensada sin Controllers, con
/// Minimal APIs y vistas Razor renderizadas a mano): las validaciones de solo-UI
/// (sesión y permisos) se quedan aquí, las reglas de negocio con datos de la base
/// (prioridad/estado válidos, estimación y criterios, aprobaciones, Sprints) las valida
/// la Api.
/// </summary>
public class BacklogController : Controller
{
    private readonly ScrumApiClient _apiClient;
    private readonly UsuarioActual _usuarioActual;

    public BacklogController(ScrumApiClient apiClient, UsuarioActual usuarioActual)
    {
        _apiClient = apiClient;
        _usuarioActual = usuarioActual;
    }

    private bool PuedeEditar => _usuarioActual.Rol is "Administrador" or "Product Owner";

    [HttpGet]
    public async Task<IActionResult> Index(int? proyectoId)
    {
        var proyectosRes = await _apiClient.ObtenerProyectosBacklogAsync();
        var proyectos = proyectosRes.Datos ?? new List<ProyectoOpcion>();

        var vm = new BacklogVista { Proyectos = proyectos };
        vm.ProyectoId = proyectos.Any(p => p.IdProyecto == proyectoId)
            ? proyectoId!.Value
            : proyectos.FirstOrDefault()?.IdProyecto ?? 0;

        if (vm.ProyectoId > 0)
        {
            var historiasRes = await _apiClient.ObtenerHistoriasBacklogAsync(vm.ProyectoId);
            if (!historiasRes.Exito)
            {
                TempData["Error"] = historiasRes.Error;
            }

            vm.Historias = historiasRes.Datos ?? new List<HistoriaDto>();
        }

        ViewBag.PuedeEditar = PuedeEditar;
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Guardar(HistoriaDto h)
    {
        if (!PuedeEditar)
        {
            return Forbid();
        }

        var redirigir = RedirectToAction(nameof(Index), new { proyectoId = h.IdProyecto });

        var payload = new
        {
            h.IdProyecto,
            h.Codigo,
            h.Como,
            h.Quiero,
            h.Para,
            h.Prioridad,
            h.OrdenPrioridad,
            h.Estimacion,
            h.Estado,
            h.CriteriosTexto
        };

        var resultado = h.IdHistoria == 0
            ? await _apiClient.CrearHistoriaAsync(payload)
            : await _apiClient.ActualizarHistoriaAsync(h.IdHistoria, payload);

        TempData[resultado.Exito ? "Mensaje" : "Error"] = resultado.Exito
            ? resultado.Datos?.Mensaje ?? "Historia guardada en MySQL."
            : resultado.Error ?? "No fue posible guardar la historia.";

        return redirigir;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id, int proyectoId)
    {
        if (!PuedeEditar)
        {
            return Forbid();
        }

        var resultado = await _apiClient.EliminarHistoriaAsync(id);
        TempData[resultado.Exito ? "Mensaje" : "Error"] = resultado.Exito
            ? resultado.Datos?.Mensaje ?? "Historia eliminada."
            : resultado.Error ?? "No fue posible eliminar la historia.";

        return RedirectToAction(nameof(Index), new { proyectoId });
    }
}
