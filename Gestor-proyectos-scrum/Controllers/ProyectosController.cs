using Gestor_proyectos_scrum.Data;
using Gestor_proyectos_scrum.Models;
using Microsoft.AspNetCore.Mvc;

namespace Gestor_proyectos_scrum.Controllers;

public class ProyectosController : Controller
{
    private readonly ProyectoRepositorio _proyectoRepositorio;

    public ProyectosController(ProyectoRepositorio proyectoRepositorio)
    {
        _proyectoRepositorio = proyectoRepositorio;
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
}
