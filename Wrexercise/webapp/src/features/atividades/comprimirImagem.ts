const maxLado = 1600;
const qualidade = 0.82;

// Os cartões mostram a miniatura em 64 px: 240 mantém a nitidez em telas 3x.
const maxLadoMiniatura = 240;
const qualidadeMiniatura = 0.6;

export type EvidenciaPreparada = {
  arquivo: File;
  miniatura: Blob | null;
};

function redimensionar(
  bitmap: ImageBitmap,
  limite: number,
  qualidadeJpeg: number,
): Promise<Blob | null> {
  const escala = Math.min(1, limite / Math.max(bitmap.width, bitmap.height));
  const canvas = document.createElement("canvas");
  canvas.width = Math.max(1, Math.round(bitmap.width * escala));
  canvas.height = Math.max(1, Math.round(bitmap.height * escala));
  const ctx = canvas.getContext("2d");
  if (!ctx) return Promise.resolve(null);

  ctx.drawImage(bitmap, 0, 0, canvas.width, canvas.height);
  return new Promise((resolve) =>
    canvas.toBlob(resolve, "image/jpeg", qualidadeJpeg),
  );
}

/**
 * Reduz a foto e gera a miniatura do feed antes do upload, para economizar banda.
 * Passar pelo canvas também descarta os metadados da foto, inclusive a localização.
 * Sem suporte a uma imagem, envia o original sem miniatura: o servidor valida o formato.
 */
export async function prepararEvidencia(
  arquivo: File,
): Promise<EvidenciaPreparada> {
  if (!arquivo.type.startsWith("image/") || arquivo.type.includes("gif")) {
    return { arquivo, miniatura: null };
  }

  let bitmap: ImageBitmap;
  try {
    bitmap = await createImageBitmap(arquivo);
  } catch {
    return { arquivo, miniatura: null };
  }

  const [foto, miniatura] = await Promise.all([
    redimensionar(bitmap, maxLado, qualidade),
    redimensionar(bitmap, maxLadoMiniatura, qualidadeMiniatura),
  ]);
  bitmap.close();

  const nome = arquivo.name.replace(/\.\w+$/, "") + ".jpg";
  return {
    arquivo: foto ? new File([foto], nome, { type: "image/jpeg" }) : arquivo,
    miniatura,
  };
}
