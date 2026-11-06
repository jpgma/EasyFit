"""Composição de uma folha de avatar a partir do kit de IDs e de um personagem.

Passos (docs/avatar-kit.md §4), feitos em 16 px por quadro e ampliados 2x no fim:
1. empilhar as camadas escolhidas, na ordem do rig;
2. escurecer o que fica atrás do braço da frente e encosta nele;
3. trocar cada (região, tom) pela cor do material que a roupa dá à região;
4. contorno grosso do template (4 vizinhos e depois 8 vizinhos) em volta do corpo,
   e contorno fino em volta do cabelo e dos acessórios que saem dele;
5. ampliar 2x; o retrato amplia 3x um recorte da cabeça.
"""

from __future__ import annotations

import pathlib
import sys

from PIL import Image

sys.path.insert(0, str(pathlib.Path(__file__).resolve().parents[2] / ".claude/skills/pixel-art-studio/scripts"))
from pixelstudio import hex2rgba, ramp  # noqa: E402

import rig  # noqa: E402

CONTORNO = "#0e071b"  # o mesmo do template

PELES = {
    "clara": ["#e0a07e", "#f6c9a4", "#fde3cc"],
    "morena": ["#a5683f", "#c98f5e", "#e2ae80"],
    "negra": ["#5a3826", "#87573a", "#a8744f"],
}
CABELOS_COR = {
    "preto": ["#1a1932", "#2a2840", "#45405a"],  # o "preto" é a cor da faixa do template
    "castanho": ["#3f2619", "#64402a", "#8a5e3e"],
    "loiro": ["#a9783a", "#d8ad5c", "#f1d488"],
    "ruivo": ["#7f321d", "#b0502b", "#d97a45"],
    "grisalho": ["#7d7b86", "#aeacb5", "#d8d7de"],
}
OLHO = [CONTORNO, CONTORNO, CONTORNO]

# O que cada roupa cobre: região -> material (ou [material, tom] para fixar o tom).
BLUSAS = {
    "camiseta": {"tronco": "blusa", "braco": "blusa"},
    "regata": {"tronco": "blusa"},
    "moletom": {"tronco": "blusa", "braco": "blusa", "antebraco": "blusa", "punho": ["blusa", 0]},
}
CALCAS = {
    "short": {"quadril": "calca", "coxa": "calca"},
    "legging": {"quadril": "calca", "coxa": "calca", "canela": "calca"},
    "jogger": {"quadril": "calca", "coxa": "calca", "canela": "calca", "tornozelo": ["calca", 0]},
}
REGIOES_BASE = {
    "pele": "pele", "mao": "pele", "braco": "pele", "antebraco": "pele", "punho": "pele",
    "coxa": "pele", "canela": "pele", "tornozelo": "pele", "olho": "olho", "cabelo": "cabelo",
    "pe": "tenis", "bone": "bone", "bone_aba": ["bone", 0], "fone": ["fone", 1],
    "fone_concha": ["fone", 0], "capuz": "blusa", "garrafa": "garrafa", "garrafa_tampa": ["garrafa_tampa", 1],
}
ACESSORIOS = {
    "bone": {"camada": "bone"},
    "fone": {"camada": "fone"},
    "garrafa": {"camada": "garrafa", "tampa": "#3a3a44"},
    "munhequeira": {"regioes": {"punho": "munhequeira"}},
}
MEIA = {"regioes": {"tornozelo": "meia"}, "exceto_calcas": ["jogger"]}
CAMADAS_BLUSA = {"moletom": ["capuz"]}
SOBREPOSTAS = {f"cabelo-{c}" for c in rig.CABELOS} | {"bone", "fone"}
RAMPA = {"steps": 3, "hue_shift": 10, "dark": 0.30, "light": 0.40}


def rampa(cor: str) -> list[str]:
    return ramp(cor, **RAMPA)


def camadas(p: dict) -> list[str]:
    t = p["corpo"]
    usar = {"braco-tras", "perna-tras", f"tronco-{t}", "perna-frente", "braco-frente", "cabeca", f"rosto-{t}"}
    if p["cabelo"]["estilo"] != "careca":
        usar.add(f"cabelo-{p['cabelo']['estilo']}")
    usar.update(CAMADAS_BLUSA.get(p["blusa"]["estilo"], []))
    for nome in p.get("acessorios", {}):
        if "camada" in ACESSORIOS[nome]:
            usar.add(ACESSORIOS[nome]["camada"])
    return [c for c in rig.CAMADAS if c in usar]


