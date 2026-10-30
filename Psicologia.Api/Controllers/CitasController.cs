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
public class CitasController : ControllerBase
{
    private static readonly string[] Estados = { "Programada", "Atendida", "Cancelada", "Inasistencia" };
    private static readonly string[] Modalidades = { "Presencial", "Virtual" };

    private readonly PsicologiaContext _db;
    public CitasController(PsicologiaContext db) => _db = db;

    private bool EsProfesional => User.IsInRole("Profesional");

    // ¿El profesional ya tiene otra cita (no cancelada) que se cruce con este horario?
    private Task<bool> HayCruce(int idProfesional, DateTime inicio, DateTime fin, int? ignorarCita)
        => _db.Citas.AnyAsync(c => c.IdProfesional == idProfesional
                                   && c.Estado != "Cancelada"
                                   && c.FechaInicio < fin
                                   && c.FechaFin > inicio
                                   && (ignorarCita == null || c.IdCita != ignorarCita));

    private static IQueryable<CitaVista> Proyectar(IQueryable<Cita> q)
        => q.Select(c => new CitaVista
        {
            IdCita = c.IdCita,
            IdConsultante = c.IdConsultante,
            Consultante = c.Consultante!.Nombres + " " + c.Consultante.Apellidos,
            IdProfesional = c.IdProfesional,
            Profesional = c.Profesional!.Nombres + " " + c.Profesional.Apellidos,
            FechaInicio = c.FechaInicio,
            FechaFin = c.FechaFin,
            Modalidad = c.Modalidad,
            Estado = c.Estado,
            Valor = c.Valor,
            EnlaceVirtual = c.EnlaceVirtual,
            NotaAdministrativa = c.NotaAdministrativa
        });

    // GET api/citas?desde=2026-11-02&hasta=2026-11-08&idProfesional=2&idConsultante=1&estado=Programada
    // Sin fechas: muestra los próximos 7 días a partir de hoy.
    [HttpGet]
    public async Task<IActionResult> Listar(DateTime? desde, DateTime? hasta, int? idProfesional, int? idConsultante, string? estado)
    {
        var inicio = (desde ?? DateTime.Today).Date;
        var fin = (hasta ?? inicio.AddDays(6)).Date.AddDays(1);   // incluye todo el día "hasta"

        var q = _db.Citas.AsNoTracking().Where(c => c.FechaInicio >= inicio && c.FechaInicio < fin);

        if (EsProfesional)
        {
            var yo = User.IdUsuario();
            q = q.Where(c => c.IdProfesional == yo);               // solo su agenda
        }
        else if (idProfesional.HasValue)
        {
            q = q.Where(c => c.IdProfesional == idProfesional.Value);
        }

        if (idConsultante.HasValue)
            q = q.Where(c => c.IdConsultante == idConsultante.Value);
        if (!string.IsNullOrWhiteSpace(estado))
            q = q.Where(c => c.Estado == estado);

        return Ok(await Proyectar(q.OrderBy(c => c.FechaInicio)).ToListAsync());
    }

    // GET api/citas/5
    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obtener(int id)
    {
        var esProf = EsProfesional;
        var yo = User.IdUsuario();

        var cita = await Proyectar(_db.Citas.AsNoTracking()
                .Where(c => c.IdCita == id && (!esProf || c.IdProfesional == yo)))
            .FirstOrDefaultAsync();

        return cita is null ? NotFound() : Ok(cita);
    }

