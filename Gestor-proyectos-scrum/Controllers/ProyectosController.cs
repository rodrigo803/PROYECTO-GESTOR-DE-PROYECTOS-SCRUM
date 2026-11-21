using Gestor_proyectos_scrum.Models;
using Gestor_proyectos_scrum.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gestor_proyectos_scrum.Controllers;

/// <summary>
/// Controlador MVC para HU-03: Gestión de Proyectos.
/// Consume la Minimal API (Gestor-proyectos-scrum.Api) mediante ScrumApiClient;
/// no accede directamente a MySQL.
/// </summary>
public class ProyectosController : Controller
{
    private readonly ScrumApiClient _apiClient;
    private readonly UsuarioActual _usuarioActual;

    public ProyectosController(ScrumApiClient apiClient, UsuarioActual usuarioActual)
    {
        _apiClient = apiClient;
        _usuarioActual = usuarioActual;
    }

    public async Task<IActionResult> Index(string? buscar, string? estado, string? orden)
    {
        var proyectosTask = _apiClient.ObtenerProyectosAsync(buscar, estado, orden);
        var metricasTask = _apiClient.ObtenerMetricasProyectosAsync();
        await Task.WhenAll(proyectosTask, metricasTask);

        var proyectosRes = await proyectosTask;
        var metricasRes = await metricasTask;

        if (!proyectosRes.Exito)
        {
            TempData["Error"] = proyectosRes.Error;
        }

        var modelo = new ProyectosIndexViewModel
        {
            Proyectos = proyectosRes.Datos ?? new List<ProyectoListaItem>(),
            Metricas = metricasRes.Datos ?? new ProyectoMetricas(),
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

        var usuariosRes = await _apiClient.ObtenerUsuariosActivosAsync();
        var modelo = new ProyectoFormViewModel
        {
            Usuarios = usuariosRes.Datos ?? new List<UsuarioOpcion>()
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

        ValidarCamposRequeridos(modelo);

        if (!ModelState.IsValid)
        {
            modelo.Usuarios = (await _apiClient.ObtenerUsuariosActivosAsync()).Datos ?? new List<UsuarioOpcion>();
            return View(modelo);
        }

        var resultado = await _apiClient.CrearProyectoAsync(new
        {
            modelo.Nombre,
            modelo.Descripcion,
            modelo.FechaInicio,
            modelo.FechaFin,
            modelo.Estado,
            modelo.IdProductOwner,
            modelo.IdScrumMaster,
            modelo.IdsDevelopers
        });

        if (!resultado.Exito)
        {
            var msg = resultado.Error ?? "No fue posible crear el proyecto.";
            if (resultado.StatusCode == 409)
            {
                ModelState.AddModelError(nameof(modelo.Nombre), msg);
            }
            else
            {
                ModelState.AddModelError(string.Empty, msg);
            }

            TempData["Error"] = msg;
            modelo.Usuarios = (await _apiClient.ObtenerUsuariosActivosAsync()).Datos ?? new List<UsuarioOpcion>();
            return View(modelo);
        }

        TempData["Mensaje"] = resultado.Datos?.Mensaje ?? $"Proyecto \"{modelo.Nombre}\" creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Editar(int id)
    {
        if (!_usuarioActual.PuedeGestionarProyectos())
        {
            TempData["Error"] = "No tienes permisos para gestionar proyectos.";
            return RedirectToAction(nameof(Index));
        }

        var resultado = await _apiClient.ObtenerProyectoParaEditarAsync(id);
        if (!resultado.Exito || resultado.Datos is null)
        {
            if (resultado.StatusCode == 404)
            {
                return NotFound();
            }

            TempData["Error"] = resultado.Error ?? "No se encontró el proyecto solicitado.";
            return RedirectToAction(nameof(Index));
        }

        var modelo = resultado.Datos;
        if (modelo.Estado == "Cerrado")
        {
            TempData["Error"] = "No se puede editar un proyecto cerrado.";
            return RedirectToAction(nameof(Index));
        }

        modelo.Usuarios = (await _apiClient.ObtenerUsuariosActivosAsync()).Datos ?? new List<UsuarioOpcion>();
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

        ValidarCamposRequeridos(modelo);

        if (!ModelState.IsValid)
        {
            modelo.Usuarios = (await _apiClient.ObtenerUsuariosActivosAsync()).Datos ?? new List<UsuarioOpcion>();
            return View(modelo);
        }

        var resultado = await _apiClient.ActualizarProyectoAsync(id, new
        {
            modelo.Nombre,
            modelo.Descripcion,
            modelo.FechaInicio,
            modelo.FechaFin,
            modelo.Estado,
            modelo.IdProductOwner,
            modelo.IdScrumMaster,
            modelo.IdsDevelopers
        });

        if (!resultado.Exito)
        {
            if (resultado.StatusCode == 404)
            {
                return NotFound();
            }

            var msg = resultado.Error ?? "No fue posible actualizar el proyecto.";

            if (msg.Contains("cerrado", StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = msg;
                return RedirectToAction(nameof(Index));
            }

            if (resultado.StatusCode == 409)
            {
                ModelState.AddModelError(nameof(modelo.Nombre), msg);
            }
            else
            {
                ModelState.AddModelError(string.Empty, msg);
            }

            TempData["Error"] = msg;
            modelo.Usuarios = (await _apiClient.ObtenerUsuariosActivosAsync()).Datos ?? new List<UsuarioOpcion>();
            return View(modelo);
        }

        TempData["Mensaje"] = resultado.Datos?.Mensaje ?? $"Proyecto \"{modelo.Nombre}\" actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Detalle(int id)
    {
        var resultado = await _apiClient.ObtenerDetalleProyectoAsync(id);
        if (!resultado.Exito || resultado.Datos is null)
        {
            return NotFound();
        }

        var modelo = resultado.Datos;
        modelo.PuedeGestionar = _usuarioActual.PuedeGestionarProyectos() && modelo.Estado != "Cerrado";
        return View(modelo);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AgregarMiembro(int id, int idUsuario, string rolProyecto)
    {
        if (!_usuarioActual.PuedeGestionarProyectos())
        {
            TempData["Error"] = "No tienes permisos para gestionar proyectos.";
            return RedirectToAction(nameof(Detalle), new { id, tab = "miembros" });
        }

        var resultado = await _apiClient.AgregarMiembroProyectoAsync(id, idUsuario, rolProyecto);
        if (!resultado.Exito)
        {
            if (resultado.StatusCode == 404)
            {
                return NotFound();
            }

            TempData["Error"] = resultado.Error ?? "No fue posible agregar el miembro.";
            return RedirectToAction(nameof(Detalle), new { id, tab = "miembros" });
        }

        TempData["Mensaje"] = resultado.Datos?.Mensaje ?? "Miembro agregado correctamente.";
        return RedirectToAction(nameof(Detalle), new { id, tab = "miembros" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarRolMiembro(int id, int idUsuario, string nuevoRol)
    {
        if (!_usuarioActual.PuedeGestionarProyectos())
        {
            TempData["Error"] = "No tienes permisos para gestionar proyectos.";
            return RedirectToAction(nameof(Detalle), new { id, tab = "miembros" });
        }

        var resultado = await _apiClient.CambiarRolMiembroProyectoAsync(id, idUsuario, nuevoRol);
        if (!resultado.Exito)
        {
            if (resultado.StatusCode == 404)
            {
                return NotFound();
            }

            TempData["Error"] = resultado.Error ?? "No fue posible cambiar el rol del miembro.";
            return RedirectToAction(nameof(Detalle), new { id, tab = "miembros" });
        }

        TempData["Mensaje"] = resultado.Datos?.Mensaje ?? "Rol actualizado correctamente.";
        return RedirectToAction(nameof(Detalle), new { id, tab = "miembros" });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> QuitarMiembro(int id, int idUsuario)
    {
        if (!_usuarioActual.PuedeGestionarProyectos())
        {
            TempData["Error"] = "No tienes permisos para gestionar proyectos.";
            return RedirectToAction(nameof(Detalle), new { id, tab = "miembros" });
        }

        var resultado = await _apiClient.QuitarMiembroProyectoAsync(id, idUsuario);
        if (!resultado.Exito)
        {
            if (resultado.StatusCode == 404)
            {
                return NotFound();
            }

            TempData["Error"] = resultado.Error ?? "No fue posible quitar al miembro.";
            return RedirectToAction(nameof(Detalle), new { id, tab = "miembros" });
        }

        TempData["Mensaje"] = resultado.Datos?.Mensaje ?? "Miembro eliminado del proyecto.";
        return RedirectToAction(nameof(Detalle), new { id, tab = "miembros" });
    }

    [HttpGet]
    public async Task<IActionResult> Cerrar(int id)
    {
        if (!_usuarioActual.PuedeGestionarProyectos())
        {
            TempData["Error"] = "No tienes permisos para gestionar proyectos.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        var resultado = await _apiClient.ObtenerResumenCierreProyectoAsync(id);
        if (!resultado.Exito || resultado.Datos is null)
        {
            if (resultado.StatusCode == 404)
            {
                return NotFound();
            }

            TempData["Error"] = resultado.Error ?? "No se encontró el proyecto solicitado.";
            return RedirectToAction(nameof(Detalle), new { id });
        }

        var modelo = resultado.Datos;
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

        var resultado = await _apiClient.CerrarProyectoAsync(id, modelo.Motivo, modelo.Observaciones);
        if (!resultado.Exito)
        {
            if (resultado.StatusCode == 404)
            {
                return NotFound();
            }

            var resumenRes = await _apiClient.ObtenerResumenCierreProyectoAsync(id);
            var resumen = resumenRes.Datos ?? new ProyectoCerrarViewModel { IdProyecto = id };
            resumen.Motivo = modelo.Motivo;
            resumen.Observaciones = modelo.Observaciones;

            var msg = resultado.Error ?? "No fue posible cerrar el proyecto.";
            ModelState.AddModelError(string.Empty, msg);
            TempData["Error"] = msg;
            return View(resumen);
        }

        TempData["Mensaje"] = resultado.Datos?.Mensaje ?? "Proyecto cerrado correctamente.";
        return RedirectToAction(nameof(Detalle), new { id });
    }

    [HttpGet]
    public async Task<IActionResult> Cliente()
    {
        var resultado = await _apiClient.ObtenerProyectosClienteAsync();
        if (!resultado.Exito)
        {
            TempData["Error"] = resultado.Error;
        }

        return View(resultado.Datos ?? new List<ProyectoClienteItem>());
    }

    [HttpGet]
    public async Task<IActionResult> DetalleCliente(int id)
    {
        var resultado = await _apiClient.ObtenerDetalleClienteProyectoAsync(id);
        if (!resultado.Exito || resultado.Datos is null)
        {
            TempData["Error"] = resultado.StatusCode == 403
                ? (resultado.Error ?? "No tienes acceso a ese proyecto.")
                : "No tienes acceso a ese proyecto.";
            return RedirectToAction(nameof(Cliente));
        }

        return View(resultado.Datos);
    }

    /// <summary>Validaciones mínimas de formulario; las reglas de negocio (PO/SM, duplicados, etc.) las valida la API.</summary>
    private void ValidarCamposRequeridos(ProyectoFormViewModel modelo)
    {
        if (modelo.FechaFin <= modelo.FechaInicio)
        {
            ModelState.AddModelError(nameof(modelo.FechaFin), "La fecha de finalización debe ser posterior a la fecha de inicio.");
        }
    }
}
