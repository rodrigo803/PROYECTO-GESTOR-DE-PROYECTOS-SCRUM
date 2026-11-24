using Gestor_proyectos_scrum.Models;

namespace Gestor_proyectos_scrum.Services;

/// <summary>
/// HU-06: métodos de ScrumApiClient para consumir /api/planificacion.
/// Deserializa directo en los Models existentes (ProyectoOpcion, SprintOpcion,
/// SprintPlanificacionInfo, HistoriaCandidataItem, etc.) para no tener que duplicarlos.
/// </summary>
public partial class ScrumApiClient
{
    // ---------- Gestión ----------

    public Task<ApiResult<List<ProyectoOpcion>>> ObtenerProyectosAbiertosAsync() =>
        EnviarAsync<List<ProyectoOpcion>>(HttpMethod.Get, "api/planificacion/proyectos");

    public Task<ApiResult<List<SprintOpcion>>> ObtenerSprintsPlanificablesAsync(int idProyecto) =>
        EnviarAsync<List<SprintOpcion>>(HttpMethod.Get, $"api/planificacion/proyectos/{idProyecto}/sprints");

    public Task<ApiResult<SprintPlanificacionInfo>> ObtenerSprintPlanificacionAsync(int idSprint) =>
        EnviarAsync<SprintPlanificacionInfo>(HttpMethod.Get, $"api/planificacion/sprints/{idSprint}");

    public Task<ApiResult<List<HistoriaNoTerminadaItem>>> ObtenerHistoriasNoTerminadasAsync(int idProyecto) =>
        EnviarAsync<List<HistoriaNoTerminadaItem>>(HttpMethod.Get, $"api/planificacion/proyectos/{idProyecto}/historias-no-terminadas");

    public Task<ApiResult<List<HistoriaCandidataItem>>> ObtenerCandidatasAsync(int idProyecto) =>
        EnviarAsync<List<HistoriaCandidataItem>>(HttpMethod.Get, $"api/planificacion/proyectos/{idProyecto}/candidatas");

    public Task<ApiResult<List<HistoriaComprometidaItem>>> ObtenerComprometidasAsync(int idSprint) =>
        EnviarAsync<List<HistoriaComprometidaItem>>(HttpMethod.Get, $"api/planificacion/sprints/{idSprint}/comprometidas");

    // ---------- Cliente ----------

    public Task<ApiResult<List<ProyectoOpcion>>> ObtenerProyectosDeMiembroAsync() =>
        EnviarAsync<List<ProyectoOpcion>>(HttpMethod.Get, "api/planificacion/cliente/proyectos");

    public Task<ApiResult<List<SprintOpcion>>> ObtenerSprintsDelProyectoClienteAsync(int idProyecto) =>
        EnviarAsync<List<SprintOpcion>>(HttpMethod.Get, $"api/planificacion/cliente/proyectos/{idProyecto}/sprints");

    public Task<ApiResult<SprintPlanificacionInfo>> ObtenerSprintClienteAsync(int idSprint) =>
        EnviarAsync<SprintPlanificacionInfo>(HttpMethod.Get, $"api/planificacion/cliente/sprints/{idSprint}");

    public Task<ApiResult<List<HistoriaComprometidaClienteItem>>> ObtenerComprometidasClienteAsync(int idSprint) =>
        EnviarAsync<List<HistoriaComprometidaClienteItem>>(HttpMethod.Get, $"api/planificacion/cliente/sprints/{idSprint}/comprometidas");

    // ---------- Mutaciones ----------

    public Task<ApiResult<ApiMessageDto>> ComprometerHistoriasAsync(int idSprint, IEnumerable<int> idsHistorias) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Post, $"api/planificacion/sprints/{idSprint}/comprometer", new { IdsHistorias = idsHistorias });

    public Task<ApiResult<ApiMessageDto>> QuitarHistoriaAsync(int idSprint, int idHistoria) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Post, $"api/planificacion/sprints/{idSprint}/historias/{idHistoria}/quitar");

    public Task<ApiResult<ApiMessageDto>> AceptarHistoriaAsync(int idSprint, int idHistoria) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Post, $"api/planificacion/sprints/{idSprint}/historias/{idHistoria}/aceptar");

    public Task<ApiResult<ApiMessageDto>> RechazarHistoriaAsync(int idSprint, int idHistoria, string comentario) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Post, $"api/planificacion/sprints/{idSprint}/historias/{idHistoria}/rechazar", new { Comentario = comentario });

    public Task<ApiResult<ApiMessageDto>> RegresarAlBacklogAsync(int idSprintHistoria) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Post, $"api/planificacion/sprint-historias/{idSprintHistoria}/regresar-backlog");

    public Task<ApiResult<ApiMessageDto>> ReplanificarAsync(int idSprintHistoria, int idSprintDestino) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Post, $"api/planificacion/sprint-historias/{idSprintHistoria}/replanificar", new { IdSprintDestino = idSprintDestino });
}
