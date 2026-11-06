"""Definições do rig do avatar base: regiões, camadas, animações e as regras que
transformam o template de 16 px (referencia/template-16px.aseprite) no rig.

O rig (rig.aseprite) tem 16 x 16 px por quadro, 53 quadros na ordem das linhas de
docs/avatar-formato.md §3, uma tag por linha e uma camada por peça. Cada pixel do rig
tem uma cor de identificação: uma região do corpo e um tom. As cores reais entram na
composição (compor.py). Detalhes em docs/avatar-kit.md.
"""

from __future__ import annotations

import colorsys
import math

N = 16  # lado do quadro no rig; a folha final amplia 2x
ESCALA = 2

# (nome, quadros, ms, repete) — docs/avatar-formato.md §3
ANIMACOES = [
    ("retrato", 1, 0, False),
    ("parado", 4, 200, True),
    ("andar", 8, 100, True),
    ("treinando", 6, 90, True),
    ("comemorando", 6, 120, True),
    ("alongar", 4, 150, False),
    ("olhar", 4, 200, False),
    ("arremessar", 5, 80, False),
    ("receber", 4, 100, False),
    ("aceitar", 4, 120, False),
    ("recusar", 3, 150, False),
    ("provocar", 4, 120, False),
]

# ---------------------------------------------------------------------------
# Regiões e cores de identificação
# ---------------------------------------------------------------------------

REGIOES = [
    "pele", "olho", "tronco", "quadril", "braco", "antebraco", "punho", "mao",
    "coxa", "canela", "tornozelo", "pe", "cabelo", "bone", "bone_aba",
    "fone", "fone_concha", "capuz", "garrafa", "garrafa_tampa",
]


def _cor_id(i: int, tom: int) -> tuple[int, int, int]:
    h = (i * 0.618034) % 1.0
    v = (0.50, 0.75, 0.98)[tom]
    s = 0.80 if i % 2 else 0.55
    r, g, b = colorsys.hsv_to_rgb(h, s, v)
    return round(r * 255), round(g * 255), round(b * 255)


CORES_ID = {(r, t): "#%02x%02x%02x" % _cor_id(i, t) for i, r in enumerate(REGIOES) for t in range(3)}
REGIAO_POR_COR = {c: k for k, c in CORES_ID.items()}
assert len(REGIAO_POR_COR) == len(CORES_ID), "cores de identificação repetidas"

# ---------------------------------------------------------------------------
# Camadas do rig, de trás para a frente (mesma ordem do template)
# ---------------------------------------------------------------------------

TIPOS_CORPO = ["masculino", "feminino"]
CABELOS = ["raspado", "curto", "crespo", "rabo", "coque"]
CAMADAS = [
    "braco-tras", "garrafa", "perna-tras",
    "tronco-masculino", "tronco-feminino", "capuz",
    "perna-frente", "braco-frente", "cabeca",
    "rosto-masculino", "rosto-feminino",
    *[f"cabelo-{c}" for c in CABELOS],
    "bone", "fone",
]
# Camadas do template -> peças do rig
DO_TEMPLATE = {
    "BackArm": "braco-tras", "BackLeg": "perna-tras", "Body": "tronco",
    "FrontLeg": "perna-frente", "FrontArm": "braco-frente", "Head": "cabeca", "Face": "rosto",
}
PECAS = list(DO_TEMPLATE.values())
INFERIOR = {"perna-tras", "perna-frente"}

# ---------------------------------------------------------------------------
# Quadros: cada quadro do rig sai de um quadro do template (0-base), com ajustes.
#   trocar: {peça: quadro do template de onde vem a peça, alinhada pelo tronco}
#   mover:  {"tudo" | "superior" | peça: (dx, dy)}
#   olho:   "frente" | "tras" | "fechado"
# ---------------------------------------------------------------------------


def _q(base, **kw):
    return {"base": base, **kw}


