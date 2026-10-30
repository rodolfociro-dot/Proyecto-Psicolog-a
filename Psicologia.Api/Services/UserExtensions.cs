using System.Security.Claims;

namespace Psicologia.Api.Services;

// Atajos para saber quién está haciendo la petición (sale del token)
public static class UserExtensions
{
    public static int IdUsuario(this ClaimsPrincipal user)
        => int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)!.Value);
}