using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Psicologia.Api.Models;

namespace Psicologia.Api.Services;

// Crea el "pase de entrada" (token JWT) que el frontend enviará en cada petición.
public class TokenService
{
    private readonly IConfiguration _config;
    public TokenService(IConfiguration config) => _config = config;

    public (string Token, DateTime Expira) Crear(Usuario usuario, string rol)
    {
        var jwt = _config.GetSection("Jwt");
        var minutos = int.Parse(jwt["ExpiraMinutos"] ?? "480");
        var expira = DateTime.UtcNow.AddMinutes(minutos);
        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt["Key"]!));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, usuario.IdUsuario.ToString()),
                new Claim(ClaimTypes.Name, $"{usuario.Nombres} {usuario.Apellidos}"),
                new Claim(ClaimTypes.Email, usuario.Email),
                new Claim(ClaimTypes.Role, rol)
            }),
            Issuer = jwt["Issuer"],
            Audience = jwt["Audience"],
            Expires = expira,
            SigningCredentials = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256)
        };

        return (new JsonWebTokenHandler().CreateToken(descriptor), expira);
    }
}