RESPIRA = {"superior": (0, 1)}
BRACO_ERGUIDO = {"braco-frente": 38}  # WallSlide: braço levantado
BRACOS_FRENTE = {"braco-frente": 34, "braco-tras": 34}  # Jump: braços à frente do peito
BRACO_ESTICADO = {"braco-frente": 29}  # Dash: braço esticado para a frente

QUADROS = {
    "retrato": [_q(31, mover={"tudo": (0, 3)})],  # JumpTurn, de frente; desce para caber o cabelo
    "parado": [_q(0), _q(0), _q(0, mover=RESPIRA), _q(0, mover=RESPIRA, olho="fechado")],
    "andar": [_q(f) for f in range(1, 9)],  # Walk 1–8
    "treinando": [_q(f) for f in (9, 10, 12, 13, 14, 16)],  # Jog sem dois quadros de passagem
    "comemorando": [_q(f) for f in (33, 34, 35, 36, 37, 0)],  # Jump no lugar e volta
    "alongar": [
        _q(0, trocar=BRACO_ERGUIDO, mover={"braco-frente": (0, 1)}),
        _q(0, trocar=BRACO_ERGUIDO),
        _q(0, trocar=BRACO_ERGUIDO, mover={"superior": (0, -1)}, olho="fechado"),
        _q(0),
    ],
    "olhar": [
        _q(0, olho="frente"),
        _q(0, olho="frente", mover={"cabeca": (1, 0), "rosto": (1, 0)}),
        _q(0, olho="tras"),
        _q(0),
    ],
    "arremessar": [_q(39), _q(40), _q(29), _q(30), _q(0)],  # Crouch (preparo), Dash (seguimento)
    "receber": [
        _q(0),
        _q(0, trocar=BRACOS_FRENTE),
        _q(0, trocar=BRACOS_FRENTE, mover={"superior": (0, 1)}),
        _q(0),
    ],
    "aceitar": [
        _q(39),
        _q(0, trocar=BRACO_ERGUIDO),
        _q(0, trocar=BRACO_ERGUIDO, olho="fechado"),
        _q(0),
    ],
    "recusar": [
        _q(0, mover={"braco-frente": (0, -1), "braco-tras": (0, -1)}),
        _q(0, mover={"braco-frente": (0, -1), "braco-tras": (0, -1)}, olho="fechado"),
        _q(0),
    ],
    "provocar": [
        _q(0, trocar=BRACO_ESTICADO),
        _q(0, trocar=BRACO_ESTICADO, mover={"braco-frente": (0, -1)}),
        _q(0, trocar=BRACO_ESTICADO),
        _q(0, trocar=BRACO_ESTICADO, mover={"braco-frente": (0, -1)}),
    ],
}
for _nome, _qt, *_ in ANIMACOES:
    assert len(QUADROS[_nome]) == _qt, _nome

# ---------------------------------------------------------------------------
# Peças sobre a cabeça, relativas ao canto (hx, hy) da cabeça de 4 x 4.
# A faixa escura acima da cabeça do template é o cabelo "curto".
# ---------------------------------------------------------------------------

FAIXA = [(-2, -1), (-1, -2), (-1, -1), (-1, 0), (0, -2), (0, -1), (1, -2), (1, -1),
         (2, -2), (2, -1), (3, -2), (3, -1), (4, -2), (4, -1), (4, 0), (5, -1)]
CABELO = {
    "raspado": [(-1, -1), (-1, 0), (0, -1), (1, -1), (2, -1), (3, -1), (4, -1), (4, 0)],
    "curto": FAIXA,
    "crespo": FAIXA + [(x, -3) for x in range(-1, 5)] + [(-2, -2), (-3, -1), (-2, 0), (5, -2)],
    "rabo": FAIXA + [(-3, -1), (-3, 0), (-4, 0), (-4, 1), (-4, 2)],
    "coque": FAIXA + [(-1, -3), (0, -3), (-1, -4), (0, -4), (-2, -3)],
}


def tom_cabelo(estilo, x, y):
    if estilo == "raspado":
        return 0
    if x < 0:
        return 0
    return 2 if (x, y) == (1, -3 if estilo == "crespo" else -2) else 1


