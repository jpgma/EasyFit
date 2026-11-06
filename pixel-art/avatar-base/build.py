"""Gera o kit de camadas e as folhas dos personagens padrão a partir de rig.aseprite.

Uso (na pasta pixel-art/avatar-base):  python build.py
Exige o pixel-plugin configurado (/pixel-setup): o rig é lido pelo servidor MCP dele.

Saídas:
- kit/<camada>.png e kit/kit.json: camadas de identificação (quadros de 16 px numa grade
  8 x 12) e as regras de composição (docs/avatar-kit.md). O kit só é relido do rig quando
  rig.aseprite muda;
- saida/<personagem>.png: folhas 256x384 no formato de docs/avatar-formato.md, que vão
  para o bundle do frontend na fase G1;
- preview/: ampliações e GIFs para conferir a arte (fora do git).
"""

from __future__ import annotations

import hashlib
import json
import os
import pathlib

from PIL import Image

import compor
import rig
from aseprite_mcp import Aseprite

AQUI = pathlib.Path(__file__).resolve().parent
RIG = AQUI / "rig.aseprite"
KIT = AQUI / "kit"
SAIDA = AQUI / "saida"
PREVIEW = AQUI / "preview"
FUNDO = (58, 60, 74, 255)


def celulas():
    """[(linha, coluna, quadro 1-base)] na ordem da folha."""
    out, q = [], 1
    for linha, (_, qt, _, _) in enumerate(rig.ANIMACOES):
        for col in range(qt):
            out.append((linha, col, q))
            q += 1
    return out


def hash_rig() -> str:
    return hashlib.sha256(RIG.read_bytes()).hexdigest()


def exportar_kit():
    """Lê cada camada de cada quadro do rig e grava kit/<camada>.png."""
    with Aseprite() as a:
        info = a("get_sprite_info", sprite_path=str(RIG))
        faltam = set(rig.CAMADAS) - set(info["layers"])
        if faltam:
            raise SystemExit(f"rig.aseprite sem as camadas {sorted(faltam)}")
        n = rig.N
        for camada in rig.CAMADAS:
            im = Image.new("RGBA", (n * 8, n * 12), (0, 0, 0, 0))
            for linha, col, q in celulas():
                for (x, y), cor in a.ler_cel(RIG, camada, q, n, n).items():
                    if cor not in rig.REGIAO_POR_COR:
                        raise SystemExit(f"{camada}, quadro {q}, ({x}, {y}): cor {cor} fora da paleta de IDs")
                    im.putpixel((col * n + x, linha * n + y), Image.new("RGB", (1, 1), cor).getpixel((0, 0)) + (255,))
            im.save(KIT / f"{camada}.png", optimize=True)
            print(f"kit: {camada}")


def ler_kit() -> dict:
    """{(linha, coluna): {camada: {(x, y): (região, tom)}}}"""
    n = rig.N
    imgs = {c: Image.open(KIT / f"{c}.png").convert("RGBA") for c in rig.CAMADAS}
    kit = {}
    for linha, col, _ in celulas():
        quadro = {}
        for camada, im in imgs.items():
            px = {}
            for y in range(n):
                for x in range(n):
                    r, g, b, a = im.getpixel((col * n + x, linha * n + y))
                    if a:
                        px[(x, y)] = rig.REGIAO_POR_COR["#%02x%02x%02x" % (r, g, b)]
            quadro[camada] = px
        kit[(linha, col)] = quadro
    return kit


def kit_json(hash_atual: str) -> dict:
    return {
        "rig_sha256": hash_atual,
        "quadro": rig.N,
        "escala": rig.ESCALA,
        "animacoes": [{"nome": n, "quadros": q, "ms": ms, "repete": r} for n, q, ms, r in rig.ANIMACOES],
        "camadas": rig.CAMADAS,
        "cores_id": {cor: [reg, tom] for (reg, tom), cor in rig.CORES_ID.items()},
        "contorno": compor.CONTORNO,
        "regioes_base": compor.REGIOES_BASE,
        "blusas": compor.BLUSAS,
        "calcas": compor.CALCAS,
        "camadas_blusa": compor.CAMADAS_BLUSA,
        "acessorios": compor.ACESSORIOS,
        "meia": compor.MEIA,
        "peles": compor.PELES,
        "cabelos": {"estilos": rig.CABELOS + ["careca"], "cores": compor.CABELOS_COR},
        "olho": compor.OLHO,
        "rampa": compor.RAMPA,
    }


def conferir(caminho: pathlib.Path) -> int:
    im = Image.open(caminho)
    assert im.size == (256, 384) and im.mode == "RGBA", (caminho, im.size, im.mode)
    alfas = {px[3] for px in im.get_flattened_data()}
    assert alfas <= {0, 255}, f"{caminho}: alfa parcial {alfas}"
    tam = os.path.getsize(caminho)
    assert tam <= 64 * 1024, f"{caminho}: {tam} bytes"
    return tam


def ampliar(im: Image.Image, k: int) -> Image.Image:
    base = Image.new("RGBA", im.size, FUNDO)
    base.alpha_composite(im)
    return base.resize((im.width * k, im.height * k), Image.NEAREST)


def previews(folhas: dict[str, Image.Image]):
    PREVIEW.mkdir(exist_ok=True)
    C = rig.N * rig.ESCALA
    for nome, f in folhas.items():
        ampliar(f, 3).save(PREVIEW / f"{nome}.png")
    nomes = list(folhas)
    for linha, (anim, qt, ms, _) in enumerate(rig.ANIMACOES):
        quadros = []
        for col in range(qt):
            q = Image.new("RGBA", (C * len(nomes), C), (0, 0, 0, 0))
            for i, n in enumerate(nomes):
                q.alpha_composite(folhas[n].crop((col * C, linha * C, (col + 1) * C, (linha + 1) * C)), (i * C, 0))
            quadros.append(ampliar(q, 3).convert("RGB"))
        quadros[0].save(PREVIEW / f"{linha:02d}-{anim}.gif", save_all=True, append_images=quadros[1:],
                        duration=ms or 1000, loop=0)


def main():
    KIT.mkdir(exist_ok=True)
    SAIDA.mkdir(exist_ok=True)
    atual = hash_rig()
    meta = KIT / "kit.json"
    anterior = json.loads(meta.read_text(encoding="utf-8")).get("rig_sha256") if meta.exists() else None
    if anterior != atual:
        exportar_kit()
    meta.write_text(json.dumps(kit_json(atual), ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    kit = ler_kit()
    folhas = {}
    for arq in sorted((AQUI / "personagens").glob("*.json")):
        p = json.loads(arq.read_text(encoding="utf-8"))
        folha = compor.compor(kit, p)
        destino = SAIDA / f"{arq.stem}.png"
        folha.save(destino, optimize=True)
        print(f"{arq.stem}: {conferir(destino)} bytes")
        folhas[arq.stem] = folha
    previews(folhas)


if __name__ == "__main__":
    main()
