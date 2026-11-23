using Gestor_proyectos_scrum.Models;
using Gestor_proyectos_scrum.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Gestor_proyectos_scrum.Controllers;

/// <summary>
/// Controlador MVC para HU-07: Gestión de Tareas, Checkpoints Inmutables (AvanceTareas)
/// y Aprobaciones/Rechazos (Aprobaciones).
/// Consume la Minimal API mediante ScrumApiClient.
/// Admin y SM gestionan y asignan; PO solo ve.
/// </summary>
[Authorize(Roles = Permisos.TareasVer)]
public class TareasController : Controller
{
    private readonly ScrumApiClient _apiClient;

    public TareasController(ScrumApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(int? idSprint, int? idResponsable, string? estado, string? buscar)
    {
        var perfilTask = _apiClient.ObtenerPerfilActivoAsync();
        var backlogTask = _apiClient.ObtenerSprintBacklogAsync(idSprint, idResponsable, estado, buscar);

        await Task.WhenAll(perfilTask, backlogTask);

        var backlogRes = await backlogTask;
        if (!backlogRes.Exito)
        {
            TempData["Error"] = backlogRes.Error;
        }

        var data = backlogRes.Datos ?? new SprintBacklogApiDto();
        var sprintSel = data.SprintSeleccionado;
        var todasTareas = data.Historias.SelectMany(h => h.Tareas).ToList();

        var vm = new TareasIndexViewModel
        {
            SprintSeleccionado = sprintSel,
            Historias = data.Historias,
            Tareas = todasTareas,
            Sprints = data.SprintsDisponibles,
            Responsables = data.ResponsablesDisponibles,
            SprintHistoriasElegibles = data.SprintHistoriasElegibles,
            ImpedimentosActivos = data.ImpedimentosActivos,
            IdSprintFiltro = sprintSel?.IdSprint ?? idSprint,
            IdResponsableFiltro = idResponsable,
            EstadoFiltro = estado,
            Buscar = buscar,
            CountPendientes = data.CountPendientes,
            HorasPendientes = data.HorasPendientes,
            CountEnProgreso = data.CountEnProgreso,
            HorasEnProgreso = data.HorasEnProgreso,
            CountEnRevision = data.CountEnRevision,
            HorasEnRevision = data.HorasEnRevision,
            CountFinalizadas = data.CountFinalizadas,
            HorasFinalizadas = data.HorasFinalizadas,
            AvanceGlobalPonderado = sprintSel?.AvancePonderado ?? 0m,
            TotalPuntosEsfuerzo = sprintSel?.PuntosComprometidos ?? 0,
            PerfilActual = await perfilTask
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        var perfilTask = _apiClient.ObtenerPerfilActivoAsync();
        var detalleTask = _apiClient.ObtenerTareaDetalleAsync(id);

        await Task.WhenAll(perfilTask, detalleTask);

        var detalleRes = await detalleTask;
        if (!detalleRes.Exito || detalleRes.Datos is null)
        {
            TempData["Error"] = detalleRes.Error ?? "No se encontró la tarea solicitada.";
            return RedirectToAction(nameof(Index));
        }

        var tarea = detalleRes.Datos.Tarea;

        var vm = new TareaDetailsViewModel
        {
            Tarea = tarea,
            Avances = detalleRes.Datos.Avances,
            Aprobaciones = detalleRes.Datos.Aprobaciones,
            ImpedimentosSprint = detalleRes.Datos.ImpedimentosSprint,
            NuevoCheckpoint = new RegistrarCheckpointViewModel
            {
                IdTarea = tarea.IdTarea,
                PorcentajeNuevo = tarea.PorcentajeAvance,
                EstadoNuevo = tarea.Estado,
                HorasTrabajadas = 2
            },
            NuevaAprobacion = new RegistrarAprobacionViewModel
            {
                IdTarea = tarea.IdTarea,
                Resultado = "Aprobado"
            },
            PerfilActual = await perfilTask
        };

        return View(vm);
    }

    [HttpGet]
    [Authorize(Roles = Permisos.TareasGestionar)]
    public async Task<IActionResult> Create(int? idSprintHistoria, int? idSprint)
    {
        var catalogosRes = await _apiClient.ObtenerCatalogosTareasAsync();
        var catalogos = catalogosRes.Datos ?? new TareaCatalogosViewModel();

        var seleccionada = catalogos.SprintHistorias.FirstOrDefault(sh => sh.IdSprintHistoria == idSprintHistoria)
            ?? (idSprint.HasValue ? catalogos.SprintHistorias.FirstOrDefault(sh => sh.IdSprint == idSprint.Value) : null)
            ?? catalogos.SprintHistorias.FirstOrDefault();

        var fechaSugerida = DateTime.Today.AddDays(5);
        if (seleccionada is not null)
        {
            if (fechaSugerida > seleccionada.SprintFechaFin.Date)
            {
                fechaSugerida = seleccionada.SprintFechaFin.Date;
            }
            if (fechaSugerida < seleccionada.SprintFechaInicio.Date)
            {
                fechaSugerida = seleccionada.SprintFechaInicio.Date;
            }
        }

        var vm = new CrearTareaViewModel
        {
            IdSprintHistoria = seleccionada?.IdSprintHistoria ?? 0,
            PuntosEsfuerzo = 3,
            HorasEstimadas = 6,
            FechaEntregaEstimada = fechaSugerida,
            SprintHistoriasDisponibles = catalogos.SprintHistorias,
            ResponsablesDisponibles = catalogos.Responsables
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Permisos.TareasGestionar)]
    public async Task<IActionResult> Create(CrearTareaViewModel vm, int? idSprintRetorno)
    {
        if (!ModelState.IsValid)
        {
            var errores = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["Error"] = string.IsNullOrWhiteSpace(errores) ? "Verifique los datos ingresados de la tarea." : errores;
            await RecargarCatalogosCrearAsync(vm);
            return View(vm);
        }

        var resultado = await _apiClient.CrearTareaAsync(new
        {
            vm.IdSprintHistoria,
            vm.Titulo,
            vm.Descripcion,
            vm.IdResponsable,
            vm.PuntosEsfuerzo,
            vm.HorasEstimadas,
            vm.FechaEntregaEstimada
        });

        if (!resultado.Exito)
        {
            var msg = resultado.Error ?? "No fue posible crear la tarea.";
            ModelState.AddModelError(string.Empty, msg);
            TempData["Error"] = msg;

            await RecargarCatalogosCrearAsync(vm);
            return View(vm);
        }

        TempData["Mensaje"] = resultado.Datos?.Mensaje ?? "Tarea creada correctamente.";
        if (idSprintRetorno.HasValue && idSprintRetorno.Value > 0)
        {
            return RedirectToAction(nameof(Index), new { idSprint = idSprintRetorno.Value });
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Authorize(Roles = Permisos.TareasGestionar)]
    public async Task<IActionResult> Edit(int id)
    {
        var detalleTask = _apiClient.ObtenerTareaDetalleAsync(id);
        var catalogosTask = _apiClient.ObtenerCatalogosTareasAsync();

        await Task.WhenAll(detalleTask, catalogosTask);

        var detalleRes = await detalleTask;
        if (!detalleRes.Exito || detalleRes.Datos is null)
        {
            TempData["Error"] = detalleRes.Error ?? "No se encontró la tarea solicitada.";
            return RedirectToAction(nameof(Index));
        }

        var t = detalleRes.Datos.Tarea;
        if (string.Equals(t.SprintEstado, "Cerrado", StringComparison.OrdinalIgnoreCase))
        {
            TempData["Error"] = $"No se puede editar la tarea porque el Sprint '{t.SprintNombre}' se encuentra Cerrado.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var catalogos = (await catalogosTask).Datos ?? new TareaCatalogosViewModel();

        var vm = new EditarTareaViewModel
        {
            IdTarea = t.IdTarea,
            IdSprintHistoria = t.IdSprintHistoria,
            HistoriaEtiqueta = $"{t.ProyectoCodigo} · {t.SprintNombre} — {t.HistoriaCodigo}: {t.HistoriaQuiero}",
            SprintNombre = t.SprintNombre,
            SprintEstado = t.SprintEstado,
            PorcentajeAvanceActual = t.PorcentajeAvance,
            EstadoActual = t.Estado,
            HorasRealesActuales = t.HorasReales,
            Titulo = t.Titulo,
            Descripcion = t.Descripcion,
            IdResponsable = t.IdResponsable,
            PuntosEsfuerzo = t.PuntosEsfuerzo,
            HorasEstimadas = t.HorasEstimadas,
            FechaEntregaEstimada = t.FechaEntregaEstimada,
            ResponsablesDisponibles = catalogos.Responsables
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Permisos.TareasGestionar)]
    public async Task<IActionResult> Edit(int? id, EditarTareaViewModel vm, int? idSprintRetorno)
    {
        var targetId = vm.IdTarea > 0 ? vm.IdTarea : (id ?? 0);
        vm.IdTarea = targetId;

        if (!ModelState.IsValid)
        {
            var errores = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["Error"] = string.IsNullOrWhiteSpace(errores) ? "Verifique los datos de la tarea." : errores;
            var catalogosRes = await _apiClient.ObtenerCatalogosTareasAsync();
            vm.ResponsablesDisponibles = catalogosRes.Datos?.Responsables ?? new List<ResponsableCatalogoViewModel>();
            return View(vm);
        }

        var resultado = await _apiClient.ActualizarTareaAsync(targetId, new
        {
            vm.Titulo,
            vm.Descripcion,
            vm.IdResponsable,
            vm.PuntosEsfuerzo,
            vm.HorasEstimadas,
            vm.FechaEntregaEstimada
        });

        if (!resultado.Exito)
        {
            var msg = resultado.Error ?? "No fue posible actualizar los metadatos de la tarea.";
            ModelState.AddModelError(string.Empty, msg);
            TempData["Error"] = msg;

            var catalogosRes = await _apiClient.ObtenerCatalogosTareasAsync();
            vm.ResponsablesDisponibles = catalogosRes.Datos?.Responsables ?? new List<ResponsableCatalogoViewModel>();
            return View(vm);
        }

        TempData["Mensaje"] = resultado.Datos?.Mensaje ?? "Metadatos de la tarea actualizados correctamente.";
        if (idSprintRetorno.HasValue && idSprintRetorno.Value > 0)
        {
            return RedirectToAction(nameof(Index), new { idSprint = idSprintRetorno.Value });
        }

        return RedirectToAction(nameof(Details), new { id = targetId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Permisos.TareasGestionar)]
    public async Task<IActionResult> RegistrarAvance(int? id, RegistrarCheckpointViewModel model, string? returnUrl)
    {
        var targetId = model.IdTarea > 0 ? model.IdTarea : (id ?? 0);

        var resultado = await _apiClient.RegistrarAvanceTareaAsync(targetId, new
        {
            model.PorcentajeNuevo,
            model.HorasTrabajadas,
            model.EstadoNuevo,
            model.Justificacion,
            model.IdImpedimento
        });

        if (resultado.Exito)
        {
            TempData["Mensaje"] = resultado.Datos?.Mensaje ?? "Checkpoint de avance registrado correctamente.";
        }
        else
        {
            TempData["Error"] = resultado.Error ?? "No fue posible registrar el checkpoint de avance.";
        }

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction(nameof(Details), new { id = targetId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Permisos.TareasGestionar)]
    public async Task<IActionResult> RegistrarAprobacion(int id, RegistrarAprobacionViewModel model)
    {
        var resultado = await _apiClient.RegistrarAprobacionTareaAsync(id, new
        {
            model.Resultado,
            model.Comentario
        });

        if (resultado.Exito)
        {
            TempData["Mensaje"] = resultado.Datos?.Mensaje ?? "Revisión formal registrada correctamente.";
        }
        else
        {
            TempData["Error"] = resultado.Error ?? "No fue posible registrar la aprobación/rechazo.";
        }

        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = Permisos.TareasGestionar)]
    public async Task<IActionResult> Eliminar(int id, int? idSprintRetorno)
    {
        var resultado = await _apiClient.EliminarTareaAsync(id);
        if (resultado.Exito)
        {
            TempData["Mensaje"] = resultado.Datos?.Mensaje ?? "Tarea eliminada correctamente.";
            if (idSprintRetorno.HasValue && idSprintRetorno.Value > 0)
            {
                return RedirectToAction(nameof(Index), new { idSprint = idSprintRetorno.Value });
            }
            return RedirectToAction(nameof(Index));
        }

        TempData["Error"] = resultado.Error ?? "No fue posible eliminar la tarea.";
        if (idSprintRetorno.HasValue && idSprintRetorno.Value > 0)
        {
            return RedirectToAction(nameof(Index), new { idSprint = idSprintRetorno.Value });
        }
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task RecargarCatalogosCrearAsync(CrearTareaViewModel vm)
    {
        var catalogosRes = await _apiClient.ObtenerCatalogosTareasAsync();
        var catalogos = catalogosRes.Datos ?? new TareaCatalogosViewModel();
        vm.SprintHistoriasDisponibles = catalogos.SprintHistorias;
        vm.ResponsablesDisponibles = catalogos.Responsables;
    }
}
