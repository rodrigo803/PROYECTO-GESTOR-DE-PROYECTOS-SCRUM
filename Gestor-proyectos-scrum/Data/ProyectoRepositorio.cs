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
                group_concat(concat(upper(left(u.Nombre, 1)), upper(left(u.Apellidos, 1))) order by pm.FechaIngreso separator ',') as InicialesCsv,
                coalesce(av.Avance, 0) as Avance
            from Proyectos p
            left join ProyectoMiembros pm on pm.IdProyecto = p.IdProyecto
            left join Usuarios u on u.IdUsuario = pm.IdUsuario
            left join (
                select IdProyecto,
                       case when sum(Estimacion) is null or sum(Estimacion) = 0 then 0
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
            InicialesMiembros = string.IsNullOrWhiteSpace(fila.InicialesCsv)
                ? new List<string>()
                : fila.InicialesCsv.Split(',', StringSplitOptions.RemoveEmptyEntries).ToList(),
            Avance = fila.Avance
        };
    }

    /// <summary>Fila cruda devuelta por Dapper para ListarAsync, antes de convertir InicialesCsv en una lista.</summary>
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
        public string? InicialesCsv { get; set; }
        public decimal Avance { get; set; }
    }
}
