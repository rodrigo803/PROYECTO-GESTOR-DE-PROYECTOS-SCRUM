using System.Net.Http.Json;
using System.Text.Json;
using Gestor_proyectos_scrum.Models;

namespace Gestor_proyectos_scrum.Services;

/// <summary>
/// Cliente HTTP tipado para consumir la Minimal API (Gestor-proyectos-scrum.Api).
/// Propaga automáticamente el header X-User-Id con el Id del usuario autenticado
/// por cookie (UsuarioActual, HU-01).
/// </summary>
public partial class ScrumApiClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly HttpClient _httpClient;
    private readonly UsuarioActual _usuarioActual;

    public ScrumApiClient(HttpClient httpClient, UsuarioActual usuarioActual)
    {
        _httpClient = httpClient;
        _usuarioActual = usuarioActual;
    }

    /// <summary>Id del usuario autenticado por cookie (HU-01); lo manda la Api como X-User-Id.</summary>
    public int ObtenerIdUsuarioActivo() => _usuarioActual.IdUsuario;

    public Task<PerfilActivoViewModel> ObtenerPerfilActivoAsync() => Task.FromResult(new PerfilActivoViewModel
    {
        IdUsuario = _usuarioActual.IdUsuario,
        Nombre = _usuarioActual.Nombre,
        Rol = _usuarioActual.Rol
    });

    // =========================================================================
    // HU-02: ENDPOINTS DE USUARIOS
    // =========================================================================

    public Task<ApiResult<List<RolItemViewModel>>> ObtenerRolesAsync() =>
        EnviarAsync<List<RolItemViewModel>>(HttpMethod.Get, "api/usuarios/roles");

    public Task<ApiResult<List<UsuarioItemViewModel>>> ObtenerUsuariosAsync(string? buscar = null, int? idRol = null, bool? activo = null)
    {
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(buscar)) query.Add($"buscar={Uri.EscapeDataString(buscar.Trim())}");
        if (idRol.HasValue && idRol.Value > 0) query.Add($"idRol={idRol.Value}");
        if (activo.HasValue) query.Add($"activo={activo.Value.ToString().ToLowerInvariant()}");

        var url = "api/usuarios" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        return EnviarAsync<List<UsuarioItemViewModel>>(HttpMethod.Get, url);
    }

    public Task<ApiResult<UsuarioItemViewModel>> ObtenerUsuarioPorIdAsync(int id) =>
        EnviarAsync<UsuarioItemViewModel>(HttpMethod.Get, $"api/usuarios/{id}");

    public Task<ApiResult<ApiMessageDto>> CrearUsuarioAsync(object payload) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Post, "api/usuarios", payload);

    public Task<ApiResult<ApiMessageDto>> ActualizarUsuarioAsync(int id, object payload) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Put, $"api/usuarios/{id}", payload);

    public Task<ApiResult<ApiMessageDto>> CambiarRolUsuarioAsync(int id, int idRol) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Patch, $"api/usuarios/{id}/rol", new { IdRol = idRol });

    public Task<ApiResult<ApiMessageDto>> CambiarEstadoUsuarioAsync(int id, bool activo) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Patch, $"api/usuarios/{id}/estado", new { Activo = activo });

    // =========================================================================
    // HU-05: ENDPOINTS DE SPRINTS
    // =========================================================================

    public Task<ApiResult<List<ProyectoItemViewModel>>> ObtenerProyectosSprintAsync() =>
        EnviarAsync<List<ProyectoItemViewModel>>(HttpMethod.Get, "api/sprints/proyectos");

    public Task<ApiResult<List<SprintItemViewModel>>> ObtenerSprintsAsync(int? idProyecto = null, string? estado = null, string? buscar = null)
    {
        var query = new List<string>();
        if (idProyecto.HasValue && idProyecto.Value > 0) query.Add($"idProyecto={idProyecto.Value}");
        if (!string.IsNullOrWhiteSpace(estado)) query.Add($"estado={Uri.EscapeDataString(estado.Trim())}");
        if (!string.IsNullOrWhiteSpace(buscar)) query.Add($"buscar={Uri.EscapeDataString(buscar.Trim())}");

        var url = "api/sprints" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        return EnviarAsync<List<SprintItemViewModel>>(HttpMethod.Get, url);
    }

    public Task<ApiResult<SprintDetalleApiDto>> ObtenerSprintDetalleAsync(int id) =>
        EnviarAsync<SprintDetalleApiDto>(HttpMethod.Get, $"api/sprints/{id}");

    public Task<ApiResult<ApiMessageDto>> CrearSprintAsync(object payload) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Post, "api/sprints", payload);

    public Task<ApiResult<ApiMessageDto>> ActualizarSprintAsync(int id, object payload) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Put, $"api/sprints/{id}", payload);

    public Task<ApiResult<ApiMessageDto>> CambiarEstadoSprintAsync(int id, string nuevoEstado, string? destinoIncompletas = null) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Patch, $"api/sprints/{id}/estado", new
        {
            NuevoEstado = nuevoEstado,
            DestinoIncompletas = destinoIncompletas
        });

    public Task<ApiResult<ApiMessageDto>> EliminarSprintAsync(int id) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Delete, $"api/sprints/{id}");

    // =========================================================================
    // HU-07: ENDPOINTS DE TAREAS, SPRINT BACKLOG, CHECKPOINTS Y APROBACIONES
    // =========================================================================

    public Task<ApiResult<TareaCatalogosViewModel>> ObtenerCatalogosTareasAsync() =>
        EnviarAsync<TareaCatalogosViewModel>(HttpMethod.Get, "api/tareas/catalogos");

    public Task<ApiResult<SprintBacklogApiDto>> ObtenerSprintBacklogAsync(
        int? idSprint = null,
        int? idResponsable = null,
        string? estado = null,
        string? buscar = null)
    {
        var query = new List<string>();
        if (idSprint.HasValue && idSprint.Value > 0) query.Add($"idSprint={idSprint.Value}");
        if (idResponsable.HasValue && idResponsable.Value > 0) query.Add($"idResponsable={idResponsable.Value}");
        if (!string.IsNullOrWhiteSpace(estado)) query.Add($"estado={Uri.EscapeDataString(estado.Trim())}");
        if (!string.IsNullOrWhiteSpace(buscar)) query.Add($"buscar={Uri.EscapeDataString(buscar.Trim())}");

        var url = "api/tareas/sprint-backlog" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        return EnviarAsync<SprintBacklogApiDto>(HttpMethod.Get, url);
    }

    public Task<ApiResult<List<TareaItemViewModel>>> ObtenerTareasAsync(
        int? idSprint = null,
        int? idResponsable = null,
        string? estado = null,
        string? buscar = null)
    {
        var query = new List<string>();
        if (idSprint.HasValue && idSprint.Value > 0) query.Add($"idSprint={idSprint.Value}");
        if (idResponsable.HasValue && idResponsable.Value > 0) query.Add($"idResponsable={idResponsable.Value}");
        if (!string.IsNullOrWhiteSpace(estado)) query.Add($"estado={Uri.EscapeDataString(estado.Trim())}");
        if (!string.IsNullOrWhiteSpace(buscar)) query.Add($"buscar={Uri.EscapeDataString(buscar.Trim())}");

        var url = "api/tareas" + (query.Count > 0 ? "?" + string.Join("&", query) : "");
        return EnviarAsync<List<TareaItemViewModel>>(HttpMethod.Get, url);
    }

    public Task<ApiResult<TareaDetalleApiDto>> ObtenerTareaDetalleAsync(int id) =>
        EnviarAsync<TareaDetalleApiDto>(HttpMethod.Get, $"api/tareas/{id}");

    public Task<ApiResult<ApiMessageDto>> CrearTareaAsync(object payload) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Post, "api/tareas", payload);

    public Task<ApiResult<ApiMessageDto>> ActualizarTareaAsync(int id, object payload) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Put, $"api/tareas/{id}", payload);

    public Task<ApiResult<ApiMessageDto>> RegistrarAvanceTareaAsync(int idTarea, object payload) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Post, $"api/tareas/{idTarea}/avances", payload);

    public Task<ApiResult<ApiMessageDto>> RegistrarAprobacionTareaAsync(int idTarea, object payload) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Post, $"api/tareas/{idTarea}/aprobaciones", payload);

    public Task<ApiResult<ApiMessageDto>> EliminarTareaAsync(int idTarea) =>
        EnviarAsync<ApiMessageDto>(HttpMethod.Delete, $"api/tareas/{idTarea}");

    // =========================================================================
    // MÉTODO INTERNO DE ENVÍO HTTP CON HEADER X-User-Id Y PARSEO DE ERRORES
    // =========================================================================

    private async Task<ApiResult<T>> EnviarAsync<T>(HttpMethod metodo, string urlRelativa, object? cuerpo = null)
    {
        try
        {
            using var request = new HttpRequestMessage(metodo, urlRelativa);
            request.Headers.Add("X-User-Id", ObtenerIdUsuarioActivo().ToString());

            if (cuerpo is not null)
            {
                request.Content = JsonContent.Create(cuerpo);
            }

            using var response = await _httpClient.SendAsync(request);
            var statusCode = (int)response.StatusCode;
            var contenido = await response.Content.ReadAsStringAsync();

            if (response.IsSuccessStatusCode)
            {
                if (string.IsNullOrWhiteSpace(contenido))
                {
                    return ApiResult<T>.Ok(default, statusCode);
                }

                var datos = JsonSerializer.Deserialize<T>(contenido, JsonOptions);
                return ApiResult<T>.Ok(datos, statusCode);
            }

            var mensajeError = ExtraerMensajeError(contenido, statusCode);
            return ApiResult<T>.Fallo(mensajeError, statusCode);
        }
        catch (HttpRequestException ex)
        {
            return ApiResult<T>.Fallo(
                $"No se pudo conectar con la Minimal API ({_httpClient.BaseAddress}). Verifique que el proyecto 'Gestor-proyectos-scrum.Api' esté en ejecución. Detalle: {ex.Message}",
                503);
        }
        catch (Exception ex)
        {
            return ApiResult<T>.Fallo($"Error al consumir la API: {ex.Message}", 500);
        }
    }

    private static string ExtraerMensajeError(string contenido, int statusCode)
    {
        if (!string.IsNullOrWhiteSpace(contenido))
        {
            try
            {
                var apiError = JsonSerializer.Deserialize<ApiErrorDto>(contenido, JsonOptions);
                if (!string.IsNullOrWhiteSpace(apiError?.Error))
                {
                    return apiError.Error;
                }
            }
            catch
            {
                // Ignorar error de deserialización y usar texto plano
            }
        }

        return statusCode switch
        {
            401 => "No autenticado para realizar esta operación en la API.",
            403 => "Su rol actual no tiene permisos para ejecutar esta acción.",
            404 => "El recurso solicitado no fue encontrado.",
            409 => "Conflicto de integridad con el estado actual de los datos.",
            _ => $"La API devolvió un código de error ({statusCode})."
        };
    }
}
