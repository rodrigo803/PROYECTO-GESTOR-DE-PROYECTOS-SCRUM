namespace Gestor_proyectos_scrum.Api.Models;

// DTOs de HU-06 (Sprint Backlog / Planificación). Nombres de propiedades iguales a
// Gestor_proyectos_scrum.Models para que ScrumApiClient deserialice directo en ellos.

public class ProyectoOpcionDto
{
    public int IdProyecto { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}

public class SprintOpcionDto
{
    public int IdSprint { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Estado { get; set; } = string.Empty;
}

public class SprintPlanificacionInfoDto
{
    public int IdSprint { get; set; }
    public int IdProyecto { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Objetivo { get; set; } = string.Empty;
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Estado { get; set; } = string.Empty;
    public int CapacidadPuntos { get; set; }
}

public class HistoriaCandidataItemDto
{
    public int IdHistoria { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Como { get; set; } = string.Empty;
    public string Quiero { get; set; } = string.Empty;
    public string Para { get; set; } = string.Empty;
    public string Prioridad { get; set; } = string.Empty;
    public int OrdenPrioridad { get; set; }
    public int? Estimacion { get; set; }
    public int CantidadCriterios { get; set; }
    public List<string> Motivos { get; set; } = new();
}

public class HistoriaComprometidaItemDto
{
    public int IdHistoria { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Quiero { get; set; } = string.Empty;
    public int? Estimacion { get; set; }
    public string Resultado { get; set; } = string.Empty;
    public int CantidadTareas { get; set; }
}

public class HistoriaNoTerminadaItemDto
{
    public int IdSprintHistoria { get; set; }
    public int IdHistoria { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Quiero { get; set; } = string.Empty;
    public int? Estimacion { get; set; }
    public string NombreSprint { get; set; } = string.Empty;
}

public class HistoriaComprometidaClienteItemDto
{
    public int IdHistoria { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Quiero { get; set; } = string.Empty;
    public int? Estimacion { get; set; }
    public string Prioridad { get; set; } = string.Empty;
    public int CantidadTareas { get; set; }
    public string EstadoAprobacion { get; set; } = string.Empty;
}

public class ComprometerRequest
{
    public List<int> IdsHistorias { get; set; } = new();
}

public class RechazarHistoriaRequest
{
    public string Comentario { get; set; } = string.Empty;
}

public class ReplanificarRequest
{
    public int IdSprintDestino { get; set; }
}
