using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Psicologia.Api.Data;
using Psicologia.Api.Models;
using Psicologia.Api.Services;

namespace Psicologia.Api.Controllers;

// Solo el Administrador gestiona a los usuarios del centro
[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public class UsuariosController : ControllerBase
{
    private readonly PsicologiaContext _db;
    public UsuariosController(PsicologiaContext db) => _db = db;

    private int IdActual() => int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)!.Value);

    // GET api/usuarios?rol=Profesional
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? rol)
    {
        var q = _db.Usuarios.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(rol))
            q = q.Where(u => u.Rol!.Nombre == rol);

        var lista = await q.OrderBy(u => u.Nombres).ThenBy(u => u.Apellidos)
            .Select(u => new
            {
                u.IdUsuario,
                u.Nombres,
                u.Apellidos,
                u.Email,
                u.IdRol,
                Rol = u.Rol!.Nombre,
                u.Telefono,
                u.Especialidad,
                u.RegistroProfesional,
                u.ValorSesion,
                u.Activo
            })
            .ToListAsync();
        return Ok(lista);
    }

    // GET api/usuarios/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        var u = await _db.Usuarios.AsNoTracking()
            .Where(x => x.IdUsuario == id)
            .Select(x => new
            {
                x.IdUsuario,
                x.Nombres,
                x.Apellidos,
                x.Email,
                x.IdRol,
                Rol = x.Rol!.Nombre,
                x.Telefono,
                x.Especialidad,
                x.RegistroProfesional,
                x.ValorSesion,
                x.Activo
            })
            .FirstOrDefaultAsync();
        return u is null ? NotFound() : Ok(u);
    }

    // POST api/usuarios
    [HttpPost]
    public async Task<IActionResult> Crear(UsuarioCrearDto dto)
    {
        var rol = await _db.Roles.FindAsync(dto.IdRol);
        if (rol is null) return BadRequest("El rol indicado no existe.");

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Usuarios.AnyAsync(u => u.Email == email))
            return Conflict("Ya existe un usuario con ese correo.");

        var usuario = new Usuario
        {
            IdRol = rol.IdRol,
            Nombres = dto.Nombres.Trim(),
            Apellidos = dto.Apellidos.Trim(),
            Email = email,
            ContrasenaHash = PasswordHasher.Hash(dto.Contrasena),
            Telefono = dto.Telefono,
            Especialidad = dto.Especialidad,
            RegistroProfesional = dto.RegistroProfesional,
            ValorSesion = dto.ValorSesion
        };
        _db.Usuarios.Add(usuario);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Obtener), new { id = usuario.IdUsuario },
            new { usuario.IdUsuario, usuario.Email, Rol = rol.Nombre });
    }

    // PUT api/usuarios/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, UsuarioActualizarDto dto)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario is null) return NotFound();

        if (await _db.Roles.FindAsync(dto.IdRol) is null)
            return BadRequest("El rol indicado no existe.");

        // Evita que el administrador se bloquee a sí mismo
        if (id == IdActual() && (!dto.Activo || dto.IdRol != usuario.IdRol))
            return BadRequest("No puedes desactivar tu propia cuenta ni cambiar tu propio rol.");

        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _db.Usuarios.AnyAsync(u => u.Email == email && u.IdUsuario != id))
            return Conflict("Ya existe otro usuario con ese correo.");

        usuario.Nombres = dto.Nombres.Trim();
        usuario.Apellidos = dto.Apellidos.Trim();
        usuario.Email = email;
        usuario.IdRol = dto.IdRol;
        usuario.Telefono = dto.Telefono;
        usuario.Especialidad = dto.Especialidad;
        usuario.RegistroProfesional = dto.RegistroProfesional;
        usuario.ValorSesion = dto.ValorSesion;
        usuario.Activo = dto.Activo;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST api/usuarios/5/restablecer-contrasena
    [HttpPost("{id:int}/restablecer-contrasena")]
    public async Task<IActionResult> RestablecerContrasena(int id, ContrasenaDto dto)
    {
        var usuario = await _db.Usuarios.FindAsync(id);
        if (usuario is null) return NotFound();

        usuario.ContrasenaHash = PasswordHasher.Hash(dto.NuevaContrasena);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}