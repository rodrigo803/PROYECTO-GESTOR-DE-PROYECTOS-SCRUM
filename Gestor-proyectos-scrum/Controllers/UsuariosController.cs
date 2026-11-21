using Gestor_proyectos_scrum.Models;
using Gestor_proyectos_scrum.Services;
using Microsoft.AspNetCore.Mvc;

namespace Gestor_proyectos_scrum.Controllers;

/// <summary>
/// Controlador MVC para HU-02: Gestión de Usuarios, Roles y Permisos.
/// Consume la Minimal API mediante ScrumApiClient.
/// </summary>
public class UsuariosController : Controller
{
    private readonly ScrumApiClient _apiClient;

    public UsuariosController(ScrumApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? buscar, int? idRol, bool? activo)
    {
        var perfilTask = _apiClient.ObtenerPerfilActivoAsync();
        var rolesTask = _apiClient.ObtenerRolesAsync();
        var filtradosTask = _apiClient.ObtenerUsuariosAsync(buscar, idRol, activo);
        var todosTask = _apiClient.ObtenerUsuariosAsync();

        await Task.WhenAll(perfilTask, rolesTask, filtradosTask, todosTask);

        var filtradosRes = await filtradosTask;
        var todosRes = await todosTask;
        var rolesRes = await rolesTask;

        if (!filtradosRes.Exito)
        {
            TempData["Error"] = filtradosRes.Error;
        }

        var todos = todosRes.Datos ?? new List<UsuarioItemViewModel>();
        var usuarios = filtradosRes.Datos ?? new List<UsuarioItemViewModel>();

        var vm = new UsuariosIndexViewModel
        {
            Usuarios = usuarios,
            Roles = rolesRes.Datos ?? new List<RolItemViewModel>(),
            Buscar = buscar,
            IdRolFiltro = idRol,
            ActivoFiltro = activo,
            TotalUsuarios = todos.Count,
            UsuariosActivos = todos.Count(u => u.Activo),
            UsuariosInactivos = todos.Count(u => !u.Activo),
            TotalAdministradores = todos.Count(u => u.Activo && string.Equals(u.RolNombre, "Administrador", StringComparison.OrdinalIgnoreCase)),
            PerfilActual = await perfilTask
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Create()
    {
        var rolesRes = await _apiClient.ObtenerRolesAsync();
        var roles = rolesRes.Datos ?? new List<RolItemViewModel>();

        var vm = new CrearUsuarioViewModel
        {
            Activo = true,
            IdRol = roles.FirstOrDefault(r => r.Nombre == "Miembro del Equipo")?.IdRol ?? 4,
            RolesDisponibles = roles
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CrearUsuarioViewModel vm)
    {
        if (!ModelState.IsValid)
        {
            var errores = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["Error"] = string.IsNullOrWhiteSpace(errores) ? "Verifique los datos ingresados del usuario." : errores;
            var rolesRes = await _apiClient.ObtenerRolesAsync();
            vm.RolesDisponibles = rolesRes.Datos ?? new List<RolItemViewModel>();
            return View(vm);
        }

        var resultado = await _apiClient.CrearUsuarioAsync(new
        {
            vm.Nombre,
            vm.Apellidos,
            vm.Correo,
            vm.Contrasena,
            vm.IdRol,
            vm.Activo
        });

        if (!resultado.Exito)
        {
            var msg = resultado.Error ?? "No fue posible crear el usuario.";
            ModelState.AddModelError(string.Empty, msg);
            TempData["Error"] = msg;

            var rolesRes = await _apiClient.ObtenerRolesAsync();
            vm.RolesDisponibles = rolesRes.Datos ?? new List<RolItemViewModel>();
            return View(vm);
        }

        TempData["Mensaje"] = resultado.Datos?.Mensaje ?? "Usuario creado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var usuarioTask = _apiClient.ObtenerUsuarioPorIdAsync(id);
        var rolesTask = _apiClient.ObtenerRolesAsync();

        await Task.WhenAll(usuarioTask, rolesTask);

        var usuarioRes = await usuarioTask;
        if (!usuarioRes.Exito || usuarioRes.Datos is null)
        {
            TempData["Error"] = usuarioRes.Error ?? "No se encontró el usuario solicitado.";
            return RedirectToAction(nameof(Index));
        }

        var u = usuarioRes.Datos;
        var rolesRes = await rolesTask;

        var vm = new EditarUsuarioViewModel
        {
            IdUsuario = u.IdUsuario,
            Nombre = u.Nombre,
            Apellidos = u.Apellidos,
            Correo = u.Correo,
            IdRol = u.IdRol,
            Activo = u.Activo,
            RolesDisponibles = rolesRes.Datos ?? new List<RolItemViewModel>()
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int? id, EditarUsuarioViewModel vm)
    {
        var targetId = vm.IdUsuario > 0 ? vm.IdUsuario : (id ?? 0);
        vm.IdUsuario = targetId;

        if (!ModelState.IsValid)
        {
            var errores = string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
            TempData["Error"] = string.IsNullOrWhiteSpace(errores) ? "Verifique los datos ingresados del usuario." : errores;
            var rolesRes = await _apiClient.ObtenerRolesAsync();
            vm.RolesDisponibles = rolesRes.Datos ?? new List<RolItemViewModel>();
            return View(vm);
        }

        var resultado = await _apiClient.ActualizarUsuarioAsync(targetId, new
        {
            vm.Nombre,
            vm.Apellidos,
            vm.Correo,
            vm.IdRol,
            vm.Activo,
            vm.NuevaContrasena
        });

        if (!resultado.Exito)
        {
            var msg = resultado.Error ?? "No fue posible actualizar el usuario.";
            ModelState.AddModelError(string.Empty, msg);
            TempData["Error"] = msg;

            var rolesRes = await _apiClient.ObtenerRolesAsync();
            vm.RolesDisponibles = rolesRes.Datos ?? new List<RolItemViewModel>();
            return View(vm);
        }

        TempData["Mensaje"] = resultado.Datos?.Mensaje ?? "Usuario actualizado correctamente.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarRol(int id, int idRol)
    {
        var resultado = await _apiClient.CambiarRolUsuarioAsync(id, idRol);
        if (resultado.Exito)
        {
            TempData["Mensaje"] = resultado.Datos?.Mensaje ?? "Rol actualizado correctamente.";
        }
        else
        {
            TempData["Error"] = resultado.Error ?? "No se pudo cambiar el rol del usuario.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CambiarEstado(int id, bool activo)
    {
        var resultado = await _apiClient.CambiarEstadoUsuarioAsync(id, activo);
        if (resultado.Exito)
        {
            TempData["Mensaje"] = resultado.Datos?.Mensaje ?? "Estado del usuario actualizado correctamente.";
        }
        else
        {
            TempData["Error"] = resultado.Error ?? "No se pudo actualizar el estado del usuario.";
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> ToggleEstado(int id, bool activo) => CambiarEstado(id, activo);
}
