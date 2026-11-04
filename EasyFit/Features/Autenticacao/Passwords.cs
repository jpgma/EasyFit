using System.Security.Cryptography;
using System.Text;

namespace EasyFit.Features.Autenticacao;

internal static class Passwords
{
    private const string Algorithm = "pbkdf2-sha256";
    private const int Iterations = 600_000;
    private const int SaltBytes = 16;
    private const int HashBytes = 32;

    // Teto de leitura: um valor corrompido no banco não pode travar a CPU.
    private const int MaxIterations = 10_000_000;

    // Verificado quando o e-mail não existe, para o tempo de resposta não revelar quem tem conta.
    // Montado sem calcular nada: gerá-lo com Hash() deixaria a primeira verificação duas vezes mais lenta.
    private static readonly string DummyHash =
        $"{Algorithm}${Iterations}${Convert.ToBase64String(new byte[SaltBytes])}${Convert.ToBase64String(new byte[HashBytes])}";

    public static string Hash(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltBytes);
        var hash = Derive(senha, salt, Iterations, HashBytes);
        return $"{Algorithm}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verify(string senha, string storedHash)
    {
        var parts = storedHash.Split('$');
        if (parts.Length != 4
            || parts[0] != Algorithm
            || !int.TryParse(parts[1], out var iterations)
            || iterations is <= 0 or > MaxIterations)
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var expected = Convert.FromBase64String(parts[3]);
            return expected.Length > 0
                && CryptographicOperations.FixedTimeEquals(Derive(senha, salt, iterations, expected.Length), expected);
        }
        catch (FormatException)
        {
            return false;
        }
    }

    /// <summary>Gasta o mesmo tempo de uma verificação real e sempre falha.</summary>
    public static void VerifyDummy(string senha) => Verify(senha, DummyHash);

    private static byte[] Derive(string senha, byte[] salt, int iterations, int length) =>
        Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(senha), salt, iterations, HashAlgorithmName.SHA256, length);
}
