using System.ComponentModel.DataAnnotations;

namespace Psicologia.Api.Models;

public class ConsultanteDto
{
    [Required, MaxLength(10)] public string TipoDocumento { get; set; } = "CC";
    [Required, MaxLength(20)] public string Documento { get; set; } = "";
    [Required, MaxLength(100)] public string Nombres { get; set; } = "";
    [Required, MaxLength(100)] public string Apellidos { get; set; } = "";
    public DateTime? FechaNacimiento { get; set; }
    [MaxLength(20)] public string? Telefono { get; set; }
    [EmailAddress, MaxLength(150)] public string? Email { get; set; }
    [MaxLength(100)] public string? ContactoEmergenciaNombre { get; set; }
    [MaxLength(20)] public string? ContactoEmergenciaTelefono { get; set; }

    // Dato clínico: solo el profesional lo escribe y lo ve
    [MaxLength(500)] public string? MotivoConsulta { get; set; }

    // Obligatorio cuando quien crea es Administrador o Asistente.
    // Si quien crea es un Profesional, se ignora: queda asignado a él.
    public int? IdProfesional { get; set; }

    public bool Activo { get; set; } = true;
}

public class CitaCrearDto
{
    public int IdConsultante { get; set; }
    public int? IdProfesional { get; set; }
    public DateTime FechaInicio { get; set; }
    [Range(15, 240)] public int DuracionMinutos { get; set; } = 60;
    [MaxLength(15)] public string Modalidad { get; set; } = "Presencial";
    [Range(0, 1000000000)] public decimal? Valor { get; set; }
    [MaxLength(300)] public string? EnlaceVirtual { get; set; }
    [MaxLength(300)] public string? NotaAdministrativa { get; set; }
}

public class CitaActualizarDto
{
    public DateTime FechaInicio { get; set; }
    [Range(15, 240)] public int DuracionMinutos { get; set; } = 60;
    [MaxLength(15)] public string Modalidad { get; set; } = "Presencial";
    [Range(0, 1000000000)] public decimal Valor { get; set; }
    [MaxLength(300)] public string? EnlaceVirtual { get; set; }
    [MaxLength(300)] public string? NotaAdministrativa { get; set; }
}

public class CitaEstadoDto
{
    [Required] public string Estado { get; set; } = "";
}

// Forma en que la API devuelve una cita
public class CitaVista
{
    public int IdCita { get; set; }
    public int IdConsultante { get; set; }
    public string Consultante { get; set; } = "";
    public int IdProfesional { get; set; }
    public string Profesional { get; set; } = "";
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    public string Modalidad { get; set; } = "";
    public string Estado { get; set; } = "";
    public decimal Valor { get; set; }
    public string? EnlaceVirtual { get; set; }
    public string? NotaAdministrativa { get; set; }
}