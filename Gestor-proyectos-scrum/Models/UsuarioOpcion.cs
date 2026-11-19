namespace Gestor_proyectos_scrum.Models;

/// <summary>Usuario activo para poblar los selects de Product Owner, Scrum Master y Developers.</summary>
public class UsuarioOpcion
{
    public int IdUsuario { get; set; }
    public string NombreCompleto { get; set; } = string.Empty;
}