def mapa_regioes(p: dict) -> dict:
    m = dict(REGIOES_BASE)
    m.update(BLUSAS[p["blusa"]["estilo"]])
    m.update(CALCAS[p["calca"]["estilo"]])
    if p.get("meia") and p["calca"]["estilo"] not in MEIA["exceto_calcas"]:
        m.update(MEIA["regioes"])
    for nome in p.get("acessorios", {}):
        m.update(ACESSORIOS[nome].get("regioes", {}))
    return m


def materiais(p: dict) -> dict[str, list[str]]:
    mats = {
        "pele": PELES[p["pele"]],
        "olho": OLHO,
        "cabelo": CABELOS_COR[p["cabelo"]["cor"]],
        "blusa": rampa(p["blusa"]["cor"]),
        "calca": rampa(p["calca"]["cor"]),
        "tenis": rampa(p["tenis"]["cor"]),
    }
    if p.get("meia"):
        mats["meia"] = rampa(p["meia"])
    for nome, cor in p.get("acessorios", {}).items():
        mats[nome] = rampa(cor)
        if "tampa" in ACESSORIOS[nome]:
            mats[f"{nome}_tampa"] = rampa(ACESSORIOS[nome]["tampa"])
    return mats


N4 = ((1, 0), (-1, 0), (0, 1), (0, -1))
N8 = tuple((dx, dy) for dx in (-1, 0, 1) for dy in (-1, 0, 1) if dx or dy)


def _dilatar(s, viz):
    return s | {(x + dx, y + dy) for x, y in s for dx, dy in viz}


def compor_quadro(quadro: dict[str, dict], p: dict) -> Image.Image:
    """Um quadro 16x16 do kit ({camada: {(x, y): (região, tom)}}) -> imagem 16x16 colorida."""
    nomes = camadas(p)
    ids: dict[tuple[int, int], tuple[str, int]] = {}
    corpo: set = set()
    sobre: set = set()
    for nome in nomes:
        novos = quadro.get(nome, {})
        if nome == "braco-frente":
            for (x, y) in novos:
                for dx, dy in N4:
                    v = (x + dx, y + dy)
                    if v in ids and v not in novos:
                        ids[v] = (ids[v][0], 0)
        ids.update(novos)
        (sobre if nome in SOBREPOSTAS else corpo).update(novos)

    m, mats = mapa_regioes(p), materiais(p)
    im = Image.new("RGBA", (rig.N, rig.N), (0, 0, 0, 0))
    dentro = lambda q: 0 <= q[0] < rig.N and 0 <= q[1] < rig.N  # noqa: E731
    cont = hex2rgba(CONTORNO)
    # Contorno grosso do corpo (regra do template), por baixo do cabelo e dos acessórios.
    grosso = {q for q in _dilatar(_dilatar(corpo, N4), N8) - corpo if dentro(q)}
    for q in grosso:
        im.putpixel(q, cont)
    for (x, y), (reg, tom) in ids.items():
        alvo = m.get(reg, reg)
        mat, tom = (alvo if isinstance(alvo, list) else (alvo, tom))
        im.putpixel((x, y), hex2rgba(mats[mat][tom]))
    # Contorno fino em volta do que sai do corpo.
    for q in _dilatar(sobre, N4) - set(ids) - grosso:
        if dentro(q):
            im.putpixel(q, cont)
    return im


def compor(kit: dict, p: dict) -> Image.Image:
    """kit: {(linha, coluna): quadro}. Retorna a folha 256x384."""
    C = rig.N * rig.ESCALA
    folha = Image.new("RGBA", (C * 8, C * 12), (0, 0, 0, 0))
    for (linha, col), quadro in kit.items():
        im = compor_quadro(quadro, p)
        if linha == 0:
            folha.alpha_composite(retrato(im, quadro), (col * C, 0))
        else:
            folha.alpha_composite(im.resize((C, C), Image.NEAREST), (col * C, linha * C))
    return folha


def retrato(im: Image.Image, quadro: dict) -> Image.Image:
    """Recorte de 10x10 em volta da cabeça, ampliado 3x e centrado na célula de 32."""
    cab = quadro["cabeca"]
    hx, hy, _, _ = rig.caixa(cab)
    x0, y0 = hx - 3, hy - 3
    rec = Image.new("RGBA", (10, 10), (0, 0, 0, 0))
    rec.alpha_composite(im, (-x0, -y0))
    out = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
    out.alpha_composite(rec.resize((30, 30), Image.NEAREST), (1, 2))
    return out
