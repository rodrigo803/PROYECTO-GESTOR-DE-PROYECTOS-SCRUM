using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace Gestor_proyectos_scrum.Endpoints;

internal static class ApiComun
{
    public static int UsuarioId(HttpContext c) =>
        int.TryParse(c.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;

    public static async Task<bool> TokenValido(HttpContext c, IAntiforgery antiforgery) =>
        await antiforgery.IsRequestValidAsync(c);

    public static IResult ErrorToken() => Results.BadRequest("Solicitud no válida. Recarga la página e inténtalo de nuevo.");

    public static void Mensaje(HttpContext c, string nombre, string valor)
    {
        // En Minimal APIs no se ejecuta SaveTempDataFilter de MVC.
        var temporal = c.RequestServices.GetRequiredService<ITempDataDictionaryFactory>().GetTempData(c);
        temporal[nombre] = valor;
        temporal.Save();
    }

    public static (bool Valido, string Mensaje) Validar(object modelo)
    {
        var errores = new List<ValidationResult>();
        var valido = Validator.TryValidateObject(modelo, new ValidationContext(modelo), errores, validateAllProperties: true);
        return (valido, string.Join(" ", errores.Select(x => x.ErrorMessage)));
    }

    public static string Campo(IFormCollection form, string nombre, string prefijo = "") =>
        form[$"{prefijo}{nombre}"].ToString().Trim();

    public static int Numero(IFormCollection form, string nombre, string prefijo = "") =>
        int.TryParse(Campo(form, nombre, prefijo), out var numero) ? numero : 0;

    public static int? NumeroOpcional(IFormCollection form, string nombre, string prefijo = "") =>
        int.TryParse(Campo(form, nombre, prefijo), out var numero) ? numero : null;

    public static decimal Decimal(IFormCollection form, string nombre) =>
        decimal.TryParse(Campo(form, nombre), System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var num) ? num :
        decimal.TryParse(Campo(form, nombre), out num) ? num : -1;

    public static DateTime Fecha(IFormCollection form, string nombre, string prefijo = "") =>
        DateTime.TryParse(Campo(form, nombre, prefijo), out var fecha) ? fecha : default;

    public static DateTime? FechaOpcional(IFormCollection form, string nombre, string prefijo = "") =>
        DateTime.TryParse(Campo(form, nombre, prefijo), out var fecha) ? fecha : null;
}
