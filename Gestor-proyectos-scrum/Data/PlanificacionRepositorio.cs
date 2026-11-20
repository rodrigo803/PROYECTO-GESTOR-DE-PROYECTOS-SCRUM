using Dapper;
using Gestor_proyectos_scrum.Models;
using MySqlConnector;

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

    /// <summary>
    /// Comprometer historias a un Sprint. Vuelve a verificar todas las reglas dentro de la
    /// transacción (con "for update" sobre las filas involucradas) para evitar que dos personas
    /// comprometan la misma historia, o superen la capacidad, al mismo tiempo.
    /// </summary>
    public async Task ComprometerAsync(int idSprint, IReadOnlyList<int> idsHistorias, int idUsuarioActual)
    {
        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();
        using var transaccion = await conexion.BeginTransactionAsync();

        try
        {
            var sprint = await conexion.QueryFirstOrDefaultAsync<SprintParaComprometerFila>(
                "select IdProyecto, Estado, CapacidadPuntos from Sprints where IdSprint = @IdSprint for update;",
                new { IdSprint = idSprint }, transaccion);

            if (sprint is null)
            {
                throw new InvalidOperationException("El Sprint no existe.");
            }

            if (sprint.Estado is not ("Planificado" or "Activo"))
            {
                throw new InvalidOperationException("El Sprint ya no está Planificado ni Activo.");
            }

            const string sqlHistorias = @"
                select
                    h.IdHistoria,
                    h.Codigo,
                    h.IdProyecto,
                    h.Estado,
                    h.Estimacion,
                    (select count(*) from CriteriosAceptacion ca where ca.IdHistoria = h.IdHistoria) as CantidadCriterios,
                    exists (
                        select 1 from SprintHistorias sh
                        where sh.IdHistoria = h.IdHistoria and sh.Resultado = 'Comprometida'
                    ) as YaComprometida
                from HistoriasUsuario h
                where h.IdHistoria in @IdsHistorias
                for update;";

            var historias = (await conexion.QueryAsync<HistoriaParaComprometerFila>(
                sqlHistorias, new { IdsHistorias = idsHistorias }, transaccion)).ToList();

            foreach (var idHistoria in idsHistorias)
            {
                var historia = historias.FirstOrDefault(h => h.IdHistoria == idHistoria);
                if (historia is null)
                {
                    throw new InvalidOperationException("Una de las historias seleccionadas ya no existe.");
                }

                if (historia.IdProyecto != sprint.IdProyecto)
                {
                    throw new InvalidOperationException($"La historia {historia.Codigo} no pertenece al proyecto del Sprint.");
                }

                if (historia.YaComprometida)
                {
                    throw new InvalidOperationException($"La historia {historia.Codigo} ya fue comprometida en un Sprint.");
                }

                if (historia.Estado != "Lista" || historia.Estimacion is null || historia.CantidadCriterios == 0)
                {
                    throw new InvalidOperationException($"La historia {historia.Codigo} ya no cumple las reglas para entrar al Sprint.");
                }
            }

            var puntosYaComprometidos = await conexion.ExecuteScalarAsync<int>(
                @"select coalesce(sum(h2.Estimacion), 0)
                  from SprintHistorias sh2
                  join HistoriasUsuario h2 on h2.IdHistoria = sh2.IdHistoria
                  where sh2.IdSprint = @IdSprint and sh2.Resultado = 'Comprometida';",
                new { IdSprint = idSprint }, transaccion);

            var puntosNuevos = historias.Sum(h => h.Estimacion ?? 0);
            if (puntosYaComprometidos + puntosNuevos > sprint.CapacidadPuntos)
            {
                var disponibles = sprint.CapacidadPuntos - puntosYaComprometidos;
                throw new InvalidOperationException(
                    $"Superas la capacidad del Sprint: disponibles {disponibles} pts, se intentaron comprometer {puntosNuevos} pts.");
            }

            foreach (var historia in historias)
            {
                await conexion.ExecuteAsync(
                    "insert into SprintHistorias (IdSprint, IdHistoria, Resultado) values (@IdSprint, @IdHistoria, 'Comprometida');",
                    new { IdSprint = idSprint, historia.IdHistoria }, transaccion);

                await conexion.ExecuteAsync(
                    "update HistoriasUsuario set Estado = 'En Sprint' where IdHistoria = @IdHistoria;",
                    new { historia.IdHistoria }, transaccion);
            }

            var codigos = string.Join(", ", historias.Select(h => h.Codigo));
            await RegistrarAuditoriaAsync(conexion, transaccion, idUsuarioActual, "Comprometer historias", "Sprints", idSprint,
                $"Comprometió las historias {codigos} al Sprint.");

            await transaccion.CommitAsync();
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    /// <summary>Quita una historia comprometida del Sprint y la devuelve a 'Lista'. Falla si tiene tareas.</summary>
    public async Task QuitarHistoriaAsync(int idSprint, int idHistoria, int idUsuarioActual)
    {
        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();
        using var transaccion = await conexion.BeginTransactionAsync();

        try
        {
            var estadoSprint = await conexion.ExecuteScalarAsync<string?>(
                "select Estado from Sprints where IdSprint = @IdSprint for update;",
                new { IdSprint = idSprint }, transaccion);

            if (estadoSprint is null)
            {
                throw new InvalidOperationException("El Sprint no existe.");
            }

            if (estadoSprint == "Cerrado")
            {
                throw new InvalidOperationException("No se puede modificar un Sprint Cerrado.");
            }

            var sprintHistoria = await conexion.QueryFirstOrDefaultAsync<SprintHistoriaParaQuitarFila>(
                @"select sh.IdSprintHistoria, h.Codigo
                  from SprintHistorias sh
                  join HistoriasUsuario h on h.IdHistoria = sh.IdHistoria
                  where sh.IdSprint = @IdSprint and sh.IdHistoria = @IdHistoria and sh.Resultado = 'Comprometida'
                  for update;",
                new { IdSprint = idSprint, IdHistoria = idHistoria }, transaccion);

            if (sprintHistoria is null)
            {
                throw new InvalidOperationException("La historia no está comprometida en este Sprint.");
            }

            var cantidadTareas = await conexion.ExecuteScalarAsync<int>(
                "select count(*) from Tareas where IdSprintHistoria = @IdSprintHistoria;",
                new { sprintHistoria.IdSprintHistoria }, transaccion);

            if (cantidadTareas > 0)
            {
                throw new InvalidOperationException("Primero hay que eliminar o mover sus tareas.");
            }

            await conexion.ExecuteAsync(
                "delete from SprintHistorias where IdSprintHistoria = @IdSprintHistoria;",
                new { sprintHistoria.IdSprintHistoria }, transaccion);

            await conexion.ExecuteAsync(
                "update HistoriasUsuario set Estado = 'Lista' where IdHistoria = @IdHistoria;",
                new { IdHistoria = idHistoria }, transaccion);

            await RegistrarAuditoriaAsync(conexion, transaccion, idUsuarioActual, "Quitar historia", "Sprints", idSprint,
                $"Quitó la historia {sprintHistoria.Codigo} del Sprint.");

            await transaccion.CommitAsync();
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    private static async Task RegistrarAuditoriaAsync(MySqlConnection conexion, MySqlTransaction transaccion, int idUsuario, string accion, string entidad, int idRegistro, string detalle)
    {
        const string sql = @"
            insert into Auditoria (IdUsuario, Accion, Entidad, IdRegistro, Detalle)
            values (@IdUsuario, @Accion, @Entidad, @IdRegistro, @Detalle);";

        await conexion.ExecuteAsync(sql, new { IdUsuario = idUsuario, Accion = accion, Entidad = entidad, IdRegistro = idRegistro, Detalle = detalle }, transaccion);
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

    private class SprintParaComprometerFila
    {
        public int IdProyecto { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int CapacidadPuntos { get; set; }
    }

    private class HistoriaParaComprometerFila
    {
        public int IdHistoria { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public int IdProyecto { get; set; }
        public string Estado { get; set; } = string.Empty;
        public int? Estimacion { get; set; }
        public int CantidadCriterios { get; set; }
        public bool YaComprometida { get; set; }
    }

    private class SprintHistoriaParaQuitarFila
    {
        public int IdSprintHistoria { get; set; }
        public string Codigo { get; set; } = string.Empty;
    }
}
