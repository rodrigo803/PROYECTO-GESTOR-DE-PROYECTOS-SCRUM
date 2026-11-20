namespace Gestor_proyectos_scrum.Services;

/// <summary>
/// Usuario autenticado de la sesión actual.
/// Mientras no exista el login de HU-01 (Jesús), devuelve un usuario fijo de
/// desarrollo. Jesús debe reemplazar esta implementación por la del usuario
/// real obtenido de la sesión cuando el login esté listo.
/// </summary>
public class UsuarioActual
{
    public int IdUsuario => 2;

    public string Nombre => "Valeria Castro";
}