    // POST api/citas
    [HttpPost]
    public async Task<IActionResult> Crear(CitaCrearDto dto)
    {
        var consultante = await _db.Consultantes.FindAsync(dto.IdConsultante);
        if (consultante is null || !consultante.Activo)
            return BadRequest("El consultante no existe o está inactivo.");

        int idProfesional;
        if (EsProfesional)
        {
            idProfesional = User.IdUsuario();
            if (consultante.IdProfesional != idProfesional)
                return BadRequest("El consultante no existe o está inactivo.");   // mismo mensaje: no revela datos ajenos
        }
        else
        {
            idProfesional = dto.IdProfesional ?? consultante.IdProfesional;
        }

        var profesional = await _db.Usuarios
            .FirstOrDefaultAsync(u => u.IdUsuario == idProfesional && u.Activo && u.Rol!.Nombre == "Profesional");
        if (profesional is null)
            return BadRequest("El profesional indicado no existe o está inactivo.");

        if (!Modalidades.Contains(dto.Modalidad))
            return BadRequest("La modalidad debe ser Presencial o Virtual.");

        var inicio = dto.FechaInicio;
        var fin = inicio.AddMinutes(dto.DuracionMinutos);
        if (await HayCruce(idProfesional, inicio, fin, null))
            return Conflict("El profesional ya tiene otra cita en ese horario.");

        var cita = new Cita
        {
            IdConsultante = consultante.IdConsultante,
            IdProfesional = idProfesional,
            FechaInicio = inicio,
            FechaFin = fin,
            Modalidad = dto.Modalidad,
            Estado = "Programada",
            Valor = dto.Valor ?? profesional.ValorSesion ?? 0,       // si no se indica, usa el valor de sesión del profesional
            EnlaceVirtual = dto.Modalidad == "Virtual" ? dto.EnlaceVirtual : null,
            NotaAdministrativa = dto.NotaAdministrativa,
            IdCreadaPor = User.IdUsuario()
        };
        _db.Citas.Add(cita);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Obtener), new { id = cita.IdCita },
            new { cita.IdCita, cita.FechaInicio, cita.FechaFin, cita.Valor });
    }

    // PUT api/citas/5  (reprogramar o corregir; solo citas "Programada")
    [HttpPut("{id:int}")]
    public async Task<IActionResult> Actualizar(int id, CitaActualizarDto dto)
    {
        var cita = await _db.Citas.FindAsync(id);
        if (cita is null || (EsProfesional && cita.IdProfesional != User.IdUsuario()))
            return NotFound();

        if (cita.Estado != "Programada")
            return Conflict("Solo se pueden modificar citas en estado Programada.");
        if (!Modalidades.Contains(dto.Modalidad))
            return BadRequest("La modalidad debe ser Presencial o Virtual.");

        var inicio = dto.FechaInicio;
        var fin = inicio.AddMinutes(dto.DuracionMinutos);
        if (await HayCruce(cita.IdProfesional, inicio, fin, id))
            return Conflict("El profesional ya tiene otra cita en ese horario.");

        cita.FechaInicio = inicio;
        cita.FechaFin = fin;
        cita.Modalidad = dto.Modalidad;
        cita.Valor = dto.Valor;
        cita.EnlaceVirtual = dto.Modalidad == "Virtual" ? dto.EnlaceVirtual : null;
        cita.NotaAdministrativa = dto.NotaAdministrativa;
        await _db.SaveChangesAsync();
        return NoContent();
    }

    // POST api/citas/5/estado   { "estado": "Atendida" }
    [HttpPost("{id:int}/estado")]
    public async Task<IActionResult> CambiarEstado(int id, CitaEstadoDto dto)
    {
        var cita = await _db.Citas.FindAsync(id);
        if (cita is null || (EsProfesional && cita.IdProfesional != User.IdUsuario()))
            return NotFound();

        if (!Estados.Contains(dto.Estado))
            return BadRequest("Estado inválido. Usa: Programada, Atendida, Cancelada o Inasistencia.");

        // Si se reactiva una cita cancelada, el horario debe seguir libre
        if (cita.Estado == "Cancelada" && dto.Estado != "Cancelada"
            && await HayCruce(cita.IdProfesional, cita.FechaInicio, cita.FechaFin, id))
            return Conflict("No se puede reactivar: el profesional ya tiene otra cita en ese horario.");

        cita.Estado = dto.Estado;
        await _db.SaveChangesAsync();
        return Ok(new { cita.IdCita, cita.Estado });
    }
}