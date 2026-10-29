using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Psicologia.Api.Data;
using Psicologia.Api.Models;

namespace Psicologia.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RolesController : ControllerBase
{
    private readonly PsicologiaContext _db;
    public RolesController(PsicologiaContext db) => _db = db;

    // GET api/roles  (prueba de conexión con la base de datos)
    [HttpGet]
    public async Task<ActionResult<IEnumerable<Rol>>> Listar()
        => await _db.Roles.AsNoTracking().OrderBy(r => r.IdRol).ToListAsync();
}