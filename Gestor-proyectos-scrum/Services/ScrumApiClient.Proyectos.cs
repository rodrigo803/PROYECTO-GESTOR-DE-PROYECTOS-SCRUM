using Gestor_proyectos_scrum.Models;

namespace Gestor_proyectos_scrum.Services;

/// <summary>
/// HU-03: métodos de ScrumApiClient para consumir /api/proyectos.
/// Deserializa directo en los Models/ViewModels existentes para no tener que
/// duplicarlos (misma convención de nombres que usa la API en Gestor-proyectos-scrum.Api/Models/ProyectosDtos.cs).
/// </summary>
public partial class ScrumApiClient
{
    public Task<ApiResult<List<ProyectoListaItem>>> ObtenerProyectosAsync(string? buscar = null, string? estado = null, string? orden = null)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(buscar)) query.Add($"buscar={Uri.EscapeDataString(buscar.Trim())}");
        if (!string.IsNullOrWhiteSpace(estado)) query.Add($"estado={Uri.EscapeDataString(estado.Trim())}");
        if (!string.IsNullOrWhiteSpace(orden)) query.Add($"orden={Uri.EscapeDataString(orden.Trim())}");

        var url = "api/proyectos" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        return EnviarAsync<List<ProyectoListaItem>>(HttpMethod.Get, url);
    }

    public Task<ApiResult<ProyectoMetricas>> ObtenerMetricasProyectosAsync() =>
        EnviarAsync<ProyectoMetricas>(HttpMethod.Get, "api/proyectos/metricas");

    public Task<ApiResult<List<UsuarioOpcion>>> ObtenerUsuariosActivosAsync() =>
        EnviarAsync<List<UsuarioOpcion>>(HttpMethod.Get, "api/proyectos/usuarios-activos");

    public Task<ApiResult<ProyectoDetalleViewModel>> ObtenerDetalleProyectoAsync(int id) =>
        EnviarAsync<ProyectoDetalleViewModel>(HttpMethod.Get, $"api/proyectos/{id}");

    public Task<ApiResult<ProyectoFormViewModel>> ObtenerProyectoParaEditarAsync(int id) =>
        EnviarAsync<ProyectoFormViewModel>(HttpMethod.Get, $"api/proyectos/{id}/editar");

    public Task<ApiResult<ApiMessageDto>> CrearProyectoAsync(object payload) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Post, "api/proyectos", payload);

    public Task<ApiResult<ApiMessageDto>> ActualizarProyectoAsync(int id, object payload) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Put, $"api/proyectos/{id}", payload);

    public Task<ApiResult<ApiMessageDto>> AgregarMiembroProyectoAsync(int id, int idUsuario, string rolProyecto) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Post, $"api/proyectos/{id}/miembros", new { IdUsuario = idUsuario, RolProyecto = rolProyecto });

    public Task<ApiResult<ApiMessageDto>> CambiarRolMiembroProyectoAsync(int id, int idUsuario, string nuevoRol) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Put, $"api/proyectos/{id}/miembros/{idUsuario}", new { NuevoRol = nuevoRol });

    public Task<ApiResult<ApiMessageDto>> QuitarMiembroProyectoAsync(int id, int idUsuario) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Delete, $"api/proyectos/{id}/miembros/{idUsuario}");

    public Task<ApiResult<ProyectoCerrarViewModel>> ObtenerResumenCierreProyectoAsync(int id) =>
        EnviarAsync<ProyectoCerrarViewModel>(HttpMethod.Get, $"api/proyectos/{id}/cierre-resumen");

    public Task<ApiResult<ApiMessageDto>> CerrarProyectoAsync(int id, string? motivo, string? observaciones) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Post, $"api/proyectos/{id}/cerrar", new { Motivo = motivo, Observaciones = observaciones });

    public Task<ApiResult<List<ProyectoClienteItem>>> ObtenerProyectosClienteAsync() =>
        EnviarAsync<List<ProyectoClienteItem>>(HttpMethod.Get, "api/proyectos/cliente");

    public Task<ApiResult<ProyectoClienteDetalleViewModel>> ObtenerDetalleClienteProyectoAsync(int id) =>
        EnviarAsync<ProyectoClienteDetalleViewModel>(HttpMethod.Get, $"api/proyectos/{id}/cliente");
}
