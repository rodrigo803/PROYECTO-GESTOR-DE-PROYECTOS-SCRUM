using Gestor_proyectos_scrum.Models;

namespace Gestor_proyectos_scrum.Services;

public partial class ScrumApiClient
{
    // =========================================================================
    // HU-04: ENDPOINTS DE PRODUCT BACKLOG
    // =========================================================================

    public Task<ApiResult<List<ProyectoOpcion>>> ObtenerProyectosBacklogAsync() =>
        EnviarAsync<List<ProyectoOpcion>>(HttpMethod.Get, "api/backlog/proyectos");

    public Task<ApiResult<List<HistoriaDto>>> ObtenerHistoriasBacklogAsync(int idProyecto) =>
        EnviarAsync<List<HistoriaDto>>(HttpMethod.Get, $"api/backlog/proyectos/{idProyecto}/historias");

    public Task<ApiResult<ApiMessageDto>> CrearHistoriaAsync(object payload) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Post, "api/backlog/historias", payload);

    public Task<ApiResult<ApiMessageDto>> ActualizarHistoriaAsync(int idHistoria, object payload) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Put, $"api/backlog/historias/{idHistoria}", payload);

    public Task<ApiResult<ApiMessageDto>> EliminarHistoriaAsync(int idHistoria) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Delete, $"api/backlog/historias/{idHistoria}");
}
