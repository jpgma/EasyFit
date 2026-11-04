using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace EasyFit.Infrastructure.Authentication;

/// <summary>O token só existe no cookie; o banco guarda apenas o hash dele.</summary>
internal static class SessaoToken
{
    private const int TokenBytes = 32;

    public static string Generate() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenBytes));

    // O token tem entropia alta: um hash rápido basta, ao contrário das senhas.
    public static byte[] Hash(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));
}
