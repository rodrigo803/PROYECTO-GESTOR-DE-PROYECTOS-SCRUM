namespace Gestor_proyectos_scrum.Models;

/// <summary>Iniciales + nombre completo de un miembro, para los avatares apilados de las tarjetas/listados.</summary>
public class MiembroAvatar
{
    public string NombreCompleto { get; set; } = string.Empty;
    public string Iniciales { get; set; } = string.Empty;
}
