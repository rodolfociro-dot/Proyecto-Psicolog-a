using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Psicologia.Api.Data;
using Psicologia.Api.Services;

namespace Psicologia.Api.Controllers;

// Lista de profesionales activos (para los menús desplegables de agenda y consultantes)
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProfesionalesController : ControllerBase
{
    private readonly PsicologiaContext _db;
    public ProfesionalesController(PsicologiaContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        var q = _db.Usuarios.AsNoTracking()
            .Where(u => u.Activo && u.Rol!.Nombre == "Profesional");

        // Un profesional solo se ve a sí mismo
        if (User.IsInRole("Profesional"))
        {
            var yo = User.IdUsuario();
            q = q.Where(u => u.IdUsuario == yo);
        }

        var lista = await q.OrderBy(u => u.Nombres)
            .Select(u => new
            {
                u.IdUsuario,
                Nombre = u.Nombres + " " + u.Apellidos,
                u.Especialidad,
                u.ValorSesion
            })
            .ToListAsync();
        return Ok(lista);
    }
}