using Dapper;
using Gestor_proyectos_scrum.Models;
using MySqlConnector;

namespace Gestor_proyectos_scrum.Data;

public class ProyectoRepositorio
{
    private readonly ConexionDb _conexionDb;

    public ProyectoRepositorio(ConexionDb conexionDb)
    {
        _conexionDb = conexionDb;
    }

    public async Task<IReadOnlyList<ProyectoListaItem>> ListarAsync(string? buscar, string? estado, string? orden)
    {
        const string sql = @"
            select
                p.IdProyecto,
                p.Codigo,
                p.Nombre,
                p.FechaInicio,
                p.FechaFin,
                p.Estado,
                max(case when pm.RolProyecto = 'Product Owner' then concat(u.Nombre, ' ', u.Apellidos) end) as ProductOwner,
                max(case when pm.RolProyecto = 'Scrum Master' then concat(u.Nombre, ' ', u.Apellidos) end) as ScrumMaster,
                count(pm.IdProyectoMiembro) as CantidadMiembros,
                group_concat(concat(u.Nombre, ' ', u.Apellidos, ':', upper(left(u.Nombre, 1)), upper(left(u.Apellidos, 1))) order by pm.FechaIngreso separator '|') as MiembrosCsv,
                av.Avance as Avance
            from Proyectos p
            left join ProyectoMiembros pm on pm.IdProyecto = p.IdProyecto
            left join Usuarios u on u.IdUsuario = pm.IdUsuario
            left join (
                select IdProyecto,
                       case when coalesce(sum(Estimacion), 0) = 0 then null
                            else sum(case when Estado = 'Terminada' then Estimacion else 0 end) * 100.0 / sum(Estimacion)
                       end as Avance
                from HistoriasUsuario
                group by IdProyecto
            ) av on av.IdProyecto = p.IdProyecto
            where (@Buscar is null or @Buscar = '' or p.Nombre like concat('%', @Buscar, '%') or p.Codigo like concat('%', @Buscar, '%'))
              and (@Estado is null or @Estado = '' or p.Estado = @Estado)
            group by p.IdProyecto, p.Codigo, p.Nombre, p.FechaInicio, p.FechaFin, p.Estado, av.Avance
            order by
                case when @Orden = 'inicio' then p.FechaInicio end asc,
                case when @Orden = 'avance' then coalesce(av.Avance, 0) end desc,
                case when @Orden = 'estado' then p.Estado end asc,
                p.Nombre asc;";

        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();
        var filas = await conexion.QueryAsync<ProyectoListaItemFila>(sql, new { Buscar = buscar, Estado = estado, Orden = orden });

        return filas.Select(MapearFila).ToList();
    }

    public async Task<ProyectoMetricas> MetricasAsync()
    {
        const string sql = @"
            select
                count(*) as Total,
                coalesce(sum(case when Estado = 'Activo' then 1 else 0 end), 0) as Activos,
                coalesce(sum(case when Estado = 'Planificado' then 1 else 0 end), 0) as Planificados,
                coalesce(sum(case when Estado = 'Cerrado' then 1 else 0 end), 0) as Cerrados
            from Proyectos;";

        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();
        return await conexion.QuerySingleAsync<ProyectoMetricas>(sql);
    }

    public async Task<IReadOnlyList<UsuarioOpcion>> ListarUsuariosActivosAsync()
    {
        const string sql = @"
            select IdUsuario, concat(Nombre, ' ', Apellidos) as NombreCompleto
            from Usuarios
            where Activo = 1
            order by Nombre, Apellidos;";

        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();
        var filas = await conexion.QueryAsync<UsuarioOpcion>(sql);
        return filas.ToList();
    }

    public async Task<string?> ObtenerEstadoAsync(int idProyecto)
    {
        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();
        return await conexion.QueryFirstOrDefaultAsync<string>(
            "select Estado from Proyectos where IdProyecto = @IdProyecto;",
            new { IdProyecto = idProyecto });
    }

