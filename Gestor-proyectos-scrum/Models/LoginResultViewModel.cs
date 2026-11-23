namespace Gestor_proyectos_scrum.Models;

/// <summary>Datos del usuario que devuelve la Api al validar sus credenciales (HU-01).</summary>
public class LoginResultViewModel
{
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string RolNombre { get; set; } = string.Empty;

    public string NombreCompleto => $"{Nombre} {Apellidos}".Trim();
}
