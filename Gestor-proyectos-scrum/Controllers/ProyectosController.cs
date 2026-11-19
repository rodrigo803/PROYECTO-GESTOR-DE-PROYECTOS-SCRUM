using Gestor_proyectos_scrum.Data;
using Gestor_proyectos_scrum.Models;
using Gestor_proyectos_scrum.Services;
using Microsoft.AspNetCore.Mvc;
using MySqlConnector;

namespace Gestor_proyectos_scrum.Controllers;

public class ProyectosController : Controller
{
    private readonly ProyectoRepositorio _proyectoRepositorio;
    private readonly UsuarioActual _usuarioActual;

    public ProyectosController(ProyectoRepositorio proyectoRepositorio, UsuarioActual usuarioActual)
    {
        _proyectoRepositorio = proyectoRepositorio;
        _usuarioActual = usuarioActual;
    }

    public async Task<IActionResult> Index(string? buscar, string? estado, string? orden)
    {
        var proyectos = await _proyectoRepositorio.ListarAsync(buscar, estado, orden);
        var metricas = await _proyectoRepositorio.MetricasAsync();

        var modelo = new ProyectosIndexViewModel
        {
            Proyectos = proyectos,
            Metricas = metricas,
            Buscar = buscar,
            Estado = estado,
            Orden = orden
        };

        return View(modelo);
    }

    [HttpGet]
    public async Task<IActionResult> Crear()
    {
        if (!_usuarioActual.PuedeGestionarProyectos())
        {
            TempData["Error"] = "No tienes permisos para gestionar proyectos.";
            return RedirectToAction(nameof(Index));
        }

        var modelo = new ProyectoFormViewModel
        {
            Usuarios = (await _proyectoRepositorio.ListarUsuariosActivosAsync()).ToList()
        };

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Crear(ProyectoFormViewModel modelo)
    {
        if (!_usuarioActual.PuedeGestionarProyectos())
        {
            TempData["Error"] = "No tienes permisos para gestionar proyectos.";
            return RedirectToAction(nameof(Index));
        }

        ValidarReglasNegocio(modelo, esEdicion: false);

        if (!ModelState.IsValid)
        {
            modelo.Usuarios = (await _proyectoRepositorio.ListarUsuariosActivosAsync()).ToList();
            return View(modelo);
        }

        try
        {
            await _proyectoRepositorio.CrearAsync(modelo, _usuarioActual.IdUsuario);
            TempData["Mensaje"] = $"Proyecto \"{modelo.Nombre}\" creado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            ModelState.AddModelError(nameof(modelo.Nombre), "Ya existe un proyecto con ese nombre.");
            modelo.Usuarios = (await _proyectoRepositorio.ListarUsuariosActivosAsync()).ToList();
            return View(modelo);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        if (!_usuarioActual.PuedeGestionarProyectos())
        {
            TempData["Error"] = "No tienes permisos para gestionar proyectos.";
            return RedirectToAction(nameof(Index));
        }

        var modelo = await _proyectoRepositorio.ObtenerParaEditarAsync(id);
        if (modelo is null)
        {
            return NotFound();
        }

        if (modelo.Estado == "Cerrado")
        {
            TempData["Error"] = "No se puede editar un proyecto cerrado.";
            return RedirectToAction(nameof(Index));
        }

        modelo.Usuarios = (await _proyectoRepositorio.ListarUsuariosActivosAsync()).ToList();
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Editar(int id, ProyectoFormViewModel modelo)
    {
        if (!_usuarioActual.PuedeGestionarProyectos())
        {
            TempData["Error"] = "No tienes permisos para gestionar proyectos.";
            return RedirectToAction(nameof(Index));
        }

        if (id != modelo.IdProyecto)
        {
            return BadRequest();
        }

        var estadoActual = await _proyectoRepositorio.ObtenerEstadoAsync(id);
        if (estadoActual is null)
        {
            return NotFound();
        }

        if (estadoActual == "Cerrado")
        {
            TempData["Error"] = "No se puede editar un proyecto cerrado.";
            return RedirectToAction(nameof(Index));
        }

        ValidarReglasNegocio(modelo, esEdicion: true);

        if (!ModelState.IsValid)
        {
            modelo.Usuarios = (await _proyectoRepositorio.ListarUsuariosActivosAsync()).ToList();
            return View(modelo);
        }

        try
        {
            await _proyectoRepositorio.ActualizarAsync(id, modelo, _usuarioActual.IdUsuario);
            TempData["Mensaje"] = $"Proyecto \"{modelo.Nombre}\" actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            ModelState.AddModelError(nameof(modelo.Nombre), "Ya existe un proyecto con ese nombre.");
            modelo.Usuarios = (await _proyectoRepositorio.ListarUsuariosActivosAsync()).ToList();
            return View(modelo);
        }
    }

    private void ValidarReglasNegocio(ProyectoFormViewModel modelo, bool esEdicion)
    {
        if (modelo.FechaFin <= modelo.FechaInicio)
        {
            ModelState.AddModelError(nameof(modelo.FechaFin), "La fecha de finalización debe ser posterior a la fecha de inicio.");
        }

        var estadosPermitidos = esEdicion
            ? new[] { "Planificado", "Activo", "En pausa" }
            : new[] { "Planificado", "Activo" };

        if (!estadosPermitidos.Contains(modelo.Estado))
        {
            ModelState.AddModelError(nameof(modelo.Estado), "Estado no válido.");
        }
        else if (modelo.Estado == "Activo" && (modelo.IdProductOwner is null || modelo.IdScrumMaster is null))
        {
            ModelState.AddModelError(nameof(modelo.Estado), "Un proyecto no puede pasar a \"Activo\" sin Product Owner y Scrum Master asignados.");
        }

        var idsAsignados = new List<int>();
        if (modelo.IdProductOwner.HasValue)
        {
            idsAsignados.Add(modelo.IdProductOwner.Value);
        }
        if (modelo.IdScrumMaster.HasValue)
        {
            idsAsignados.Add(modelo.IdScrumMaster.Value);
        }
        idsAsignados.AddRange(modelo.IdsDevelopers);

        if (idsAsignados.Count != idsAsignados.Distinct().Count())
        {
            ModelState.AddModelError(string.Empty, "Un mismo usuario no puede tener más de un rol en el proyecto.");
        }
    }
}