    private const string SqlMiembrosProyecto = @"
        select pm.IdUsuario, concat(u.Nombre, ' ', u.Apellidos) as NombreCompleto, pm.RolProyecto, pm.FechaIngreso
        from ProyectoMiembros pm
        join Usuarios u on u.IdUsuario = pm.IdUsuario
        where pm.IdProyecto = @IdProyecto
        order by case pm.RolProyecto
                     when 'Product Owner' then 1
                     when 'Scrum Master' then 2
                     when 'Developer' then 3
                     else 4
                 end, u.Nombre;";

    public async Task<IReadOnlyList<ProyectoMiembroItem>> ObtenerMiembrosAsync(int idProyecto)
    {
        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();
        var filas = await conexion.QueryAsync<ProyectoMiembroItem>(SqlMiembrosProyecto, new { IdProyecto = idProyecto });
        return filas.ToList();
    }

    public async Task<ProyectoDetalleViewModel?> ObtenerDetalleAsync(int idProyecto)
    {
        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();

        var proyecto = await conexion.QueryFirstOrDefaultAsync<Proyecto>(
            @"select IdProyecto, Codigo, Nombre, Descripcion, FechaInicio, FechaFin, Estado
              from Proyectos where IdProyecto = @IdProyecto;",
            new { IdProyecto = idProyecto });

        if (proyecto is null)
        {
            return null;
        }

        var miembros = (await conexion.QueryAsync<ProyectoMiembroItem>(SqlMiembrosProyecto, new { IdProyecto = idProyecto })).ToList();

        var sprints = (await conexion.QueryAsync<SprintResumenItem>(
            @"select IdSprint, Nombre, FechaInicio, FechaFin, Estado
              from Sprints where IdProyecto = @IdProyecto order by FechaInicio;",
            new { IdProyecto = idProyecto })).ToList();

        var avanceInfo = await conexion.QuerySingleAsync<AvanceInfo>(
            @"select
                  case when coalesce(sum(Estimacion), 0) = 0 then null
                       else sum(case when Estado = 'Terminada' then Estimacion else 0 end) * 100.0 / sum(Estimacion)
                  end as Avance,
                  count(*) as CantidadHistorias
              from HistoriasUsuario
              where IdProyecto = @IdProyecto;",
            new { IdProyecto = idProyecto });

        var historial = (await conexion.QueryAsync<AuditoriaItem>(
            @"select concat(u.Nombre, ' ', u.Apellidos) as Usuario, a.Accion, a.Detalle, a.Fecha
              from Auditoria a
              join Usuarios u on u.IdUsuario = a.IdUsuario
              where a.Entidad = 'Proyectos' and a.IdRegistro = @IdProyecto
              order by a.Fecha desc;",
            new { IdProyecto = idProyecto })).ToList();

        var usuariosActivos = await ListarUsuariosActivosAsync();
        var idsMiembros = miembros.Select(m => m.IdUsuario).ToHashSet();
        var disponibles = usuariosActivos.Where(u => !idsMiembros.Contains(u.IdUsuario)).ToList();

        return new ProyectoDetalleViewModel
        {
            IdProyecto = proyecto.IdProyecto,
            Codigo = proyecto.Codigo,
            Nombre = proyecto.Nombre,
            Descripcion = proyecto.Descripcion,
            FechaInicio = proyecto.FechaInicio,
            FechaFin = proyecto.FechaFin,
            Estado = proyecto.Estado,
            Avance = avanceInfo.Avance,
            CantidadHistorias = avanceInfo.CantidadHistorias,
            CantidadSprints = sprints.Count,
            CantidadMiembros = miembros.Count,
            Sprints = sprints,
            Miembros = miembros,
            Historial = historial,
            UsuariosDisponibles = disponibles
        };
    }

