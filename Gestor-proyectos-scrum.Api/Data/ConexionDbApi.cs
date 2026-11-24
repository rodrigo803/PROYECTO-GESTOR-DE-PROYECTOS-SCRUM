using Dapper;
using MySqlConnector;

namespace Gestor_proyectos_scrum.Api.Data;

/// <summary>
/// Entrega conexiones MySQL abiertas a partir de la cadena "ScrumDb"
/// (User Secrets / appsettings.Development.json en local / appsettings.Production.json en Plesk).
/// </summary>
public class ConexionDbApi
{
    private static bool _sincronizacionInicialEjecutada;
    private static readonly SemaphoreSlim SyncLock = new(1, 1);

    private readonly string _cadenaConexion;

    public ConexionDbApi(IConfiguration configuracion)
    {
        _cadenaConexion = configuracion.GetConnectionString("ScrumDb")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'ScrumDb' en la configuración de la API.");
    }

    public async Task<MySqlConnection> CrearConexionAbiertaAsync()
    {
        var conexion = new MySqlConnection(_cadenaConexion);
        await conexion.OpenAsync();

        if (!_sincronizacionInicialEjecutada)
        {
            await AsegurarSincronizacionCheckpointsAsync(conexion);
        }

        return conexion;
    }

    /// <summary>
    /// Garantiza que la tabla Tareas tenga sincronizados PorcentajeAvance, Estado, HorasReales
    /// y FechaEntregaReal a partir de los checkpoints de AvanceTareas (útil cuando los datos
    /// iniciales fueron importados en MySQL/phpMyAdmin sin que se disparara el trigger tr_AvanceTareas_Registrar).
    /// </summary>
    public static async Task SincronizarTareasConAvancesAsync(MySqlConnection conexion, int? idTarea = null)
    {
        var sql = @"
            UPDATE Tareas t
            INNER JOIN (
                SELECT
                    a.IdTarea,
                    COALESCE(SUM(a.HorasTrabajadas), 0) AS TotalHoras,
                    MAX(a.IdAvance) AS UltimoIdAvance
                FROM AvanceTareas a
                " + (idTarea.HasValue ? "WHERE a.IdTarea = @IdTarea" : "") + @"
                GROUP BY a.IdTarea
            ) res ON res.IdTarea = t.IdTarea
            INNER JOIN AvanceTareas ult ON ult.IdAvance = res.UltimoIdAvance
            SET
                t.PorcentajeAvance = ult.PorcentajeNuevo,
                t.Estado = ult.EstadoNuevo,
                t.HorasReales = res.TotalHoras,
                t.FechaEntregaReal = CASE WHEN ult.PorcentajeNuevo = 100 THEN ult.Fecha ELSE NULL END
            WHERE t.PorcentajeAvance <> ult.PorcentajeNuevo
               OR t.HorasReales <> res.TotalHoras
               OR t.Estado <> ult.EstadoNuevo;";

        await conexion.ExecuteAsync(sql, new { IdTarea = idTarea });
    }

    private static async Task AsegurarSincronizacionCheckpointsAsync(MySqlConnection conexion)
    {
        await SyncLock.WaitAsync();
        try
        {
            if (_sincronizacionInicialEjecutada)
            {
                return;
            }

            // Verificar si las 20 tareas iniciales existen pero AvanceTareas quedó vacía al importar el script inicial
            var totalTareas = await conexion.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM Tareas;");
            var totalAvances = await conexion.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM AvanceTareas;");

            if (totalTareas >= 20 && totalAvances == 0)
            {
                await SembrarCheckpointsInicialesFaltantesAsync(conexion);
            }

            await SincronizarTareasConAvancesAsync(conexion);
            _sincronizacionInicialEjecutada = true;
        }
        catch
        {
            // No bloquear la apertura de conexión si ocurre cualquier excepción
        }
        finally
        {
            SyncLock.Release();
        }
    }