BONE = {(x, y): ("bone", 0 if x < 0 else 1) for x in range(-1, 5) for y in (-2, -1)} | {
    (5, -1): ("bone_aba", 1), (6, -1): ("bone_aba", 1)}
FONE = {(0, 1): ("fone_concha", 1), (0, 2): ("fone_concha", 1), (-1, 1): ("fone_concha", 0),
        (-1, 2): ("fone_concha", 0), (0, 0): ("fone", 1), (0, -1): ("fone", 1),
        (1, -2): ("fone", 1), (2, -2): ("fone", 1), (3, -2): ("fone", 1)}
CAPUZ = {(-1, 4): ("capuz", 1), (-2, 4): ("capuz", 0), (-2, 3): ("capuz", 0), (-1, 3): ("capuz", 1)}

# ---------------------------------------------------------------------------
# Regiões das peças do template
# ---------------------------------------------------------------------------

TOM_TEMPLATE = {  # cor do template -> tom
    "#99e65f": 1, "#5ac54f": 0, "#ffa214": 1, "#ed7614": 0,
    "#00cdf9": 1, "#0098dc": 0, "#f5555d": 1, "#c42430": 0, "#ea323c": 0,
}


def _eixo(pixels, origem):
    """Posição de cada pixel ao longo do eixo origem -> ponta do membro."""
    ox, oy = origem
    ponta = max(pixels, key=lambda p: (p[0] + 0.5 - ox) ** 2 + (p[1] + 0.5 - oy) ** 2)
    dx, dy = ponta[0] + 0.5 - ox, ponta[1] + 0.5 - oy
    n = math.hypot(dx, dy) or 1.0
    return {p: ((p[0] + 0.5 - ox) * dx + (p[1] + 0.5 - oy) * dy) / n for p in pixels}, n


def regioes_braco(pixels: set, ombro) -> dict:
    t, n = _eixo(pixels, ombro)
    out = {}
    for p, v in t.items():
        if v >= n - 0.6:
            out[p] = "mao"
        elif v >= n - 1.6:
            out[p] = "punho"
        elif v <= n / 2:
            out[p] = "braco"
        else:
            out[p] = "antebraco"
    return out


def regioes_perna(pixels: set) -> dict:
    """Pé: a linha mais baixa; tornozelo: a de cima; coxa: a metade de cima da perna."""
    ys = sorted({p[1] for p in pixels})
    baixo, topo = ys[-1], ys[0]
    out = {}
    for p in pixels:
        if p[1] == baixo:
            out[p] = "pe"
        elif p[1] == baixo - 1:
            out[p] = "tornozelo"
        elif p[1] - topo < (baixo - topo) / 2:
            out[p] = "coxa"
        else:
            out[p] = "canela"
    return out


def caixa(pixels):
    xs = [p[0] for p in pixels]
    ys = [p[1] for p in pixels]
    return min(xs), min(ys), max(xs), max(ys)


