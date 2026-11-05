using System.Security.Cryptography;
using Microsoft.Extensions.Options;

namespace EasyFit.Infrastructure.Files;

internal sealed class ArmazenamentoEvidencias(IOptions<EvidenciasOptions> options)
{
    private const int BufferSize = 81920;
    private readonly string _diretorioRaiz = options.Value.Diretorio;

    public async Task<(string CaminhoRelativo, string TipoMime, int TamanhoBytes, string Sha256)> SalvarAsync(
        Stream origem,
        int maxBytes,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_diretorioRaiz);

        var agora = DateTimeOffset.UtcNow;
        var subpasta = Path.Combine(agora.Year.ToString("D4"), agora.Month.ToString("D2"));
        var diretorioDestino = Path.Combine(_diretorioRaiz, subpasta);
        Directory.CreateDirectory(diretorioDestino);

        var nomeArquivo = $"{Guid.NewGuid():N}";
        var caminhoTemporario = Path.Combine(diretorioDestino, nomeArquivo + ".tmp");
        FormatoImagemEvidencia? formatoDetectado = null;

        string sha256;
        await using (var destino = new FileStream(
            caminhoTemporario,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            BufferSize,
            FileOptions.Asynchronous))
        {
            var buffer = new byte[BufferSize];
            var header = new byte[12];
            var headerLength = 0;
            long total = 0;
            using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);

            while (true)
            {
                var lidos = await origem.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
                if (lidos == 0)
                {
                    break;
                }

                if (headerLength < header.Length)
                {
                    var copiar = Math.Min(lidos, header.Length - headerLength);
                    buffer.AsSpan(0, copiar).CopyTo(header.AsSpan(headerLength));
                    headerLength += copiar;
                }

                total += lidos;
                if (total > maxBytes)
                {
                    throw new EvidenciaInvalidaException("Arquivo excede o limite permitido.");
                }

                hasher.AppendData(buffer, 0, lidos);
                await destino.WriteAsync(buffer.AsMemory(0, lidos), cancellationToken);
            }

            if (total == 0)
            {
                throw new EvidenciaInvalidaException("Arquivo vazio.");
            }

            if (!ValidacaoImagemEvidencia.TryDetectarFormato(header.AsSpan(0, headerLength), out var formato))
            {
                throw new EvidenciaInvalidaException("Formato de imagem não suportado.");
            }

            formatoDetectado = formato;
            sha256 = Convert.ToHexString(hasher.GetHashAndReset());
        }

        var extensao = ValidacaoImagemEvidencia.GetExtensao(formatoDetectado!.Value);
        var caminhoFinal = Path.Combine(diretorioDestino, nomeArquivo + extensao);
        File.Move(caminhoTemporario, caminhoFinal);

        var caminhoRelativo = Path.Combine(subpasta, nomeArquivo + extensao).Replace('\\', '/');
        return (
            caminhoRelativo,
            ValidacaoImagemEvidencia.GetTipoMime(formatoDetectado.Value),
            (int)new FileInfo(caminhoFinal).Length,
            sha256);
    }

    public string ResolverCaminhoCompleto(string caminhoRelativo)
    {
        var caminhoCompleto = Path.Combine(_diretorioRaiz, caminhoRelativo.Replace('/', Path.DirectorySeparatorChar));
        var raizNormalizado = Path.GetFullPath(_diretorioRaiz);
        var completoNormalizado = Path.GetFullPath(caminhoCompleto);
        if (!completoNormalizado.StartsWith(raizNormalizado, StringComparison.OrdinalIgnoreCase))
        {
            throw new EvidenciaInvalidaException("Caminho inválido.");
        }

        return completoNormalizado;
    }

    public void ExcluirSeExistir(string caminhoRelativo)
    {
        var caminhoCompleto = Path.Combine(_diretorioRaiz, caminhoRelativo.Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(caminhoCompleto))
        {
            File.Delete(caminhoCompleto);
        }
    }

}

internal sealed class EvidenciaInvalidaException(string message) : Exception(message);
