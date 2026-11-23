namespace Gestor_proyectos_scrum.Api.Models;

// =========================================================================
// HU-01: ACCESO (LOGIN)
// =========================================================================

public class LoginRequest
{
    public string Correo { get; set; } = string.Empty;
    public string Contrasena { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public int IdUsuario { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public string Correo { get; set; } = string.Empty;
    public string RolNombre { get; set; } = string.Empty;
}
