using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Routing;

namespace Gestor_proyectos_scrum.Services;

/// <summary>
/// Renderiza las vistas Razor originales desde Minimal APIs, sin un Controller.
/// Conserva Layout, ViewBag, TempData, Html.AntiForgeryToken y secciones Scripts.
/// </summary>
public sealed class RazorVista
{
    private readonly IRazorViewEngine _engine;
    private readonly ITempDataDictionaryFactory _tempDataFactory;
    private readonly IModelMetadataProvider _metadata;

    public RazorVista(IRazorViewEngine engine, ITempDataDictionaryFactory tempDataFactory,
        IModelMetadataProvider metadata)
    {
        _engine = engine;
        _tempDataFactory = tempDataFactory;
        _metadata = metadata;
    }

    public async Task<IResult> Mostrar(HttpContext http, string carpeta, string pagina,
        object? modelo = null, IDictionary<string, object?>? opciones = null)
    {
        var routeData = new RouteData();
        routeData.Values["controller"] = carpeta;
        routeData.Values["action"] = pagina;
        var action = new ActionContext(http, routeData, new ActionDescriptor());
        var ruta = $"/Views/{carpeta}/{pagina}.cshtml";
        var buscar = _engine.GetView(null, ruta, isMainPage: true);
        if (!buscar.Success)
            throw new InvalidOperationException($"No se encontró la vista: {ruta}. {string.Join(", ", buscar.SearchedLocations ?? Array.Empty<string>())}");

        var datos = new ViewDataDictionary(_metadata, new ModelStateDictionary()) { Model = modelo };
        if (opciones != null)
            foreach (var par in opciones) datos[par.Key] = par.Value;

        using var escritor = new StringWriter(CultureInfo.InvariantCulture);
        var contexto = new ViewContext(action, buscar.View, datos,
            _tempDataFactory.GetTempData(http), escritor, new HtmlHelperOptions());
        await buscar.View.RenderAsync(contexto);
        // Guarda los valores consumidos de TempData (por ejemplo alertas de éxito/error).
        contexto.TempData.Save();
        return Results.Content(escritor.ToString(), "text/html; charset=utf-8");
    }
}
