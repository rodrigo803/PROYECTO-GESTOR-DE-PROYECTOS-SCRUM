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

    public async Task<IReadOnlyList<HistoriaNoTerminadaItem>> ListarHistoriasNoTerminadasAsync(int idProyecto)
    {
        const string sql = @"
            select sh.IdSprintHistoria, h.IdHistoria, h.Codigo, h.Quiero, h.Estimacion, s.Nombre as NombreSprint
            from SprintHistorias sh
            join HistoriasUsuario h on h.IdHistoria = sh.IdHistoria
            join Sprints s on s.IdSprint = sh.IdSprint
            where s.IdProyecto = @IdProyecto and s.Estado = 'Cerrado' and sh.Resultado = 'Comprometida'
            order by s.FechaInicio, h.OrdenPrioridad;";

        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();
        var filas = await conexion.QueryAsync<HistoriaNoTerminadaItem>(sql, new { IdProyecto = idProyecto });
        return filas.ToList();
    }

    /// <summary>Aprueba (Sprint Review) una historia comprometida en un Sprint Activo: debe tener al
    /// menos un criterio y, si tiene tareas, todas deben estar Finalizadas.</summary>
    public async Task AceptarHistoriaAsync(int idSprint, int idHistoria, int idUsuarioActual)
    {
        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();
        using var transaccion = await conexion.BeginTransactionAsync();

        try
        {
            var sprintHistoria = await BloquearSprintHistoriaComprometidaAsync(conexion, transaccion, idSprint, idHistoria, estadoSprintRequerido: "Activo");

            var cantidadCriterios = await conexion.ExecuteScalarAsync<int>(
                "select count(*) from CriteriosAceptacion where IdHistoria = @IdHistoria;",
                new { IdHistoria = idHistoria }, transaccion);

            if (cantidadCriterios == 0)
            {
                throw new InvalidOperationException($"La historia {sprintHistoria.Codigo} no tiene criterios de aceptación.");
            }

            var cantidadTareasSinFinalizar = await conexion.ExecuteScalarAsync<int>(
                "select count(*) from Tareas where IdSprintHistoria = @IdSprintHistoria and Estado <> 'Finalizado';",
                new { sprintHistoria.IdSprintHistoria }, transaccion);

            if (cantidadTareasSinFinalizar > 0)
            {
                throw new InvalidOperationException($"La historia {sprintHistoria.Codigo} tiene tareas sin Finalizar.");
            }

            await conexion.ExecuteAsync(
                "update SprintHistorias set Resultado = 'Terminada' where IdSprintHistoria = @IdSprintHistoria;",
                new { sprintHistoria.IdSprintHistoria }, transaccion);

            await conexion.ExecuteAsync(
                "update HistoriasUsuario set Estado = 'Terminada' where IdHistoria = @IdHistoria;",
                new { IdHistoria = idHistoria }, transaccion);

            await conexion.ExecuteAsync(
                "insert into Aprobaciones (IdHistoria, IdAprobador, Resultado) values (@IdHistoria, @IdAprobador, 'Aprobado');",
                new { IdHistoria = idHistoria, IdAprobador = idUsuarioActual }, transaccion);

            await RegistrarAuditoriaAsync(conexion, transaccion, idUsuarioActual, "Aceptar historia", "Sprints", idSprint,
                $"Aceptó la historia {sprintHistoria.Codigo} en la revisión del Sprint.");

            await transaccion.CommitAsync();
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    /// <summary>Rechaza (Sprint Review) una historia comprometida en un Sprint Activo. La historia
    /// sigue comprometida; solo queda registrado el rechazo con su comentario.</summary>
    public async Task RechazarHistoriaAsync(int idSprint, int idHistoria, string comentario, int idUsuarioActual)
    {
        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();
        using var transaccion = await conexion.BeginTransactionAsync();

        try
        {
            var sprintHistoria = await BloquearSprintHistoriaComprometidaAsync(conexion, transaccion, idSprint, idHistoria, estadoSprintRequerido: "Activo");

            await conexion.ExecuteAsync(
                "insert into Aprobaciones (IdHistoria, IdAprobador, Resultado, Comentario) values (@IdHistoria, @IdAprobador, 'Rechazado', @Comentario);",
                new { IdHistoria = idHistoria, IdAprobador = idUsuarioActual, Comentario = comentario }, transaccion);

            await RegistrarAuditoriaAsync(conexion, transaccion, idUsuarioActual, "Rechazar historia", "Sprints", idSprint,
                $"Rechazó la historia {sprintHistoria.Codigo} en la revisión del Sprint. Comentario: {comentario}");

            await transaccion.CommitAsync();
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    /// <summary>Devuelve al Backlog una historia comprometida en un Sprint ya Cerrado que nunca se Terminó.</summary>
    public async Task RegresarAlBacklogAsync(int idSprintHistoria, int idUsuarioActual)
    {
        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();
        using var transaccion = await conexion.BeginTransactionAsync();

        try
        {
            var fila = await conexion.QueryFirstOrDefaultAsync<HistoriaNoTerminadaFila>(
                @"select sh.IdSprintHistoria, sh.IdHistoria, h.Codigo, s.Estado as EstadoSprint
                  from SprintHistorias sh
                  join HistoriasUsuario h on h.IdHistoria = sh.IdHistoria
                  join Sprints s on s.IdSprint = sh.IdSprint
                  where sh.IdSprintHistoria = @IdSprintHistoria and sh.Resultado = 'Comprometida'
                  for update;",
                new { IdSprintHistoria = idSprintHistoria }, transaccion);

            if (fila is null)
            {
                throw new InvalidOperationException("La historia ya no está comprometida en ese Sprint.");
            }

            if (fila.EstadoSprint != "Cerrado")
            {
                throw new InvalidOperationException("Esta acción solo aplica a historias de Sprints ya Cerrados.");
            }

            await conexion.ExecuteAsync(
                "update SprintHistorias set Resultado = 'Devuelta' where IdSprintHistoria = @IdSprintHistoria;",
                new { IdSprintHistoria = idSprintHistoria }, transaccion);

            await conexion.ExecuteAsync(
                "update HistoriasUsuario set Estado = 'Lista' where IdHistoria = @IdHistoria;",
                new { fila.IdHistoria }, transaccion);

            await RegistrarAuditoriaAsync(conexion, transaccion, idUsuarioActual, "Regresar al Backlog", "HistoriasUsuario", fila.IdHistoria,
                $"Devolvió la historia {fila.Codigo} al Product Backlog desde un Sprint Cerrado.");

            await transaccion.CommitAsync();
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    /// <summary>Replanifica una historia no terminada de un Sprint Cerrado a un Sprint Planificado del
    /// mismo proyecto. Las tareas no Finalizadas se mueven a la nueva fila; las Finalizadas se quedan
    /// en el Sprint viejo.</summary>
    public async Task ReplanificarAsync(int idSprintHistoria, int idSprintDestino, int idUsuarioActual)
    {
        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();
        using var transaccion = await conexion.BeginTransactionAsync();

        try
        {
            var vieja = await conexion.QueryFirstOrDefaultAsync<ReplanificarOrigenFila>(
                @"select sh.IdSprintHistoria, sh.IdHistoria, h.Codigo, h.Estimacion, s.Estado as EstadoSprint, s.IdProyecto
                  from SprintHistorias sh
                  join HistoriasUsuario h on h.IdHistoria = sh.IdHistoria
                  join Sprints s on s.IdSprint = sh.IdSprint
                  where sh.IdSprintHistoria = @IdSprintHistoria and sh.Resultado = 'Comprometida'
                  for update;",
                new { IdSprintHistoria = idSprintHistoria }, transaccion);

            if (vieja is null)
            {
                throw new InvalidOperationException("La historia ya no está comprometida en ese Sprint.");
            }

            if (vieja.EstadoSprint != "Cerrado")
            {
                throw new InvalidOperationException("Esta acción solo aplica a historias de Sprints ya Cerrados.");
            }

            var destino = await conexion.QueryFirstOrDefaultAsync<SprintParaComprometerFila>(
                "select IdProyecto, Estado, CapacidadPuntos from Sprints where IdSprint = @IdSprintDestino for update;",
                new { IdSprintDestino = idSprintDestino }, transaccion);

            if (destino is null)
            {
                throw new InvalidOperationException("El Sprint destino no existe.");
            }

            if (destino.Estado != "Planificado")
            {
                throw new InvalidOperationException("El Sprint destino debe estar Planificado.");
            }

            if (destino.IdProyecto != vieja.IdProyecto)
            {
                throw new InvalidOperationException("El Sprint destino debe ser del mismo proyecto.");
            }

            var puntosYaComprometidos = await conexion.ExecuteScalarAsync<int>(
                @"select coalesce(sum(h2.Estimacion), 0)
                  from SprintHistorias sh2
                  join HistoriasUsuario h2 on h2.IdHistoria = sh2.IdHistoria
                  where sh2.IdSprint = @IdSprintDestino and sh2.Resultado = 'Comprometida';",
                new { IdSprintDestino = idSprintDestino }, transaccion);

            var puntosHistoria = vieja.Estimacion ?? 0;
            if (puntosYaComprometidos + puntosHistoria > destino.CapacidadPuntos)
            {
                var disponibles = destino.CapacidadPuntos - puntosYaComprometidos;
                throw new InvalidOperationException(
                    $"El Sprint destino no tiene capacidad: disponibles {disponibles} pts, la historia {vieja.Codigo} necesita {puntosHistoria} pts.");
            }

            await conexion.ExecuteAsync(
                "update SprintHistorias set Resultado = 'Replanificada' where IdSprintHistoria = @IdSprintHistoria;",
                new { IdSprintHistoria = idSprintHistoria }, transaccion);

            await conexion.ExecuteAsync(
                "insert into SprintHistorias (IdSprint, IdHistoria, Resultado) values (@IdSprint, @IdHistoria, 'Comprometida');",
                new { IdSprint = idSprintDestino, vieja.IdHistoria }, transaccion);

            var idSprintHistoriaNueva = await conexion.ExecuteScalarAsync<int>("select last_insert_id();", transaction: transaccion);

            await conexion.ExecuteAsync(
                "update Tareas set IdSprintHistoria = @Nueva where IdSprintHistoria = @Vieja and Estado <> 'Finalizado';",
                new { Nueva = idSprintHistoriaNueva, Vieja = idSprintHistoria }, transaccion);

            await RegistrarAuditoriaAsync(conexion, transaccion, idUsuarioActual, "Replanificar historia", "Sprints", idSprintDestino,
                $"Replanificó la historia {vieja.Codigo} al nuevo Sprint; sus tareas no Finalizadas se movieron con ella.");

            await transaccion.CommitAsync();
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    private static async Task<SprintHistoriaParaQuitarFila> BloquearSprintHistoriaComprometidaAsync(
        MySqlConnection conexion, MySqlTransaction transaccion, int idSprint, int idHistoria, string estadoSprintRequerido)
    {
        var estadoSprint = await conexion.ExecuteScalarAsync<string?>(
            "select Estado from Sprints where IdSprint = @IdSprint for update;",
            new { IdSprint = idSprint }, transaccion);

        if (estadoSprint is null)
        {
            throw new InvalidOperationException("El Sprint no existe.");
        }

        if (estadoSprint != estadoSprintRequerido)
        {
            throw new InvalidOperationException($"El Sprint debe estar {estadoSprintRequerido} para esta acción.");
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

        return sprintHistoria;
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

    private class HistoriaNoTerminadaFila
    {
        public int IdSprintHistoria { get; set; }
        public int IdHistoria { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string EstadoSprint { get; set; } = string.Empty;
    }

    private class ReplanificarOrigenFila
    {
        public int IdSprintHistoria { get; set; }
        public int IdHistoria { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public int? Estimacion { get; set; }
        public string EstadoSprint { get; set; } = string.Empty;
        public int IdProyecto { get; set; }
    }
}
