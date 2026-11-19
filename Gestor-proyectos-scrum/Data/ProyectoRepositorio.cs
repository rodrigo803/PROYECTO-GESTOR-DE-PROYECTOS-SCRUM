using Dapper;
using Gestor_proyectos_scrum.Models;

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
