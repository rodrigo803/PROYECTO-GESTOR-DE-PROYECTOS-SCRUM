using Dapper;
using Gestor_proyectos_scrum.Models;

namespace Gestor_proyectos_scrum.Data;

public class PlanificacionRepositorio
{
    private readonly ConexionDb _conexionDb;

    public PlanificacionRepositorio(ConexionDb conexionDb)
    {
        _conexionDb = conexionDb;
    }

    public async Task<IReadOnlyList<ProyectoOpcion>> ListarProyectosAbiertosAsync()
    {
        const string sql = @"
            select IdProyecto, Codigo, Nombre, Estado
            from Proyectos
            where Estado <> 'Cerrado'
            order by Codigo;";

        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();
        var filas = await conexion.QueryAsync<ProyectoOpcion>(sql);
        return filas.ToList();
    }

    public async Task<IReadOnlyList<SprintOpcion>> ListarSprintsPlanificablesAsync(int idProyecto)
    {
        const string sql = @"
            select IdSprint, Nombre, Estado
            from Sprints
            where IdProyecto = @IdProyecto and Estado in ('Planificado', 'Activo')
            order by FechaInicio;";

        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();
        var filas = await conexion.QueryAsync<SprintOpcion>(sql, new { IdProyecto = idProyecto });
        return filas.ToList();
    }

    public async Task<SprintPlanificacionInfo?> ObtenerSprintAsync(int idSprint)
    {
        const string sql = @"
            select IdSprint, IdProyecto, Nombre, Objetivo, FechaInicio, FechaFin, Estado, CapacidadPuntos
            from Sprints
            where IdSprint = @IdSprint;";

        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();
        return await conexion.QueryFirstOrDefaultAsync<SprintPlanificacionInfo>(sql, new { IdSprint = idSprint });
    }

    public async Task<IReadOnlyList<HistoriaCandidataItem>> ListarCandidatasAsync(int idProyecto)
    {
        const string sql = @"
            select
                h.IdHistoria,
                h.Codigo,
                h.Como,
                h.Quiero,
                h.Para,
                h.Prioridad,
                h.OrdenPrioridad,
                h.Estimacion,
                h.Estado,
                count(ca.IdCriterio) as CantidadCriterios
            from HistoriasUsuario h
            left join CriteriosAceptacion ca on ca.IdHistoria = h.IdHistoria
            where h.IdProyecto = @IdProyecto
              and h.Estado <> 'Terminada'
              and not exists (
                  select 1 from SprintHistorias sh
                  where sh.IdHistoria = h.IdHistoria
                    and sh.Resultado = 'Comprometida'
              )
            group by h.IdHistoria, h.Codigo, h.Como, h.Quiero, h.Para, h.Prioridad, h.OrdenPrioridad, h.Estimacion, h.Estado
            order by h.OrdenPrioridad;";

        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();
        var filas = await conexion.QueryAsync<HistoriaCandidataItemFila>(sql, new { IdProyecto = idProyecto });

        return filas.Select(MapearCandidata).ToList();
    }

    public async Task<IReadOnlyList<HistoriaComprometidaItem>> ListarComprometidasAsync(int idSprint)
    {
        const string sql = @"
            select
                h.IdHistoria,
                h.Codigo,
                h.Quiero,
                h.Estimacion,
                sh.Resultado,
                (select count(*) from Tareas t where t.IdSprintHistoria = sh.IdSprintHistoria) as CantidadTareas
            from SprintHistorias sh
            join HistoriasUsuario h on h.IdHistoria = sh.IdHistoria
            where sh.IdSprint = @IdSprint and sh.Resultado = 'Comprometida'
            order by h.OrdenPrioridad;";

        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();
        var filas = await conexion.QueryAsync<HistoriaComprometidaItem>(sql, new { IdSprint = idSprint });
        return filas.ToList();
    }

    private static HistoriaCandidataItem MapearCandidata(HistoriaCandidataItemFila fila)
    {
        var motivos = new List<string>();
        if (fila.Estado != "Lista")
        {
            motivos.Add("No está en estado Lista");
        }

        if (fila.Estimacion is null)
        {
            motivos.Add("Sin estimación");
        }

        if (fila.CantidadCriterios == 0)
        {
            motivos.Add("Sin criterios de aceptación");
        }

        return new HistoriaCandidataItem
        {
            IdHistoria = fila.IdHistoria,
            Codigo = fila.Codigo,
            Como = fila.Como,
            Quiero = fila.Quiero,
            Para = fila.Para,
            Prioridad = fila.Prioridad,
            OrdenPrioridad = fila.OrdenPrioridad,
            Estimacion = fila.Estimacion,
            CantidadCriterios = fila.CantidadCriterios,
            Motivos = motivos
        };
    }

    /// <summary>Fila cruda devuelta por Dapper para ListarCandidatasAsync, antes de calcular los Motivos.</summary>
    private class HistoriaCandidataItemFila
    {
        public int IdHistoria { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Como { get; set; } = string.Empty;
        public string Quiero { get; set; } = string.Empty;
        public string Para { get; set; } = string.Empty;
        public string Prioridad { get; set; } = string.Empty;
        public int OrdenPrioridad { get; set; }
        public int? Estimacion { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int CantidadCriterios { get; set; }
    }
}
