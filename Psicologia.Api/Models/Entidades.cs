using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Psicologia.Api.Models;

[Table("Rol")]
public class Rol
{
    [Key] public int IdRol { get; set; }
    [Required, MaxLength(30)] public string Nombre { get; set; } = "";
}

[Table("Usuario")]
public class Usuario
{
    [Key] public int IdUsuario { get; set; }
    public int IdRol { get; set; }
    public Rol? Rol { get; set; }
    [Required, MaxLength(100)] public string Nombres { get; set; } = "";
    [Required, MaxLength(100)] public string Apellidos { get; set; } = "";
    [Required, MaxLength(150)] public string Email { get; set; } = "";

    // Nunca se devuelve en las respuestas de la API
    [JsonIgnore, MaxLength(255)] public string ContrasenaHash { get; set; } = "";

    [MaxLength(20)] public string? Telefono { get; set; }
    [MaxLength(100)] public string? Especialidad { get; set; }
    [MaxLength(50)] public string? RegistroProfesional { get; set; }
    public decimal? ValorSesion { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
}

[Table("Consultante")]
public class Consultante
{
    [Key] public int IdConsultante { get; set; }
    [Required, MaxLength(10)] public string TipoDocumento { get; set; } = "";
    [Required, MaxLength(20)] public string Documento { get; set; } = "";
    [Required, MaxLength(100)] public string Nombres { get; set; } = "";
    [Required, MaxLength(100)] public string Apellidos { get; set; } = "";
    [Column(TypeName = "date")] public DateTime? FechaNacimiento { get; set; }
    [MaxLength(20)] public string? Telefono { get; set; }
    [MaxLength(150)] public string? Email { get; set; }
    [MaxLength(100)] public string? ContactoEmergenciaNombre { get; set; }
    [MaxLength(20)] public string? ContactoEmergenciaTelefono { get; set; }
    [MaxLength(500)] public string? MotivoConsulta { get; set; }
    public int IdProfesional { get; set; }
    public Usuario? Profesional { get; set; }
    public bool Activo { get; set; } = true;
    public DateTime FechaRegistro { get; set; } = DateTime.Now;
}

[Table("Cita")]
public class Cita
{
    [Key] public int IdCita { get; set; }
    public int IdConsultante { get; set; }
    public Consultante? Consultante { get; set; }
    public int IdProfesional { get; set; }
    public Usuario? Profesional { get; set; }
    public DateTime FechaInicio { get; set; }
    public DateTime FechaFin { get; set; }
    [MaxLength(15)] public string Modalidad { get; set; } = "Presencial";
    [MaxLength(15)] public string Estado { get; set; } = "Programada";
    public decimal Valor { get; set; }
    [MaxLength(300)] public string? EnlaceVirtual { get; set; }
    [MaxLength(300)] public string? NotaAdministrativa { get; set; }
    public int IdCreadaPor { get; set; }
    public Usuario? CreadaPor { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
}

[Table("NotaSesion")]
public class NotaSesion
{
    [Key] public int IdNota { get; set; }
    public int IdCita { get; set; }
    public Cita? Cita { get; set; }
    public int IdConsultante { get; set; }
    public Consultante? Consultante { get; set; }
    public int IdProfesional { get; set; }
    public Usuario? Profesional { get; set; }
    [MaxLength(500)] public string? Motivo { get; set; }
    public string? Desarrollo { get; set; }
    public string? PlanSeguimiento { get; set; }
    public DateTime FechaCreacion { get; set; } = DateTime.Now;
    public DateTime? FechaModificacion { get; set; }
}

[Table("Pago")]
public class Pago
{
    [Key] public int IdPago { get; set; }
    public int IdConsultante { get; set; }
    public Consultante? Consultante { get; set; }
    public int? IdCita { get; set; }
    public Cita? Cita { get; set; }
    public decimal Monto { get; set; }
    [Required, MaxLength(20)] public string MetodoPago { get; set; } = "";
    public DateTime FechaPago { get; set; } = DateTime.Now;
    [MaxLength(100)] public string? Referencia { get; set; }
    public int IdRegistradoPor { get; set; }
    public Usuario? RegistradoPor { get; set; }
}

[Table("RegistroAcceso")]
public class RegistroAcceso
{
    [Key] public long IdRegistro { get; set; }
    public int IdUsuario { get; set; }
    public Usuario? Usuario { get; set; }
    [Required, MaxLength(30)] public string Accion { get; set; } = "";
    [Required, MaxLength(30)] public string Entidad { get; set; } = "";
    public int? IdEntidad { get; set; }
    public DateTime FechaHora { get; set; } = DateTime.Now;
}