using Dapper;
using MySqlConnector;

namespace Gestor_proyectos_scrum.Api.Services;

/// <summary>
/// Registra acciones relevantes en la tabla Auditoria de MySQL.
/// </summary>
public class AuditoriaService
{
    public async Task RegistrarAsync(
        MySqlConnection conexion,
        int idUsuario,
        string accion,
        string entidad,
        int? idRegistro,
        string? detalle,
        MySqlTransaction? transaccion = null)
    {
        const string sql = @"
            INSERT INTO Auditoria (IdUsuario, Accion, Entidad, IdRegistro, Detalle, Fecha)
            VALUES (@IdUsuario, @Accion, @Entidad, @IdRegistro, @Detalle, NOW());";

        var detalleLimpio = string.IsNullOrWhiteSpace(detalle)
            ? null
            : (detalle.Length > 500 ? detalle[..500] : detalle);

        await conexion.ExecuteAsync(
            sql,
            new
            {
                IdUsuario = idUsuario,
                Accion = accion.Length > 30 ? accion[..30] : accion,
                Entidad = entidad.Length > 40 ? entidad[..40] : entidad,
                IdRegistro = idRegistro,
                Detalle = detalleLimpio
            },
            transaction: transaccion);
    }
}
