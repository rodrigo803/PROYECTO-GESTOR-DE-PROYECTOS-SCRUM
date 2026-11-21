using Gestor_proyectos_scrum.Models;
using Gestor_proyectos_scrum.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gestor_proyectos_scrum.Controllers;

/// <summary>
/// Controlador MVC para HU-05: Gestión de Sprints.
/// Consume la Minimal API mediante ScrumApiClient.
/// </summary>
public class SprintsController : Controller
{
    private readonly ScrumApiClient _apiClient;

    public SprintsController(ScrumApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? idProyecto, string? estado, string? buscar)
    {
        var perfilTask = _apiClient.ObtenerPerfilActivoAsync();
        var proyectosTask = _apiClient.ObtenerProyectosSprintAsync();
        var sprintsTask = _apiClient.ObtenerSprintsAsync(idProyecto, estado, buscar);

        await Task.WhenAll(perfilTask, proyectosTask, sprintsTask);

        var sprintsRes = await sprintsTask;
        var proyectosRes = await proyectosTask;

        if (!sprintsRes.Exito)
        {
            TempData["Error"] = sprintsRes.Error;
        }

        var sprints = sprintsRes.Datos ?? new List<SprintItemViewModel>();
        var sprintsActivos = sprints.Where(s => string.Equals(s.Estado, "Activo", StringComparison.OrdinalIgnoreCase)).ToList();
        var primerActivo = sprintsActivos.FirstOrDefault();
        var sprintsConTareas = sprints.Where(s => s.TotalTareas > 0).ToList();

        var vm = new SprintsIndexViewModel
        {
            Sprints = sprints,
            Proyectos = proyectosRes.Datos ?? new List<ProyectoItemViewModel>(),
            IdProyectoFiltro = idProyecto,
            EstadoFiltro = estado,
            Buscar = buscar,
            TotalSprints = sprints.Count,
            SprintsActivosCount = sprintsActivos.Count,
            NombreSprintActivoDestacado = primerActivo is not null ? primerActivo.Nombre : "Sin activo",
            SubtituloSprintActivo = primerActivo is not null
                ? $"Finaliza en {primerActivo.DiasRestantes} días ({primerActivo.ProyectoCodigo})"
                : "No hay sprints en ejecución",
            TotalPuntosComprometidos = sprints.Sum(s => s.PuntosComprometidos),
            TotalCapacidadPuntos = sprints.Sum(s => s.CapacidadPuntos),
            TotalHorasEstimadas = sprints.Sum(s => s.HorasEstimadas),
            AvancePonderadoPromedio = sprintsConTareas.Count > 0
                ? Math.Round(sprintsConTareas.Average(s => s.AvancePonderado), 1)
                : (sprints.Count > 0 ? Math.Round(sprints.Average(s => s.AvancePonderado), 1) : 0),
            PerfilActual = await perfilTask
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var perfilTask = _apiClient.ObtenerPerfilActivoAsync();
        var detalleTask = _apiClient.ObtenerSprintDetalleAsync(id);

        await Task.WhenAll(perfilTask, detalleTask);

        var detalleRes = await detalleTask;
        if (!detalleRes.Exito || detalleRes.Datos is null)
        {
            TempData["Error"] = detalleRes.Error ?? "No se encontró el Sprint solicitado.";
            return RedirectToAction(nameof(Index));
        }

        var vm = new SprintDetailsViewModel
        {
            Sprint = detalleRes.Datos.Sprint,
            Historias = detalleRes.Datos.Historias,
            PerfilActual = await perfilTask
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create(int? idProyecto)
    {
        var proyectosRes = await _apiClient.ObtenerProyectosSprintAsync();
        var proyectosDisponibles = (proyectosRes.Datos ?? new List<ProyectoItemViewModel>())
            .Where(p => !string.Equals(p.Estado, "Cerrado", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var proySeleccionado = proyectosDisponibles.FirstOrDefault(p => p.IdProyecto == idProyecto)
            ?? proyectosDisponibles.FirstOrDefault();

        var fechaInicioSugerida = DateTime.Today;
        var fechaFinSugerida = DateTime.Today.AddDays(14);

        if (proySeleccionado is not null)
        {
            if (fechaInicioSugerida < proySeleccionado.FechaInicio.Date || fechaInicioSugerida >= proySeleccionado.FechaFin.Date)
            {
                fechaInicioSugerida = proySeleccionado.FechaInicio.Date;
            }

            fechaFinSugerida = fechaInicioSugerida.AddDays(14);
            if (fechaFinSugerida > proySeleccionado.FechaFin.Date)
            {
                fechaFinSugerida = proySeleccionado.FechaFin.Date;
            }
        }

        var vm = new CrearSprintViewModel
        {
            IdProyecto = proySeleccionado?.IdProyecto ?? 0,
            FechaInicio = fechaInicioSugerida,
            FechaFin = fechaFinSugerida,
            CapacidadPuntos = 35,
            ProyectosDisponibles = proyectosDisponibles
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CrearSprintViewModel vm)
    {
        if (vm.FechaInicio.Date >= vm.FechaFin.Date)
        {
            ModelState.AddModelError(nameof(vm.FechaFin), "La fecha de finalización debe ser posterior a la fecha de inicio.");
        }

        if (!ModelState.IsValid)
        {
            var errores = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["Error"] = string.IsNullOrWhiteSpace(errores) ? "Verifique los datos ingresados del Sprint." : errores;
            await RecargarProyectosDisponiblesAsync(vm);
            return View(vm);
        }

        var resultado = await _apiClient.CrearSprintAsync(new
        {
            vm.IdProyecto,
            vm.Nombre,
            vm.Objetivo,
            vm.FechaInicio,
            vm.FechaFin,
            vm.CapacidadPuntos
        });

        if (!resultado.Exito)
        {
            var msg = resultado.Error ?? "No fue posible crear el Sprint.";
            ModelState.AddModelError(string.Empty, msg);
            TempData["Error"] = msg;

            await RecargarProyectosDisponiblesAsync(vm);
            return View(vm);
        }

        TempData["Mensaje"] = resultado.Datos?.Mensaje ?? "Sprint creado correctamente en estado Planificado.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var detalleRes = await _apiClient.ObtenerSprintDetalleAsync(id);
        if (!detalleRes.Exito || detalleRes.Datos is null)
        {
            TempData["Error"] = detalleRes.Error ?? "No se encontró el Sprint solicitado.";
            return RedirectToAction(nameof(Index));
        }

        var s = detalleRes.Datos.Sprint;
        if (string.Equals(s.Estado, "Cerrado", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = "No se puede editar un Sprint que ya se encuentra Cerrado.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var vm = new EditarSprintViewModel
        {
            IdSprint = s.IdSprint,
            IdProyecto = s.IdProyecto,
            ProyectoCodigo = s.ProyectoCodigo,
            ProyectoNombre = s.ProyectoNombre,
            ProyectoFechaInicio = s.ProyectoFechaInicio,
            ProyectoFechaFin = s.ProyectoFechaFin,
            Estado = s.Estado,
            Nombre = s.Nombre,
            Objetivo = s.Objetivo,
            FechaInicio = s.FechaInicio,
            FechaFin = s.FechaFin,
            CapacidadPuntos = s.CapacidadPuntos
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, EditarSprintViewModel vm)
    {
        var targetId = vm.IdSprint > 0 ? vm.IdSprint : (id ?? 0);
        vm.IdSprint = targetId;

        if (vm.FechaInicio.Date >= vm.FechaFin.Date)
        {
            ModelState.AddModelError(nameof(vm.FechaFin), "La fecha de finalización debe ser posterior a la fecha de inicio.");
        }

        if (!ModelState.IsValid)
        {
            var errores = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["Error"] = string.IsNullOrWhiteSpace(errores) ? "Verifique los datos ingresados del Sprint." : errores;
            return View(vm);
        }

        var resultado = await _apiClient.ActualizarSprintAsync(targetId, new
        {
            vm.Nombre,
            vm.Objetivo,
            vm.FechaInicio,
            vm.FechaFin,
            vm.CapacidadPuntos
        });

        if (!resultado.Exito)
        {
            var msg = resultado.Error ?? "No fue posible actualizar el Sprint.";
            ModelState.AddModelError(string.Empty, msg);
            TempData["Error"] = msg;
            return View(vm);
        }

        TempData["Mensaje"] = resultado.Datos?.Mensaje ?? "Sprint actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Iniciar(int id, string? returnUrl)
    {
        var resultado = await _apiClient.CambiarEstadoSprintAsync(id, "Activo");
        if (resultado.Exito)
        {
            TempData["Mensaje"] = resultado.Datos?.Mensaje ?? "Sprint iniciado correctamente.";
        }
        else
        {
            TempData["Error"] = resultado.Error ?? "No fue posible iniciar el Sprint.";
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cerrar(int id, string? destinoIncompletas, string? returnUrl)
    {
        var destino = string.Equals(destinoIncompletas, "Replanificada", StringComparison.OrdinalIgnoreCase)
            ? "Replanificada"
            : "Devuelta";

        var resultado = await _apiClient.CambiarEstadoSprintAsync(id, "Cerrado", destino);
        if (resultado.Exito)
        {
            TempData["Mensaje"] = resultado.Datos?.Mensaje ?? "Sprint cerrado correctamente.";
        }
        else
        {
            TempData["Error"] = resultado.Error ?? "No fue posible cerrar el Sprint.";
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Eliminar(int id)
    {
        var resultado = await _apiClient.EliminarSprintAsync(id);
        if (resultado.Exito)
        {
            TempData["Mensaje"] = resultado.Datos?.Mensaje ?? "Sprint eliminado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = resultado.Error ?? "No fue posible eliminar el Sprint.";
        return RedirectToAction(nameof(Index));
    }

    private async Task RecargarProyectosDisponiblesAsync(CrearSprintViewModel vm)
    {
        var proyectosRes = await _apiClient.ObtenerProyectosSprintAsync();
        vm.ProyectosDisponibles = (proyectosRes.Datos ?? new List<ProyectoItemViewModel>())
            .Where(p => !string.Equals(p.Estado, "Cerrado", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }
}
