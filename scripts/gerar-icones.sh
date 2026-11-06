#!/usr/bin/env bash
# Gera todos os ícones do aplicativo a partir de um PNG quadrado (de preferência 1024x1024),
# com cantos arredondados transparentes ou fundo até a borda. Exige o ImageMagick 7 (`magick`).
#   scripts/gerar-icones.sh caminho/novo-icone.png   troca a origem e gera tudo
#   scripts/gerar-icones.sh                          gera de novo a partir da origem atual
# A origem fica versionada em EasyFit/webapp/icon-source.png, fora de public/ para não entrar no pré-cache.
set -euo pipefail

# Resolve o caminho recebido antes de mudar de pasta. O cygpath aceita caminhos do Windows (D:\pasta\icone.png).
novo=""
if [[ $# -ge 1 ]]; then
  novo=$1
  command -v cygpath >/dev/null && novo=$(cygpath -u "$novo")
  [[ -f "$novo" ]] || { echo "Arquivo não encontrado: $1" >&2; exit 2; }
  novo=$(realpath "$novo")
fi

cd "$(dirname "$0")/.."

webapp=EasyFit/webapp
origem=$webapp/icon-source.png

case $# in
  0) ;;
  1)
    [[ "$novo" == "$(realpath "$origem" 2>/dev/null || true)" ]] || cp "$novo" "$origem"
    ;;
  *) echo "Uso: scripts/gerar-icones.sh [icone.png]" >&2; exit 2 ;;
esac

command -v magick >/dev/null || { echo "ImageMagick 7 (magick) não encontrado." >&2; exit 1; }
[[ -f "$origem" ]] || { echo "Origem não encontrada: $origem" >&2; exit 1; }

largura=$(magick "$origem" -format '%w' info:)
altura=$(magick "$origem" -format '%h' info:)
[[ "$largura" == "$altura" ]] || { echo "A imagem precisa ser quadrada (${largura}x${altura})." >&2; exit 1; }

# Todo o trabalho em 1024 px, para as medidas abaixo valerem para qualquer origem.
# Recorte interno sem os cantos arredondados: 6,25% de cada lado cobre um raio de até ~20%.
base=(-background none "$origem" -filter Lanczos -resize 1024x1024 +repage)
quadrado=(-crop 896x896+64+64 +repage)

# Cor de fundo para ícones sem transparência: a média da faixa superior do recorte.
fundo="#$(magick "${base[@]}" "${quadrado[@]}" -crop 896x32+0+0 -alpha off -resize 1x1! -format '%[hex:p{0,0}]' info:)"

out=$webapp/public
# Tudo em public/ entra no pré-cache do Service Worker: 255 cores, sem pontilhado, cortam o tamanho pela metade.
# Nos ícones transparentes fica PNG32, porque o PNG8 do ImageMagick só tem transparência de 1 bit e serrilha os cantos.
paleta="-dither None -colors 255"

# Navegador e manifesto: a arte inteira, com os cantos transparentes.
magick "${base[@]}" -resize 512x512 $paleta -strip PNG32:"$out/icon-512.png"
magick "${base[@]}" -resize 192x192 $paleta -strip PNG32:"$out/icon-192.png"
magick "${base[@]}" \( -clone 0 -resize 16x16 \) \( -clone 0 -resize 32x32 \) \( -clone 0 -resize 48x48 \) \
  -delete 0 "$out/favicon.ico"

# iOS: opaco e até a borda, porque o sistema arredonda os cantos e pinta transparência de preto.
magick "${base[@]}" "${quadrado[@]}" -resize 180x180 -background "$fundo" -alpha remove -alpha off \
  $paleta -strip PNG8:"$out/apple-touch-icon.png"

# Android (maskable): fundo cheio e a arte a 72%, com a borda esmaecida, dentro da zona segura de 80%.
magick -size 1024x1024 "xc:$fundo" \
  \( "${base[@]}" -background "$fundo" -alpha remove -alpha off \
     \( -size 1024x1024 xc:black -fill white -draw 'roundrectangle 112,112 911,911 120,120' -blur 0x20 \) \
     -compose CopyOpacity -composite -resize 737x737 \) \
  -gravity center -compose over -composite -alpha off -resize 512x512 $paleta -strip PNG8:"$out/icon-maskable-512.png"

echo "Ícones gerados em $out a partir de $origem (fundo $fundo)."
