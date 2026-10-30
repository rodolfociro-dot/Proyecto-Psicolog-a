using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Psicologia.Api.Data;
using Psicologia.Api.Models;
using Psicologia.Api.Services;

namespace Psicologia.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ConsultantesController : ControllerBase
{
    private readonly PsicologiaContext _db;
    public ConsultantesController(PsicologiaContext db) => _db = db;

    private bool EsProfesional => User.IsInRole("Profesional");

    private void Auditar(string accion, int idConsultante)
        => _db.Accesos.Add(new RegistroAcceso
        {
            IdUsuario = User.IdUsuario(),
            Accion = accion,
            Entidad = "Consultante",
            IdEntidad = idConsultante
        });

    private Task<bool> ProfesionalValido(int idProfesional)
        => _db.Usuarios.AnyAsync(u => u.IdUsuario == idProfesional && u.Activo && u.Rol!.Nombre == "Profesional");

    // GET api/consultantes?buscar=ana&activo=true&idProfesional=2
    [HttpGet]
    public async Task<IActionResult> Listar([FromQuery] string? buscar, [FromQuery] bool? activo, [FromQuery] int? idProfesional)
    {
        var q = _db.Consultantes.AsNoTracking().AsQueryable();

        if (EsProfesional)
        {
            var yo = User.IdUsuario();
            q = q.Where(c => c.IdProfesional == yo);          // solo los suyos
        }
        else if (idProfesional.HasValue)
        {
            q = q.Where(c => c.IdProfesional == idProfesional.Value);
        }

        if (activo.HasValue)
            q = q.Where(c => c.Activo == activo.Value);

        if (!string.IsNullOrWhiteSpace(buscar))
            q = q.Where(c => c.Nombres.Contains(buscar) || c.Apellidos.Contains(buscar) || c.Documento.Contains(buscar));

        var lista = await q.OrderBy(c => c.Apellidos).ThenBy(c => c.Nombres)
            .Select(c => new
            {
                c.IdConsultante,
                c.TipoDocumento,
                c.Documento,
                c.Nombres,
                c.Apellidos,
                c.Telefono,
                c.Email,
                c.IdProfesional,
                Profesional = c.Profesional!.Nombres + " " + c.Profesional.Apellidos,
                c.Activo,
                c.FechaRegistro
            })
            .ToListAsync();
        return Ok(lista);
    }

    // GET api/consultantes/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        var esProf = EsProfesional;
        var yo = User.IdUsuario();

        var c = await _db.Consultantes.AsNoTracking()
            .Where(x => x.IdConsultante == id && (!esProf || x.IdProfesional == yo))
            .Select(x => new
            {
                x.IdConsultante,
                x.TipoDocumento,
                x.Documento,
                x.Nombres,
                x.Apellidos,
                x.FechaNacimiento,
                x.Telefono,
                x.Email,
                x.ContactoEmergenciaNombre,
                x.ContactoEmergenciaTelefono,
                MotivoConsulta = esProf ? x.MotivoConsulta : null,   // dato clínico: solo el profesional
                x.IdProfesional,
                Profesional = x.Profesional!.Nombres + " " + x.Profesional.Apellidos,
                x.Activo,
                x.FechaRegistro
            })
            .FirstOrDefaultAsync();

        // Si es de otro profesional, responde 404 (no revela que existe)
        if (c is null) return NotFound();

        Auditar("Ver", id);
        await _db.SaveChangesAsync();
        return Ok(c);
    }

    // POST api/consultantes
    [HttpPost]
    public async Task<IActionResult> Crear(ConsultanteDto dto)
    {
        int idProfesional;
        if (EsProfesional)
        {
            idProfesional = User.IdUsuario();
        }
        else
        {
            if (dto.IdProfesional is null)
                return BadRequest("Indica el profesional que atenderá al consultante (idProfesional).");
            idProfesional = dto.IdProfesional.Value;
        }

        if (!await ProfesionalValido(idProfesional))
            return BadRequest("El profesional indicado no existe o está inactivo.");

        var tipo = dto.TipoDocumento.Trim().ToUpperInvariant();
        var documento = dto.Documento.Trim();
        if (await _db.Consultantes.AnyAsync(c => c.TipoDocumento == tipo && c.Documento == documento))
            return Conflict("Ya existe un consultante con ese documento.");

        var consultante = new Consultante
        {
            TipoDocumento = tipo,
            Documento = documento,
            Nombres = dto.Nombres.Trim(),
            Apellidos = dto.Apellidos.Trim(),
            FechaNacimiento = dto.FechaNacimiento,
            Telefono = dto.Telefono,
            Email = dto.Email,
            ContactoEmergenciaNombre = dto.ContactoEmergenciaNombre,
            ContactoEmergenciaTelefono = dto.ContactoEmergenciaTelefono,
            MotivoConsulta = EsProfesional ? dto.MotivoConsulta : null,
            IdProfesional = idProfesional
        };
        _db.Consultantes.Add(consultante);
        await _db.SaveChangesAsync();

        Auditar("Crear", consultante.IdConsultante);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Obtener), new { id = consultante.IdConsultante },
            new { consultante.IdConsultante, consultante.Nombres, consultante.Apellidos, consultante.IdProfesional });
    }

    // PUT api/consultantes/5
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, ConsultanteDto dto)
    {
        var c = await _db.Consultantes.FindAsync(id);
        if (c is null || (EsProfesional && c.IdProfesional != User.IdUsuario()))
            return NotFound();

        var tipo = dto.TipoDocumento.Trim().ToUpperInvariant();
        var documento = dto.Documento.Trim();
        if (await _db.Consultantes.AnyAsync(x => x.TipoDocumento == tipo && x.Documento == documento && x.IdConsultante != id))
            return Conflict("Ya existe otro consultante con ese documento.");

        // Solo Administrador y Asistente pueden pasar al consultante a otro profesional
        if (!EsProfesional && dto.IdProfesional.HasValue && dto.IdProfesional.Value != c.IdProfesional)
        {
            if (!await ProfesionalValido(dto.IdProfesional.Value))
                return BadRequest("El profesional indicado no existe o está inactivo.");
            c.IdProfesional = dto.IdProfesional.Value;
        }

        c.TipoDocumento = tipo;
        c.Documento = documento;
        c.Nombres = dto.Nombres.Trim();
        c.Apellidos = dto.Apellidos.Trim();
        c.FechaNacimiento = dto.FechaNacimiento;
        c.Telefono = dto.Telefono;
        c.Email = dto.Email;
        c.ContactoEmergenciaNombre = dto.ContactoEmergenciaNombre;
        c.ContactoEmergenciaTelefono = dto.ContactoEmergenciaTelefono;
        c.Activo = dto.Activo;

        // El motivo de consulta solo lo modifica el profesional; los demás roles no lo tocan
        if (EsProfesional) c.MotivoConsulta = dto.MotivoConsulta;

        Auditar("Modificar", id);
        await _db.SaveChangesAsync();
        return NoContent();
    }
}