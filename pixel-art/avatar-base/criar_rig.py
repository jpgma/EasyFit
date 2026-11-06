"""Cria rig.aseprite a partir do template de 16 px, pelo servidor MCP do pixel-plugin.

Uso (na pasta pixel-art/avatar-base):
    python criar_rig.py              # recusa sobrescrever um rig existente
    python criar_rig.py --substituir # recria o rig do zero (perde edições feitas no Aseprite)

Depois de criado, o rig é a fonte da verdade: edite rig.aseprite no Aseprite e rode build.py.
"""

from __future__ import annotations

import pathlib
import sys

import rig
from aseprite_mcp import Aseprite

AQUI = pathlib.Path(__file__).resolve().parent
TEMPLATE = AQUI / "referencia" / "template-16px.aseprite"
RIG = AQUI / "rig.aseprite"


def ler_template(a: Aseprite) -> dict:
    info = a("get_sprite_info", sprite_path=str(TEMPLATE))
    out = {}
    for f in range(info["frame_count"]):
        for camada in rig.DO_TEMPLATE:
            out[(f, camada)] = a.ler_cel(TEMPLATE, camada, f + 1, info["width"], info["height"])
    return out


def quadros_do_rig(template: dict) -> list[tuple[str, dict]]:
    """Os 53 quadros na ordem da folha: [(animação, {camada: {(x, y): (região, tom)}})]."""
    out = []
    for nome, *_ in rig.ANIMACOES:
        for q in rig.QUADROS[nome]:
            out.append((nome, rig.montar_quadro(template, q)))
    return out


def escrever_rig(a: Aseprite, quadros: list[tuple[str, dict]], destino: pathlib.Path):
    r = a("create_canvas", width=rig.N, height=rig.N, color_mode="rgb")
    tmp = r["file_path"]
    a("save_as", sprite_path=tmp, output_path=str(destino))
    s = str(destino)
    padrao = a("get_sprite_info", sprite_path=s)["layers"]
    for camada in rig.CAMADAS:
        a("add_layer", sprite_path=s, layer_name=camada)
    for camada in padrao:
        a("delete_layer", sprite_path=s, layer_name=camada)
    duracoes = [ms or 1000 for nome, qt, ms, _ in rig.ANIMACOES for _ in range(qt)]
    a("set_frame_duration", sprite_path=s, frame_number=1, duration_ms=duracoes[0])
    for ms in duracoes[1:]:
        a("add_frame", sprite_path=s, duration_ms=ms)
    for i, (_, quadro) in enumerate(quadros, start=1):
        for camada, pixels in quadro.items():
            if pixels:
                a("draw_pixels", sprite_path=s, layer_name=camada, frame_number=i,
                  pixels=[{"x": x, "y": y, "color": rig.CORES_ID[v]} for (x, y), v in sorted(pixels.items())])
        print(f"quadro {i}/{len(quadros)}", end="\r", flush=True)
    inicio = 1
    for nome, qt, _, _ in rig.ANIMACOES:
        a("create_tag", sprite_path=s, tag_name=nome, from_frame=inicio, to_frame=inicio + qt - 1, direction="forward")
        inicio += qt
    a("set_palette", sprite_path=s, colors=sorted(set(rig.CORES_ID.values())))
    print()


def main():
    if RIG.exists() and "--substituir" not in sys.argv:
        sys.exit(f"{RIG.name} já existe; use --substituir para recriar (perde as edições).")
    with Aseprite() as a:
        template = ler_template(a)
        quadros = quadros_do_rig(template)
        escrever_rig(a, quadros, RIG)
    print(f"rig criado: {RIG.relative_to(AQUI)}")


if __name__ == "__main__":
    main()