    public async Task AgregarMiembroAsync(int idProyecto, int idUsuario, string rolProyecto, int idUsuarioActual, string nombreUsuario)
    {
        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();
        using var transaccion = await conexion.BeginTransactionAsync();

        try
        {
            await conexion.ExecuteAsync(
                "insert into ProyectoMiembros (IdProyecto, IdUsuario, RolProyecto) values (@IdProyecto, @IdUsuario, @RolProyecto);",
                new { IdProyecto = idProyecto, IdUsuario = idUsuario, RolProyecto = rolProyecto }, transaccion);

            await RegistrarAuditoriaAsync(conexion, transaccion, idUsuarioActual, "Agregar Miembro", "Proyectos", idProyecto,
                $"Agregó a {nombreUsuario} como {rolProyecto}.");

            await transaccion.CommitAsync();
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public async Task CambiarRolMiembroAsync(int idProyecto, int idUsuario, string nuevoRol, int idUsuarioActual, string nombreUsuario, string rolAnterior)
    {
        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();
        using var transaccion = await conexion.BeginTransactionAsync();

        try
        {
            await conexion.ExecuteAsync(
                "update ProyectoMiembros set RolProyecto = @RolProyecto where IdProyecto = @IdProyecto and IdUsuario = @IdUsuario;",
                new { RolProyecto = nuevoRol, IdProyecto = idProyecto, IdUsuario = idUsuario }, transaccion);

            await RegistrarAuditoriaAsync(conexion, transaccion, idUsuarioActual, "Cambiar Rol Miembro", "Proyectos", idProyecto,
                $"Cambió el rol de {nombreUsuario} de {rolAnterior} a {nuevoRol}.");

            await transaccion.CommitAsync();
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public async Task QuitarMiembroAsync(int idProyecto, int idUsuario, int idUsuarioActual, string nombreUsuario)
    {
        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();
        using var transaccion = await conexion.BeginTransactionAsync();

        try
        {
            await conexion.ExecuteAsync(
                "delete from ProyectoMiembros where IdProyecto = @IdProyecto and IdUsuario = @IdUsuario;",
                new { IdProyecto = idProyecto, IdUsuario = idUsuario }, transaccion);

            await RegistrarAuditoriaAsync(conexion, transaccion, idUsuarioActual, "Quitar Miembro", "Proyectos", idProyecto,
                $"Quitó a {nombreUsuario} del proyecto.");

            await transaccion.CommitAsync();
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    private class AvanceInfo
    {
        public decimal? Avance { get; set; }
        public int CantidadHistorias { get; set; }
    }

    private class SprintsResumen
    {
        public int Total { get; set; }
        public int Cerrados { get; set; }
    }

    public async Task<ProyectoCerrarViewModel?> ObtenerResumenCierreAsync(int idProyecto)
    {
        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();

        var proyecto = await conexion.QueryFirstOrDefaultAsync<Proyecto>(
            @"select IdProyecto, Codigo, Nombre, Descripcion, FechaInicio, FechaFin, Estado
              from Proyectos where IdProyecto = @IdProyecto;",
            new { IdProyecto = idProyecto });

        if (proyecto is null)
        {
            return null;
        }

        var sprints = await conexion.QuerySingleAsync<SprintsResumen>(
            @"select count(*) as Total, coalesce(sum(case when Estado = 'Cerrado' then 1 else 0 end), 0) as Cerrados
              from Sprints where IdProyecto = @IdProyecto;",
            new { IdProyecto = idProyecto });

        var sprintActivo = await conexion.QueryFirstOrDefaultAsync<string>(
            "select Nombre from Sprints where IdProyecto = @IdProyecto and Estado = 'Activo' limit 1;",
            new { IdProyecto = idProyecto });

        var historiasPendientes = await conexion.ExecuteScalarAsync<int>(
            "select count(*) from HistoriasUsuario where IdProyecto = @IdProyecto and Estado <> 'Terminada';",
            new { IdProyecto = idProyecto });

        var impedimentosAbiertos = await conexion.ExecuteScalarAsync<int>(
            @"select count(*) from Impedimentos i
              join Sprints s on s.IdSprint = i.IdSprint
              where s.IdProyecto = @IdProyecto and i.Estado <> 'Resuelto';",
            new { IdProyecto = idProyecto });

        var cantidadMiembros = await conexion.ExecuteScalarAsync<int>(
            "select count(*) from ProyectoMiembros where IdProyecto = @IdProyecto;",
            new { IdProyecto = idProyecto });

        return new ProyectoCerrarViewModel
        {
            IdProyecto = proyecto.IdProyecto,
            Codigo = proyecto.Codigo,
            Nombre = proyecto.Nombre,
            Estado = proyecto.Estado,
            FechaInicio = proyecto.FechaInicio,
            FechaFin = proyecto.FechaFin,
            CantidadSprints = sprints.Total,
            SprintsCerrados = sprints.Cerrados,
            SprintActivoNombre = sprintActivo,
            HistoriasPendientes = historiasPendientes,
            ImpedimentosAbiertos = impedimentosAbiertos,
            CantidadMiembros = cantidadMiembros
        };
    }

    public async Task CerrarAsync(int idProyecto, string motivoCierre, int idUsuarioActual)
    {
        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();
        using var transaccion = await conexion.BeginTransactionAsync();

        try
        {
            await conexion.ExecuteAsync(
                @"update Proyectos
                  set Estado = 'Cerrado', FechaCierre = curdate(), MotivoCierre = @MotivoCierre
                  where IdProyecto = @IdProyecto;",
                new { MotivoCierre = motivoCierre, IdProyecto = idProyecto }, transaccion);

            await RegistrarAuditoriaAsync(conexion, transaccion, idUsuarioActual, "Cerrar", "Proyectos", idProyecto,
                $"Cerró el proyecto. Motivo: {motivoCierre}");

            await transaccion.CommitAsync();
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public async Task<ProyectoFormViewModel?> ObtenerParaEditarAsync(int idProyecto)
    {
        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();

        var proyecto = await conexion.QueryFirstOrDefaultAsync<Proyecto>(
            @"select IdProyecto, Codigo, Nombre, Descripcion, FechaInicio, FechaFin, Estado
              from Proyectos where IdProyecto = @IdProyecto;",
            new { IdProyecto = idProyecto });

        if (proyecto is null)
        {
            return null;
        }

        var miembros = (await conexion.QueryAsync<(int IdUsuario, string RolProyecto)>(
            "select IdUsuario, RolProyecto from ProyectoMiembros where IdProyecto = @IdProyecto;",
            new { IdProyecto = idProyecto })).ToList();

        return new ProyectoFormViewModel
        {
            IdProyecto = proyecto.IdProyecto,
            Codigo = proyecto.Codigo,
            Nombre = proyecto.Nombre,
            Descripcion = proyecto.Descripcion,
            FechaInicio = proyecto.FechaInicio,
            FechaFin = proyecto.FechaFin,
            Estado = proyecto.Estado,
            IdProductOwner = miembros.Where(m => m.RolProyecto == "Product Owner").Select(m => (int?)m.IdUsuario).FirstOrDefault(),
            IdScrumMaster = miembros.Where(m => m.RolProyecto == "Scrum Master").Select(m => (int?)m.IdUsuario).FirstOrDefault(),
            IdsDevelopers = miembros.Where(m => m.RolProyecto == "Developer").Select(m => m.IdUsuario).ToList()
        };
    }

    public async Task<int> CrearAsync(ProyectoFormViewModel modelo, int idUsuarioActual)
    {
        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();
        using var transaccion = await conexion.BeginTransactionAsync();

        try
        {
            var codigo = await GenerarSiguienteCodigoAsync(conexion, transaccion);

            await conexion.ExecuteAsync(
                @"insert into Proyectos (Codigo, Nombre, Descripcion, FechaInicio, FechaFin, Estado)
                  values (@Codigo, @Nombre, @Descripcion, @FechaInicio, @FechaFin, @Estado);",
                new { Codigo = codigo, modelo.Nombre, modelo.Descripcion, modelo.FechaInicio, modelo.FechaFin, modelo.Estado },
                transaccion);

            var idProyecto = await conexion.ExecuteScalarAsync<int>("select last_insert_id();", transaction: transaccion);

            await InsertarMiembrosAsync(conexion, transaccion, idProyecto, modelo);
            await RegistrarAuditoriaAsync(conexion, transaccion, idUsuarioActual, "Crear", "Proyectos", idProyecto,
                $"Creó el proyecto {codigo} - {modelo.Nombre}.");

            await transaccion.CommitAsync();
            return idProyecto;
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    public async Task ActualizarAsync(int idProyecto, ProyectoFormViewModel modelo, int idUsuarioActual)
    {
        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();
        using var transaccion = await conexion.BeginTransactionAsync();

        try
        {
            await conexion.ExecuteAsync(
                @"update Proyectos
                  set Nombre = @Nombre, Descripcion = @Descripcion, FechaInicio = @FechaInicio,
                      FechaFin = @FechaFin, Estado = @Estado
                  where IdProyecto = @IdProyecto;",
                new { modelo.Nombre, modelo.Descripcion, modelo.FechaInicio, modelo.FechaFin, modelo.Estado, IdProyecto = idProyecto },
                transaccion);

            var actuales = (await conexion.QueryAsync<(int IdUsuario, string RolProyecto)>(
                "select IdUsuario, RolProyecto from ProyectoMiembros where IdProyecto = @IdProyecto;",
                new { IdProyecto = idProyecto }, transaccion))
                .ToDictionary(m => m.IdUsuario, m => m.RolProyecto);

            var deseados = ConstruirMiembrosDeseados(modelo);

            foreach (var idUsuario in actuales.Keys.Except(deseados.Keys).ToList())
            {
                await conexion.ExecuteAsync(
                    "delete from ProyectoMiembros where IdProyecto = @IdProyecto and IdUsuario = @IdUsuario;",
                    new { IdProyecto = idProyecto, IdUsuario = idUsuario }, transaccion);
            }

            foreach (var par in deseados.Where(d => !actuales.ContainsKey(d.Key)))
            {
                await conexion.ExecuteAsync(
                    "insert into ProyectoMiembros (IdProyecto, IdUsuario, RolProyecto) values (@IdProyecto, @IdUsuario, @RolProyecto);",
                    new { IdProyecto = idProyecto, IdUsuario = par.Key, RolProyecto = par.Value }, transaccion);
            }

            foreach (var par in deseados.Where(d => actuales.ContainsKey(d.Key) && actuales[d.Key] != d.Value))
            {
                await conexion.ExecuteAsync(
                    "update ProyectoMiembros set RolProyecto = @RolProyecto where IdProyecto = @IdProyecto and IdUsuario = @IdUsuario;",
                    new { IdProyecto = idProyecto, IdUsuario = par.Key, RolProyecto = par.Value }, transaccion);
            }

            await RegistrarAuditoriaAsync(conexion, transaccion, idUsuarioActual, "Editar", "Proyectos", idProyecto,
                $"Actualizó el proyecto {modelo.Codigo} - {modelo.Nombre}.");

            await transaccion.CommitAsync();
        }
        catch
        {
            await transaccion.RollbackAsync();
            throw;
        }
    }

    private static Dictionary<int, string> ConstruirMiembrosDeseados(ProyectoFormViewModel modelo)
    {
        var deseados = new Dictionary<int, string>();

        if (modelo.IdProductOwner.HasValue)
        {
            deseados[modelo.IdProductOwner.Value] = "Product Owner";
        }

        if (modelo.IdScrumMaster.HasValue)
        {
            deseados[modelo.IdScrumMaster.Value] = "Scrum Master";
        }

        foreach (var idDeveloper in modelo.IdsDevelopers.Distinct())
        {
            deseados[idDeveloper] = "Developer";
        }

        return deseados;
    }

    private static async Task InsertarMiembrosAsync(MySqlConnection conexion, MySqlTransaction transaccion, int idProyecto, ProyectoFormViewModel modelo)
    {
        const string sql = @"
            insert into ProyectoMiembros (IdProyecto, IdUsuario, RolProyecto)
            values (@IdProyecto, @IdUsuario, @RolProyecto);";

        foreach (var par in ConstruirMiembrosDeseados(modelo))
        {
            await conexion.ExecuteAsync(sql, new { IdProyecto = idProyecto, IdUsuario = par.Key, RolProyecto = par.Value }, transaccion);
        }
    }

    private static async Task RegistrarAuditoriaAsync(MySqlConnection conexion, MySqlTransaction transaccion, int idUsuario, string accion, string entidad, int idRegistro, string detalle)
    {
        const string sql = @"
            insert into Auditoria (IdUsuario, Accion, Entidad, IdRegistro, Detalle)
            values (@IdUsuario, @Accion, @Entidad, @IdRegistro, @Detalle);";

        await conexion.ExecuteAsync(sql, new { IdUsuario = idUsuario, Accion = accion, Entidad = entidad, IdRegistro = idRegistro, Detalle = detalle }, transaccion);
    }

    private static async Task<string> GenerarSiguienteCodigoAsync(MySqlConnection conexion, MySqlTransaction transaccion)
    {
        const string sql = @"
            select coalesce(max(cast(substring(Codigo, 5) as unsigned)), 0) + 1
            from Proyectos
            where Codigo like 'PRY-%';";

        var siguienteNumero = await conexion.ExecuteScalarAsync<int>(sql, transaction: transaccion);
        return $"PRY-{siguienteNumero:000}";
    }

    private static ProyectoListaItem MapearFila(ProyectoListaItemFila fila)
    {
        return new ProyectoListaItem
        {
            IdProyecto = fila.IdProyecto,
            Codigo = fila.Codigo,
            Nombre = fila.Nombre,
            FechaInicio = fila.FechaInicio,
            FechaFin = fila.FechaFin,
            Estado = fila.Estado,
            ProductOwner = fila.ProductOwner,
            ScrumMaster = fila.ScrumMaster,
            CantidadMiembros = fila.CantidadMiembros,
            Miembros = MapearMiembrosCsv(fila.MiembrosCsv),
            Avance = fila.Avance
        };
    }

    /// <summary>Convierte el "Nombre Apellido:AB|Nombre Apellido:CD" de group_concat en una lista de MiembroAvatar.</summary>
    private static List<MiembroAvatar> MapearMiembrosCsv(string? miembrosCsv)
    {
        if (string.IsNullOrWhiteSpace(miembrosCsv))
        {
            return new List<MiembroAvatar>();
        }

        return miembrosCsv
            .Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(entrada =>
            {
                var partes = entrada.Split(':', 2);
                return new MiembroAvatar
                {
                    NombreCompleto = partes[0],
                    Iniciales = partes.Length > 1 ? partes[1] : string.Empty
                };
            })
            .ToList();
    }

    /// <summary>Fila cruda devuelta por Dapper para ListarAsync, antes de convertir MiembrosCsv en una lista.</summary>
    private class ProyectoListaItemFila
    {
        public int IdProyecto { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string Estado { get; set; } = string.Empty;
        public string? ProductOwner { get; set; }
        public string? ScrumMaster { get; set; }
        public int CantidadMiembros { get; set; }
        public string? MiembrosCsv { get; set; }
        public decimal? Avance { get; set; }
    }

    public async Task<IReadOnlyList<ProyectoClienteItem>> ListarDeMiembroAsync(int idUsuario)
    {
        const string sql = @"
            select
                p.IdProyecto,
                p.Codigo,
                p.Nombre,
                p.Descripcion,
                p.Estado,
                p.FechaInicio,
                p.FechaFin,
                yo.RolProyecto as MiRol,
                max(case when pm.RolProyecto = 'Product Owner' then concat(u.Nombre, ' ', u.Apellidos) end) as ProductOwner,
                max(case when pm.RolProyecto = 'Scrum Master' then concat(u.Nombre, ' ', u.Apellidos) end) as ScrumMaster,
                (select case when coalesce(sum(h.Estimacion), 0) = 0 then null
                             else sum(case when h.Estado = 'Terminada' then h.Estimacion else 0 end) * 100.0 / sum(h.Estimacion)
                        end
                 from HistoriasUsuario h where h.IdProyecto = p.IdProyecto) as Avance,
                (select s.Nombre from Sprints s where s.IdProyecto = p.IdProyecto and s.Estado = 'Activo' limit 1) as SprintActivoNombre,
                group_concat(concat(u.Nombre, ' ', u.Apellidos, ':', upper(left(u.Nombre, 1)), upper(left(u.Apellidos, 1))) order by pm.FechaIngreso separator '|') as MiembrosCsv
            from Proyectos p
            join ProyectoMiembros yo on yo.IdProyecto = p.IdProyecto and yo.IdUsuario = @IdUsuario
            left join ProyectoMiembros pm on pm.IdProyecto = p.IdProyecto
            left join Usuarios u on u.IdUsuario = pm.IdUsuario
            where p.Estado <> 'Cerrado'
            group by p.IdProyecto, p.Codigo, p.Nombre, p.Descripcion, p.Estado, p.FechaInicio, p.FechaFin, yo.RolProyecto
            order by p.FechaInicio desc;";

        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();
        var filas = await conexion.QueryAsync<ProyectoClienteItemFila>(sql, new { IdUsuario = idUsuario });

        return filas.Select(fila => new ProyectoClienteItem
        {
            IdProyecto = fila.IdProyecto,
            Codigo = fila.Codigo,
            Nombre = fila.Nombre,
            Descripcion = fila.Descripcion,
            Estado = fila.Estado,
            FechaInicio = fila.FechaInicio,
            FechaFin = fila.FechaFin,
            MiRol = fila.MiRol,
            ProductOwner = fila.ProductOwner,
            ScrumMaster = fila.ScrumMaster,
            Avance = fila.Avance,
            SprintActivoNombre = fila.SprintActivoNombre,
            Miembros = MapearMiembrosCsv(fila.MiembrosCsv)
        }).ToList();
    }

    public async Task<ProyectoClienteDetalleViewModel?> ObtenerDetalleClienteAsync(int idProyecto, int idUsuarioActual)
    {
        using var conexion = await _conexionDb.CrearConexionAbiertaAsync();

        var miRol = await conexion.QueryFirstOrDefaultAsync<string>(
            "select RolProyecto from ProyectoMiembros where IdProyecto = @IdProyecto and IdUsuario = @IdUsuarioActual;",
            new { IdProyecto = idProyecto, IdUsuarioActual = idUsuarioActual });

        if (miRol is null)
        {
            return null;
        }

        var proyecto = await conexion.QueryFirstOrDefaultAsync<Proyecto>(
            @"select IdProyecto, Codigo, Nombre, Descripcion, FechaInicio, FechaFin, Estado
              from Proyectos where IdProyecto = @IdProyecto;",
            new { IdProyecto = idProyecto });

        if (proyecto is null)
        {
            return null;
        }

        var miembros = (await conexion.QueryAsync<ProyectoMiembroItem>(SqlMiembrosProyecto, new { IdProyecto = idProyecto })).ToList();

        var sprints = (await conexion.QueryAsync<SprintResumenItem>(
            @"select IdSprint, Nombre, FechaInicio, FechaFin, Estado
              from Sprints where IdProyecto = @IdProyecto order by FechaInicio;",
            new { IdProyecto = idProyecto })).ToList();

        var avanceInfo = await conexion.QuerySingleAsync<AvanceInfo>(
            @"select
                  case when coalesce(sum(Estimacion), 0) = 0 then null
                       else sum(case when Estado = 'Terminada' then Estimacion else 0 end) * 100.0 / sum(Estimacion)
                  end as Avance,
                  count(*) as CantidadHistorias
              from HistoriasUsuario
              where IdProyecto = @IdProyecto;",
            new { IdProyecto = idProyecto });

        return new ProyectoClienteDetalleViewModel
        {
            IdProyecto = proyecto.IdProyecto,
            Codigo = proyecto.Codigo,
            Nombre = proyecto.Nombre,
            Descripcion = proyecto.Descripcion,
            FechaInicio = proyecto.FechaInicio,
            FechaFin = proyecto.FechaFin,
            Estado = proyecto.Estado,
            ProductOwner = miembros.Where(m => m.RolProyecto == "Product Owner").Select(m => m.NombreCompleto).FirstOrDefault(),
            ScrumMaster = miembros.Where(m => m.RolProyecto == "Scrum Master").Select(m => m.NombreCompleto).FirstOrDefault(),
            Avance = avanceInfo.Avance,
            MiRol = miRol,
            IdUsuarioActual = idUsuarioActual,
            Sprints = sprints,
            Miembros = miembros
        };
    }

    /// <summary>Fila cruda devuelta por Dapper para ListarDeMiembroAsync, antes de convertir MiembrosCsv en una lista.</summary>
    private class ProyectoClienteItemFila
    {
        public int IdProyecto { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Descripcion { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public DateTime FechaInicio { get; set; }
        public DateTime FechaFin { get; set; }
        public string MiRol { get; set; } = string.Empty;
        public string? ProductOwner { get; set; }
        public string? ScrumMaster { get; set; }
        public decimal? Avance { get; set; }
        public string? SprintActivoNombre { get; set; }
        public string? MiembrosCsv { get; set; }
    }
}
