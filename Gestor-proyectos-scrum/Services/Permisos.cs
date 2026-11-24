namespace Gestor_proyectos_scrum.Services;

/// <summary>
/// Matriz de permisos por rol, en un solo lugar. La usan los sidebars (qué enlaces
/// mostrar), las vistas (qué botones mostrar) y los controllers (qué pueden abrir o
/// ejecutar), vía los "Roles = Permisos.X" de [Authorize] y los métodos de UsuarioActual.
/// Los campos son const string porque [Authorize(Roles = ...)] exige una constante de
/// compilación; son listas separadas por coma, el formato que ASP.NET Core espera ahí.
/// </summary>
public static class Permisos
{
    public const string Administrador = "Administrador";
    public const string ProductOwner = "Product Owner";
    public const string ScrumMaster = "Scrum Master";
    public const string Miembro = "Miembro del Equipo";

    /// <summary>Roles que usan la vista administrativa (_Layout). Miembro del Equipo usa solo _LayoutCliente.</summary>
    public const string VistaAdministrativa = Administrador + "," + ProductOwner + "," + ScrumMaster;
    public const string VistaCliente = Miembro;

    // Usuarios: solo Administrador (ver y gestionar).
    public const string UsuariosAcceso = Administrador;

    // Proyectos: Admin y PO gestionan; SM solo ve.
    public const string ProyectosVer = VistaAdministrativa;
    public const string ProyectosGestionar = Administrador + "," + ProductOwner;

    // Product Backlog: Admin y PO gestionan; SM solo ve.
    public const string BacklogVer = VistaAdministrativa;
    public const string BacklogGestionar = Administrador + "," + ProductOwner;

    // Sprints: Admin y SM gestionan; PO solo ve.
    public const string SprintsVer = VistaAdministrativa;
    public const string SprintsGestionar = Administrador + "," + ScrumMaster;

    // Planificación: Admin, PO y SM planifican (comprometer/quitar/regresar/replanificar);
    // aceptar/rechazar historias solo Admin y PO.
    public const string PlanificacionVer = VistaAdministrativa;
    public const string PlanificacionPlanificar = VistaAdministrativa;
    public const string PlanificacionAprobar = Administrador + "," + ProductOwner;

    // Tareas: Admin y SM gestionan y asignan; PO solo ve.
    public const string TareasVer = VistaAdministrativa;
    public const string TareasGestionar = Administrador + "," + ScrumMaster;

    // Tablero, Impedimentos, Retrospectiva, Dashboard: Admin, PO y SM (sin distinción ver/gestionar).
    // Los controllers de Tablero/Impedimentos/Retrospectiva son de Proyecto Final y todavía no existen.
    public const string TableroAcceso = VistaAdministrativa;
    public const string ImpedimentosAcceso = VistaAdministrativa;
    public const string RetrospectivasAcceso = VistaAdministrativa;
    public const string DashboardAcceso = VistaAdministrativa;

    /// <summary>Para los enlaces del sidebar: true si el rol es de la vista administrativa.</summary>
    public static bool EsVistaAdministrativa(string rol) => rol is Administrador or ProductOwner or ScrumMaster;

    /// <summary>Para los enlaces del sidebar: true si el rol es el de la vista cliente (Miembro del Equipo).</summary>
    public static bool EsVistaCliente(string rol) => rol == Miembro;

    public static bool TieneUnoDeLosRoles(string rol, string rolesSeparadosPorComa) =>
        rolesSeparadosPorComa.Split(',').Contains(rol);
}
