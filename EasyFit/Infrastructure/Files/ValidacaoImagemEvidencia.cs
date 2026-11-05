namespace EasyFit.Infrastructure.Files;

internal enum FormatoImagemEvidencia
{
    Jpeg,
    Png,
    Webp,
}

internal static class ValidacaoImagemEvidencia
{
    public const int MaxBytes = 5 * 1024 * 1024;
    public const int MaxBytesMiniatura = 100 * 1024;
    public const string TipoMimeMiniatura = "image/jpeg";

    private static ReadOnlySpan<byte> JpegMagic => [0xFF, 0xD8, 0xFF];
    private static ReadOnlySpan<byte> PngMagic => [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
    private static ReadOnlySpan<byte> WebpRiff => "RIFF"u8;
    private static ReadOnlySpan<byte> WebpMagic => "WEBP"u8;

    public static bool TryDetectarFormato(ReadOnlySpan<byte> inicio, out FormatoImagemEvidencia formato)
    {
        if (inicio.Length >= 3 && inicio.StartsWith(JpegMagic))
        {
            formato = FormatoImagemEvidencia.Jpeg;
            return true;
        }

        if (inicio.Length >= 8 && inicio.StartsWith(PngMagic))
        {
            formato = FormatoImagemEvidencia.Png;
            return true;
        }

        if (inicio.Length >= 12
            && inicio.StartsWith(WebpRiff)
            && inicio.Slice(8, 4).SequenceEqual(WebpMagic))
        {
            formato = FormatoImagemEvidencia.Webp;
            return true;
        }

        formato = default;
        return false;
    }

    public static string GetTipoMime(FormatoImagemEvidencia formato) => formato switch
    {
        FormatoImagemEvidencia.Jpeg => "image/jpeg",
        FormatoImagemEvidencia.Png => "image/png",
        FormatoImagemEvidencia.Webp => "image/webp",
        _ => throw new ArgumentOutOfRangeException(nameof(formato)),
    };

    public static string GetExtensao(FormatoImagemEvidencia formato) => formato switch
    {
        FormatoImagemEvidencia.Jpeg => ".jpg",
        FormatoImagemEvidencia.Png => ".png",
        FormatoImagemEvidencia.Webp => ".webp",
        _ => throw new ArgumentOutOfRangeException(nameof(formato)),
    };
}
