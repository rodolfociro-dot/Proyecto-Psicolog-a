using System.ComponentModel.DataAnnotations;

namespace Psicologia.Api.Models;

public class LoginDto
{
    [Required, EmailAddress] public string Email { get; set; } = "";
    [Required] public string Contrasena { get; set; } = "";
}

public class PrimerAdminDto
{
    [Required, MaxLength(100)] public string Nombres { get; set; } = "";
    [Required, MaxLength(100)] public string Apellidos { get; set; } = "";
    [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = "";
    [Required, MinLength(8)] public string Contrasena { get; set; } = "";
}

public class UsuarioCrearDto
{
    [Required, MaxLength(100)] public string Nombres { get; set; } = "";
    [Required, MaxLength(100)] public string Apellidos { get; set; } = "";
    [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = "";
    [Required, MinLength(8)] public string Contrasena { get; set; } = "";
    public int IdRol { get; set; }
    [MaxLength(20)] public string? Telefono { get; set; }
    [MaxLength(100)] public string? Especialidad { get; set; }
    [MaxLength(50)] public string? RegistroProfesional { get; set; }
    public decimal? ValorSesion { get; set; }
}

public class UsuarioActualizarDto
{
    [Required, MaxLength(100)] public string Nombres { get; set; } = "";
    [Required, MaxLength(100)] public string Apellidos { get; set; } = "";
    [Required, EmailAddress, MaxLength(150)] public string Email { get; set; } = "";
    public int IdRol { get; set; }
    [MaxLength(20)] public string? Telefono { get; set; }
    [MaxLength(100)] public string? Especialidad { get; set; }
    [MaxLength(50)] public string? RegistroProfesional { get; set; }
    public decimal? ValorSesion { get; set; }
    public bool Activo { get; set; } = true;
}

public class ContrasenaDto
{
    [Required, MinLength(8)] public string NuevaContrasena { get; set; } = "";
}