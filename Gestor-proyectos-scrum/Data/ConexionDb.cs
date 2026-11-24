using MySqlConnector;

namespace Gestor_proyectos_scrum.Data;

/// <summary>
/// Entrega conexiones MySQL abiertas a partir de la cadena "ScrumDb"
/// (appsettings.Development.json en local / appsettings.Production.json en Plesk).
/// </summary>
public class ConexionDb
{
    private readonly string _cadenaConexion;

    public ConexionDb(IConfiguration configuracion)
    {
        _cadenaConexion = configuracion.GetConnectionString("ScrumDb")
            ?? throw new InvalidOperationException("No se encontró la cadena de conexión 'ScrumDb' en la configuración.");
    }

    public async Task<MySqlConnection> CrearConexionAbiertaAsync()
    {
        var conexion = new MySqlConnection(_cadenaConexion);
        await conexion.OpenAsync();
        return conexion;
    }
}
