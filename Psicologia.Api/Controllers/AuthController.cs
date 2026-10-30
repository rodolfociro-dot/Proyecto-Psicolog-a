using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Psicologia.Api.Data;
using Psicologia.Api.Models;
using Psicologia.Api.Services;

namespace Psicologia.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly PsicologiaContext _db;
    private readonly TokenService _tokens;

    public AuthController(PsicologiaContext db, TokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    // POST api/auth/primer-administrador
    // Solo funciona UNA vez: cuando todavía no existe ningún usuario.
    [HttpPost("primer-administrador")]
    public async Task<IActionResult> PrimerAdministrador(PrimerAdminDto dto)
    {
        if (await _db.Usuarios.AnyAsync())
            return Conflict("Ya existe al menos un usuario. Esta opción solo sirve para crear el primero.");

        var rol = await _db.Roles.FirstOrDefaultAsync(r => r.Nombre == "Administrador");
        if (rol is null)
            return Problem("No existe el rol 'Administrador'. Revisa que el script de la base de datos se ejecutó completo.");

        var usuario = new Usuario
        {
            IdRol = rol.IdRol,
            Nombres = dto.Nombres.Trim(),
            Apellidos = dto.Apellidos.Trim(),
            Email = dto.Email.Trim().ToLowerInvariant(),
            ContrasenaHash = PasswordHasher.Hash(dto.Contrasena)
        };
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        return Created($"/api/Usuarios/{usuario.IdUsuario}",
            new { usuario.IdUsuario, usuario.Email, Rol = rol.Nombre });
    }

    // POST api/auth/login
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        var usuario = await _db.Usuarios.Include(u => u.Rol)
            .FirstOrDefaultAsync(u => u.Email == email);

        // Mismo mensaje en todos los casos: no revelamos si el correo existe
        if (usuario is null || !usuario.Activo || !PasswordHasher.Verificar(dto.Contrasena, usuario.ContrasenaHash))
            return Unauthorized("Correo o contraseña incorrectos.");

        var (token, expira) = _tokens.Crear(usuario, usuario.Rol!.Nombre);

        _db.Accesos.Add(new RegistroAcceso
        {
            IdUsuario = usuario.IdUsuario,
            Accion = "Login",
            Entidad = "Usuario",
            IdEntidad = usuario.IdUsuario
        });
        await _db.SaveChangesAsync();

        return Ok(new
        {
            token,
            expira,
            usuario = new
            {
                usuario.IdUsuario,
                Nombre = $"{usuario.Nombres} {usuario.Apellidos}",
                usuario.Email,
                Rol = usuario.Rol.Nombre
            }
        });
    }

    // GET api/auth/yo  (requiere token)
    [Authorize]
    [HttpGet("yo")]
    public IActionResult Yo()
    {
        return Ok(new
        {
            IdUsuario = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            Nombre = User.FindFirst(ClaimTypes.Name)?.Value,
            Email = User.FindFirst(ClaimTypes.Email)?.Value,
            Rol = User.FindFirst(ClaimTypes.Role)?.Value
        });
    }
}
