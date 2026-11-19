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

    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        var modelo = await _proyectoRepositorio.ObtenerDetalleAsync(id);
        if (modelo is null)
        {
            return NotFound();
        }

        modelo.PuedeGestionar = _usuarioActual.PuedeGestionarProyectos() && modelo.Estado != "Cerrado";
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarMiembro(int id, int idUsuario, string rolProyecto)
    {
        var estado = await _proyectoRepositorio.ObtenerEstadoAsync(id);
        if (estado is null)
        {
            return NotFound();
        }

        if (!_usuarioActual.PuedeGestionarProyectos())
        {
            TempData["Error"] = "No tienes permisos para gestionar proyectos.";
            return RedirectToAction(nameof(Detalle), new { id, tab = "miembros" });
        }

        if (estado == "Cerrado")
        {
            TempData["Error"] = "No se puede modificar un proyecto cerrado.";
            return RedirectToAction(nameof(Detalle), new { id, tab = "miembros" });
        }

        var rolesPermitidos = new[] { "Product Owner", "Scrum Master", "Developer", "Stakeholder" };
        if (!rolesPermitidos.Contains(rolProyecto))
        {
            TempData["Error"] = "Rol no válido.";
            return RedirectToAction(nameof(Detalle), new { id, tab = "miembros" });
        }

        var miembros = await _proyectoRepositorio.ObtenerMiembrosAsync(id);
        if (miembros.Any(m => m.IdUsuario == idUsuario))
        {
            TempData["Error"] = "El usuario ya es miembro del proyecto.";
            return RedirectToAction(nameof(Detalle), new { id, tab = "miembros" });
        }

        var usuariosActivos = await _proyectoRepositorio.ListarUsuariosActivosAsync();
        var usuarioAgregado = usuariosActivos.FirstOrDefault(u => u.IdUsuario == idUsuario);
        if (usuarioAgregado is null)
        {
            TempData["Error"] = "El usuario seleccionado no es válido.";
            return RedirectToAction(nameof(Detalle), new { id, tab = "miembros" });
        }

        await _proyectoRepositorio.AgregarMiembroAsync(id, idUsuario, rolProyecto, _usuarioActual.IdUsuario, usuarioAgregado.NombreCompleto);
        TempData["Mensaje"] = "Miembro agregado correctamente.";
        return RedirectToAction(nameof(Detalle), new { id, tab = "miembros" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarRolMiembro(int id, int idUsuario, string nuevoRol)
    {
        var estado = await _proyectoRepositorio.ObtenerEstadoAsync(id);
        if (estado is null)
        {
            return NotFound();
        }

        if (!_usuarioActual.PuedeGestionarProyectos())
        {
            TempData["Error"] = "No tienes permisos para gestionar proyectos.";
            return RedirectToAction(nameof(Detalle), new { id, tab = "miembros" });
        }

        if (estado == "Cerrado")
        {
            TempData["Error"] = "No se puede modificar un proyecto cerrado.";
            return RedirectToAction(nameof(Detalle), new { id, tab = "miembros" });
        }

        var rolesPermitidos = new[] { "Product Owner", "Scrum Master", "Developer", "Stakeholder" };
        if (!rolesPermitidos.Contains(nuevoRol))
        {
            TempData["Error"] = "Rol no válido.";
            return RedirectToAction(nameof(Detalle), new { id, tab = "miembros" });
        }

        var miembros = await _proyectoRepositorio.ObtenerMiembrosAsync(id);
        var miembro = miembros.FirstOrDefault(m => m.IdUsuario == idUsuario);
        if (miembro is null)
        {
            TempData["Error"] = "El usuario no pertenece al proyecto.";
            return RedirectToAction(nameof(Detalle), new { id, tab = "miembros" });
        }

        if (nuevoRol != miembro.RolProyecto && EsUnicoResponsableCritico(miembros, miembro) && estado is "Activo" or "En pausa")
        {
            TempData["Error"] = $"No se puede cambiar de rol al único {miembro.RolProyecto} mientras el proyecto está {estado}.";
            return RedirectToAction(nameof(Detalle), new { id, tab = "miembros" });
        }

        await _proyectoRepositorio.CambiarRolMiembroAsync(id, idUsuario, nuevoRol, _usuarioActual.IdUsuario, miembro.NombreCompleto, miembro.RolProyecto);
        TempData["Mensaje"] = "Rol actualizado correctamente.";
        return RedirectToAction(nameof(Detalle), new { id, tab = "miembros" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuitarMiembro(int id, int idUsuario)
    {
        var estado = await _proyectoRepositorio.ObtenerEstadoAsync(id);
        if (estado is null)
        {
            return NotFound();
        }

        if (!_usuarioActual.PuedeGestionarProyectos())
        {
            TempData["Error"] = "No tienes permisos para gestionar proyectos.";
            return RedirectToAction(nameof(Detalle), new { id, tab = "miembros" });
        }

        if (estado == "Cerrado")
        {
            TempData["Error"] = "No se puede modificar un proyecto cerrado.";
            return RedirectToAction(nameof(Detalle), new { id, tab = "miembros" });
        }

        var miembros = await _proyectoRepositorio.ObtenerMiembrosAsync(id);
        var miembro = miembros.FirstOrDefault(m => m.IdUsuario == idUsuario);
        if (miembro is null)
        {
            TempData["Error"] = "El usuario no pertenece al proyecto.";
            return RedirectToAction(nameof(Detalle), new { id, tab = "miembros" });
        }

        if (EsUnicoResponsableCritico(miembros, miembro) && estado is "Activo" or "En pausa")
        {
            TempData["Error"] = $"No se puede quitar al único {miembro.RolProyecto} mientras el proyecto está {estado}.";
            return RedirectToAction(nameof(Detalle), new { id, tab = "miembros" });
        }

        await _proyectoRepositorio.QuitarMiembroAsync(id, idUsuario, _usuarioActual.IdUsuario, miembro.NombreCompleto);
        TempData["Mensaje"] = "Miembro eliminado del proyecto.";
        return RedirectToAction(nameof(Detalle), new { id, tab = "miembros" });
    }

    private static readonly string[] MotivosCierre =
    {
        "Proyecto completado",
        "Cancelado por el cliente",
        "Reemplazado por otro proyecto"
    };

    [HttpGet]
    public async Task<IActionResult> Cerrar(int id)
    {
        if (!_usuarioActual.PuedeGestionarProyectos())
        {
            TempData["Error"] = "No tienes permisos para gestionar proyectos.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        var modelo = await _proyectoRepositorio.ObtenerResumenCierreAsync(id);
        if (modelo is null)
        {
            return NotFound();
        }

        if (modelo.Estado == "Cerrado")
        {
            TempData["Error"] = "Este proyecto ya está cerrado.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Cerrar(int id, ProyectoCerrarViewModel modelo)
    {
        if (!_usuarioActual.PuedeGestionarProyectos())
        {
            TempData["Error"] = "No tienes permisos para gestionar proyectos.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        var resumen = await _proyectoRepositorio.ObtenerResumenCierreAsync(id);
        if (resumen is null)
        {
            return NotFound();
        }

        if (resumen.Estado == "Cerrado")
        {
            TempData["Error"] = "Este proyecto ya está cerrado.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        if (!string.IsNullOrEmpty(resumen.SprintActivoNombre))
        {
            ModelState.AddModelError(string.Empty,
                $"No se puede cerrar el proyecto: el Sprint \"{resumen.SprintActivoNombre}\" está Activo.");
        }

        if (string.IsNullOrWhiteSpace(modelo.Motivo) || !MotivosCierre.Contains(modelo.Motivo))
        {
            ModelState.AddModelError(nameof(modelo.Motivo), "Debes seleccionar un motivo de cierre válido.");
        }

        if (!ModelState.IsValid)
        {
            resumen.Motivo = modelo.Motivo;
            resumen.Observaciones = modelo.Observaciones;
            return View(resumen);
        }

        var motivoCompleto = string.IsNullOrWhiteSpace(modelo.Observaciones)
            ? modelo.Motivo!
            : $"{modelo.Motivo}. {modelo.Observaciones.Trim()}";

        await _proyectoRepositorio.CerrarAsync(id, motivoCompleto, _usuarioActual.IdUsuario);
        TempData["Mensaje"] = $"Proyecto \"{resumen.Nombre}\" cerrado correctamente.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    private static bool EsUnicoResponsableCritico(IReadOnlyList<ProyectoMiembroItem> miembros, ProyectoMiembroItem miembro)
    {
        if (miembro.RolProyecto is not ("Product Owner" or "Scrum Master"))
        {
            return false;
        }

        return miembros.Count(m => m.RolProyecto == miembro.RolProyecto) == 1;
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
