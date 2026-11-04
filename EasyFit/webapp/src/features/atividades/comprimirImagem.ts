const maxLado = 1600;
const qualidade = 0.82;

/** Reduz fotos antes do upload para economizar banda em conexões móveis. */
export async function comprimirImagem(arquivo: File): Promise<File> {
  if (!arquivo.type.startsWith("image/") || arquivo.type.includes("gif")) {
    return arquivo;
  }

  const bitmap = await createImageBitmap(arquivo);
  const escala = Math.min(1, maxLado / Math.max(bitmap.width, bitmap.height));
  const largura = Math.max(1, Math.round(bitmap.width * escala));
  const altura = Math.max(1, Math.round(bitmap.height * escala));

  const canvas = document.createElement("canvas");
  canvas.width = largura;
  canvas.height = altura;
  const ctx = canvas.getContext("2d");
  if (!ctx) return arquivo;

  ctx.drawImage(bitmap, 0, 0, largura, altura);
  bitmap.close();

  const blob = await new Promise<Blob | null>((resolve) =>
    canvas.toBlob(resolve, "image/jpeg", qualidade),
  );
  if (!blob) return arquivo;

  const nome = arquivo.name.replace(/\.\w+$/, "") + ".jpg";
  return new File([blob], nome, { type: "image/jpeg" });
}