def montar_quadro(template: dict, q: dict) -> dict[str, dict[tuple[int, int], tuple[str, int]]]:
    """Monta um quadro do rig: {camada: {(x, y): (região, tom)}}.

    `template` é {(quadro, camada_do_template): {(x, y): '#rrggbb'}}.
    """
    base = q["base"]
    pecas = {peca: dict(template.get((base, tl), {})) for tl, peca in DO_TEMPLATE.items()}
    bx, by, _, _ = caixa(template[(base, "Body")])
    for peca, src in q.get("trocar", {}).items():
        sx, sy, _, _ = caixa(template[(src, "Body")])
        tl = next(k for k, v in DO_TEMPLATE.items() if v == peca)
        pecas[peca] = {(x - sx + bx, y - sy + by): c for (x, y), c in template.get((src, tl), {}).items()}
    for alvo, (dx, dy) in q.get("mover", {}).items():
        alvos = {"tudo": PECAS, "superior": [p for p in PECAS if p not in INFERIOR]}.get(alvo, [alvo])
        for peca in alvos:
            pecas[peca] = {(x + dx, y + dy): c for (x, y), c in pecas[peca].items()}

    out: dict[str, dict] = {c: {} for c in CAMADAS}
    tronco = pecas["tronco"]
    tx0, ty0, tx1, ty1 = caixa(tronco)
    pernas = pecas["perna-tras"].keys() | pecas["perna-frente"].keys()
    topo_pernas = min((p[1] for p in pernas), default=ty1 + 1)

    masc = {p: ("quadril" if p[1] >= topo_pernas else "tronco", TOM_TEMPLATE.get(c, 1)) for p, c in tronco.items()}
    out["tronco-masculino"] = masc
    out["tronco-feminino"] = tronco_feminino(masc)

    ombro = ((tx0 + tx1 + 1) / 2, ty0 + 0.5)
    for peca in ("braco-tras", "braco-frente"):
        px = pecas[peca]
        if px:
            regs = regioes_braco(set(px), ombro)
            out[peca] = {p: (regs[p], 0 if peca == "braco-tras" else 1) for p in px}
    for peca in ("perna-tras", "perna-frente"):
        px = pecas[peca]
        if px:
            regs = regioes_perna(set(px))
            out[peca] = {p: (regs[p], 0 if peca == "perna-tras" else 1) for p in px}

    cab = pecas["cabeca"]
    out["cabeca"] = {p: ("pele", TOM_TEMPLATE.get(c, 1)) for p, c in cab.items()}
    rosto = {p: (("olho", 1) if c == "#0e071b" else ("pele", 0)) for p, c in pecas["rosto"].items()}
    hx, hy, hx1, _ = caixa(cab)
    olho = q.get("olho")
    olhos = [p for p, r in rosto.items() if r[0] == "olho"]
    if olho:
        for p in olhos:
            del rosto[p]
        for (x, y) in olhos:
            if olho == "fechado":
                rosto[(x, y)] = ("pele", 0)
            else:
                nx = x + (1 if olho == "frente" else -1)
                rosto[(nx if hx <= nx <= hx1 else x, y)] = ("olho", 1)
    out["rosto-masculino"] = rosto
    fem = dict(rosto)
    for (x, y), r in rosto.items():  # cílio acima e à frente do olho
        if r[0] == "olho" and x + 1 <= hx1:
            fem[(x + 1, y - 1)] = ("olho", 1)
    out["rosto-feminino"] = fem

    # Cabelo, boné, fone e capuz presos à cabeça; garrafa na mão de trás.
    for estilo, pts in CABELO.items():
        out[f"cabelo-{estilo}"] = {(hx + x, hy + y): ("cabelo", tom_cabelo(estilo, x, y)) for x, y in pts}
    out["bone"] = {(hx + x, hy + y): v for (x, y), v in BONE.items()}
    out["fone"] = {(hx + x, hy + y): v for (x, y), v in FONE.items()}
    out["capuz"] = {(hx + x, hy + y): v for (x, y), v in CAPUZ.items()}
    maos = [p for p, r in out["braco-tras"].items() if r[0] == "mao"]
    if maos:
        mx, my = max(maos, key=lambda p: p[1])
        out["garrafa"] = {(mx, my - 1): ("garrafa_tampa", 1), (mx, my): ("garrafa", 1), (mx, my + 1): ("garrafa", 1)}

    return {c: {p: v for p, v in px.items() if 0 <= p[0] < N and 0 <= p[1] < N} for c, px in out.items()}


def tronco_feminino(masc: dict) -> dict:
    """Corpo feminino: peito um pixel à frente e cintura um pixel mais fina."""
    fem = dict(masc)
    linhas: dict[int, list[int]] = {}
    for (x, y), (reg, _) in masc.items():
        if reg == "tronco":
            linhas.setdefault(y, []).append(x)
    ys = sorted(linhas)
    if len(ys) >= 4:
        peito, cintura = ys[1], ys[3]
        fem[(max(linhas[peito]) + 1, peito)] = ("tronco", 1)
        if len(linhas[cintura]) >= 3:
            fem.pop((max(linhas[cintura]), cintura), None)
    return fem
