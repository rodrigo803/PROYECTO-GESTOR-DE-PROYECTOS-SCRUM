namespace Gestor_proyectos_scrum.Models;

/// <summary>ViewModel de diagnóstico para Home/EstadoBd (solo Development).</summary>
public class EstadoBdViewModel
{
    public bool Conectado { get; set; }
    public string? Error { get; set; }
    public int Tablas { get; set; }
    public int Vistas { get; set; }
    public int Triggers { get; set; }
    public int RegistrosProyectos { get; set; }
}