    /// <summary>
    /// Si al importar BDScrumManager_MySQL_datos_iniciales.sql falló el bloque multi-row de AvanceTareas
    /// (por ejemplo si tr_AvanceTareas_Validar estaba activo pero tr_AvanceTareas_Registrar no se creó),
    /// inserta fila por fila los 21 checkpoints oficiales del script inicial sincronizando Tareas en cada paso.
    /// </summary>
    private static async Task SembrarCheckpointsInicialesFaltantesAsync(MySqlConnection conexion)
    {
        var checkpoints = new (int IdTarea, int IdUsuario, string Fecha, int Ant, int Nuevo, decimal Horas, string EstAnt, string EstNuevo, string? Just, int? Imp)[]
        {
            (1, 4, "2026-10-28 10:00:00", 0, 50, 4m, "Pendiente", "En progreso", null, null),
            (1, 4, "2026-10-30 16:00:00", 50, 100, 5m, "En progreso", "Finalizado", null, null),
            (2, 5, "2026-10-29 11:00:00", 0, 60, 4m, "Pendiente", "En progreso", null, null),
            (2, 5, "2026-11-02 15:00:00", 60, 100, 3m, "En progreso", "Finalizado", null, null),
            (3, 6, "2026-11-03 16:00:00", 0, 100, 7m, "Pendiente", "Finalizado", null, null),
            (4, 9, "2026-11-04 14:00:00", 0, 100, 4m, "Pendiente", "Finalizado", null, null),
            (5, 4, "2026-11-05 10:00:00", 0, 70, 6m, "Pendiente", "En progreso", null, null),
            (5, 4, "2026-11-07 12:00:00", 70, 100, 3m, "En progreso", "Finalizado", null, null),
            (6, 4, "2026-11-11 10:00:00", 0, 25, 5m, "Pendiente", "En progreso", null, null),
            (6, 4, "2026-11-14 16:00:00", 25, 50, 6m, "En progreso", "En progreso", null, null),
            (6, 4, "2026-11-17 09:00:00", 50, 40, 2m, "En progreso", "En progreso", "La pasarela cambió la versión de su API; se rehace la autorización", 1),
            (7, 5, "2026-11-12 15:00:00", 0, 100, 6m, "Pendiente", "Finalizado", null, null),
            (9, 9, "2026-11-12 11:00:00", 0, 40, 4m, "Pendiente", "En progreso", null, null),
            (9, 9, "2026-11-16 17:00:00", 40, 80, 4m, "En progreso", "En revisión", null, null),
            (10, 5, "2026-11-13 10:00:00", 0, 30, 2m, "Pendiente", "En progreso", null, null),
            (13, 10, "2026-10-30 16:00:00", 0, 100, 9m, "Pendiente", "Finalizado", null, null),
            (14, 10, "2026-11-12 14:00:00", 0, 100, 5m, "Pendiente", "Finalizado", null, null),
            (15, 5, "2026-11-16 11:00:00", 0, 50, 4m, "Pendiente", "En progreso", null, null),
            (16, 10, "2026-11-18 10:00:00", 0, 20, 3m, "Pendiente", "En progreso", null, null),
            (17, 6, "2026-11-11 09:00:00", 0, 60, 8m, "Pendiente", "En progreso", null, null),
            (19, 10, "2026-11-17 15:00:00", 0, 100, 5m, "Pendiente", "Finalizado", null, null)
        };

        const string insertSql = @"
            INSERT INTO AvanceTareas (
                IdTarea, IdUsuario, Fecha,
                PorcentajeAnterior, PorcentajeNuevo,
                HorasTrabajadas, EstadoAnterior, EstadoNuevo,
                Justificacion, IdImpedimento
            )
            VALUES (
                @IdTarea, @IdUsuario, @Fecha,
                @Ant, @Nuevo,
                @Horas, @EstAnt, @EstNuevo,
                @Just, @Imp
            );";

        foreach (var c in checkpoints)
        {
            await conexion.ExecuteAsync(insertSql, new
            {
                c.IdTarea,
                c.IdUsuario,
                c.Fecha,
                c.Ant,
                c.Nuevo,
                c.Horas,
                c.EstAnt,
                c.EstNuevo,
                c.Just,
                c.Imp
            });

            await SincronizarTareasConAvancesAsync(conexion, c.IdTarea);
        }
    }
}
