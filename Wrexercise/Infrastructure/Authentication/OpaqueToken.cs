using System.Buffers.Text;
using System.Security.Cryptography;
using System.Text;

namespace Wrexercise.Infrastructure.Authentication;

/// <summary>Token aleatório de sessão ou convite: quem o recebe guarda o valor; o banco, apenas o hash.</summary>
internal static class OpaqueToken
{
    private const int TokenBytes = 32;

    public static string Generate() => Base64Url.EncodeToString(RandomNumberGenerator.GetBytes(TokenBytes));

    // O token tem entropia alta: um hash rápido basta, ao contrário das senhas.
    public static byte[] Hash(string token) => SHA256.HashData(Encoding.UTF8.GetBytes(token));
}
