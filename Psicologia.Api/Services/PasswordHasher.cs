using System.Security.Cryptography;

namespace Psicologia.Api.Services;

// Las contraseñas NUNCA se guardan en texto plano: se guarda un "hash" con sal (PBKDF2).
public static class PasswordHasher
{
    private const int Iteraciones = 210_000;

    public static string Hash(string contrasena)
    {
        var sal = RandomNumberGenerator.GetBytes(16);
        var hash = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, Iteraciones, HashAlgorithmName.SHA256, 32);
        return $"pbkdf2${Iteraciones}${Convert.ToBase64String(sal)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string contrasena, string almacenado)
    {
        var partes = almacenado.Split('$');
        if (partes.Length != 4 || partes[0] != "pbkdf2") return false;

        var iteraciones = int.Parse(partes[1]);
        var sal = Convert.FromBase64String(partes[2]);
        var esperado = Convert.FromBase64String(partes[3]);
        var calculado = Rfc2898DeriveBytes.Pbkdf2(contrasena, sal, iteraciones, HashAlgorithmName.SHA256, esperado.Length);
        return CryptographicOperations.FixedTimeEquals(calculado, esperado);
    }
}